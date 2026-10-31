# Versioning

The version scheme for the platform, the additive-then-breaking rule for `/api/v1`, how EF Core migrations are versioned, and how a changelog is assembled.

**Links:** [Release notes](./02-release-notes.md) · [Deployment](./03-deployment.md) · [Rollback](./04-rollback.md) · [Branching](../05-development/03-workflow/02-branching.md) · [Migrations](../03-architecture/03-data/04-migrations.md) · [Solution layout](../03-architecture/02-components/01-solution-layout.md) · [Release index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This file is the single version contract for resys-eshop. It fixes three independent version
namespaces — the **platform release**, the **public HTTP API**, and the **database schema** — and
states how each changes and how the change is recorded in the release notes (`07/02`). The commit
vocabulary that feeds the changelog is owned by `05/03-02`; the migration mechanics are owned by
`03/03-04`; the API surface being versioned is defined in §7 of the
[spec](../11-spec/README.md) and `03/04-01`.

The rule that ties the three together: **one platform release cadence, one current API version,
append-only schema migrations**. A version is a promise to consumers, so a bump is a deliberate
decision recorded in the release notes, never a side effect of a merge.

---

## Platform Versions

The platform follows **Semantic Versioning 2.0.0** — `MAJOR.MINOR.PATCH` — and a single release
train covers all three deployables (`Resys.Api`, `web/storefront`, `web/admin`) plus `web/shared`.
The front-ends and the API are versioned and released together because the generated OpenAPI client
in `web/shared` is built from the API's contract; a front-end shipped against a different API
version than the one it was built for is not a supported combination.

| Segment | Incremented when | Examples |
|---|---|---|
| `MAJOR` | A change removes or breaks a supported contract: a breaking API change, a dropped front-end route, or a release note marked `BREAKING CHANGE:`. | `1.0.0 → 2.0.0` |
| `MINOR` | New capability that is backward compatible: a new module endpoint, a new admin screen, an additive API field. This is the normal release. | `0.1.0 → 0.2.0` |
| `PATCH` | Bug fixes, documentation corrections, dependency patch bumps, and other backward-compatible corrections with no new capability. | `0.2.0 → 0.2.1` |

Pre-release versions use the SemVer pre-release suffix and sort before the corresponding final
version: `1.0.0-rc.1`, then `1.0.0-rc.2`, then `1.0.0`. Build metadata (`+build.7`) may be
appended for traceability but never changes precedence. The repository tags a release as
`v<version>` (for example `v0.1.0`), and the tag is the immutable identity of the release.

**Version 0 rules.** While the `MAJOR` segment is `0`, the platform is pre-1.0 and the API may
change shape in a `MINOR` bump, but only through the API-version procedure below — never by
silently breaking `/api/v1` in place. `v0.1.0` is the initial platform foundation (its complete
notes are in `07/02`).

---

## API Versions

The public HTTP surfaces are versioned independently of the platform version.

| Surface | Current version | Base path | Owner |
|---|---|---|---|
| Public API | `v1` | `/api/v1` | `03/04-01` |
| Admin API | unversioned | `/api/admin` | `03/04-02` |

**`/api/v1` is additive until a break.** A change to `/api/v1` is allowed without a new version
when it is backward compatible:

- Add an optional request field, a new endpoint, a new response property, or a new enum value that
  clients must tolerate.
- Relax a validation rule so previously rejected input is accepted.
- Add a new HTTP header or an opt-in query parameter.

A change requires `/api/v2` when it is backward incompatible:

- Remove or rename a field, endpoint, or route.
- Change a field's type, meaning, or nullability.
- Make an optional request field required, or tighten validation in a way that rejects
  previously valid input.
- Change authentication or authorization semantics for an existing route.
- Change or remove an enum value a client may already rely on.

When a break is unavoidable, the procedure is:

1. Introduce `/api/v2` alongside `/api/v1`. The two versions run in the same process; a new base
   path is a routing concern, not a second deployable (`03/01-02`).
2. Document the v1→v2 difference in the release notes with a `BREAKING CHANGE:` entry naming the
   affected routes and the migration step.
3. Mark `/api/v1` deprecated in `03/04-01` and the OpenAPI document (`03/04-04`), and announce a
   removal release. Deprecation is a documentation and `Deprecation`/`Sunset` response-header
   signal; it is not a removal.
4. Remove `/api/v1` only in the announced `MAJOR` release, after the deprecation window.

The admin surface is treated the same way in spirit: `/api/admin` requires the `admin` role and a
store scope (`00/01-04`), its only consumer is `web/admin`, and a breaking change is coordinated
with that app in the same release train. The admin base path is not version-segmented today; if the
admin contract ever needs to break independently, it follows the same add-new-then-deprecate
procedure and the versioning decision is recorded in `03/04-02`.

---

## Database Migration Versions

Schema change is versioned by **EF Core migrations**, not by SemVer. Each `Resys.Modules.<Name>`
project owns a migration history and a dedicated PostgreSQL schema with its own
`__EFMigrationsHistory` table (`03/02-01`, `03/03-04`). Migration identity is therefore
per-module, ordered by the UTC timestamp EF prefixes to each file; there is no global schema
version number.

Rules, restated here so release work does not contradict `03/03-04`:

- **Append-only.** Migrations are never edited or deleted after they are merged; a correction is a
  new migration. Each module's history is independent, and migrations from different modules are
  not ordered relative to one another.
- **Named for the effect.** A migration name describes the schema change (`AddVariantWeight`,
  `AddPromotionUsageLimit`), not a ticket or release number; the timestamp carries the order.
- **Committed with their schema.** The entity or configuration change and its migration land in
  the same pull request, so a release's code and schema are always consistent.
- **Tied to a release, not numbered by it.** The release notes list the migration names a release
  introduces (and any destructive change needs an explicit note; `03/03-04`), but the platform
  version does not appear in the migration name.
- **Forward-only in production.** Production never applies an old migration to go backward;
  rollback is a compensating migration, covered in `07/04`.

A release that carries migrations is a release whose deployment includes the migration step in
`07/03`; a release with no migrations omits that step but still ships its application images.

---

## Changelog

The changelog is the user-facing list of what changed between releases; the fuller narrative is the
release notes (`07/02`). Entries are assembled from the **Conventional Commit** history
(`05/03-02`) between the previous release tag and the new one, grouped by category.

| Category | Source commit types | Release-note section |
|---|---|---|
| Added | `feat` | Added |
| Changed | `refactor`, `perf`, `build`, `ci`, `chore` (behaviour-visible only) | Changed |
| Fixed | `fix` | Fixed |
| Security | any commit touching auth, secrets, or a vulnerability | Security |
| Breaking | any commit with a `BREAKING CHANGE:` footer | Breaking changes |

Rules for a changelog entry:

- **One line per user-visible change**, written in the imperative and naming the area, for example
  "add wishlist endpoints to `/api/v1`". Internal-only commits (a pure test or formatting change)
  do not earn an entry.
- **Every breaking change is called out** under Breaking changes with the affected contract and the
  consumer action required. A `feat` that is also breaking is categorized as Breaking.
- **Reference the spec or ADR** when a change is not self-explanatory, citing the identifier
  (`03/03-04`, `00/01-04`) rather than restating the decision.
- **No placeholders.** A category with nothing to report is omitted, not left empty; an unresolved
  question is written under the release's `## Open Questions`.
- **The changelog and the release notes are generated from the same source**, so they cannot
  disagree; the release owner curates the wording, the commits are the record.

The first entry, `v0.1.0`, is worked through end to end in `07/02` so the format is unambiguous
before the second release.

---

## What a Version Bump Requires

A release is cut only when the bump is justified and recorded:

1. **Decide the bump.** From the changelog, the highest-impact category wins: any Breaking change
   forces `MAJOR`, otherwise any Added forces `MINOR`, otherwise `PATCH`.
2. **Update the API contract if it moved.** An additive `/api/v1` change updates `03/04-01` and
   the generated OpenAPI document (`03/04-04`); a break follows the `/api/v2` procedure above and
   is recorded as a locked-decision change under `00/01-*`.
3. **Confirm the schema is coherent.** Every entity change in the release has its migration in the
   same release (`03/03-04`); a destructive change carries its release-note warning.
4. **Write the release notes** from the template in `07/02` and tag `v<version>`.
5. **Record the release in the metrics report.** Coverage, escape rate, and P1/P2 escapes are read
   per release and reported in the release notes (`06/03-01`).

---

## Open Questions

1. Whether a release is cut on a fixed cadence or on demand is a project-planning decision owned by
   `02/01`; the bump rules and the one-release-train rule here are fixed.
2. The exact deprecation window between marking `/api/v1` deprecated and removing it is finalized
   with the public contract in `03/04-01`; the add-new-then-deprecate order is fixed.
3. The CI provider's tag/release workflow is scaffold configuration owned by `05/03-03`; the tag
   naming (`v<version>`) and the changelog source (Conventional Commits) are fixed.

## Links

- [Release notes](./02-release-notes.md) — the template and the complete `v0.1.0` entry.
- [Deployment](./03-deployment.md) — how a tagged release is built and promoted.
- [Rollback](./04-rollback.md) — application and migration recovery.
- [Branching and commits](../05-development/03-workflow/02-branching.md) — the Conventional Commit
  vocabulary that feeds the changelog.
- [Migrations](../03-architecture/03-data/04-migrations.md) — per-module history, naming, and
  rollback expectations.
- [Solution layout](../03-architecture/02-components/01-solution-layout.md) — the deployables the
  platform version covers.
- [Design spec](../11-spec/README.md) — §7 (API versioning), §5
  (layout).
- API contracts: `03/04-01` (public), `03/04-02` (admin), `03/04-04` (OpenAPI); locked auth: `00/01-04`.
