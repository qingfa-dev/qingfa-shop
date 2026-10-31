# Database Runbook

Symptom, diagnosis, and mitigation for PostgreSQL unavailable, slow, or failing over, plus the backup and point-in-time restore steps that meet the RPO and RTO in `01/03-03`.

**Links:** [On-call and incident response](./01-oncall.md) · [Gateway runbook](./03-runbook-gateway.md) · [Double-charge runbook](./04-runbook-double-charge.md) · [Alerts](../02-monitoring/03-alerts.md) · [Dashboards](../02-monitoring/02-dashboards.md) · [Telemetry](../02-monitoring/01-telemetry.md) · [Topology](../01-infrastructure/01-topology.md) · [Environments](../01-infrastructure/02-environments.md) · [Capacity planning](../04-scaling/01-capacity.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

PostgreSQL is the only shared stateful service in resys-eshop (`03/01-02`), so almost every
availability and latency event resolves here. This runbook is `08/03-02`: it takes a database
alert or symptom and drives it to diagnosis and mitigation, then gives the restore procedure that
returns the platform after data loss. It is invoked by the on-call policy in `08/03-01`, it
references the alerts in `08/02-03` and the panels in `08/02-02`, and its RPO/RTO targets are
owned by `01/03-03`.

The single rule: **protect writes and data integrity first.** Read offload is optional and
recoverable; order, inventory, payment, and outbox writes are not. When the two conflict, the
primary serves writes and the replica tier absorbs the loss.

---

## Entry Alerts

| Alert (`08/02-03`) | Signal | First runbook section |
|---|---|---|
| `DatabaseReadinessFailing` | `/health/ready` database check failing on all replicas > 2 min. | [Database unavailable](#symptom-database-unavailable). |
| `OrderStateWriteErrors` | Any 5xx on an order/payment state-transition write for 5 min. | [Database unavailable](#symptom-database-unavailable) or [slow](#symptom-database-slow-and-pool-saturation). |
| `ConnectionPoolSaturation` | `db.client.connection.pending_requests` > 0 with wait > 1 s, or used > 90%. | [Slow and pool saturation](#symptom-database-slow-and-pool-saturation). |
| `StatementTimeoutObserved` | > 5 request-path statements at the 5 s timeout in 10 min. | [Slow and pool saturation](#symptom-database-slow-and-pool-saturation). |
| `ReplicationLagHigh` / `ReplicationLagCritical` | Replica lag > 30 s for 10 min / > 5 min. | [Replication lag](#symptom-replication-lag). |
| `WalArchiveLag` / `RpoBreach` | Last successful WAL archive > 5 min / > 15 min. | [Backup and point-in-time restore](#backup-and-point-in-time-restore). |
| `BackupFailed` | Any failed scheduled backup run. | [Backup and point-in-time restore](#backup-and-point-in-time-restore). |
| `DatabaseDiskPressure` / `DatabaseDiskCritical` | Disk > 85% / > 95%. | [Disk pressure](#symptom-disk-pressure). |

Related runbooks: `08/03-01` (declaration, escalation, comms), `08/03-03` (gateway), and
`08/03-04` (duplicate charge). The database health board in `08/02-02` is the shared view for every
step below.

---

## Triage — First Five Minutes

1. **Confirm scope.** Is the failure the primary, one replica, the pooler, or the network path? A
   single lagging replica is a Sev2 read-offload problem; a primary that cannot serve writes is a
   Sev1 revenue-path problem.
2. **Check readiness.** Read `/health/ready` per instance and the `DatabaseReadinessFailing` state
   in `08/02-02`. If all instances fail readiness, the API is out of rotation and the public path
   is down.
3. **Check the pooler and connections.** `db.client.connection.count` (idle vs used),
   `db.client.connection.pending_requests`, and `db.client.connection.timeouts` show whether the
   fault is the database or the pool budget (`08/04-01`).
4. **Check the write path.** Confirm whether the failure is read-only or write-affecting by
   watching order/payment state-transition errors (`OrderStateWriteErrors`).
5. **Declare.** If the primary cannot serve writes or all instances fail readiness, declare a
   Sev1 under `08/03-01` and start the database bridge. Read-only degradation is declared Sev2.

---

## Symptom: Database Unavailable

**Looks like.** `/health/ready` fails its database check on all instances; connection attempts time
out or are refused; `OrderStateWriteErrors` fires; checkout confirmations fail and the order stays
in `cart` (`FR-CHK-004`).

**Diagnosis.**

1. From a break-glass session (`08/01-02`), test reachability with `pg_isready` against the primary
   endpoint and the pooler endpoint. Distinguish "cannot connect" from "connects but rejects
   writes".
2. Read the managed-database status: is the primary running, recovering, or failed over? Check the
   provider event log and the last maintenance action.
3. Check whether the pooler is the fault: if the pooler is down but the database is up, the API
   cannot reach it through the transaction pool (`08/04-01`).
4. Confirm whether a recent change is responsible: a migration job (`07/03`, `03/03-04`), a
   configuration change, or a storage/network event.
5. Determine data-loss exposure: compare the last successful WAL archive timestamp against the RPO
   target (below).

**Mitigation.**

1. **If the pooler is the fault**, restart or fail over the pooler and confirm connections recover.
   The database and its data are untouched.
2. **If the primary is unreachable**, trigger managed failover to the replica (see
   [Primary failover](#symptom-primary-failover)). Failover is the fast path; a restore is the slow
   path.
3. **If the failure is total and no replica is promotable**, restore from backup and WAL archive
   (below). Declare the expected data-loss window to the IC for customer comms.
4. **Protect the write path while degraded.** Do not enable a write cache or bypass the payment
   and outbox transactions; a confirm that cannot reserve stock and write its outbox row in one
   transaction must fail safely and preserve the cart (`FR-CHK-004`, NFR-AVAIL-005).
5. **Reduce load if needed.** Shed non-essential read traffic and pause non-critical jobs so the
   recovering primary is not immediately re-saturated.
6. **Verify.** `/health/ready` returns `200`, confirmations succeed, and the pool pending count
   returns to zero before declaring recovery.

---

## Symptom: Database Slow and Pool Saturation

**Looks like.** `StatementTimeoutObserved` fires; checkout step p95 rises; `db.client.operation.duration`
p95 climbs; `ConnectionPoolSaturation` shows pending requests waiting or the pool near its cap;
latency alerts (`08/02-03`) may fire as a consequence.

**Diagnosis.**

1. Identify the top statements with `pg_stat_statements` and the slow-query log (`08/02-01`), and
   join them to request spans to name the route and store.
2. Check for a lock wait or a long transaction: a transaction held across an external call
   exhausts the pool and is a known anti-pattern (NFR-PERF-009, `08/04-01`).
3. Check whether a missing or unused index appeared after a catalog import or schema change.
4. Check pool arithmetic: per-replica Npgsql pool ≤ 20 and PgBouncer `default_pool_size` 25
   (`08/04-01`); if replicas scaled without the pool budget, the pooler is the bottleneck.
5. Distinguish primary slowness from replica slowness; only read-only catalog, listing, and search
   traffic may be served from a replica (`08/04-01`).

**Mitigation.**

1. **Kill the offending statement or transaction** where safe, then let the pool drain. Prefer
   terminating the specific backend over restarting the database.
2. **Add a missing index** through the migration path (`03/03-04`); do not add it by hand on the
   primary outside the reviewed bundle.
3. **Scale the read path**: add a read replica or route more read-only queries to the replica tier
   while lag is below the eligibility threshold (`08/04-01`).
4. **Raise pooler capacity** only as a bounded step, keeping the sum of pools under the server
   `max_connections` budget; recurrent saturation is a signal to add a pooler or replica, not to
   raise the pool without bound (`08/04-01`).
5. **Shed load** by pausing batch/reporting queries during the incident; they are not
   revenue-path.
6. **Verify** that p95 returns inside the NFR-PERF-001 budget, statement timeouts stop, and the
   pending-request count returns to zero.

---

## Symptom: Replication Lag

**Looks like.** `ReplicationLagHigh` or `ReplicationLagCritical` fires; replica reads may serve
stale catalog data; a lagging replica is ineligible to serve reads.

**Diagnosis.**

1. Check the replica's receive/replay position against the primary's write position.
2. Check for a long-running write or a bulk operation on the primary that the replica must replay.
3. Check replica resource saturation (CPU, disk) and network throughput between primary and
   replica.

**Mitigation.**

1. **Remove the lagging replica from read rotation** until it catches up; serving stale data is a
   correctness risk, not a performance trade (`08/04-01`).
2. **Pause bulk writes** that are driving the lag; resume them after the replica catches up.
3. **If the replica cannot catch up**, rebuild it from a fresh base backup and rejoin it to
   streaming replication.
4. **If lag threatens failover readiness**, verify another replica is current before any failover
   decision.
5. **Verify** lag is back below the read-eligibility threshold and the replica has rejoined
   rotation.

---

## Symptom: Primary Failover

**Looks like.** The primary is lost or unrecoverable; the platform must promote a replica to
restore writes.

**Diagnosis.**

1. Confirm the primary is genuinely unrecoverable (not a transient network partition) before
   promoting; a split-brain promotion is worse than a short outage.
2. Confirm the candidate replica is current: lag is within the RPO window (below).
3. Identify the failover mechanism (managed automatic failover or an operator-driven promotion) and
   the write endpoint the API uses.

**Mitigation.**

1. **Trigger the managed failover** and wait for the promoted replica to accept writes.
2. **Repoint the API** if the write endpoint changed; the readiness probe and short connection
   retry absorb the transition (`08/04-01`, NFR-AVAIL-006).
3. **Re-establish replication**: rebuild the old primary (or a new replica) from the promoted
   primary and rejoin it.
4. **Verify** that `/health/ready` is green, order and payment writes succeed, and outbox events
   drain. The relay resumes publishing committed events; no event is lost because it was written in
   the same transaction as its state change (NFR-AVAIL-007).
5. **Reconcile** any in-flight checkout that failed during the transition; the cart is preserved
   and the shopper may retry (`FR-CHK-007`).

---

## Symptom: Disk Pressure

**Looks like.** `DatabaseDiskPressure` (> 85%) or `DatabaseDiskCritical` (> 95%) fires.

**Diagnosis.** Identify the growth source: WAL retention, table/index bloat, temporary files from
large sorts, or log volume.

**Mitigation.** Confirm WAL archiving is keeping up (a stalled archive is the usual cause of WAL
accumulation) and resolve that first; reclaim space from bloat or temporary files; extend the
volume before it reaches the critical threshold. Never delete WAL segments that have not been
archived, or the RPO target is lost.

---

## Backup and Point-in-Time Restore

### Backups

- Continuous WAL archiving to object storage, plus periodic base backups, gives point-in-time
  recovery (`08/01-01`).
- Backups are encrypted at rest and in transit and stored off the primary host (`08/01-02`).
- `WalArchiveLag`, `RpoBreach`, and `BackupFailed` in `08/02-03` guard the archive and the backup
  jobs; a stalled archive is treated as an RPO breach, not a warning.

### Restore Procedure

1. **Declare and isolate.** Declare the incident under `08/03-01`; stop writes to the failed
   primary so the restore point is stable.
2. **Choose the target time.** Pick the latest point before the data-loss event, within the RPO
   window. Confirm the chosen point with the IC before restoring.
3. **Restore the base backup** to a new instance using the managed restore path or `pg_basebackup`
   / the provider console, into an isolated instance first.
4. **Replay WAL to the target time** using point-in-time recovery (recovery target time or
   transaction). Confirm the replay stops exactly at the chosen point.
5. **Validate before cutover.** Run integrity checks: order/payment state consistency, outbox rows
   matching their state changes, and store scoping intact (`03/03-01`). Confirm no schema migration
   is pending (`03/03-04`).
6. **Cut over.** Repoint the write endpoint to the restored primary, rejoin replicas, and confirm
   `/health/ready` is green.
7. **Reconcile the gap.** Reconcile payments and orders that occurred after the restore point using
   the payment reconciliation job and the gateway runbook `08/03-03`; a payment captured after the
   restore point is corrected, never silently dropped (`03/04-05`).
8. **Verify.** Confirmations succeed, outbox drains, dashboards recover, and the availability and
   error budgets are reported for the incident window.
9. **Record.** Capture the actual RPO achieved and the restore duration for the postmortem; feed
   any gap back into `08/04-01` and `08/02-02`.

### Restore Rehearsal

A timed restore rehearsal runs on a defined cadence from this procedure against a scratch
environment (`08/01-02`, NFR-AVAIL-004). The rehearsal proves the RTO, validates the backup chain,
and exercises the reconciliation step; recovery reports are reviewed under `08/03-01`.

---

## RPO and RTO

| Target | Value | Owner |
|---|---|---|
| **RPO** | ≤ 15 minutes, via continuous WAL archiving / point-in-time recovery. | NFR-AVAIL-004 (`01/03-03`). |
| **RTO** | ≤ 1 hour to restore service after a database loss. | NFR-AVAIL-004 (`01/03-03`). |

An RPO breach (`WalArchiveLag` > 15 min) is a Sev1 in `08/02-03`: the platform's data-loss exposure
has exceeded its target even if no restore is needed yet. The restore procedure above is written to
the RTO, and the rehearsal measures the real number.

---

## Verification and Exit Criteria

- `/health/ready` is green on every API instance and the database check passes.
- Order, payment, inventory, and outbox writes succeed; `OrderStateWriteErrors` is clear.
- Connection pool pending count is zero and statement timeouts have stopped.
- Replication is re-established and lag is below the read-eligibility threshold.
- WAL archiving has resumed; the last archive is inside the RPO window.
- The outbox relay is draining and no `OrderPlaced` / `PaymentCompleted` event is stranded.
- The incident is downgraded or resolved under `08/03-01`, with a postmortem opened for Sev1/Sev2.

---

## Escalation and Communication

- Declare and escalate per `08/03-01`; a primary loss or all-replica readiness failure is Sev1 and
  pages immediately.
- The Communications Lead states customer impact (browse, cart, checkout) and the workaround;
  confirmations that fail safely preserve the cart (`FR-CHK-007`).
- Never publish a cause or an ETA that is not confirmed; never include PAN, CVV, tokens, or
  customer PII in an update (NFR-SEC-007).
- Engage the managed-database provider at L5 when the fault is provider-side (failover, quota,
  storage).

---

## Open Questions

1. The exact backup cadence, retention window, and restore-rehearsal schedule are finalized with
   `08/01-02`; the RPO/RTO targets are fixed by `01/03-03`.
2. Whether recovery is single-region or multi-region is a capacity/infrastructure decision owned by
   `08/01-01` and `08/04-01`; the RPO/RTO targets hold either way.
3. The concrete failover mechanism and write endpoint are deployment values recorded with
   `08/01-02`; the failover procedure and its validation are fixed here.
4. The replication-lag threshold that gates replica read eligibility is finalized with `08/02-01`
   and `08/01-01`; the lag-gating rule is fixed by `08/04-01`.
