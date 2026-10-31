# Capacity Planning

The reference topology sizing, the request and user assumptions behind it, and the triggers for scaling the API, the database, and the SSR cache/CDN.

**Links:** [Topology](../01-infrastructure/01-topology.md) · [Environments](../01-infrastructure/02-environments.md) · [Performance NFRs](../../01-requirements/03-nonfunctional/01-performance.md) · [Performance budgets](../../06-verification/03-quality/02-performance-budgets.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Deployment](../../07-release/03-deployment.md) · [Email & files](../../03-architecture/04-integration/06-email-files.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the capacity owner for resys-eshop: it defines the **reference topology sizing** that
the performance budgets in `06/03-02` are measured against, the request and user assumptions the
platform is planned for, and the rules for when each tier scales. It is named as that owner by
`01/03-01` and `06/03-02`, and it fills the sizing slot the deployment rules in `07/03` leave open.

The planning model is deliberately simple and evidence-driven: capacity is a function of
**sustained request rate per API instance**, and every scale decision is gated on measured
telemetry (`08/02-01`) rather than on a static guess. The one invariant: scale-out must never
trade away store scoping, the `Money` type, or database correctness (`01/03-01`).

---

## Reference Topology and Sizing

These are the sizing values the k6 profiles in `06/03-02` assume. They are provider-neutral units
that a cloud instance class is mapped to at deploy time.

| Tier | Reference size | Notes |
|---|---|---|
| API instance (`resys-api`) | **2 vCPU / 4 GiB** | Stateless Kestrel host; baseline capacity ≥ 200 mixed public reads/s (NFR-PERF-010). |
| Storefront / Admin instance | **1 vCPU / 1.5 GiB** | Nitro Node server, one process per pod; SSR render budget NFR-PERF-006. |
| PostgreSQL primary | **4 vCPU / 16 GiB, 100 GiB SSD** | `max_connections` 200; request-path statement timeout 5 s. |
| PostgreSQL read replica | **4 vCPU / 16 GiB, 100 GiB SSD** | Streaming replication, automatic failover. |
| PgBouncer pooler | **2 vCPU / 2 GiB** | Transaction pooling; `default_pool_size` 25. |
| Object storage / CDN | Managed | Public derivatives immutable and aggressively cached. |
| API replica floor | **2** | Failure-domain spread; always at or above the floor. |
| PostgreSQL replica floor | **1** | Read offload and failover. |

The reference load profiles run against this topology with a single API instance unless the
throughput budget is under test (`06/03-02`). A change to this table is a change to the reference
conditions and must be reflected back in `06/03-02`.

---

## Request and User Assumptions

Planning is expressed in request rate, not seat count, because the API is the shared resource.
The assumptions below are the planning envelope; real traffic is measured and can revise them.

- **Per-instance baseline.** One reference API instance sustains **≥ 200 req/s** of mixed public
  reads within the NFR-PERF-001 latency budget (NFR-PERF-010). For planning headroom, treat the
  **safe sustained rate as 120 req/s per instance** — 60% utilization — so a node loss or a spike
  does not immediately breach a latency budget.
- **Read-heavy mix.** The request mix mirrors `06/03-02`: public reads ~60%, faceted search ~20%,
  cart/checkout ~15%, admin ~5%. Search is the heaviest per request, so a search-heavy workload
  lowers effective per-instance capacity.
- **Session model.** A browsing session issues on the order of tens of requests; checkout adds a
  handful of write steps. Write traffic is a small fraction of total requests and does not drive
  API replica count — it drives database write capacity.
- **Peak-to-average.** Traffic is planned for a **5×–10× peak-to-average ratio** around seasonal
  and promotional events, with the promotional calendar in `02/01` as the forward signal.
- **Growth.** Catalog size is planned at ≥ 10,000 active products per store (`06/03-02`), with
  additional stores adding linear read load that is isolated per store scope.
- **Concurrency.** By Little's law, in-flight requests ≈ request rate × latency; at 200 req/s and
  300 ms this is ~60 concurrent requests per instance, which the reference instance absorbs.

The replica count for a given peak is:

```text
replicas = max(2, ceil(peak_RPS / (per_instance_RPS × target_utilization)))
```

with `per_instance_RPS = 200` and `target_utilization = 0.60`, so each replica plans **120
req/s**. A 300 req/s peak therefore plans 3 API replicas; a 1,000 req/s promotional peak plans 9.

---

## When to Scale the API Horizontally

The API scales horizontally because it is stateless and every replica shares the same database
(`03/01-02`, `08/01-01`). Scaling is driven by a horizontal pod autoscaler over these signals:

- **Scale out** when, over a **10-minute** window, any of: average CPU > **65%** of request, or
  per-replica request rate > **150 req/s**, or public-read p95 > **70%** of the NFR-PERF-001
  budget (≈ 210 ms), or sustained request-queueing/thread-pool pressure.
- **Scale in** when, over a **15-minute** window, average CPU < **40%** and per-replica request
  rate < **80 req/s**, staying at or above the **2-replica floor**.
- **Stabilization.** Use separate scale-up and scale-down stabilization windows so a brief spike
  does not flap replicas; scale-down waits longer than scale-up.
- **Custom metric.** Prefer an RPS-per-replica or concurrent-request metric over CPU alone, since
  a search-heavy mix can saturate latency before CPU is high.
- **Front-end scaling.** Scale storefront replicas on request rate and Node event-loop lag, and
  admin replicas on operator traffic; both are stateless at the HTTP layer.
- **Capacity ceiling.** Horizontal scale is bounded only by database connection budget (below)
  and by cost; it is not bounded by application state.

Scaling the outbox relay deserves care: the relay is safe across replicas because only one leases
a given row (NFR-AVAIL-007), but relay throughput is bounded by the database, so adding API
replicas adds request capacity, not necessarily relay capacity. Relay tuning is owned by
`03/02-03`.

---

## Database Connection Pooling

PostgreSQL is the shared bottleneck, so API replica count is coupled to the connection budget.
The rule: **the sum of all application connection pools must stay well under the server's
`max_connections`.**

- **Server budget.** Reference `max_connections` is **200**. Reserve ~**30** connections for
  replication, the migration job, runbooks, and pooler administration, leaving ~**170** for
  application traffic.
- **Per-replica pool.** Cap each API replica's Npgsql pool at **≤ 20** connections. At the
  2-replica floor this is 40; at 8 replicas it is 160 — the point at which the pooler, not the
  pool cap, must absorb further growth.
- **PgBouncer.** In **transaction** pooling mode, many client connections multiplex onto a bounded
  `default_pool_size` (reference **25**) of server connections per database/user. This decouples
  replica count from backend connections and is what allows the API to scale beyond the raw
  `max_connections` budget.
- **Migrations outside the pool.** The per-module migration bundle connects directly, not through
  the transaction pool, and runs as a single migrator before rollout (`07/03`, `03/03-04`).
- **No long transaction across an external call.** A transaction must never be held open across a
  gateway, email, or storage call, or the pool is exhausted under load (NFR-PERF-009).
- **Monitoring.** Pool saturation, checkout failures, and connection wait time are dashboard and
  alert signals in `08/02-02`/`08/02-03`; recurrent saturation is a signal to add a pooler or a
  read replica, not to raise the pool without bound.

---

## Read Replicas

The read replica tier offloads read-only work from the primary, which then carries writes,
checkout, and the outbox.

- **What routes to a replica.** Read-only, non-transactional catalog, listing, and search queries
  where a bounded replication lag is acceptable.
- **What stays on the primary.** Orders, checkout, inventory decrements, payments, outbox writes,
  authentication and authorization reads, and any read-after-write flow. Serving a stale
  Inventory or Order read from a replica is a correctness bug, not a performance trade.
- **Lag policy.** A replica is eligible to serve only while its replication lag is below the
  configured threshold; a lagging or unhealthy replica is removed from rotation until it catches
  up. The concrete threshold is a value finalized here with `08/02-01` metrics and owned by the
  routing rules in `08/01-01`.
- **When to add a replica.** Add a read replica when replica read utilization or primary read
  latency signals saturation under load; adding one is a configuration and routing change with no
  application code change.
- **Failover.** A lost primary fails over to a replica with automatic promotion; the API's
  readiness probe and short connection retry absorb the transition (NFR-AVAIL-006).

---

## SSR Cache and CDN Strategy

The front-end caches layer in front of the API so read traffic does not all reach the origin
(NFR-PERF-008).

- **Static assets.** Storefront and admin build assets, and public product-image derivatives
  (`thumb`, `card`, `full`), are immutable per key and served by the CDN with a long TTL.
- **Public catalog GETs.** Public storefront pages and their API responses carry correct
  `ETag`/`Cache-Control`; anonymous responses are shared-cacheable ~60 s, and a catalog mutation
  invalidates the affected responses within that window (NFR-PERF-008).
- **Nuxt route rules.** Public storefront routes use Nuxt route rules / stale-while-revalidate
  with a **per-store cache key** so one store's content never serves another's. Authenticated,
  cart, checkout, and account routes bypass shared caching entirely.
- **CDN invalidation.** Because catalog keys and pages embed content that can change, the cache
  strategy pairs a short page TTL with event-driven or time-based invalidation; image keys rotate
  on change, so images are cached indefinitely without invalidation.
- **Where caching terminates.** Whether public catalog caching is terminated at the CDN, a reverse
  proxy, or API output caching is an architecture decision owned by `03/01-02`; this file owns the
  capacity intent and the per-store/invalidation rules regardless of the layer that implements
  them.
- **Effect on scaling.** A healthy CDN hit ratio means SSR replica and API replica counts are
  driven by uncached traffic. A falling hit ratio is a capacity signal and an alert (`08/02-02`).

---

## Capacity Review

- **Load-test driven.** Capacity figures are validated by the smoke/steady-state/peak/soak
  profiles in `06/03-02`; a miss blocks a release candidate and opens a performance defect.
- **Before a promotional event.** Apply the peak-to-average assumption to the event forecast,
  pre-scale API and front-end replicas, and confirm the connection budget still fits.
- **After a material catalog or store change.** A large catalog import or a new store changes the
  read profile; re-check replica counts and search latency.
- **Quarterly.** Review the reference sizing, the replica floors, and the connection budget
  against measured telemetry, and update this file and `06/03-02` together when a value changes.

---

## Open Questions

1. The concrete cloud instance classes that map to the reference sizes, and whether the pooler is
   a sidecar or a shared service, are deployment values recorded with `08/01-02`.
2. The replication-lag threshold that gates replica read eligibility is finalized with the
   metrics in `08/02-01` and the routing rules in `08/01-01`; the lag-gating rule is fixed here.
3. Whether public catalog caching is terminated at the CDN, a reverse proxy, or API output caching
   is owned by `03/01-02`; the per-store key and 60 s invalidation window are fixed by NFR-PERF-008.
4. The exact HPA custom-metric wiring and scale stabilization windows are a deployment-scaffold
   detail refined when the cluster manifests are written; the trigger thresholds above are the
   planning contract.
