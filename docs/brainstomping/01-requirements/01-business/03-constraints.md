# Constraints

The non-negotiable boundaries — technical, regulatory, operational, and documentation — that
every requirement must respect.

**Links:** [Goals](./01-goals.md) · [Actors](./02-actors.md) · [Requirements index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Goals describe what the platform should achieve; constraints describe what it must never do
and which fixed choices it must use. A requirement that violates a constraint is invalid and
must be revised, because the constrained choices below are **locked** by the spec's §2 and
the governance ADRs (`00/01-01` … `00/01-04`).

This document is the requirements-area mirror of those decisions. Where it and the spec
disagree, the spec is canonical.

---

## Technical

Locked stack and versions. Requirements must be written against exactly these; a change is a
new ADR, not a quiet substitution.

### Backend

| Concern | Constraint |
|---|---|
| Runtime | **.NET 10.0 LTS (10.0.12)**, supported to 2028-11-14. .NET 11 is RC/STS and is not targeted. |
| Web | ASP.NET Core **Minimal API**, **REST only**. |
| ORM | **Entity Framework Core 10**. |
| DB provider | **Npgsql**. |
| Database | **PostgreSQL 18**. |
| OpenAPI | Built-in `Microsoft.AspNetCore.OpenApi` + **Scalar**. |
| Payments | **Stripe.net** and **PayPal .NET** SDKs, behind `IPaymentGateway`. |
| Auth | ASP.NET Identity (users) + **JWT Bearer**; admin token in an httpOnly cookie. |
| Testing | xUnit, Testcontainers (.NET, Postgres), Playwright. |

### Frontend

| Concern | Constraint |
|---|---|
| Framework | **Nuxt 4.5.x**. Nuxt 3 is EOL 2026-07-31 and is not targeted. |
| Vue | **Vue 3.5.43** (stable). 3.6 is RC and must not be used. |
| Language | TypeScript, strict mode. |
| State | **Pinia**. |
| Fetch | `ofetch` / Nuxt `useFetch`, typed via shared contracts. |
| Storefront styling | **Tailwind CSS v4**. |
| Admin components | **PrimeVue 4**. |
| i18n | `@nuxtjs/i18n`, per-store supported locales. |
| Payments UI | Stripe Elements and PayPal JS SDK embedded in checkout. |

### Architecture

- **Modular monolith, headless** — one deployable .NET service split into internal module
  projects with DI contracts. Not microservices.
- **No Blazor** — the front-ends are Nuxt 4 (Vue 3) only.
- **No GraphQL** — REST only, `/api/v1` and `/api/admin`.
- **No CMS** — no pages, page builder, blog, or content blocks.
- **No recommendation engine**.
- **Twelve modules**: Catalog, Orders, Inventory, Shipping, Tax, Promotions, Payments,
  Returns, Wishlists, Reviews, Accounts, Stores (spec Appendix A).
- **Versioning** — additive `/api/v1` until a breaking change forces `/api/v2`.
- **Money** is always a strongly-typed `(decimal, ISO-4217)` value object; raw decimals are
  not used for prices or totals.
- **Store scoping** applies to every catalog, inventory, and order row.

---

## Regulatory / Payment

- **No PAN storage.** The platform must never store, log, or transmit primary account
  numbers (card numbers) or CVV. Card data is captured by the provider's UI (Stripe
  Elements / PayPal JS SDK) and handled by the gateway; the platform persists only gateway
  tokens/references and the resulting payment records.
- **No PII in logs.** Customer personal data must not appear in application logs or traces;
  this is enforced together with the security requirements in `01/03-02`.
- **Refunds and voids** must go through the gateway that captured the payment; the platform
  records the reference and the outcome.
- **Sensitive input is server-validated.** Amounts, currencies, and coupon codes are
  validated server-side; the client is never trusted for pricing.
- **Audit trail.** Admin mutations are recorded in an audit log (who/what/when), per
  `03/04-03`.

---

## Operational

- **One deployable backend.** Scaling is horizontal at the service level; no per-module
  deployment is required.
- **Stateless API instances.** Session state lives in tokens and the database, not in the
  process, so instances can be replaced freely.
- **Health checks** at `/health/live` and `/health/ready`.
- **Observability** via OpenTelemetry (traces, metrics, structured logs through Serilog),
  with request-id propagation.
- **Configuration** through the options pattern with per-environment `appsettings` and
  environment variables; front-end runtime config carries API base URL and per-store
  branding.
- **Reliable events.** Domain events are dispatched through an outbox table in the same
  transaction as the state change, so delivery survives restarts.
- **Data migrations** use EF Core; schema changes ship with a documented migration path.

---

## Documentation

- **Docs-first.** The immediate deliverable is documentation, not code; code follows the
  implementation plans derived from the spec.
- **The spec is canonical.** `docs/11-spec/README.md` is the
  single source of truth. No area may silently contradict it; a conflict is resolved by
  correcting the area or superseding a decision with an ADR.
- **Format.** Every doc starts with a title, a one-line purpose, and a `**Links:**` block;
  one concern per file; 100–300 lines.
- **No unresolved markers.** Every section is complete; when a detail is genuinely open it
  is stated in an explicit `## Open Questions` section, never deferred with a marker token.
- **Stable identifiers.** Files are numbered for cross-reference; another doc is cited by its
  identifier (`AA/BB-CC`, or `AA/NN` for a flat area) and linked when a jump is useful.
- **Link integrity.** Every relative markdown link between docs must resolve; verification of
  the set follows the spec's §12.

---

## Open Questions

1. The precise log-redaction rules are finalized with the security requirements in
   `01/03-02`; the constraint itself (no PII in logs) is fixed.
2. The audit-log retention window is an operational policy detail owned by `08/03-01`.
