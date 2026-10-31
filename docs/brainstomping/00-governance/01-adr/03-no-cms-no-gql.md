# ADR 0003: No CMS, No GraphQL, No Blazor, No Recommendation Engine

Records the explicit scope exclusions that keep resys-eshop a focused commerce platform.

**Links:** [Governance index](../README.md) · [Spec](../../11-spec/README.md)

---

## Status

Accepted — 2026-09-23. Locked by the design spec (§1 out-of-scope, §2 decisions 1, 2,
6, 10) and reaffirmed by the Global Constraints of the implementation plan.

## Context

An e-commerce platform can accrete adjacent capabilities — content management, a
flexible query layer, an in-process UI framework, and personalisation — until its scope
is unbounded. Each of these would materially change architecture, delivery cost, and
the documentation set.

The exclusion decisions were taken during design so that later docs and plans do not
re-open them. They are recorded as one ADR because they share a single rationale:
**resys-eshop is a commerce platform with a headless REST API and separately built
front-ends, and nothing else.** "resys" is only the project name and does not imply a
recommendation system.

Forces:

- **Focus.** The deliverable is a Spree-parity commerce domain; CMS and
  personalisation are distinct products.
- **Contract simplicity.** One API style (REST) keeps the OpenAPI contract, generated
  clients, caching, and authorization model uniform.
- **Front-end freedom.** The front-ends must be independent HTTP clients; embedding a
  .NET UI framework in the service would couple rendering to the back end.
- **Honesty of scope.** Excluded capabilities are documented as exclusions, not left
  ambiguous.

## Decision

The following are **explicitly out of scope** and must not appear as planned features
in any area (`01`–`10`):

1. **No CMS.** No pages, page builder, blog, or content blocks. Product and category
   copy is commerce data owned by the Catalog module (with per-locale translations),
   not free-form web content.
2. **No GraphQL.** The API is REST only: public `/api/v1` and admin `/api/admin`, with
   OpenAPI generated from the Minimal API and browsable in Scalar.
3. **No Blazor.** Neither Blazor Server nor Blazor WebAssembly is used. The storefront
   is Nuxt 4 SSR and the admin is Nuxt 4 with PrimeVue; both consume the API over HTTP.
4. **No recommendation engine.** No personalisation, ranking, or "you may also like"
   subsystem. Search facets and standard catalog queries are in scope; recommendations
   are not.

This ADR is the single citation point for these exclusions. When a later doc needs to
explain why something is absent, it cites this ADR rather than restating the reasoning.

## Consequences

**Positive**

- The API surface stays one uniform REST contract, simplifying generated clients,
  versioning, CORS, and authorization.
- The service remains headless: rendering lives entirely in the Nuxt apps, so the
  storefront can change without touching the back end.
- The documentation set stays scoped to commerce concerns and can be completed at
  implementation grade instead of describing adjacent products.
- No content-modelling or ranking data enters the domain model.

**Negative / costs**

- Marketing pages and editorial content must be served by an external tool (for
  example a static site or a headless CMS used outside this platform); resys-eshop does
  not provide them.
- Clients wanting flexible, client-shaped queries must compose several REST calls
  rather than issuing one GraphQL query.
- Personalisation features are unavailable until a future, separate decision reverses
  this ADR.
- The team must keep the exclusions visible in requirements so scope does not creep
  back in; this ADR and area `01`/`02` are where that guardrail lives.

**Neutral**

- Excluding these capabilities does not reduce Spree parity for the commerce domain
  itself, which is the stated goal.

## Alternatives Considered

- **Include a lightweight CMS.** Rejected: content management is a separate product
  with its own authoring UX, versioning, and publishing model; bundling it would expand
  the domain and the docs well beyond commerce.
- **GraphQL instead of REST.** Rejected: the known consumers are the storefront, the
  admin, and a shared typed client — all served well by generated REST clients. GraphQL
  would add a schema server, resolver layer, and a second authorization surface for no
  demonstrated query-flexibility need.
- **Blazor for one or both front-ends.** Rejected: it conflicts with the chosen Vue
  ecosystem, is a poorer fit for the SEO-critical public storefront, and would couple
  UI delivery to the .NET service.
- **Recommendation / personalisation engine.** Rejected: it is a distinct data and
  ML-serving problem; the project name is not a requirement.
- **Defer the exclusions and decide later.** Rejected: leaving scope ambiguous invites
  contradictory plans. Recording the decisions now keeps every area consistent.

## Open Questions

None. These exclusions are final for the current design; reversing any of them requires
superseding this ADR.

## Links

- [Governance index](../README.md)
- [Design spec](../../11-spec/README.md) — §1
  (out of scope), §2 (locked decisions), §7 (REST API surface).
- [ADR 0001 — Application stack](./01-stack.md)
- [ADR 0004 — Headless API and JWT auth](./04-headless-jwt.md)
- Extension architecture (contracts, not hooks): `03/02-07`.
