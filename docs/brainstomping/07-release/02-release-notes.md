# Release Notes

The release-notes template every release follows, plus the complete `v0.1.0` entry for the initial platform foundation.

**Links:** [Versioning](./01-versioning.md) · [Deployment](./03-deployment.md) · [Rollback](./04-rollback.md) · [Roadmap](../02-planning/01-roadmap.md) · [Quality metrics](../06-verification/03-quality/01-metrics.md) · [Release index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This file defines the **release-notes format** and demonstrates it with a complete entry. Every
tagged release (`07/01`) gets one document in this shape, assembled from the Conventional Commit
history and the migration list, and stored with the release artifact so a reader can see what
changed, what to do about it, and how it was verified.

The notes are the human-readable half of the changelog defined in `07/01`; the per-release quality
report required by `06/03-01` (coverage, escape rate, P1/P2 escapes) is attached to the entry's
`## Verification` section. The notes are also the source the metrics report reads to attribute a
production defect to the release that introduced it (`06/03-01`).

---

## Template

Copy this skeleton for each release. A section with nothing to report is omitted, never left with
an empty bullet; genuinely undecided work appears under the release's `## Open Questions`.

````markdown
# <Platform> v<MAJOR.MINOR.PATCH> — <Codename or release theme>

- **Date:** <YYYY-MM-DD>
- **Tag:** `v<MAJOR.MINOR.PATCH>`
- **Commit range:** `v<previous>..v<version>`
- **API version:** `/api/v1` (additive) — or the v1→v2 note if the surface changed
- **Schema migrations:** <count> across <modules>, or "none"

## Highlights

Two to five sentences on the release theme and what a user or operator gains. Written for a
reader who will not read the rest.

## Added

- <user-visible capability, imperative, naming the area>

## Changed

- <behaviour change a consumer must know about>

## Fixed

- <defect corrected, naming the surface>

## Security

- <auth, secret-handling, or vulnerability change; omit if none>

## Breaking changes

- <contract removed or changed> — <consumer action required>

## Migrations

| Module | Migration | Effect | Destructive |
|---|---|---|---|
| <Module> | `<MigrationName>` | <schema effect> | no/yes — backup required |

## Verification

- Unit, integration, front-end, and E2E suites: <pass/fail summary>
- Coverage: <line/branch summary>, targets met per `06/01-05`
- Defect escape rate this release: <rate>; P1/P2 escapes: <count>
- Performance budgets (`06/03-02`): <met/not met>

## Upgrade notes

The ordered operator actions: which image tags to deploy, the migration step and its order
relative to the application rollout (`07/03`), and any environment-variable addition or rename.

## Rollback

The previous known-good tag and the rollback path (`07/04`): application redeploy, any
compensating migration, and the feature-flag kill switch for the riskiest change in the release.

## Links

- Previous release: `v<previous>`
- Versioning policy: `07/01`
- Deployment: `07/03` · Rollback: `07/04`
````

Rules that keep entries comparable across releases:

- **Dates are ISO `YYYY-MM-DD` and match the tag's commit date.**
- **Every removed or changed contract appears under Breaking changes**, carried over from the
  `BREAKING CHANGE:` footer in the commit history (`05/03-02`).
- **The migration table is exhaustive** for the release; a destructive migration must appear with
  `yes` and a backup note, and it must match the migration list in `07/01` and `07/03`.
- **Verification is evidence, not a claim.** A suite that did not run is stated as not run; a
  missing report is recorded under Open Questions rather than implied (`00/02-03`).

---

The worked example below follows the template exactly but is re-leveled for this file: the title
line becomes a section heading and each template heading is shifted down one level.

## v0.1.0 — Initial Platform Foundation

- **Date:** 2026-09-23
- **Tag:** `v0.1.0`
- **Commit range:** — (first release; no predecessor)
- **API version:** `/api/v1` (initial, additive)
- **Schema migrations:** 12 initial migrations, one per module

### Highlights

The first releasable cut of resys-eshop: the repository skeleton, the modular-monolith service,
and both Nuxt front-ends are in place and can run end to end. This release establishes the version
contract (`07/01`), the deployment path (`07/03`), and the rollback path (`07/04`) rather than
shipping a feature-complete store. It corresponds to the P1 platform core phase of `02/01` and its
M1 walking-skeleton milestone; catalog, checkout, promotions, and the rest of the Spree-parity
scope follow in later `MINOR` releases.

### Added

- **Solution foundation** — `Resys.sln` with `Resys.Core`, `Resys.Shared`, the twelve
  `Resys.Modules.*` projects, `Resys.Infrastructure`, and the `Resys.Api` host, wired in the
  dependency direction fixed by `03/02-01`.
- **Persistence** — EF Core 10 + Npgsql against PostgreSQL 18, one `DbContext` and one PostgreSQL
  schema per module, each with its own migrations history (`03/03-04`).
- **Identity and auth** — ASP.NET Identity for users; JWT Bearer with short-lived access tokens;
  roles `customer` and `admin` with per-store admin assignment; the admin token in an httpOnly
  cookie (`00/01-04`).
- **API host** — Minimal API exposing `/api/v1` for the storefront and `/api/admin` for the
  back-office, with the module endpoint groups registered by the composition root (`03/04-01`,
  `03/04-02`).
- **Outbox and events** — the transactional outbox table and the relay that dispatches domain
  events in process, concurrency-safe across instances (`03/02-03`).
- **Observability** — liveness and readiness endpoints (`/health/live`, `/health/ready`),
  OpenTelemetry traces and metrics, and Serilog structured logs with request-id propagation.
- **OpenAPI and clients** — OpenAPI documents generated for both surfaces and browsable in Scalar
  (`03/04-04`); the generated typed client in `web/shared`.
- **Storefront shell** — `web/storefront`, Nuxt 4 SSR with Tailwind CSS v4 and `@nuxtjs/i18n`,
  reading runtime config per environment (`05/01-02`).
- **Admin shell** — `web/admin`, Nuxt 4 with PrimeVue 4 and the multi-store switcher shell,
  calling `/api/admin`.
- **Configuration** — the options pattern with per-environment `appsettings` and environment
  variables; local disk storage in development and S3-compatible storage in production (`05/01-02`).
- **CI and documentation gates** — the build, unit, integration, front-end, E2E, and documentation
  stages, including the forbidden-token scan and the link check (`05/03-03`).

### Changed

None — this is the first release.

### Fixed

None — this is the first release.

### Security

- JWT signing keys and all provider secrets are injected through the deployment secret store and
  never committed; only non-secret local values ship in `appsettings.Development.json`
  (`05/01-02`).
- The admin JWT is stored in an httpOnly cookie so it is not reachable from JavaScript
  (`00/01-04`).
- CORS is an allow-list of exactly the two configured front-end origins, never a wildcard.
- Audit-log rows are written for admin mutations (`03/01-02`), and logs carry no PAN or PII.

### Breaking changes

None — `/api/v1` is introduced additively and `/api/admin` is new.

### Migrations

| Module | Migration | Effect | Destructive |
|---|---|---|---|
| Accounts | `InitialAccounts` | Identity users, roles, addresses, per-store assignment | no |
| Stores | `InitialStores` | Stores, domain mapping, currencies, locales, options | no |
| Catalog | `InitialCatalog` | Products, options, taxonomy, images, price sets | no |
| Orders | `InitialOrders` | Order aggregate, line items, addresses, adjustments | no |
| Inventory | `InitialInventory` | Stock items, reservations, movement ledger | no |
| Shipping | `InitialShipping` | Shipping methods, zones, rate rules | no |
| Tax | `InitialTax` | Jurisdiction hierarchy, rates, categories | no |
| Promotions | `InitialPromotions` | Promotions, rules, actions | no |
| Payments | `InitialPayments` | Payment records, attempts, refunds | no |
| Returns | `InitialReturns` | Return authorizations and items | no |
| Wishlists | `InitialWishlists` | Wishlists and their items | no |
| Reviews | `InitialReviews` | Product reviews, ratings, moderation state | no |

All twelve migrations are non-destructive. They are applied at deploy time by the migration step
in `07/03`, before the application rollout, and the reference rows (`customer` and `admin` roles,
ISO currencies, a default store, the default tax category) are carried by the same migrations per
`03/03-04`.

### Verification

- Unit, integration, front-end lint/typecheck, and E2E suites pass on the release commit; the
  pipeline order and gates are `05/03-03`.
- Integration suites run against Testcontainers PostgreSQL 18 and apply the module migrations
  (`06/01-03`).
- Coverage targets per layer are met as defined in `06/01-05`; the merged coverage summary is the
  attached CI artifact.
- This is the baseline release, so the defect escape rate metric (`06/03-01`) starts with it;
  P1/P2 escapes: 0.
- Performance budgets (`06/03-02`) are not yet enforced at v0.1.0; the storefront has no
  production traffic. They become a release gate from the first feature release that serves users.

### Upgrade notes

1. Provision PostgreSQL 18 and the S3-compatible bucket, then set the environment variables in the
   matrix in `07/03`; the names are the contract from `05/01-02`.
2. Run the migration step (`07/03`) before rolling out the API image so the schema is current.
3. Deploy the `v0.1.0` images for `Resys.Api`, `web/storefront`, and `web/admin`; the front-ends
   require the API base URL through their Nuxt runtime config.
4. No data migration is required — the database is new. The development demo seeder is disabled in
   production; only schema and reference rows are present, per `03/03-04`.

### Rollback

There is no previous release to roll back to. The relevant path is forward-fix: if the `v0.1.0`
rollout fails before serving traffic, stop the rollout and redeploy the images once corrected
(`07/04`). The twelve migrations are non-destructive, so no compensating migration is needed for a
failed initial deploy; a full reset is a database restore, not a schema rollback.

### Links

- Versioning policy: [01-versioning.md](./01-versioning.md)
- Deployment: [03-deployment.md](./03-deployment.md) · Rollback: [04-rollback.md](./04-rollback.md)
- Roadmap phase: [Roadmap](../02-planning/01-roadmap.md) — P1 (platform core), milestone M1.
- Quality metrics: [Quality metrics](../06-verification/03-quality/01-metrics.md)
- Design spec: [Spec](../11-spec/README.md) — §5, §7, §10.
