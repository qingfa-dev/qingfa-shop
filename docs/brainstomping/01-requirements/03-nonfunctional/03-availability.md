# Availability — Non-Functional Requirements

Health probing, uptime, backup/recovery, and graceful-degradation behavior for the API and its
stateful dependencies.

**Links:** [Performance](./01-performance.md) · [Security](./02-security.md) · [Checkout](../02-functional/03-checkout.md) · [Requirements Index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Availability is defined for the whole request path: the API host, its PostgreSQL 18 database,
and the external payment gateways. The service is a **stateless** modular monolith, so API
instances can be replaced freely and scaling is horizontal (spec §10, `01/01-03`). Availability
requirements must therefore describe how the platform **probes**, **recovers**, and **degrades**
without depending on a single process holding state.

The spec fixes the health surface — `/health/live` and `/health/ready` (spec §10) — and the
observability stack that reports on it: OpenTelemetry and Serilog (`08/02-01`). Incident
response procedures live in `08/03-01`; the database runbook is `08/03-02`; the gateway runbook
is `08/03-03`; alerts are defined in `08/02-03`. Checkout behavior under gateway failure is
constrained by FR-CHK-007 and FR-CHK-010 (`01/02-03`) and the payment requirements (`01/02-05`).

---

## NFR-AVAIL-001 — Liveness endpoint

- **Description:** `/health/live` reports whether the API process is running and able to serve
  requests, so an orchestrator can restart a wedged instance.
- **Target:** `/health/live` returns `200` while the process is healthy and returns a failure
  status when the process is unrecoverably stuck; it performs **no** dependency checks and
  never blocks on the database, so a slow dependency cannot cause a restart loop.
- **Measurement:** Deployment probes call the endpoint on a fixed interval (`08/01-01`); an
  integration test asserts the endpoint is `200` with the database unreachable.

---

## NFR-AVAIL-002 — Readiness endpoint

- **Description:** `/health/ready` reports whether the instance can serve real traffic, including
  its dependencies, so a load balancer removes it before it fails requests.
- **Target:** `/health/ready` returns `200` only when the instance can reach PostgreSQL and has
  completed startup migrations/wiring; it returns a failure status while the instance is
  starting, migrating, or disconnected; it completes within its probe timeout even when a
  dependency is slow.
- **Measurement:** Integration tests cover starting, healthy, and database-down states; the
  probe timeout and thresholds are configured in `08/01-01`; readiness state is observable in
  `08/02-02`.

---

## NFR-AVAIL-003 — Service availability target

- **Description:** The public storefront and its API meet a monthly availability target measured
  at the edge, excluding announced maintenance windows.
- **Target:** **≥ 99.9 %** monthly availability for public `/api/v1` and the SSR storefront;
  **≥ 99.5 %** for `/api/admin`. Error-budget consumption is tracked and drives the alerting
  policy.
- **Measurement:** Synthetic probes and edge request success metrics (`08/02-02`); monthly
  availability is computed from the telemetry backend; alerts fire on budget burn rate
  (`08/02-03`).

---

## NFR-AVAIL-004 — Backup and recovery (RPO/RTO)

- **Description:** The PostgreSQL database is the only shared stateful store, so its backup and
  recovery behavior defines the platform's data-loss and downtime exposure.
- **Target:** **RPO ≤ 15 minutes** via continuous WAL archiving / point-in-time recovery;
  **RTO ≤ 1 hour** to restore service after a database loss; backups are encrypted, stored off
  the primary host, and their restores are rehearsed on a defined cadence. Object storage for
  product images (`03/04-06`) is backed by its own durability guarantees.
- **Measurement:** Backup job success and WAL-archive lag metrics (`08/02-02`); a documented and
  timed restore rehearsal from the runbook in `08/03-02`; recovery reports reviewed under
  `08/03-01`.

---

## NFR-AVAIL-005 — Graceful payment-gateway degradation

- **Description:** A payment gateway outage must not take down browsing or corrupt checkout
  state (FR-CHK-007, FR-CHK-010). The storefront remains useful and orders are not lost or double-charged.
- **Target:** Catalog browse, cart, and account features stay available when a gateway is
  unreachable; checkout fails **safely** — no order is marked paid without a confirmed capture,
  the cart is preserved, and the customer receives a clear retry path; gateway calls use bounded
  timeouts and circuit breaking so a hung gateway does not exhaust API threads; reconciliation
  and double-charge handling follow `08/03-04`.
- **Measurement:** Chaos tests simulate gateway timeout and error responses and assert
  browse/cart stay `200`, checkout returns a recoverable error, and no payment is recorded as
  captured; gateway error-rate and circuit-state metrics in `08/02-02`; runbook `08/03-03`.

---

## NFR-AVAIL-006 — Graceful shutdown and rolling deployment

- **Description:** Instances are replaced during deploys and scaling events without dropping
  in-flight requests or corrupting state.
- **Target:** A terminating instance stops accepting new connections, finishes in-flight
  requests within a bounded grace period, releases resources, and flips `/health/ready` to
  failing first; rolling deploys keep at least one ready instance serving; no request is served
  by a half-migrated schema.
- **Measurement:** Deployment verification in `07/03`; a drain test asserts zero failed requests
  during a rolling restart; migration ordering is enforced per `03/03-04`.

---

## NFR-AVAIL-007 — Durable event delivery

- **Description:** Domain events (for example `OrderPlaced`, `PaymentCompleted`, `OrderShipped`)
  drive email and downstream consumers and must survive process restarts (spec §6, §10).
- **Target:** Events are written to an **outbox table** in the same transaction as the state
  change and relayed at-least-once; a restart between commit and relay does not lose the event;
  consumers are idempotent so redelivery does not duplicate side effects.
- **Measurement:** Integration tests kill the relay after commit and assert eventual delivery;
  outbox backlog age is an alerting metric (`08/02-03`).

---

## NFR-AVAIL-008 — Dependency failure isolation

- **Description:** A slow or failing external dependency (payment gateway, email sender, object
  storage) must not cascade into a platform-wide outage.
- **Target:** Every outbound call has an explicit timeout, bounded retries with backoff, and a
  circuit breaker where appropriate; a failing dependency degrades only its own feature; request
  threads are never blocked indefinitely; failures surface as typed, retryable errors.
- **Measurement:** Integration and chaos tests stub dependency latency/failure and assert
  bounded response times and isolated feature impact; outbound-call latency/error metrics in
  `08/02-01`; alert thresholds in `08/02-03`.

---

## Open Questions

1. The exact backup cadence, retention, and restore-rehearsal schedule are finalized with the
   database runbook in `08/03-02`.
2. Whether multi-region or single-region recovery is required is a capacity/infrastructure
   decision owned by `08/01-01` and `08/04-01`; the RPO/RTO targets above hold either way.
3. The precise synthetic-probe set and maintenance-window definition used for the availability
   percentage are finalized with the monitoring docs `08/02-02` and `08/02-03`.
