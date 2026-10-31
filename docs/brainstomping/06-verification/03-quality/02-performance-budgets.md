# Performance Budgets

The pass/fail latency, rendering, and throughput budgets that operationalize the performance NFRs, and the CI check that enforces each one.

**Links:** [Performance NFRs](../../01-requirements/03-nonfunctional/01-performance.md) · [Quality metrics](./01-metrics.md) · [E2E tests](../01-test-strategy/04-e2e.md) · [Integration tests](../01-test-strategy/03-integration.md) · [Coverage](../01-test-strategy/05-coverage.md) · [CI pipeline](../../05-development/03-workflow/03-ci.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

The performance NFRs in `01/03-01` state the quality bar as targets and measurement methods. This file
turns each target into an **executable budget**: the exact threshold, the endpoint group or route it
applies to, the reference conditions it is measured under, and the CI check that fails when it is
missed. A budget that no check enforces is not a budget.

This file is subordinate to `01/03-01`. Where a number here disagrees with the NFR, the NFR wins and
this file is corrected. The reference topology, dataset, and load profiles that make a measurement
reproducible are defined here: the k6 profiles and thresholds in §Load-Test Profiles and §CI
Load-Test Thresholds below, the reference dataset in §Reference Dataset for Load Tests, the telemetry
backend (`08/02-01`), and the topology in `08/01-01`. `01/03-01` names this file as the owner of the
load profiles, datasets, and pass/fail thresholds, so those details live here rather than in a
separate document.

The guiding rule: **a budget is measured on the reference conditions, at the percentile the NFR names,
and gated at the point in the pipeline where the cost of a regression is still cheap to fix.**

---

## Reference Conditions

Every budget below is measured under the same reference conditions, so two runs are comparable and a
miss is a regression rather than a different machine:

- **Dataset.** At least **10,000 active products per store**, two stores (`resys-main` and
  `resys-eu`), with the catalog, tax, shipping, and promotion shapes of the demo seed (`05/01-04`)
  scaled up. The generator, its scale, and the reset between runs are specified in §Reference Dataset
  for Load Tests below.
- **Topology.** The reference topology in `08/01-01`; a single API instance unless the throughput
  budget is being tested, PostgreSQL 18 sized there, and the two Nuxt front-ends built in production
  mode.
- **Server-side timing.** API budgets are server-side durations from OpenTelemetry HTTP histograms
  (`08/02-01`), aggregated per route and store, and **exclude client network time**. Storefront
  metrics are field p75 values reported by the `web-vitals` library, not a single lab run.
- **Window.** API percentiles are computed over a rolling 10-minute window at steady state, after a
  warm-up long enough to fill caches and connection pools; a cold start is measured separately and is
  not the budget.
- **Isolation.** External payment-gateway time is excluded from the cart and checkout budget and
  measured on its own span; the gateway timeout policy is owned by `03/04-05`.

---

## Load-Test Profiles (k6)

The load budgets are measured by four committed k6 profiles of increasing cost. Each is a script with a
`scenarios` block and a `thresholds` block; all four run against the reference dataset (§Reference
Dataset for Load Tests) on the reference topology (`08/01-01`), and all drive the real API through
HTTP. Checkout traffic uses the E2E `stub` `IPaymentGateway` (`06/01-04`) so the run never calls a live
gateway.

| Profile | Purpose | Load | Ramp | Steady | Total |
|---|---|---|---|---|---|
| **Smoke** | Wire-up and threshold sanity before a real run | 1 VU | none | 1 min | 1 min |
| **Steady-state** | The default gate: sustained mixed reads | 50 VUs | 2 min to 50 | 10 min | 12 min |
| **Peak** | Headroom under a traffic spike | 50 → 200 VUs | 3 min to 200 | 5 min at 200 | 8 min |
| **Soak** | Leak, connection-pool, and cache degradation over time | 50 VUs | 2 min to 50 | 2 h | ~2 h |

The **steady-state** profile is the one that gates a release candidate; **smoke** runs first and fails
fast; **peak** asserts that the error rate and public-read p95 hold under 4× the steady load while p99
is allowed to drift; **soak** fails on an upward trend in p95 or process memory rather than on a single
sample.

The request mix is shared by every profile, weighted per iteration:

- **Public reads** — browse, category listing, product detail: ~60%.
- **Faceted search** — `FR-CAT-006` with facets: ~20%.
- **Cart and checkout** — cart mutations through the delivery → payment → confirm steps to the stub
  gateway: ~15%.
- **Admin** — paged list and detail reads (`/api/admin`): ~5%.

Each request is tagged with its endpoint group (`public_read`, `search`, `checkout`, `admin`) so the
per-group thresholds in §CI Load-Test Thresholds can be evaluated from one run.

---

## Reference Dataset for Load Tests

The load dataset is a **scale-up of the demo seed shape**, produced by a committed load-data generator
in the performance lane; it is not the demo dataset itself, whose exact counts are owned by `05/01-04`.
The generator:

- Creates **at least 10,000 active products per store** for the two stores `resys-main` (`USD`,
  `en-US`) and `resys-eu` (`EUR`, `de-DE`), preserving the category tree, option types, per-currency
  `Money` prices, images, stock locations, shipping methods, tax rates, and at least one active
  promotion of the `demo` shape (`05/01-04`).
- Runs through the public and admin APIs (or the seeder's domain operations) rather than raw inserts,
  so catalog, inventory, and money invariants hold in the generated rows.
- Records the rows it creates so a run can be reset by the same generator without touching reference
  data, mirroring the owned-rows rule of `05/01-04`.

**Reset between runs.** A load run starts from a known state: the generator is applied to a freshly
migrated database, the smoke profile warms the caches and connection pools, and the steady-state run
begins after that warm-up. The database is not shared with another concurrent lane, so a run's
percentiles are not polluted by a neighbour. The dataset is re-applied before a release-candidate run
so that a mutation-heavy soak does not carry into the gating measurement.

---

## API Latency Budgets per Endpoint Group

| NFR | Endpoint group | p95 | p99 | Excludes / notes |
|---|---|---|---|---|
| NFR-PERF-001 | Public reads — browse, category listing, product detail (`/api/v1`) | **≤ 300 ms** | **≤ 800 ms** | Per route, per store; storefront-traffic endpoints. |
| NFR-PERF-002 | Public faceted search (`/api/v1`, FR-CAT-006) | **≤ 500 ms** | **≤ 1,200 ms** | Includes facet aggregation over the full store-scoped result set. |
| NFR-PERF-003 | Cart mutations and checkout steps (FR-CHK-001 … FR-CHK-008) | **≤ 500 ms** | **≤ 1,000 ms** | Excludes the external gateway round-trip, which is bounded by `03/04-05`. |
| NFR-PERF-004 | Admin lists and detail (`/api/admin`) | **≤ 800 ms** | **≤ 2,000 ms** | Report and export routes are excluded and must run asynchronously. |

A **miss** is any route whose p95 or p99 over the window exceeds its cell. The public-read p95 is the
one the throughput budget also depends on: a run that meets NFR-PERF-010's RPS but breaches the
public-read p95 fails both.

The database guardrail that underpins these budgets is a **5 s request-path statement timeout**
(NFR-PERF-009); a query that hits the timeout is a budget failure even if the route would otherwise be
below its percentile. Long queries are surfaced by `pg_stat_statements` and the slow-query log
(`08/02-01`), and index coverage is reviewed with each migration (`03/03-04`).

---

## Storefront Core Web Vitals Budgets

| Metric | Field p75 target | Routes | Source |
|---|---|---|---|
| LCP | **≤ 2.5 s** | Home, listing, product detail, cart | `web-vitals` → telemetry backend; Lighthouse CI on the same routes |
| INP | **≤ 200 ms** | Same four routes | `web-vitals`; Lighthouse CI hydration metrics |
| CLS | **≤ 0.1** | Same four routes | `web-vitals`; Lighthouse CI |
| TTFB | **≤ 800 ms** (lab) | Same four routes | Lighthouse CI |

These are NFR-PERF-005. The field p75 is the release gate; Lighthouse CI is the per-pull-request
proxy that catches a regression before it reaches field data. Both run against the built storefront
served in production mode, not the dev server. The route set is exactly the four routes in
`06/01-04`, so the E2E and Lighthouse stages do not diverge.

---

## SSR Render Budget

NFR-PERF-006 constrains the Nuxt 4 server-render path itself, because a fast API behind a slow render
still misses LCP:

- **Server render p95 ≤ 300 ms per route.**
- **At most one batched data fetch on the critical path per route.** Chaining blocking server-side API
  calls that multiply latency is a failure, regardless of the individual route percentiles.
- **Hydration completes without long tasks that break the INP budget**, so the render pays for itself
  once rather than reappearing as main-thread work.

Measurement is Nuxt/Nitro server-timing hooks exported as OTel spans, correlated with the API spans,
plus the Lighthouse CI hydration metrics. A render over budget is attributable to a route and to the
fetch chain that caused it.

---

## Throughput and Cacheability Budgets

| NFR | Budget | Check |
|---|---|---|
| NFR-PERF-007 | Public list default **20**, max **100**; search default **20**, max **60**; facet values capped; every query store-scoped, deterministically ordered, and paged; no per-row query in a loop. | Integration query-count guard per endpoint (`06/01-03`); validation rejects over-cap page sizes. |
| NFR-PERF-008 | Public catalog GETs carry correct `ETag`/`Cache-Control`; authenticated responses are not shared-cached; a mutation invalidates affected responses within **60 s**. | Response-header assertions and an invalidation re-read test in the integration suite (`06/01-03`); edge hit-ratio metric (`08/02-02`). |
| NFR-PERF-010 | **≥ 200 req/s** of mixed public reads per instance, error rate **< 0.1%**, p95 within NFR-PERF-001. | k6 steady-state and peak profiles in §Load-Test Profiles, on the reference topology (`08/01-01`). |

Throughput scales approximately linearly with instances because the API is stateless (spec §10); the
k6 run measures per-instance RPS and the error rate in `08/02-02`. A run that trades correctness for
throughput — relaxing store scoping or the `Money` invariant — is void.

---

## The Check That Enforces Them in CI

The budgets are enforced by three complementary checks, each at the layer where it is cheapest:

1. **Lighthouse CI — blocking, pull request, CI stage 5.** Runs on the storefront home, listing,
   product, and cart routes against the built app and asserts the NFR-PERF-005 budgets (LCP, INP, CLS,
   TTFB) from a committed budget file. A miss fails stage 5, and the Lighthouse report is uploaded as
   an artifact (`05/03-03`). This is the per-change regression gate for the render path.
2. **k6 load profiles — scheduled and release-blocking, not per pull request.** The four profiles in
   §Load-Test Profiles run on the reference topology (`08/01-01`) against the reference dataset. The
   lane runs the **smoke and steady-state** profiles on a **nightly schedule** and on every **release
   candidate**; the **peak** and **soak** profiles run on the release candidate only. A threshold miss
   blocks the release candidate, not an ordinary pull request, because the run needs the reference
   dataset and topology. The stage order, required-check semantics, and CI provider workflow layout are
   the contract of `05/03-03`; the thresholds the lane evaluates are the ones in §CI Load-Test
   Thresholds below, and the topology sizing is `08/04-01`.
3. **Integration query guards — blocking, pull request, CI stage 3.** The integration suite asserts a
   maximum query count per endpoint and the cacheability headers, so an N+1 or a missing `ETag` fails
   stage 3 before it can reach a load run (`06/01-03`).

In production, the same OTel histograms (`08/02-01`) feed the dashboards (`08/02-02`) and the alerts
(`08/02-03`), so a budget breach that slips past CI still pages an owner with the offending route and
store.

---

## CI Load-Test Thresholds

The k6 scripts declare these thresholds in their `thresholds` block, so a breach returns a non-zero k6
exit and the lane is red. Percentiles are evaluated per endpoint-group tag over the steady-state
window; the numbers are the same NFR numbers restated in §API Latency Budgets per Endpoint Group, so
there is one source of truth.

| k6 metric / threshold | Value | Applies to | NFR |
|---|---|---|---|
| `http_req_duration{group:public_read}` `p(95)` | **< 300 ms** | Public browse, listing, product detail | NFR-PERF-001 |
| `http_req_duration{group:public_read}` `p(99)` | **< 800 ms** | Same | NFR-PERF-001 |
| `http_req_duration{group:search}` `p(95)` | **< 500 ms** | Faceted search | NFR-PERF-002 |
| `http_req_duration{group:search}` `p(99)` | **< 1,200 ms** | Faceted search | NFR-PERF-002 |
| `http_req_duration{group:checkout}` `p(95)` | **< 500 ms** | Cart mutations and checkout (stub gateway) | NFR-PERF-003 |
| `http_req_duration{group:checkout}` `p(99)` | **< 1,000 ms** | Same | NFR-PERF-003 |
| `http_req_duration{group:admin}` `p(95)` | **< 800 ms** | Admin list and detail | NFR-PERF-004 |
| `http_req_duration{group:admin}` `p(99)` | **< 2,000 ms** | Admin list and detail | NFR-PERF-004 |
| `http_req_failed` `rate` | **< 0.001** (< 0.1%) | All profiles | NFR-PERF-010 |
| `checks` `rate` | **> 0.99** | All profiles; asserts response shape and status | NFR-PERF-007 |
| `http_reqs` `rate` (steady-state, per instance) | **> 200** | Mixed public reads | NFR-PERF-010 |
| `http_req_duration{group:public_read}` `p(95)` (peak) | **< 300 ms** | Peak profile at 200 VUs | NFR-PERF-001, NFR-PERF-010 |

The database guardrail is asserted after the run, not by a k6 threshold: `pg_stat_statements` and the
slow-query log must show no request-path statement over the **5 s** timeout (NFR-PERF-009), and a
non-zero count fails the lane. Soak additionally fails on an upward p95 or memory trend across the two
hours. A threshold breach is handled per §Budget Failure Handling; the threshold is never relaxed to
green a release.

---

## Budget Failure Handling

- **Lighthouse miss on a pull request** — fix the render regression; do not raise the budget. The
  route and the failing metric are in the uploaded report.
- **k6 miss on the release lane** — the release is held and a performance defect is opened with the
  route, the window, and the dataset. A budget is raised only by a decision recorded here and in
  review, with the corresponding NFR change in `01/03-01`.
- **Integration query-count miss** — the change introduces an N+1 or an unbounded query; fix the
  query, not the assertion.
- **Production breach** — the alert in `08/02-03` names the route; the runbook path is `08/03-02` for
  database causes and the performance-tuning notes in `10/03`.

No budget is raised to make a failing release pass. The NFR targets in `01/03-01` are the source of
truth, and a change to a target is a requirements change, not a pipeline tweak.

---

## Reporting and Alignment

- **`01/03-01` is the source of truth.** This file restates each target in an executable form; a
  conflict is resolved in favor of the NFR.
- **The metrics report (`06/03-01`) tracks coverage, flakiness, build duration, and defect escape
  rate**; performance budgets are separate evidence and are reported alongside them on the dashboard
  (`08/02-02`).
- **The E2E stage owns the Lighthouse position and the route list** (`06/01-04`); this file owns the
  thresholds. The two are kept aligned.
- **The reference conditions** — load profiles, thresholds, and the dataset generator — are defined in
  this file (§Load-Test Profiles, §Reference Dataset for Load Tests, §CI Load-Test Thresholds); the
  reference topology is `08/01-01`, its sizing is `08/04-01`, and the demo seed shape the generator
  scales is owned by `05/01-04`.

---

## Open Questions

1. Whether the nightly k6 lane is a workflow in the main pipeline or a separate scheduled workflow is a
   pipeline-scaffold decision owned by `05/03-03`; the profiles, thresholds, and the smoke/steady-state
   nightly plus peak/soak release-candidate split are fixed here.
2. The reference topology sizing — instance class, Postgres sizing, instance count — that the
   throughput budget assumes is defined with the capacity plan in `08/04-01`.
3. Whether INP is asserted from Lighthouse lab data, field data, or both as the release gate is a
   measurement decision; the p75 target above and the route set are fixed.
4. Whether public catalog caching is terminated at a CDN, a reverse proxy, or the API is an
   architecture decision owned by `03/01-02`; NFR-PERF-008's invalidation window is cache-layer
   neutral.
