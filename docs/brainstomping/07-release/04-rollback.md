# Rollback

Application rollback, migration rollback and forward-fix, and the feature-flag kill switch that disables a risky change without a redeploy.

**Links:** [Versioning](./01-versioning.md) · [Release notes](./02-release-notes.md) · [Deployment](./03-deployment.md) · [Migrations](../03-architecture/03-data/04-migrations.md) · [App topology](../03-architecture/01-overview/02-app-topology.md) · [Environment config](../05-development/01-setup/02-environment.md) · [Release index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This file is the recovery path when a deployment (`07/03`) goes wrong. It defines when to roll
back versus roll forward, how an application rollback redeploys the previous image digests, why a
database rollback is a compensating migration rather than an old migration, and how a feature flag
kills a risky change while leaving the release in place. It is the operations companion to the
release notes' `## Rollback` section (`07/02`) and consumes the migration rules in `03/03-04` and
the topology in `03/01-02`.

The governing principle: **the database is the state you cannot lose.** An application rollback is
cheap and reversible because it replaces stateless containers; a schema change is not, because the
data written after a migration cannot be recovered by running the old migration. Every rollback
decision is therefore made relative to the schema, and the default response to a bad migration is
forward-fix, not backward migration.

---

## Decide: Roll Back or Roll Forward

| Symptom | Response |
|---|---|
| New application version is broken; schema is unchanged or backward compatible. | **Application rollback** to the previous image digests. |
| A single feature misbehaves behind a flag; the rest of the release is healthy. | **Kill switch** — disable the flag; no redeploy. |
| Migration applied and the new code cannot tolerate the post-migration schema. | **Forward-fix** with a compensating migration, or expand/contract on the next deploy. |
| Migration partially applied or failed mid-run. | **Stop the rollout**, keep the previous application version, and **forward-fix**; then restore only if the data is unrecoverable. |
| Destructive change lost data and no valid backup exists. | **Point-in-time restore** as the last resort (`03/03-04`); this is an incident, not a routine rollback. |

The two rules that decide the path:

- **If no migration is involved, roll the application back.** The previous image is a known-good
  stateless artifact; deploy it and stop the rollout.
- **If a migration is involved, do not run the old migration backward.** Roll the application back
  only if it tolerates the current (new) schema; otherwise forward-fix. Production schema rollback
  is never `dotnet ef database update <PreviousMigration>` (`03/03-04`).

---

## Application Rollback

An application rollback redeploys the previous release's immutable image digests — never a
rebuilt tag — for all three deployables together, because `web/shared` is generated from the API
contract and the release train is versioned as a unit (`07/01`).

Procedure:

1. **Identify the last known-good release.** The `v<previous>` tag and its three image digests are
   recorded in the previous release notes (`07/02`) and in the registry.
2. **Confirm the schema assumption.** Check the migration table in the new release's notes. If the
   release carried no migrations, the previous application version runs against the current schema
   unchanged. If it carried migrations, verify the previous version tolerates the current schema
   (the expand/contract window is designed to allow exactly this, `03/03-04`); if it does not,
   forward-fix instead.
3. **Redeploy the previous digests.** Update the three Deployments to the previous image digests.
   The rolling update replaces pods as readiness passes; `maxUnavailable: 0` keeps capacity
   through the change (`07/03`).
4. **Confirm health.** Watch `/health/ready` for the API and the front-end probes; a rollback is
   complete only when the previous version serves traffic and the error signal that triggered it
   has cleared.
5. **Record the event.** Open a release note or incident entry with the trigger, the digests
   deployed, and whether the schema was left in place; the metrics report reads this to attribute
   the escape (`06/03-01`).
6. **Re-verify before redeploying the failed version.** The failed version returns only with a fix
   and, if the failure was user-visible, a test at the layer mapped in `06/01-01`.

Rolling back the front-ends alone is allowed when only a UI regression is at fault; the API
rollback is the one that must respect the schema. The three images are still versioned together,
so a partial rollback is recorded as a deliberate deviation with the API version it pairs with.

---

## Migration Rollback and Forward-Fix

Because migrations are append-only and one history exists per module (`03/03-04`), there is no
global "previous schema version" to return to. The recovery options, in order of preference:

**1. Forward-fix with a compensating migration.** The normal response. Add a new migration that
restores the intended schema shape or corrects the faulty change. Applying it is the same migration
step as a normal release (`07/03`), it is idempotent, and it never requires running an old
migration. The compensating migration restores the **shape**, not necessarily lost rows; this is
why a destructive change needs a backup and a release-note warning.

**2. Expand and contract.** If a change was staged but the contract step is unsafe, stop at the
current stage and leave the old column or table in place. The new code already tolerates both
shapes by design, so the release is safe without a schema change; the contract step waits for a
later release. This is the reason the expand/contract pattern exists (`03/03-04`).

**3. Application rollback against the new schema.** Allowed only while the previous application
version can read and write the migrated schema. This is the intended pairing for a bad code change
that shipped alongside a backward-compatible migration.

**4. Point-in-time restore.** The last resort for data loss from a destructive migration with no
acceptable compensating migration. It restores the PostgreSQL instance to a timestamp before the
change; the runbook and RPO/RTO are owned by `08/01-01`. Everything written after the restore
point is lost, so this is an incident-level decision, not routine rollback.

Rules:

- **Never edit or delete a merged migration** to undo it; that rewrites schema history other
  environments already applied (`03/03-04`).
- **Every migration ships a `Down`, but production does not use it to roll back.** The `Down` is
  reviewed for correctness and used by tests and local resets; production recovery is forward-fix.
- **A destructive migration is irreversible in practice.** It ships only with a verified backup and
  a release-note warning (`07/02`); the compensating migration restores the shape, not the rows.
- **The migration step is single-instance and idempotent** (`07/03`), so a retry after a partial
  failure is safe and is the first move when a migration job fails.

---

## Feature-Flag Kill Switch

A feature flag is a configuration toggle that enables or disables a code path without a redeploy.
It exists so an operator can neutralize the riskiest change in a release in seconds, before the
slower application-rollback path, and without touching the schema.

Flags follow the options pattern in `05/01-02`: they are bound from configuration, read per
request or per operation, and default to **off** for a new risky path and **on** for an established
one. They are supplied as environment variables so staging and production differ by configuration,
not by image:

```
Features__<Area>__<Flag>   # e.g. Features__Checkout__NewPaymentRetry=true
```

Rules:

- **Default off for risky changes.** A new checkout, payment, or pricing path ships disabled in
  production; the release note names the flag and the default.
- **A flag has one owner and one removal release.** The flag's name and the release that removes it
  are recorded in the release notes, so flags do not accumulate. A flag with no removal plan is
  debt (`10/01`).
- **The kill switch is a config change plus a restart or a per-request re-read of the bound
  option.** Disabling a flag must not require a new image; it is the first response in the decision
  table above.
- **A flag never hides a schema dependency.** Disabling a flag is not a schema rollback; if the
  code path being disabled wrote rows under the new schema, those rows remain and the forward-fix
  rules above apply.
- **Flags are not a substitute for tests.** A path behind a flag still has its tests at the layer
  mapped in `06/01-01`; the flag controls exposure, not correctness.

The flag abstraction itself (a strongly-typed options object or a dedicated flag service) is an
implementation choice owned by the implementation plan after `03/03-1x`; the names, defaults, and
kill-switch semantics are fixed here.

---

## Rollback Readiness

Rollback is rehearsed, not improvised. Before a release is cut:

- The previous release's image digests are resolvable from the registry and recorded in `07/02`.
- Every destructive migration has a verified backup; the restore runbook is exercised in staging.
- The riskiest change of the release has a flag with an off default and a named kill switch.
- The `## Rollback` section of the release notes (`07/02`) names the previous tag, the
  compensating-migration decision, and the flag to disable.
- P7 hardening includes a rollback rehearsal that restores the previous release without data loss
  beyond the stated RPO (`02/01`).

---

## Open Questions

1. Whether the feature-flag abstraction is a plain options object or a dedicated service with
   per-request evaluation is an implementation choice owned by the implementation plan; the
   naming, off-by-default, and no-redeploy kill-switch rules are fixed here.
2. The production restore runbook, RPO/RTO, and the point-in-time-restore procedure are owned by
   `08/01-01`; this file fixes when restore is the correct last resort.
3. Whether the registry or the release pipeline is the authoritative record of previous image
   digests is scaffold configuration owned by `08/01-01`; the digests are recorded in the release
   notes (`07/02`) either way.
4. How a partial front-end rollback is represented when the API stays forward is finalized with the
   release checklist; the versioning train (`07/01`) treats the three deployables as one unit.

## Links

- [Versioning](./01-versioning.md) · [Release notes](./02-release-notes.md) · [Deployment](./03-deployment.md)
- [Migrations](../03-architecture/03-data/04-migrations.md) — append-only history, expand/contract,
  forward-only production.
- [App topology](../03-architecture/01-overview/02-app-topology.md) — health endpoints, stateless
  API, outbox relay safety across replicas.
- [Environment configuration](../05-development/01-setup/02-environment.md) — the options pattern in
  which feature flags are bound.
- [Roadmap](../02-planning/01-roadmap.md) — the P7 rollback rehearsal exit criterion.
- [Quality metrics](../06-verification/03-quality/01-metrics.md) — defect attribution per release.
- [Design spec](../11-spec/README.md) — §10 (configuration),
  §12 (verification).
- Operations restore runbook and RPO/RTO: `08/01-01`; release checklist: `07/02`.
