# 07 — Release

How resys-eshop is versioned, released, deployed, and recovered: the version contract, the release-notes format, the deployment path, and the rollback procedures.

**Links:** [Versioning](./01-versioning.md) · [Release notes](./02-release-notes.md) · [Deployment](./03-deployment.md) · [Rollback](./04-rollback.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area is the release discipline for resys-eshop. It turns a merged, green change set (`05/03-03`)
into a tagged, deployable, recoverable release, and it fixes the three version namespaces — the
platform release, the `/api/v1` contract, and the per-module schema migrations — so the rest of the
documentation can cite one source. The deployment detail pairs with the operations topology in
`08/01-01` and the secrets/environment contract in `08/01-02`; the migration mechanics it relies on
are owned by `03/03-04`.

The area is four numbered files, each one concern and one reading path. Start with `01-versioning`
to know when and how something is versioned, then follow `02-release-notes` for the format and the
worked `v0.1.0` example, `03-deployment` for the build → migrate → promote path, and `04-rollback`
for the recovery options when a release goes wrong. When a statement here conflicts with the
[design spec](../11-spec/README.md), the spec wins and this area
is corrected.

Release work consumes the branching and commit rules in `05/03-02`, the CI gates in `05/03-03`, the
migration workflow in `03/03-04`, and the quality metrics in `06/03-01`, whose per-release report is
attached to the release notes.

---

## File Index

| File | Concern | Read it when |
|---|---|---|
| [01-versioning.md](./01-versioning.md) | Semantic versioning, the additive-then-breaking `/api/v1` rule, EF Core migration versioning, and the changelog rules. | You are cutting a release or changing a public contract. |
| [02-release-notes.md](./02-release-notes.md) | The release-notes template and the complete `v0.1.0` initial-foundation entry. | You are writing notes for a release. |
| [03-deployment.md](./03-deployment.md) | Environments, container build and publish for `Resys.Api` and both Nuxt apps, the PostgreSQL migration step, the env-var matrix, and Kubernetes notes. | You are shipping a tagged release to staging or production. |
| [04-rollback.md](./04-rollback.md) | Application rollback, migration rollback and forward-fix, and the feature-flag kill switch. | A release misbehaves and you need to recover. |

---

## How to Read This Area

1. **Version the change.** `07/01` decides whether the release is a `MAJOR`, `MINOR`, or `PATCH`
   bump, whether `/api/v1` may change in place or a `/api/v2` is required, and how the changelog is
   assembled from Conventional Commits (`05/03-02`).
2. **Write the notes.** `07/02` fixes the template every release uses and shows `v0.1.0` filled in,
   including its migrations, verification evidence, upgrade steps, and rollback section.
3. **Build, migrate, promote.** `07/03` builds three immutable images, runs the once-only
   idempotent migration step before the application rollout, and promotes the same digests from
   staging to production with the per-environment variable matrix.
4. **Be ready to recover.** `07/04` gives the rollback-versus-forward-fix decision, the application
   rollback procedure, the forward-only migration rule, and the flag kill switch.
5. **Cite by identifier, link what exists.** Every doc in this set exists; cite another doc as
   `AA/BB-CC` (flat areas as `AA/NN`) and link it when a jump is useful.

---

## Release at a Glance

| Concern | Decision | Owner |
|---|---|---|
| Platform version | SemVer `MAJOR.MINOR.PATCH`, one release train for API and both front-ends. | `07/01` |
| API contract | `/api/v1` additive until a break; a break adds `/api/v2` and deprecates v1. | `07/01` |
| Schema | Per-module EF Core migrations, append-only, forward-only in production. | `03/03-04`, `07/01` |
| Notes | One entry per tag, generated from commits, with migrations and verification. | `07/02` |
| Deploy | Build once per tag; migrate once before rollout; promote by digest. | `07/03` |
| Recover | Application rollback, compensating migration, or flag kill switch. | `07/04` |
| Release report | Coverage, escape rate, and P1/P2 escapes attached to the notes. | `06/03-01` |

The three deployables are exactly those in `03/01-02`: one `Resys.Api` service and the two Nuxt
front-ends. `web/shared` and the `Resys.*` libraries are build-time inputs, never deployed.

---

## Related Areas

- `00` (governance) — the ADRs that lock the stack and auth posture the release ships (`00/01-*`),
  and the review/definition-of-done policies (`00/02-*`).
- `02` (planning) — the roadmap and its P7 hardening phase, which includes the rollback rehearsal.
- `03` (architecture) — the app topology, module layout, data migrations, and API contracts the
  release deploys.
- `05` (development) — the build/run commands, the branch and commit rules, and the CI gates.
- `06` (verification) — the test strategy and the quality metrics reported per release.
- `08` (operations) — the production topology, environments, secrets, and scaling that `07/03`
  deploys into.
- `10` (maintenance) — the dependency policy that governs upgrades behind a version bump.

---

## Keeping This Area Current

- Update `07/01` when the API version changes, a deprecation window is set, or the changelog
  categories change; keep it aligned with `03/04-01` and `03/04-04`.
- Update `07/02` when the template gains a section; add one entry per release and keep each
  entry's migration table aligned with `03/03-04`.
- Update `07/03` when an environment, an image, the migration step, or a variable changes; keep the
  variable contract aligned with `05/01-02`.
- Update `07/04` when the rollback path or the feature-flag rules change; keep the forward-fix rule
  aligned with `03/03-04` and the restore runbook in `08/01-01`.

## Open Questions

None at the area level. Unresolved details live in the owning file under its `## Open Questions`
section: the release cadence and `/api/v1` deprecation window in `07/01`, the tag workflow in
`05/03-03`, the registry and migration-step mechanism in `08/01-01`, and the feature-flag
abstraction in the implementation plan.
