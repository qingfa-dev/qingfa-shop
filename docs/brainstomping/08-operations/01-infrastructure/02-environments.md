# Environments

The development, test, staging, and production environments of resys-eshop: what configures each one, the promotion path between them, and the data-handling rules that keep real customer data out of lower environments.

**Links:** [Topology](./01-topology.md) · [Capacity](../04-scaling/01-capacity.md) · [Environment configuration](../../05-development/01-setup/02-environment.md) · [Docker Compose](../../05-development/01-setup/03-docker-compose.md) · [Deployment](../../07-release/03-deployment.md) · [Migrations](../../03-architecture/03-data/04-migrations.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file defines the environments resys-eshop runs in and the rules that separate them. It is
the companion to `08/01-01`: the topology says **what runs** in production, and this file says
**how the same artifacts are configured differently** across development, test, staging, and
production. The variable contract itself is owned by `05/01-02`; the release mechanics that move an
artifact between environments are owned by `07/03`. Where a value here conflicts with `05/01-02`,
the environment-configuration file wins and this file is corrected.

The governing principle: **build once, promote the same image digest, change only configuration
and data.** Production never rebuilds from source, so staging and production run byte-identical
artifacts; everything that differs is supplied as environment configuration or lives in the
environment's data.

---

## Environment Set

| Environment | `ASPNETCORE_ENVIRONMENT` | Purpose | Data | Access |
|---|---|---|---|---|
| Development | `Development` | Local workstation; fastest iteration. | Disposable local PostgreSQL 18 and demo seed (`05/01-04`). | Localhost only. |
| Test / CI | `Test` | Automated unit, integration, and E2E runs in the pipeline. | Ephemeral Testcontainers PostgreSQL, reset per run; no shared data. | Pipeline only. |
| Staging | `Staging` | Pre-production verification of a release candidate; the last gate before production. | A copied or anonymized production-like snapshot; never a live customer dataset. | Internal, authenticated, not public. |
| Production | `Production` | Live stores and customer traffic. | Real commerce data with backups and point-in-time recovery. | Public HTTPS at the edge. |

`Development` and `Test` are the only environments where the local storage backend and the demo
seeder are permitted. `Staging` and `Production` share the same deployment mechanism and differ
only by configuration and data (`07/03`).

---

## Configuration Differences

Only a small set of concerns flips between environments. The full API and Nuxt tables are in
`05/01-02`; this matrix is the operational summary and never a second source of variable names.

| Concern | Development / Test | Staging | Production |
|---|---|---|---|
| Hosting | Local processes and Docker Compose (`05/01-03`). | Kubernetes, one namespace. | Kubernetes, production namespace. |
| `Storage:Provider` | `local` (files under `./.data/storage`, served by the API). | `s3`. | `s3`. |
| `Email:Provider` | `smtp` to a local catcher on `1025`. | `ses` (or SMTP relay). | `ses` (or SMTP relay). |
| Payment keys | Provider test keys. | Provider test keys. | Provider live keys from the secret store. |
| CORS origins | `http://localhost:3000`, `http://localhost:3001`. | Staging storefront/admin hosts. | Production storefront/admin hosts. |
| `Jwt:SigningKey` | `dotnet user-secrets`. | Secret store, staging-only key. | Secret store, production-only key. |
| Store selection | `X-Store` header honored. | Header effectively disabled; domain resolution. | Header not honored; domain resolution only. |
| Demo seeder | Enabled by `Development`. | Disabled. | Disabled. |
| Backups / PITR | None. | Snapshot of anonymized data; no PITR guarantee. | WAL archiving, PITR, rehearsed restores (`08/01-01`). |
| Replica floor | Single container. | At least one API replica; no read replica required. | API replica floor ≥ 2, PostgreSQL primary + replica (`08/01-01`, `08/04-01`). |
| Observability | Optional local OTLP. | Collector endpoint; dashboards on staging signals. | Collector endpoint; production dashboards and alerts. |

Rules:

- **Rejected combinations fail fast.** `Storage:Provider=local` outside `Development`/`Test`, a
  missing signing key, or a wildcard CORS origin aborts startup rather than degrading silently
  (`05/01-02`, NFR-SEC-010).
- **Secrets never travel between environments.** Staging and production use distinct keys and
  distinct provider credentials; a shared signing key or gateway key is a defect.
- **`X-Store` is a development affordance only.** It must not be able to override production store
  resolution, which is by request domain (`03/01-02`).
- **The demo seeder and the `local` backend are absent from production**, so no production host
  serves files from a container filesystem or seeds demo rows.

---

## Promotion Path

Every release follows the same path; the details of the build and the once-only migration step are
in `07/03`.

1. **Build once.** CI builds the three immutable images (`Resys.Api`, `web/storefront`,
   `web/admin`) from a tagged commit and records each by digest (`05/03-03`).
2. **Deploy to staging.** Apply the staging variable set, run the per-module migration bundle
   once, then roll out the application images. Confirm `/health/ready` is green (`08/01-01`).
3. **Verify.** Run the release-candidate verification and the performance lane against the
   staging environment (`07/02`, `06/03-02`).
4. **Promote the same digests.** Redeploy the identical image digests to production, run the
   idempotent migration bundle once, then roll out. Production never rebuilds.
5. **Confirm and record.** Watch production health and dashboards (`08/02-02`), then record the
   outcome in the release notes. A problem routes to `07/04`.

Configuration is promoted by updating the environment's `ConfigMap`/`Secret` set, not by editing
images. A configuration change that affects production follows the same review as a code change.

---

## Data-Handling Rules

- **Direction of data.** Data may flow **up** from a lower environment into a snapshot only for
  staging, and only after anonymization. Production data must never be copied into development,
  test, or CI.
- **Staging data.** Staging seeds from a copy or anonymized snapshot of production-like data. Real
  customer PII must not appear in shared previews; emails, names, addresses, and payment
  references are masked or synthesized before the snapshot is used.
- **Production data.** Production holds real commerce data. Access is restricted to the API
  workload and to break-glass operators through the database runbook in `08/03-02`; direct
  developer access is not granted.
- **Backups.** Production backups are encrypted at rest and in transit, stored off the primary
  host, and restorable to a scratch environment for rehearsed recovery (NFR-AVAIL-004).
- **Deletion.** A data-deletion request in production is applied to the production database and
  propagated to any staging snapshot on its next refresh; lower environments do not retain a copy
  of deleted production records.
- **No PAN or PII in logs.** Card data never reaches the platform, and logs/traces redact PII in
  every environment, not only production (NFR-SEC-007).
- **Migrations are environment-run, once.** Each environment applies the same per-module,
  idempotent bundle with its own history; a migration is never applied from a developer
  workstation to staging or production (`03/03-04`, `07/03`).

---

## Isolation and Access

- **Network.** Development is localhost-only; test runs in the pipeline network; staging is
  internal and authenticated; production is public at the edge and private internally
  (`08/01-01`).
- **Identity.** Staging and production have separate databases, separate secret stores, and
  separate provider credentials. Sharing any of these between environments is prohibited.
- **Operators.** Production access is role-controlled and audited; break-glass access is logged
  and reviewed under the on-call policy in `08/03-01`.
- **Observability data.** Telemetry from staging and production is tagged per environment so a
  staging signal never fires a production page; alert routing is `08/02-03`.

---

## Open Questions

1. The concrete cloud accounts, cluster names, and namespace names per environment are deployment
   values owned by the manifests in `08/01-01`; this file fixes the separation rules, not the
   names.
2. The exact staging anonymization tool and its field-masking rule set are refined with the seed
   and snapshot tooling in `05/01-04`; the no-real-PII rule is fixed here.
3. Whether staging runs a read replica is a cost/scale decision under `08/04-01`; production's
   primary-plus-replica shape is fixed in `08/01-01`.
4. The break-glass operator process and its audit retention are finalized with the on-call policy
   in `08/03-01`; production access is role-controlled and audited either way.
