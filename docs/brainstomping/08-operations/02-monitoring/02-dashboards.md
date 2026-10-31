# Dashboards

The operational dashboards for resys-eshop: API latency, error rate, and throughput, database health, the checkout funnel, payment success, and outbox/relay lag, plus the SLI panels that drive alerting.

**Links:** [Telemetry](./01-telemetry.md) · [Alerts](./03-alerts.md) · [Infrastructure topology](../01-infrastructure/01-topology.md) · [Capacity planning](../04-scaling/01-capacity.md) · [Environment configuration](../../05-development/01-setup/02-environment.md) · [Performance NFRs](../../01-requirements/03-nonfunctional/01-performance.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Performance budgets](../../06-verification/03-quality/02-performance-budgets.md) · [Quality metrics](../../06-verification/03-quality/01-metrics.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file defines the dashboards that operators, release owners, and incident responders watch. Each
dashboard answers one operational question, and each panel is fed by a signal whose schema is fixed
in `08/02-01`. The alert rules in `08/02-03` are built from the same series, so a panel and its alert
never disagree.

The dashboards cover exactly the concerns the operations contract needs to see: **API latency, error
rate, and throughput; database health; the checkout funnel; payment success; and outbox/relay lag.**
A front-end experience dashboard and a capacity/saturation dashboard complete the set that
`08/04-01` and `06/03-02` reference. All panels are read against the targets in `01/03-01` and
`01/03-03`; where a panel shows a budget line, the line is the NFR value, not a local guess.

The one rule for every dashboard: **show user-facing impact first, then the dependency or resource
that explains it.** A panel that cannot be acted on by a runbook in `08/03-*` is not on a dashboard.

---

## Conventions

- **Data source.** The telemetry backend behind the collector in `08/02-01`. No dashboard queries a
  production database directly; database read load for dashboards is zero.
- **Environment variable.** Every dashboard is templated by `deployment.environment` and defaults to
  `Production`. A staging signal never appears on a production board (`08/01-02`).
- **Store variable.** Commerce panels are templated by `store` (from store resolution, `03/01-02`)
  with an "All stores" default; totals across stores are sums of per-store series, never an
  un-scoped aggregate.
- **Route and gateway variables.** Latency panels expose a `http.route` filter; payment panels
  expose a `gateway` filter.
- **Windows.** Rates and error budgets use the multi-window windows in `08/02-03`. Latency panels
  default to a rolling 1 h with a 10 min view on demand; capacity panels default to 6 h or 24 h.
- **Budget lines.** Each latency panel draws the NFR p95/p99 as annotations so a glance shows
  whether the route is inside budget.
- **Drill-down.** Every panel's legend links to a trace search pre-filtered by the same labels, so
  an operator moves from a bad number to a sample trace in one click.

---

## Dashboard Catalogue

| # | Dashboard | Audience | Question it answers | Owner |
|---|---|---|---|---|
| 1 | Service Overview | on-call, release owner | Are the public and admin APIs fast, available, and serving throughput? | `08/03-01` |
| 2 | Database Health | on-call, DBA | Is PostgreSQL healthy, pooled, replicating, and backed up? | `08/03-02` |
| 3 | Checkout Funnel | on-call, product owner | Where does checkout drop off, and are confirmations succeeding? | `08/03-01` |
| 4 | Payment Success | on-call, payments owner | What share of payment attempts succeed, per gateway, and are circuits healthy? | `08/03-03` |
| 5 | Outbox and Relay Lag | on-call, integration owner | Are domain events draining, and are any dead-lettered? | `08/03-01` |
| 6 | Front-end Experience | on-call, front-end owner | Are Core Web Vitals and SSR render within budget? | `08/03-01` |
| 7 | Capacity and Saturation | capacity owner, release owner | Is each tier near a scaling trigger or the connection budget? | `08/04-01` |

The runbooks in the Owner column are `08/03-*`; the specific runbook per alert is in `08/02-03`.

---

## 1. Service Overview

**Purpose.** The first board an on-call opens: request health of `/api/v1` and `/api/admin`, split
by route and store, with the error-budget status that the availability NFRs define.

| Panel | Signal | Visualization | Budget / threshold |
|---|---|---|---|
| Request rate per instance | `resys.http.requests.total` split by `endpoint_group`; `http.server.active_requests` | Time series + stat | Scale trigger 150 req/s per replica (`08/04-01`). |
| Latency by endpoint group | `http.server.request.duration` p95/p99 by `endpoint_group` | Heatmap + percentile lines | Public read p95 ≤ 300 ms / p99 ≤ 800 ms; search ≤ 500 / 1,200 ms; checkout ≤ 500 / 1,000 ms; admin ≤ 800 / 2,000 ms (`06/03-02`). |
| Error rate | 5xx share of `resys.http.requests.total` by `endpoint_group`, route | Time series | Public error rate < 0.1% (NFR-PERF-010). |
| Top failing routes | `resys.http.requests.total{status_class="5xx"}` by route | Table | Route that breaches its error budget. |
| Availability vs target | Probe success and edge success | Gauge + burn line | ≥ 99.9% public, ≥ 99.5% admin (NFR-AVAIL-003). |
| Error-budget burn | Multi-window burn-rate series | Time series | Burn-rate lines from `08/02-03`. |
| Request in-flight | `http.server.active_requests` | Time series | Little's-law sanity (`08/04-01`). |

---

## 2. Database Health

**Purpose.** PostgreSQL is the only shared stateful dependency (`03/01-02`), so its board explains
most latency and availability events. A failing panel here points at `08/03-02`.

| Panel | Signal | Visualization | Threshold / note |
|---|---|---|---|
| Primary reachability | `/health/ready` state and `db.client.connection.timeouts` | State timeline | Readiness failing > 2 min is a page (`08/02-03`). |
| Query duration | `db.client.operation.duration` p95/p99 by operation and store | Heatmap + percentiles | Statement timeout 5 s; a timeout is a budget failure (NFR-PERF-009). |
| Slow queries | `pg_stat_statements` top statements over 5 s | Table | Joined to spans to name the route. |
| Pool usage | `db.client.connection.count` idle vs used; `db.client.connection.pending_requests` | Time series | Per-replica pool cap ≤ 20; pooler `default_pool_size` 25 (`08/04-01`). |
| Replication lag | Managed replica lag | Time series | Lag threshold gates replica reads (`08/04-01`). |
| WAL archive lag / last backup | Managed backup metrics | Stat + time series | RPO ≤ 15 min; RTO ≤ 1 h (NFR-AVAIL-004). |
| Disk and connections | Managed database disk % and connection count | Gauge | `max_connections` budget in `08/04-01`. |

---

## 3. Checkout Funnel

**Purpose.** Checkout is the revenue path; the funnel shows where shoppers stop and whether the
confirm transaction (the only step that places an order) is succeeding. Step names match
`03/02-03`.

| Panel | Signal | Visualization | Threshold / note |
|---|---|---|---|
| Funnel by step | `resys.checkout.step.duration` count and `resys.checkout.completed` | Funnel + time series | Delivery → payment → confirm (FR-CHK-001 … FR-CHK-008). |
| Step latency | `resys.checkout.step.duration` p95 by `step` | Time series | Checkout p95 ≤ 500 ms excluding gateway (NFR-PERF-003). |
| Confirm failures | Failed `resys.checkout.completed` / total confirms | Time series | > 2% over 10 min pages (`08/02-03`). |
| Reservation conflicts | `resys.outbox` and order logs for failed reservations | Table | A failed confirm holds the order in `cart` (FR-CHK-004). |
| Abandonment | Checkout starts minus confirms over the window | Funnel + drop line | Trend and step where the drop concentrates. |
| Gateway contribution | `resys.payment.gateway.duration` overlayed on the payment step | Time series | Separated from platform work (`03/04-05`). |
| Cart and order writes | `db.client.operation.duration` for order/line-item operations | Time series | Cross-check with step latency. |

---

## 4. Payment Success

**Purpose.** Whether money is being collected, per gateway, and whether the failure/retry isolation
in `03/04-05` is protecting checkout. The board supports the gateway runbook `08/03-03` and the
double-charge runbook `08/03-04`.

| Panel | Signal | Visualization | Threshold / note |
|---|---|---|---|
| Success rate by gateway | `resys.payment.attempts{outcome}` captured+authorized vs total | Time series + stat | < 97% over 15 min is urgent; < 90% over 5 min pages (`08/02-03`). |
| Attempts by outcome | `resys.payment.attempts` split `authorized`/`captured`/`declined`/`failed` | Stacked time series | Terminal vs retryable separation. |
| Gateway latency | `resys.payment.gateway.duration` p95 by gateway and operation | Time series | Bounded by timeout policy in `03/04-05`; not part of NFR-PERF-003. |
| Gateway errors and retries | `resys.payment.gateway.errors{retryable}` | Time series | A retryable spike precedes circuit opening. |
| Circuit state | `resys.payment.circuit.open` by gateway | State timeline | Open circuit is a page (`08/02-03`). |
| Duplicate suspicion | `resys.payment.duplicate_suspected` | Stat + table | > 0 routes to `08/03-04`. |
| Unknown-outcome reconciliations | Reconciliation job count and resolutions | Time series | Timeouts after a possible charge (`03/04-05`). |
| Refund and void activity | `resys.payment.attempts` refund/void outcomes | Time series | Confirms returns path is settling (`03/02-03`). |

---

## 5. Outbox and Relay Lag

**Purpose.** Durable event delivery (NFR-AVAIL-007) drives email, analytics, and downstream
consumers. This board proves events drain and isolates a stuck relay from a failing consumer. Relay
semantics are `03/02-03`.

| Panel | Signal | Visualization | Threshold / note |
|---|---|---|---|
| Backlog size | `resys.outbox.pending` by store | Time series | Growing backlog is investigated via `08/03-01`. |
| Oldest event age | `resys.outbox.oldest_age_seconds` by store | Time series + stat | > 5 min is urgent; > 30 min pages (`08/02-03`). |
| Publish throughput | `resys.outbox.published` by `event_type` | Time series | Must match order/payment transition rate. |
| Relay lag | `resys.outbox.relay_lag_seconds` p95 | Time series | Commit-to-publish delay. |
| Failures and retries | `resys.outbox.failed` by `event_type` | Time series | A failing consumer shows as repeated failures. |
| Dead-letter | `resys.outbox.dead_lettered` | Stat + table | Any increment is an alert (`08/02-03`); operator action via `08/03-01`. |
| Consumer delivery | Email/analytics consumer counters by `event_type` | Time series | Distinguishes relay-stuck from consumer-stuck (`03/04-06`). |
| Event type mix | `resys.outbox.published` by `event_type` | Stacked time series | `OrderPlaced`, `PaymentCompleted`, `OrderShipped`, `ReturnStatusChanged`. |

---

## 6. Front-end Experience

**Purpose.** The storefront is SSR and must meet Core Web Vitals (NFR-PERF-005) and a render budget
(NFR-PERF-006); this board catches a regression in the browser before it becomes a revenue metric.

| Panel | Signal | Visualization | Threshold / note |
|---|---|---|---|
| LCP / INP / CLS | `resys.webvitals.*` field p75 by route | Stat + percentile lines | LCP ≤ 2.5 s, INP ≤ 200 ms, CLS ≤ 0.1 (NFR-PERF-005). |
| TTFB | `resys.webvitals.ttfb` lab p75 | Time series | ≤ 800 ms. |
| SSR render | `resys.ssr.render.duration` p95 by `route` | Time series | ≤ 300 ms per route (NFR-PERF-006). |
| SSR requests and errors | Nitro request counters | Time series | Probe `/healthz` health for the Node pods (`08/01-01`). |
| One-fetch rule | API spans per render trace | Table | At most one batched fetch on the critical path (NFR-PERF-006). |
| Cache hit ratio | Edge/CDN hit ratio | Time series | Falling ratio is a capacity signal (`08/04-01`). |

---

## 7. Capacity and Saturation

**Purpose.** The signals that drive horizontal scale and the database connection budget. This board
operationalizes `08/04-01`; a sustained panel over a scale trigger is why a replica count changes.

| Panel | Signal | Visualization | Threshold / note |
|---|---|---|---|
| Per-replica RPS | `resys.http.requests.total` per pod | Time series | Scale out > 150 req/s, scale in < 80 req/s (`08/04-01`). |
| CPU vs utilization target | `process.cpu.utilization` per pod | Time series | Scale out > 65%, scale in < 40% (`08/04-01`). |
| Latency vs budget | Public read p95 | Time series | Scale out above ~70% of the 300 ms budget. |
| Connection budget | `db.client.connection.count` used vs `max_connections` | Gauge | Sum of pools stays under the server budget (`08/04-01`). |
| Replica count vs floor | Pod count per workload | Stat | API floor ≥ 2; database replica floor ≥ 1 (`08/01-01`). |
| Front-end saturation | Node event-loop lag and Nitro RPS | Time series | Storefront/admin scaling triggers (`08/01-01`). |
| Pod restarts | Restart count per workload | Table | Repeated restarts are an availability signal (`08/03-01`). |

---

## SLI/SLO and Error-Budget Panels

The availability targets are ≥ 99.9% for public `/api/v1` and the SSR storefront and ≥ 99.5% for
`/api/admin` (NFR-AVAIL-003). One SLI row sits at the top of the Service Overview board:

- **SLI.** Successful (non-5xx) public requests ÷ total public requests, measured at the edge and
  the API, over the NFR window.
- **SLO.** The monthly availability target, excluding announced maintenance.
- **Error budget.** Remaining budget and the multi-window burn rate that `08/02-03` alerts on; a
  Sev1 burn reduces the budget visibly on this panel.

The panel names the route and store that consume the most budget, so the first step of an
error-budget incident is a drill-down, not a search.

---

## Ownership and Maintenance

- **Named owner per dashboard.** The Owner column above ties each board to an on-call role defined
  in `08/03-01`; ownership is reviewed with the alert thresholds.
- **Thresholds are not panel-local.** Every budget line traces to `01/03-01`, `01/03-03`, or
  `06/03-02`; changing a line is a requirements change, not a dashboard edit.
- **Alerts mirror panels.** Each alert in `08/02-03` names the panel and metric it is built from, so
  a threshold change is applied in one place and verified in the other.
- **New signals.** A new panel requires a signal with the schema in `08/02-01`; a signal without a
  dashboard and an alert is either an experiment or catalog-debt, not an operational control.
- **Review cadence.** Dashboards and their annotations are reviewed with the quarterly capacity
  review in `08/04-01` and after any incident that revealed a missing signal.

## Open Questions

1. The concrete dashboarding product and its query language are `08/01-01` choices; this file fixes
   the dashboards, panels, sources, and budget lines they must express.
2. The default time range and refresh interval per dashboard are operator preferences; the windows
   used by alerts are fixed in `08/02-03` and must not drift from the panels.
3. Whether the edge/CDN hit-ratio and cache panels are sourced from the CDN or the API cache layer
   is owned by `08/04-01`; the capacity interpretation is the same either way.
4. The exact KPI definitions on the admin product dashboard (`04/02-02`) are a product concern; they
   are not the operational dashboards defined here.
