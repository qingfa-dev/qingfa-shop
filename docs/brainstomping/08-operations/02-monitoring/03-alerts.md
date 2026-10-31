# Alerts

The alert rules for resys-eshop: what fires, at what threshold, at what severity, and which incident runbook an on-call engineer follows.

**Links:** [Telemetry](./01-telemetry.md) · [Dashboards](./02-dashboards.md) · [Infrastructure topology](../01-infrastructure/01-topology.md) · [Capacity planning](../04-scaling/01-capacity.md) · [Environments](../01-infrastructure/02-environments.md) · [Performance NFRs](../../01-requirements/03-nonfunctional/01-performance.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Security NFRs](../../01-requirements/03-nonfunctional/02-security.md) · [Payment gateways](../../03-architecture/04-integration/05-payment-gateways.md) · [Performance budgets](../../06-verification/03-quality/02-performance-budgets.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the alerting contract for resys-eshop. Each rule below names the signal it evaluates,
the threshold and duration that make it fire, the severity that sets its routing, and the incident
runbook an on-call engineer follows. The signals are defined in `08/02-01`; the panels that show the
same data are in `08/02-02`; the runbooks are `08/03-01` (on-call), `08/03-02` (database),
`08/03-03` (payment gateway), and `08/03-04` (double charge). The targets the thresholds are derived
from are the availability NFRs (`01/03-03`), the performance NFRs (`01/03-01`), and the security
NFRs (`01/03-02`).

The rules follow three principles:

- **Page on user impact, not on causes.** Error-budget burn, failed requests, and failed
  confirmations page; CPU, a single slow query, or one pod restart does not unless it is visibly
  degrading a customer path.
- **Every alert has a runbook.** A rule whose `Runbook` cell has no owner is removed or converted to
  a dashboard annotation, never left to page into a vacuum.
- **Multi-window, burn-rate first.** Availability alerts use paired long and short windows so a
  brief spike does not page, but a sustained burn escalates.

Alert state is environment-scoped: a `Staging` signal never pages a production rotation
(`08/01-02`). The routing table and severity definitions below are owned by the on-call policy in
`08/03-01`.

---

## Severity Levels

| Severity | Meaning | Routing | Response |
|---|---|---|---|
| **Sev1 — Page** | Customer-facing loss or severe impairment of a revenue path; error-budget fast burn; data-integrity risk (for example a duplicate charge). | Page primary on-call immediately, 24/7. | Acknowledge and start the named runbook; declare an incident if impact is confirmed (`08/03-01`). |
| **Sev2 — Urgent** | Significant degradation with partial impact or a workaround; a dependency or resource is close to failure; slow error-budget burn. | Page on-call during working hours; out-of-hours page only if the rule is marked critical. | Investigate within the on-call response target; open a ticket if not customer-visible yet. |
| **Sev3 — Ticket** | Warning or trend that needs attention but is not currently customer-visible. | Ticket to the owning team; no page. | Address in the next working cycle; convert to Sev2 if it worsens. |

`Sev1` and `Sev2` pages carry the runbook id in the notification body, so the responder does not
have to translate an alert name into a procedure.

---

## Alert Rules

### Service availability and latency

| Name | Signal | Threshold | Severity | Runbook |
|---|---|---|---|---|
| PublicAPIErrorBudgetFastBurn | `resys.http.requests.total{endpoint_group="public_read",status_class="5xx"}` vs total (edge and API) | 14.4× burn over 1 h **and** 6× over 5 min (2% of monthly budget consumed) | Sev1 | `08/03-01` |
| PublicAPIErrorBudgetSlowBurn | Same signal | 6× burn over 6 h **and** 3× over 30 min | Sev2 | `08/03-01` |
| PublicAPIErrorBudgetTicketBurn | Same signal | 1× burn over 72 h (budget will be exhausted before month end) | Sev3 | `08/03-01` |
| AdminAPIErrorBudgetBurn | `resys.http.requests.total{endpoint_group="admin",status_class="5xx"}` vs total | 14.4× burn over 1 h (against the 99.5% admin target) | Sev2 | `08/03-01` |
| PublicAPIAvailabilityBreach | Edge/API request success over the monthly window | Success < 99.9% month-to-date | Sev1 | `08/03-01` |
| ReadinessFlapping | `/health/ready` transitions per pod | ≥ 3 ready→not-ready transitions in 10 min on any pod | Sev2 | `08/03-01` |
| PublicReadLatencyBudget | `http.server.request.duration` p95 by public read route | p95 > 300 ms for 10 min | Sev2 | `08/03-01` |
| PublicReadLatencySevere | Same signal | p95 > 600 ms (2× budget) for 5 min | Sev1 | `08/03-01` |
| SearchLatencyBudget | `http.server.request.duration` p95 for the search group | p95 > 500 ms for 10 min | Sev2 | `08/03-01` |
| CheckoutStepLatency | `resys.checkout.step.duration` p95 by step | p95 > 500 ms for 10 min, excluding gateway | Sev2 | `08/03-01` |

### Checkout and orders

| Name | Signal | Threshold | Severity | Runbook |
|---|---|---|---|---|
| CheckoutConfirmFailureRate | Failed confirmations ÷ confirmations | > 2% over 10 min | Sev1 | `08/03-01` |
| CheckoutConfirmFailureElevated | Same signal | > 0.5% over 30 min | Sev2 | `08/03-01` |
| OrderPlacementStalled | `resys.checkout.completed` rate vs checkout start rate | Confirm rate < 50% of checkout starts over 15 min | Sev1 | `08/03-01` |
| ReservationConflictSpike | Failed stock reservations at confirm | > 50 in 10 min per store | Sev2 | `08/03-01` |
| OrderStateWriteErrors | Order/payment database operation errors | Any 5xx on a state-transition write for 5 min | Sev1 | `08/03-02` |

### Payment gateways

| Name | Signal | Threshold | Severity | Runbook |
|---|---|---|---|---|
| PaymentSuccessRateLow | `resys.payment.attempts` captured+authorized ÷ total by gateway | < 90% over 5 min | Sev1 | `08/03-03` |
| PaymentSuccessRateDegraded | Same signal | < 97% over 15 min | Sev2 | `08/03-03` |
| GatewayCircuitOpen | `resys.payment.circuit.open` by gateway | Circuit open for > 1 min | Sev1 | `08/03-03` |
| GatewayLatencyHigh | `resys.payment.gateway.duration` p95 by gateway/operation | p95 > the configured timeout budget for 10 min | Sev2 | `08/03-03` |
| GatewayRetryableErrorSpike | `resys.payment.gateway.errors{retryable="true"}` | Errors > 5× the trailing 1 h baseline for 10 min | Sev2 | `08/03-03` |
| DuplicateChargeSuspected | `resys.payment.duplicate_suspected` from the reconciliation job | Any increment | Sev1 | `08/03-04` |
| ReconciliationBacklog | Unknown-outcome payments awaiting reconciliation | Oldest unresolved > 30 min | Sev2 | `08/03-03` |

### Database and durability

| Name | Signal | Threshold | Severity | Runbook |
|---|---|---|---|---|
| DatabaseReadinessFailing | `/health/ready` database check | Failing > 2 min on all replicas | Sev1 | `08/03-02` |
| ConnectionPoolSaturation | `db.client.connection.pending_requests` and pool used ratio | Pending > 0 with wait > 1 s for 5 min, or used > 90% of the per-replica cap | Sev2 | `08/03-02` |
| StatementTimeoutObserved | Count of request-path statements reaching the 5 s timeout | > 5 in 10 min | Sev2 | `08/03-02` |
| ReplicationLagHigh | Replica replication lag | > 30 s for 10 min | Sev2 | `08/03-02` |
| ReplicationLagCritical | Same signal | > 5 min | Sev1 | `08/03-02` |
| WalArchiveLag | Time since last successful WAL archive | > 5 min | Sev2 | `08/03-02` |
| RpoBreach | Time since last successful WAL archive | > 15 min (RPO target) | Sev1 | `08/03-02` |
| BackupFailed | Scheduled backup job status | Any failed backup run | Sev2 | `08/03-02` |
| DatabaseDiskPressure | Managed database disk utilization | > 85% | Sev2 | `08/03-02` |
| DatabaseDiskCritical | Same signal | > 95% | Sev1 | `08/03-02` |

### Outbox and event delivery

| Name | Signal | Threshold | Severity | Runbook |
|---|---|---|---|---|
| OutboxOldestEventAge | `resys.outbox.oldest_age_seconds` by store | > 30 min | Sev1 | `08/03-01` |
| OutboxBacklogWarning | Same signal | > 5 min | Sev2 | `08/03-01` |
| OutboxDeadLetter | `resys.outbox.dead_lettered` | Any increment | Sev2 | `08/03-01` |
| OutboxPublishFailures | `resys.outbox.failed` by `event_type` | > 20 in 15 min | Sev2 | `08/03-01` |
| EventConsumerStalled | Consumer delivery counters vs publish counters | Consumer lag > 15 min while relay publishes | Sev2 | `08/03-01` |

### Capacity, infrastructure, and security

| Name | Signal | Threshold | Severity | Runbook |
|---|---|---|---|---|
| ApiReplicasBelowFloor | Running API pods vs the ≥ 2 floor | < 2 running for > 2 min | Sev1 | `08/03-01` |
| ApiSaturation | `process.cpu.utilization` and per-replica request rate | CPU > 65% or > 150 req/s per replica for 10 min | Sev3 | `08/03-01` |
| PodRestartLoop | Restart count per workload | ≥ 3 restarts in 10 min | Sev2 | `08/03-01` |
| FrontendEventLoopLag | Node event-loop lag on storefront/admin | Lag > 200 ms p95 for 10 min | Sev2 | `08/03-01` |
| CacheHitRatioDrop | Edge/CDN hit ratio | Drop > 20 percentage points vs trailing 24 h for 30 min | Sev3 | `08/03-01` |
| EdgeCertificateExpiry | TLS certificate days to expiry | < 14 days | Sev3 | `08/03-01` |
| EdgeCertificateExpiryUrgent | Same signal | < 7 days | Sev2 | `08/03-01` |
| CredentialAbuseSpike | Auth/registration `429` and `401` rate from a client group | > 10× trailing baseline for 10 min | Sev2 | `08/03-01` |

---

## Error-Budget Burn Rate

Availability alerts use the standard multi-window burn-rate pairs so a short spike does not page:

| Condition | Long window | Short window | Burn rate | Action |
|---|---|---|---|---|
| Fast burn | 1 h | 5 min | 14.4× → Sev1 page | 2% of the monthly budget consumed in 1 h. |
| Slow burn | 6 h | 30 min | 6× → Sev2 page | 5% of budget consumed in 6 h. |
| Ticket burn | 72 h | 1 h | 1× → Sev3 ticket | Budget on pace to exhaust before month end. |

The public target is 99.9% and the admin target is 99.5% (NFR-AVAIL-003); each target has its own
budget and its own burn series. The Service Overview dashboard (`08/02-02`) renders the same series
so a page and a panel never disagree.

---

## Routing, Suppression, and Deduplication

- **Routing by severity and owner.** Sev1 pages the primary on-call immediately; Sev2 pages the
  owning team's on-call in hours and the primary out of hours for revenue-path rules; Sev3 opens a
  ticket. The rotation, escalation, and contact policy are `08/03-01`.
- **Environment scoping.** Alerts carry `deployment.environment`; staging and development alerts
  route to a non-paging channel (`08/01-02`).
- **Grouping.** Alerts are grouped by service, store, and route (or gateway) so one database event
  produces one page, not one page per route.
- **Inhibition.** A database Sev1 suppresses the latency and error alerts it explains; a readiness
  failure suppresses dependent request alerts for the same instance. The runbook, not the alert
  cascade, drives the next check.
- **Maintenance windows.** A planned deployment or migration (`07/03`, `07/04`) opens a change
  window that suppresses the affected rules for its duration; the window is closed and verified, not
  left on. Announced maintenance is excluded from the availability target (NFR-AVAIL-003).
- **Deduplication.** A repeat of the same group within its clear period is a reminder on the open
  incident, not a new page.
- **Runbook link in every notification.** The notification body carries the runbook id from the
  table, so an on-call engineer starts the correct procedure from the alert itself.

---

## Alert Hygiene and Review

- **No silent acceptance.** A flapping or routinely-ignored rule is fixed by tuning the threshold or
  deleting the rule; it is never muted indefinitely.
- **Thresholds trace to an NFR or an SLO.** Changing a threshold is a decision recorded here; a
  budget or target change is a requirements change in `01/03-01`/`01/03-03`.
- **Post-incident review.** Every Sev1 review under `08/03-01` asks whether an alert fired in time,
  too late, or too loudly, and updates this file and `08/02-02` together.
- **Coverage check.** Each panel in `08/02-02` that shows a budget has a matching rule or an
  explicit decision that dashboard-only is acceptable; a customer-facing budget without a rule is a
  gap.
- **Runbook validity.** A rule whose runbook no longer exists or no longer matches is a defect;
  runbooks are `08/03-01` … `08/03-04`.

## Open Questions

1. The concrete paging product, escalation ladder, and out-of-hours contact policy are owned by
   `08/03-01`; this file fixes severities and which rules page.
2. The exact burn-rate percentages used for an organization with more than one service on the same
   budget are reviewed with the on-call policy in `08/03-01`; the multi-window structure and the
   99.9%/99.5% targets are fixed here and in NFR-AVAIL-003.
3. Whether `ApiSaturation` remains a ticket or becomes a page is a capacity decision owned by
   `08/04-01`; the scaling triggers it reflects are `08/04-01` values.
4. The precise credential-abuse baseline used by `CredentialAbuseSpike` is tuned with the rate-limit
   policy in `03/04-03`; the requirement to limit public endpoints is fixed by NFR-SEC-006.
