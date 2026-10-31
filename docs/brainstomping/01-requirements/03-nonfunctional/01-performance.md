# Performance — Non-Functional Requirements

The latency, rendering, and query budgets that the API and the two Nuxt front-ends must meet
under a representative multi-store catalog.

**Links:** [Security](./02-security.md) · [Availability](./03-availability.md) · [Constraints](../01-business/03-constraints.md) · [Requirements Index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

resys-eshop is a headless commerce platform: two independent Nuxt clients call a .NET Minimal
API that fronts a modular monolith and PostgreSQL 18 (spec §3, §4). Performance is therefore a
property of the **request path** — store resolution → authorization → module service → EF Core →
Postgres → serialization — plus the **storefront render path** in `web/storefront` (Nuxt SSR).

These requirements set the measurable quality bar. They are deliberately expressed as budgets
with a target and a measurement method, not as implementation prescriptions. The concrete load
profiles, datasets, and reference topology that operationalize them are owned by the performance
budgets doc (`06/03-02`), the telemetry doc (`08/02-01`), and the infrastructure topology
(`08/01-01`).

Two invariants constrain every target below: every catalog query is **store-scoped** (spec §6),
and money is never a raw decimal — prices and totals are `Money` values of
`(decimal, ISO-4217 currency)` (`01/01-03`). Meeting a budget by relaxing either invariant is not
acceptable.

---

## NFR-PERF-001 — Public read latency

- **Description:** Public `/api/v1` browse, category-listing, and product-detail responses are
  served within interactive budgets. This covers the endpoints that dominate storefront traffic
  and are exercised by FR-CAT-009.
- **Target:** Server-side duration **p95 ≤ 300 ms** and **p99 ≤ 800 ms** per route, measured on
  a representative dataset of at least 10,000 active products per store, excluding client
  network time.
- **Measurement:** OpenTelemetry HTTP server-duration histograms (`08/02-01`), aggregated per
  route and store; the k6 read profile defined in `06/03-02` is the source of truth for the
  dataset and concurrency.

---

## NFR-PERF-002 — Faceted search latency

- **Description:** Public catalog search with facets (FR-CAT-006) stays interactive even when
  facet counts are computed over the full store-scoped result set.
- **Target:** Server-side duration **p95 ≤ 500 ms** and **p99 ≤ 1,200 ms** per search request
  including facet aggregation, on the reference dataset.
- **Measurement:** OTel histograms for the search route plus Postgres `pg_stat_statements`
  timing for the underlying query; the search load profile in `06/03-02`.

---

## NFR-PERF-003 — Cart and checkout write latency

- **Description:** Cart mutations and the delivery → payment → confirm checkout steps
  (FR-CHK-001 … FR-CHK-008) remain responsive so the funnel does not time out.
- **Target:** Server-side duration **p95 ≤ 500 ms** and **p99 ≤ 1,000 ms** per request,
  **excluding** the external payment-gateway round-trip, which is bounded by the gateway's own
  latency and the timeout policy in `03/04-05`.
- **Measurement:** OTel histograms split into "platform work" and "gateway call" spans; the
  checkout load profile in `06/03-02`.

---

## NFR-PERF-004 — Admin API latency

- **Description:** Data-dense admin list and detail endpoints (`/api/admin`) are fast enough for
  back-office work without blocking the operator.
- **Target:** Server-side duration **p95 ≤ 800 ms** and **p99 ≤ 2,000 ms** for paged list and
  detail routes. Report/export routes are excluded from this budget and must run
  **asynchronously** rather than holding a request open.
- **Measurement:** OTel histograms per admin route; long-running report jobs emit their own job
  duration metric (`08/02-01`).

---

## NFR-PERF-005 — Storefront Core Web Vitals

- **Description:** The SSR storefront must meet the Core Web Vitals thresholds that search
  engines and users expect, on the mid-tier mobile profile the storefront targets.
- **Target:** Field **p75** — **LCP ≤ 2.5 s**, **INP ≤ 200 ms**, **CLS ≤ 0.1**; lab **TTFB
  ≤ 800 ms**. Budgets apply to home, listing, product detail, and cart routes.
- **Measurement:** Real-user monitoring via the `web-vitals` library reporting into the
  telemetry backend, plus Lighthouse CI on the same routes in the E2E pipeline (`06/01-04`).

---

## NFR-PERF-006 — SSR render budget

- **Description:** Server-side rendering in `web/storefront` (Nuxt 4 SSR, spec §4, §9) must not
  become the bottleneck. The render path must not chain blocking server-side API calls that
  multiply latency.
- **Target:** Server render **p95 ≤ 300 ms** per route; at most **one** batched data fetch per
  route on the critical path; hydration completes without long tasks that break the INP budget.
- **Measurement:** Nuxt server timing / Nitro hooks exported as OTel spans; Lighthouse CI
  hydration metrics; route-level traces correlated with the API spans.

---

## NFR-PERF-007 — Catalog query limits

- **Description:** Every list and search query is bounded, store-scoped, deterministically
  ordered, and paginated (FR-CAT-006, FR-CAT-009). Unbounded scans and N+1 access patterns are
  prohibited on the request path.
- **Target:** Public list page size defaults to **20** with a hard maximum of **100**; search
  page size defaults to **20** with a hard maximum of **60**; facet values are capped; every
  query filters by store and visibility, orders deterministically, and pages with a stable key.
  Related rows are loaded by projection or explicit eager loading — no per-row query in a loop.
- **Measurement:** Integration tests assert a maximum query count per endpoint (`06/01-03`);
  `pg_stat_statements` and the slow-query log surface scans; requests over the page-size cap are
  rejected by validation.

---

## NFR-PERF-008 — Cacheability of public catalog reads

- **Description:** Public catalog GET responses are cacheable so a CDN or reverse proxy can
  absorb read traffic without reaching the origin (FR-CAT-009).
- **Target:** Public catalog GETs carry correct `ETag`/`Cache-Control`; anonymous responses are
  cacheable, authenticated or personalized responses are **not** stored in shared caches; a
  catalog mutation invalidates affected cached responses within **60 s**.
- **Measurement:** Response-header assertions in integration tests; cache hit-ratio metric at the
  edge (`08/02-02`); an invalidation test that mutates a product and re-reads within the window.

---

## NFR-PERF-009 — Database performance guardrails

- **Description:** PostgreSQL 18 is the only shared stateful dependency on the request path
  (spec §4), so query hygiene is a first-class performance control.
- **Target:** Request-path statement timeout of **5 s**; all foreign keys and high-selectivity
  filter columns indexed; connection pooling enabled; no long-running transaction held open
  across an external call (gateway, email, storage).
- **Measurement:** `pg_stat_statements` and slow-query logging; index-coverage review in
  migrations (`03/03-04`); OTel database spans flag queries over the timeout budget.

---

## NFR-PERF-010 — Sustained read throughput

- **Description:** A single API instance on the reference topology handles a baseline sustained
  load with headroom, and additional instances scale horizontally because the service is
  stateless (spec §10, `01/01-03`).
- **Target:** **≥ 200 req/s** of mixed public reads per instance with **error rate < 0.1 %** and
  p95 within the NFR-PERF-001 budget; throughput scales approximately linearly with instances.
- **Measurement:** k6 sustained-load run from `06/03-02` on the reference topology (`08/01-01`);
  per-instance RPS and error-rate metrics in `08/02-02`.

---

## Open Questions

1. The concrete load profiles, reference dataset size, and pass/fail thresholds that
   operationalize these budgets are finalized in `06/03-02`.
2. Whether public catalog caching is terminated at a CDN, a reverse proxy, or output caching in
   the API is an architecture decision owned by `03/01-02`; the requirement above is
   cache-layer-neutral.
3. The search backend (database full-text vs. an external search service) remains open per
   `01/02-01`; the latency budgets apply regardless of the backend chosen in `03/02-01`.
4. The reference topology (instance size, Postgres sizing) that the throughput target assumes is
   defined with the capacity plan in `08/04-01`.
