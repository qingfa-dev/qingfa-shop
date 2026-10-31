# Performance Tuning

The known hot paths in resys-eshop, the indexing, caching, and query guardrails that hold them within budget, and where their performance is measured.

**Links:** [Technical debt](./01-technical-debt.md) · [Dependency policy](./02-dependency-policy.md) · [Performance budgets](../06-verification/03-quality/02-performance-budgets.md) · [Risks](../02-planning/03-risks.md) · [Maintenance index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

This file is the maintenance entry point for performance: it names the paths where resys-eshop is
most likely to slow down, states the indexing and caching decisions that protect them, and points to
the budgets and telemetry that turn a slowdown into evidence. It does not restate the pass/fail
numbers — those are owned by `06/03-02` — and it does not restate the NFR targets, owned by `01/03-01`.

The guiding rule: **tune the measured path, at the narrowest layer, and prove the change against the
budget.** A tuning change that is not attributable to a route, a query, or a render chain is a guess.

When a budget and this file disagree, `06/03-02` and `01/03-01` win; this file is corrected.

---

## Known Hot Paths

Three paths carry most of the platform's latency and coupling risk. Each already has a budget in
`06/03-02`, tagged to a load-profile group.

### Catalog search

Faceted search (`FR-CAT-006`, NFR-PERF-002) is the heaviest public read: it filters a full
store-scoped result set and aggregates facet counts before paging. The risks are a facet aggregation
that scans the whole catalog, deep offset paging, and per-row queries for facet values.

- Keep every search query store-scoped and index-backed, and cap the facet values returned.
- Paginate with the documented search sizes (default 24, maximum 60, NFR-PERF-007); a deep page must
  not degrade linearly with offset.
- Return facets from one aggregated query rather than one query per facet.
- Product detail and listing (`NFR-PERF-001`) share the catalog indexes below and must not regress
  when search changes.

### Checkout totals

Order totals are computed from source rows plus adjustments plus tax every time — never stored in a
single cached column (spec §6). That correctness decision makes the totals path recompute line items,
adjustments, and tax on each cart and checkout step (NFR-PERF-003). The risks are an N+1 over line
items and adjustments, a tax resolution repeated per line, and promotion evaluation re-run on every
read.

- Load a cart's line items, adjustments, and tax components in batched queries, not per line.
- Recompute totals on mutation only; serve reads from the same computed projection.
- Keep the external gateway round-trip off the totals span — it is measured separately (`03/04-05`).
- The integration suite asserts a maximum query count per checkout endpoint (`06/01-03`).

### Promotion application

Promotion evaluation (`FR-PRM-004`, `03/02-04`) runs rules across channels, windows, coupon, user,
product, order-total, and shipping conditions, then applies actions with per-line clamping. The risks
are evaluating every promotion for every cart, re-evaluating on each totals read, and an interaction
between rules that grows worse as promotions accumulate.

- Pre-filter promotions by store, channel, active window, and usage limit before evaluating rules.
- Evaluate once per totals computation and cache results for the duration of the request.
- Keep discounts as adjustments; never recompute them in the read path independently of the totals
  computation.
- The promotion test plan (`06/02-02`) owns correctness; this file owns the cost of the engine.

---

## Indexing Strategy

Indexes are owned by each module's own history and reviewed with every migration (`03/03-04`). The
platform-wide rules:

- **Store first.** Every catalog, inventory, and order index leads with the store column, because
  every query is store-scoped (`03/03-02`). A non-store-scoped index on a store-scoped table is
  redundant and is rejected in review.
- **Match the predicate and the sort.** An index covers the equality predicate columns and the
  deterministic sort column, so a paged listing is a single index range scan rather than a sort.
- **Catalog.** Unique slug per store; status plus publish date for listing; the category join table;
  option values used by facets. Full-text or trigram indexes back the product name search, with the
  same store-first leading column.
- **Inventory.** Unique `StockItem` on (variant, location); `StockMovement` on (variant, occurred
  at) for the ledger (`03/02-06`).
- **Orders and adjustments.** Order number unique per store; (customer, created) for order history;
  (state, updated) for admin queues; adjustments keyed by their line item or order.
- **Outbox.** A partial index over rows whose delivered marker is null, keyed by created time, so the
  relay never scans delivered rows (`03/01-03`).
- **No unmeasured index.** A new index cites the query it serves in the migration review; an unused
  index is removed. Index coverage is reviewed with each migration, and query plans for a new hot
  query are checked before merge.

The database guardrail is the **5 s request-path statement timeout** (NFR-PERF-009): a query that
hits it is a failure even if the route would otherwise be under its percentile. Long queries are
surfaced by `pg_stat_statements` and the slow-query log (`08/02-01`), and triaged through `08/03-02`.

---

## Caching

Caching is applied where it cannot violate store isolation or money correctness:

- **Public catalog GETs** carry correct `ETag`/`Cache-Control`; authenticated responses are never
  shared-cached; a mutation invalidates the affected responses within **60 s** (NFR-PERF-008). The
  cache layer is terminated per `03/01-02`, and the CDN plus Nuxt route rules are sized in `08/04-01`.
- **Keys are per store.** A cached response for one store must never be served to another; the store
  is part of the cache key, and the isolation cases in `06/01-03` cover it.
- **Reference data** that changes rarely (tax rates, shipping methods, currency rates) may be cached
  in-process with a short TTL and event-based invalidation; it is never cached in a way that can mix
  stores or currencies.
- **SSR batching.** The storefront keeps at most one batched data fetch on the critical render path
  per route (NFR-PERF-006); chaining blocking server-side calls is a failure regardless of the
  individual API percentiles.
- **Money is never cached stale.** Any cache in front of pricing, promotions, or totals is
  invalidated by the same event that changes the source, not by TTL alone.

---

## Query Guardrails

These rules apply to every read path and are checked in code review and CI:

1. **No per-row query in a loop.** The integration suite asserts a query-count ceiling per endpoint
   (`06/01-03`); a new N+1 fails CI stage 3.
2. **Every query is store-scoped, deterministically ordered, and paged.** Over-cap page sizes are
   rejected by validation, not clamped silently (NFR-PERF-007).
3. **Reads are projected.** Read models project to DTOs and avoid tracking; write paths load the
   aggregate they need.
4. **Hot reads are compiled or cached at the query layer**, not re-planned per request.
5. **The statement timeout is a guardrail, not a target.** A query near 5 s is a defect even when it
   passes.

---

## Measurement Pointers

Performance work starts from a measurement, not an opinion. The sources:

| What | Where |
|---|---|
| Pass/fail budgets, load profiles, thresholds, and the reference dataset | `06/03-02` |
| NFR targets and measurement methods (source of truth) | `01/03-01` |
| OpenTelemetry traces, HTTP histograms, database metrics, slow-query log | `08/02-01` |
| Latency, throughput, pool-saturation, and front-end dashboards | `08/02-02` |
| Alert rules and routing for a budget breach | `08/02-03` |
| Database failure and slow-query runbook | `08/03-02` |
| Reference topology and capacity model | `08/01-01`, `08/04-01` |
| Integration query-count and cacheability guards | `06/01-03` |

A production breach is attributed through the trace (`08/02-01`) and the alert (`08/02-03`) to a
route and a store before any code changes.

---

## Tuning Workflow

1. **Detect.** An alert, a dashboard trend, or a k6 threshold names the endpoint group and route.
2. **Reproduce.** Run the relevant load profile (`06/03-02`) or replay the trace against the
   reference dataset, so the change is measurable.
3. **Attribute.** Find the cause: a missing index, an N+1, a render chain, or a cache miss. The trace
   and `pg_stat_statements` name the query or span; do not tune past the evidence.
4. **Change at the narrowest layer.** Prefer an index or a query fix over a cache; prefer a cache
   over a rewrite; never trade correctness (store scope, money) for throughput.
5. **Verify.** Re-run the budget that failed. A tuning change that misses its budget is reverted, not
   merged with a relaxed threshold.
6. **Record.** A fix that lands is noted against its debt item in `10/01`; a deferred fix is filed
   there as a new item with the measurement attached.

---

## Open Questions

1. Whether product search is served by PostgreSQL full-text search or an external search engine is an
   architecture decision owned by `03/01`; the store-first, paged, non-N+1 requirements above hold
   either way.
2. Whether facet counts are materialized or computed per query is a search-implementation decision;
   the NFR-PERF-002 budget is the same either way.
3. The concrete TTL for in-process reference-data caching is finalized with the caching layer in
   `03/01-02`; the event-based invalidation requirement and the 60 s public-catalog window are fixed.
4. Whether the CDN terminates caching at the edge or a reverse proxy is an infrastructure decision
   owned by `03/01-02` and `08/01-01`; NFR-PERF-008 is cache-layer neutral.
