# Deployment

The environments, the container build and publish for `Resys.Api` and the two Nuxt apps, the PostgreSQL migration step, the environment-variable matrix, and the Kubernetes notes.

**Links:** [Versioning](./01-versioning.md) · [Release notes](./02-release-notes.md) · [Rollback](./04-rollback.md) · [CI](../05-development/03-workflow/03-ci.md) · [Environment config](../05-development/01-setup/02-environment.md) · [App topology](../03-architecture/01-overview/02-app-topology.md) · [Migrations](../03-architecture/03-data/04-migrations.md) · [Release index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This file is the path a tagged release (`07/01`) takes from build to running service. It defines
the three environments and their purpose, the container artifacts built for each deployable, the
migration step that must run before the application rollout, the environment variables each
environment supplies, and the Kubernetes shape that runs them. The local equivalent of every step
is in `05/03-01`; the production topology, secrets store, and scaling policy are owned by
`08/01-01` and `08/01-02`, and this file is their deployment-detail companion.

The deployables are exactly the three in `03/01-02`: one `Resys.Api` service and two Nuxt
front-ends (`web/storefront`, `web/admin`). `web/shared` and the `Resys.*` libraries are
compile-time inputs and are never deployed on their own. PostgreSQL 18, object storage, and the
email transport are managed dependencies, not application images.

---

## Environments

| Environment | Purpose | Data | Migrations | Access |
|---|---|---|---|---|
| Development | Local workstation and CI. | Disposable local PostgreSQL 18 (`05/01-03`); demo seed allowed. | Applied by the API `migrate` verb or the test fixture. | Localhost only. |
| Staging | Pre-production verification of a release candidate; the last gate before production. | A copy or anonymized snapshot of production-like data; no real customer PII in shared previews. | Applied by the same migration step as production, from the release artifact. | Internal, authenticated. |
| Production | Live stores and customer traffic. | Real commerce data; backups and point-in-time recovery (`03/03-04`). | Applied by the migration step in the release job, exactly once. | Public HTTPS at the edge. |

The promotion path is **build once, promote the same image**: an image is built from a tagged
commit, deployed to staging, verified against the release notes' `## Verification` evidence, and
promoted to production by redeploying the identical image digest. Production never rebuilds from
source, so staging and production run byte-identical artifacts. The environment differences are
configuration and data only, supplied through the variable matrix below.

Development is documented separately in `05/01-01` through `05/01-04`; the rest of this file
covers staging and production, which share the same deployment mechanism.

---

## Container Build and Publish

Every deployable produces one immutable image tagged with the platform version and identified by
its digest. Builds run in CI on the release tag (`05/03-03`); the registry is configuration owned
by `08/01-01`.

### `Resys.Api`

The API image is a multi-stage build against the .NET 10 SDK, publishing in Release. Warnings are
errors (`05/02-01`), so a warning fails the image build.

```bash
# build stage: restore once, publish once
dotnet restore Resys.sln
dotnet publish src/Resys.Api -c Release --no-restore -o /out

# runtime stage: aspnet:10.0, non-root, exposes 8080
# the image contains the published output only — no SDK, no source
```

The published output also contains the generated OpenAPI documents the build emits to
`artifacts/openapi/` (`03/04-04`); they are copied into the image so the running host serves the
contract that matches the build. The image runs as a non-root user, listens on `8080`, and is
stateless: all state is in PostgreSQL, object storage, and the outbox, so any replica may serve any
request (`03/01-02`).

### `web/storefront` and `web/admin`

Both Nuxt apps build from the pnpm workspace and ship their Nitro server output as a Node image.

```bash
cd web
pnpm install --frozen-lockfile
pnpm --filter storefront build      # -> web/storefront/.output
pnpm --filter admin build           # -> web/admin/.output

# runtime stage: node:24-slim (LTS), non-root, copies .output, runs the Nitro server
```

`web/shared` is built and consumed during this step and is not deployed. The storefront runs in SSR
mode (it renders per request and serves SEO output); the admin runs the shell with the
multi-store switcher and talks to `/api/admin` over HTTP with the JWT in an httpOnly cookie
(`00/01-04`). Each app receives its API base URL and public runtime config as environment
variables at start, not baked into the image, so one image serves staging and production
(`05/01-02`).

Publish rules:

- **Tags are the version and the digest is the identity.** The `v<version>` release notes
  (`07/02`) record the three image digests; a rollback references those digests, not the tag.
- **No mutable `latest` in production.** Staging and production reference a version tag pinned to
  a digest.
- **The API image and both front-end images are promoted together**, because `web/shared` is
  generated from the API's OpenAPI contract (`07/01`).

---

## PostgreSQL Migration Step

Schema change is **not** applied by the API at startup; it is a discrete step that runs before the
new application pods become ready (`03/03-04`). Applying migrations on startup is forbidden because
it races across replicas and couples schema change to pod lifecycle.

Because each module owns its `DbContext`, its migrations, and an **independent migration history**
(`03/02-01`, `03/03-04`), a bundle is produced **per module** from that module's own project and
context; a single `dotnet ef migrations bundle` invocation targets exactly one context and cannot
cover the others. The release job builds one idempotent bundle for each of the twelve modules and
applies each, once, as a single migrator:

```bash
# build one idempotent bundle per module, from that module's project and context
# (project/context names mirror the workflow in 03/03-04)
for m in Catalog Orders Inventory Shipping Tax Promotions \
         Payments Returns Wishlists Reviews Accounts Stores; do
  dotnet ef migrations bundle \
    --project "src/Resys.Modules.$m" \
    --startup-project src/Resys.Api \
    --context "${m}DbContext" \
    --idempotent -o "/out/migrate-<version>-$m"
done

# apply each module's bundle once, as a single instance, before the application rollout
for m in Catalog Orders Inventory Shipping Tax Promotions \
         Payments Returns Wishlists Reviews Accounts Stores; do
  "/out/migrate-<version>-$m"
done
```

Each bundle is idempotent, so re-running it is safe, and because the histories are independent the
bundles are applied independently without a cross-module order. Ordering rules:

1. Migrations run **before** the new application image is rolled out; the new code must be able to
   tolerate both the pre- and post-migration schema during a rolling deploy.
2. A breaking column change uses **expand and contract** across releases: add and backfill first,
   switch reads and writes, then drop the old column in a later release. This is what keeps a
   rolling deploy safe and makes rollback a code revert (`07/04`).
3. A destructive migration runs only with a verified backup and an explicit release-note warning
   (`07/02`); it is never applied from a developer workstation.
4. The migration step runs **once**, as a single migrator, never as a per-replica init step, to
   avoid concurrent `database update` calls (`03/03-04`).
5. If the migration step fails, the rollout stops; the previous application version keeps running
   against the previous schema. Recovery is the forward-fix path in `07/04`.

The exact mechanism (a Kubernetes Job, a bundle invoked by the release pipeline, or a
provider-native migration hook) is owned by `08/01-01`; the ordering and once-only rules are fixed
here.

---

## Environment-Variable Matrix

The API reads the options tree with double-underscore nesting; the Nuxt apps read their runtime
config from `NUXT_*` variables. The full API settings table and Nuxt runtime-config tables are in
`05/01-02`; this matrix shows what differs per environment. Secret values come from the deployment
secret store (`08/01-02`) and are never committed.

### API (`Resys.Api`)

| Variable | Development | Staging | Production |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Staging` | `Production` |
| `ASPNETCORE_URLS` | `http://localhost:8080` (local) | `http://+:8080` | `http://+:8080` |
| `ConnectionStrings__ResysDb` | local container (`05/01-03`) | staging PostgreSQL 18 | production PostgreSQL 18 (secret) |
| `Jwt__Issuer` | `https://localhost:8443` | staging host | production host |
| `Jwt__Audience` | `resys` | `resys` | `resys` |
| `Jwt__SigningKey` | user-secrets | secret store | secret store |
| `Jwt__AccessTokenMinutes` | `15` | `15` | `15` |
| `Jwt__RefreshTokenDays` | `30` | `30` | `30` |
| `Cors__AllowedOrigins__0` | `http://localhost:3000` | staging storefront origin | production storefront origin |
| `Cors__AllowedOrigins__1` | `http://localhost:3001` | staging admin origin | production admin origin |
| `Stores__HeaderName` | `X-Store` | `X-Store` (disabled effect by domain) | `X-Store` (not honored) |
| `Payments__DefaultProvider` | `stripe` | `stripe` | `stripe` |
| `Payments__Stripe__SecretKey` | test key | test key | live key (secret) |
| `Payments__Stripe__PublishableKey` | test key | test key | live key |
| `Payments__Stripe__WebhookSecret` | `whsec_dev_placeholder` | test secret | live secret |
| `Email__Provider` | `smtp` | `ses` | `ses` |
| `Email__FromAddress` | `no-reply@resys.test` | staging sender | production sender |
| `Storage__Provider` | `local` | `s3` | `s3` |
| `Storage__S3__Bucket` | — | staging bucket | production bucket |
| `Storage__S3__AccessKey` / `SecretKey` | — | secret store | secret store |
| `Observability__OtlpEndpoint` | optional local | collector endpoint | collector endpoint |

Key differences: **`Storage__Provider` flips from `local` to `s3`** and a `local` value is
rejected outside `Development`/`Test` (`05/01-02`); **email flips from SMTP to SES**; **CORS
origins and the JWT signing key are environment-specific** and must not be shared; and the
development demo seeder and `X-Store` header fallback are disabled in production. Secret-valued
variables are marked in `05/01-02` and are injected only from the secret store.

### Nuxt front-ends

| Variable | App | Development | Staging / Production |
|---|---|---|---|
| `NUXT_API_BASE_URL` | storefront, admin | `http://localhost:8080` | API service URL (in-cluster) |
| `NUXT_PUBLIC_API_BASE_URL` | storefront, admin | `http://localhost:8080` | API public URL |
| `NUXT_PUBLIC_SITE_URL` | storefront | `http://localhost:3000` | public storefront URL |
| `NUXT_PUBLIC_DEFAULT_LOCALE` | storefront | `en` | per-store default |
| `NUXT_PUBLIC_BRANDING_JSON` | storefront, admin | dev branding | per-store fallback branding |
| `NUXT_PUBLIC_STORE_HEADER` | storefront | `X-Store` | not honored in production |
| `NUXT_PUBLIC_ADMIN_API_BASE` | admin | `/api/admin` | `/api/admin` |
| `NUXT_PUBLIC_DEFAULT_STORE` | admin | `resys-main` | initial switcher store |

Neither front-end holds database or provider credentials (`03/01-02`); only the API does.
Production store resolution is by request domain, so the branding fallback never overrides a
resolved store (`05/01-02`).

---

## Kubernetes Notes

The cluster, ingress, and managed services are owned by `08/01-01`; these are the deployment-shape
rules the release must satisfy.

- **One Deployment per deployable** — `resys-api`, `resys-storefront`, `resys-admin` — each
  referencing an immutable image digest. The API is stateless and horizontally scalable; the
  front-ends run the Nitro Node server.
- **Probes** — the API exposes `/health/live` (process liveness, no dependency checks) and
  `/health/ready` (readiness, verifies PostgreSQL and required dependencies). Kubernetes uses
  `livenessProbe` on `/health/live` and `readinessProbe` on `/health/ready` so traffic reaches a
  pod only after the database is reachable (`03/01-02`). The Nuxt pods use a TCP or HTTP probe on
  their server port.
- **Rollout** — a rolling update with `maxUnavailable: 0`; the readiness probe gates traffic, and
  the new pods must tolerate both schemas during the expand/contract window (`03/03-04`). The
  migration step (above) is a `Job` that completes before the Deployment is updated, or a pipeline
  stage that the manifest rollout waits on.
- **Configuration and secrets** — non-secret settings come from a `ConfigMap`; secrets
  (`ConnectionStrings__ResysDb`, `Jwt__SigningKey`, gateway keys, S3 credentials) come from the
  secret store via `Secret` references and are never in the image or the manifest (`08/01-02`).
  `ASPNETCORE_ENVIRONMENT` and the `NUXT_*` values are set per environment.
- **CORS and ingress** — the ingress terminates TLS at the edge; browser origins are the two
  configured front-end origins and are the only entries in the API's allow-list (`03/01-02`).
- **Scaling and capacity** — replica counts, horizontal pod autoscaling, connection pooling, and
  the outbox relay's single-consumer safety across replicas are owned by `08/04-01` and
  `03/02-03`; the deployment only requires that every API replica binds the same ports and shares
  the database.
- **No in-cluster database migration on pod start.** A pod must never run migrations; that is the
  Job's responsibility.

---

## Release Deployment Sequence

1. CI builds and publishes the three images from the tagged commit, tagged `v<version>` and
   recorded by digest (`05/03-03`).
2. Deploy to **staging** with the staging variable set; run the migration Job, then roll out the
   application images.
3. Verify against the release notes' `## Verification` section and `06/01-01`; confirm
   `/health/ready` is green.
4. Promote the **same digests** to production: run the migration Job (idempotent, once), then roll
   out the application images.
5. Confirm health, watch the release's dashboards (`06/03-01`), and record the outcome in the
   release notes. If a problem appears, follow `07/04`.

---

## Open Questions

1. The registry, cluster provider, ingress controller, and secret store are owned by `08/01-01`
   and `08/01-02`; this file fixes the artifacts, the migration ordering, and the variable
   contract.
2. Whether the migration step is a Kubernetes Job or a pipeline-invoked bundle is a scaffold
   choice owned by `08/01-01`; the once-only and before-rollout rules are fixed.
3. Whether the admin runs SSR or a static SPA in production is resolved with `04/02-01`; the
   deployment shape here supports either (the Nitro server or static assets behind the ingress).
4. The autoscaling thresholds and replica floors are owned by `08/04-01`; the API's statelessness
   and shared-port requirement are fixed.

## Links

- [Versioning](./01-versioning.md) · [Release notes](./02-release-notes.md) · [Rollback](./04-rollback.md)
- [CI pipeline](../05-development/03-workflow/03-ci.md) · [Build and run](../05-development/03-workflow/01-build-run.md)
- [Environment configuration](../05-development/01-setup/02-environment.md) — the full settings
  and runtime-config tables and the secret marks.
- [Docker Compose](../05-development/01-setup/03-docker-compose.md) — local PostgreSQL 18.
- [App topology](../03-architecture/01-overview/02-app-topology.md) — deployables, ports, health,
  CORS, store resolution.
- [Migrations](../03-architecture/03-data/04-migrations.md) — per-module history, expand/contract,
  forward-only production.
- [Design spec](../11-spec/README.md) — §7 (API), §10
  (configuration and observability).
- Operations topology and secrets: `08/01-01`, `08/01-02`; scaling: `08/04-01`.
