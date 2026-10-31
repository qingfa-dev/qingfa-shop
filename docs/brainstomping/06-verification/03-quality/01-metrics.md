# Quality Metrics

The four trailing metrics — coverage, flaky-test rate, build duration, and defect escape rate — with the definition, target, collection source, and reporting location of each.

**Links:** [Coverage](../01-test-strategy/05-coverage.md) · [Test matrix](../01-test-strategy/01-matrix.md) · [E2E tests](../01-test-strategy/04-e2e.md) · [Integration tests](../01-test-strategy/03-integration.md) · [Performance budgets](./02-performance-budgets.md) · [CI pipeline](../../05-development/03-workflow/03-ci.md) · [Performance NFRs](../../01-requirements/03-nonfunctional/01-performance.md) · [Risks](../../02-planning/03-risks.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Quality is a trend, not a gate that is passed once. This file defines the small set of metrics that the
team watches between releases, so that the same number means the same thing in a CI comment, a
dashboard (`08/02-02`), and a release report. It does not restate the coverage model — the per-layer
targets, the tools, and the excluded code are owned by `06/01-05` — and it does not restate the
latency budgets, which are owned by `06/03-02`.

A metric belongs here only if it is **computed from an automated source**, has a **named target**, and
has an **owner**. A metric that requires manual bookkeeping is not tracked; it is either automated or
dropped.

The guiding rule: **measure the thing that erodes confidence, and set the target at the level that
still lets a person trust a green build.** Coverage alone does not do that — a suite can be covered and
flaky, or fast and blind — so the four metrics are read together.

---

## Metric Summary

| Metric | One-line definition | Target | Primary source | Reported in |
|---|---|---|---|---|
| Coverage | Executed code percentage per layer and assembly | Per-layer floors in `06/01-05` | Coverlet (Cobertura) and Vitest (`v8`) | Layer artifacts, merged PR comment, dashboard |
| Flaky-test rate | First-run failures that pass on retry without a code change | ≤ 2% of blocking-lane runs; zero unowned quarantines | Playwright report retries; xUnit `trx` | Dashboard, weekly triage |
| Build duration | Wall-clock time of the PR pipeline and its slowest stages | PR p50 ≤ 15 min, p95 ≤ 25 min; nightly ≤ 45 min | CI stage timestamps | CI checks, dashboard |
| Defect escape rate | Defects found after release as a share of a release's defects | ≤ 5%; zero P1/P2 escapes | Issue tracker labels; release notes | Release report, dashboard |

The four are trailing indicators over a window, never a snapshot of one commit. The window is **14
days** for the flaky-test rate and build duration, and **per release** for coverage and defect escape
rate, so a single bad run never paints the trend.

---

## 1. Coverage

**Definition.** The percentage of executable lines and branches exercised by the tests at each layer,
computed per assembly for .NET and per app for the front-end. The layer scopes are `06/01-01`; the
tools, collection commands, exclusions, and thresholds are `06/01-05`. This file adds only how the
number is trended, not how it is produced.

**Formula.**
- Per assembly: `covered lines ÷ instrumentable lines`, and `covered branches ÷ instrumentable
  branches`, after the `coverlet.runsettings` filters.
- Front-end: the `v8` provider's `lines` percentage per app over `web/shared` and the stores and
  composables with logic.
- The solution figure is **not** a simple average; it is the sum of covered lines over the sum of
  instrumentable lines across assemblies, so a large well-covered project cannot mask a small
  uncovered one.

**Target.** The per-layer and per-assembly floors are **owned by `06/01-05`** — that file is the single
source for the unit, integration, front-end, and E2E journey thresholds and for how each is gated. They
are deliberately not restated here, so a floor can never drift between the coverage contract and this
metrics file. The floors ratchet up, never down; a reduction requires a decision recorded in `06/01-05`
and in review, and this file reads the same numbers from there.

**Source.** Coverlet through `dotnet test` for the .NET layers; Vitest with the `v8` provider for the
front-end; the Playwright JSON report for journey coverage. No separate coverage stage is added; the
flags ride the existing test commands in `05/03-03`.

**Reported in.**
- Each layer job uploads `artifacts/coverage/<layer>/` as a build artifact.
- The merged per-assembly summary is posted as a pull-request comment on the run.
- The dashboard plots each assembly's line percentage against its floor so a downward trend is visible
  before a job goes red.

**Notes.** Coverage is a signal, not a goal (`06/01-05`). A high number never substitutes for a test
at the right layer, a meaningful assertion, or a passing E2E journey.

---

## 2. Flaky-Test Rate

**Definition.** The share of test **executions** that fail on the first attempt and pass on a retry
within the same run, where no code changed between the attempts. A test that fails consistently is a
real defect, not a flake, and is counted as a failure rather than a flake.

**Formula.**
- Per lane, over the trailing 14 days: `flaky executions ÷ total executions`.
- An execution is flaky when it is failed before its final retry and passed at the end, or when a
  rerun of the same commit on the same lane passes after a first-run failure.
- A test that never passes is excluded from the numerator but still fails the lane.

**Target.** **≤ 2%** of blocking-lane runs. A spec that flakes must be quarantined with a named owner
and an expiry, and a quarantine is a tracked exception, never a silent `skip` (`06/01-04`). The count
of quarantined specs with no owner or past expiry is a hard zero. Risk R-12 in `02/03` is the
tracked reason for this metric.

**Source.** The Playwright JSON report records each spec's retry attempts and final status. xUnit
`trx` results record the same for the .NET layers, including a rerun of the same artifact (`05/03-03`).
The quarantine list is committed alongside the tests, so the metric and the exemption share one
source.

**Reported in.**
- The dashboard trends the rate per lane — unit, integration, front-end, and E2E — so a single
  unstable lane is visible.
- The weekly triage reads the flakiest specs, assigns an owner, and either fixes the cause or sets a
  quarantine expiry.
- A release cannot ship with an expired quarantine on a critical journey (`06/01-04`).

**Notes.** A fixed delay is the top cause of flakes, and determinism rules in `06/01-04` are the
prevention; this metric measures whether that prevention is working.

---

## 3. Build Duration

**Definition.** The wall-clock time from the start of stage 1 to the finish of the last required
stage for a pull-request pipeline, plus the duration of each stage. The stage list and required-check
semantics are owned by `05/03-03`.

**Formula.**
- `pipeline_duration = last_required_stage.end − stage1.start`, excluding time spent waiting for a
  human or for a concurrency queue.
- `stage_duration = stage.end − stage.start`, reported per stage number.
- The nightly full run is measured separately because it adds the Firefox and WebKit browser matrix
  (`06/01-04`).

**Target.** On a pull request: **p50 ≤ 15 minutes** and **p95 ≤ 25 minutes**. On the nightly lane
including the full browser matrix: **≤ 45 minutes**. The p95 target is the one that matters; a median
that hugs the floor while the tail stretches means one stage is pathological.

**Source.** CI stage timestamps. No test instrumentation is needed; the pipeline is the source.

**Reported in.**
- The CI checks themselves show each stage's duration on the run.
- The dashboard plots the per-stage trend, so a slow-growing integration or E2E stage is caught before
  it breaches the tail budget.
- A stage that becomes the p95 outlier is the input to the pipeline-scaling decisions owned by
  `05/03-03`.

**Notes.** Duration is a proxy for developer feedback speed, not an end in itself. A change that
shortens the pipeline by skipping a required check is not a fix; the required-check set is the
contract of `05/03-03`.

---

## 4. Defect Escape Rate

**Definition.** The share of a release's defects that were found **after** the release shipped rather
than before it closed. A defect is attributed to the release whose change introduced it, not the
release during which it was found.

**Formula.**
- Per release: `defects_found_in_production ÷ (defects_found_in_production + defects_found_before_release)`.
- The denominator counts defects attributed to changes in that release, whether caught by a test,
  by review, or in production.
- Severity is taken from the issue tracker so the P1/P2 sub-target is computable.

**Target.** **≤ 5%** of a release's defects escape, and **zero P1/P2 escapes**. A P1 or P2 escape is
not traded off against the percentage; it opens a review of the missing test at the layer mapped in
`06/01-01`.

**Source.** Issue-tracker labels for severity and the release a defect is attributed to, plus the
release notes in `07-release`. The mapping from a production defect to its introducing release is the
one thing this metric needs from a human; everything else is counted.

**Reported in.**
- The per-release report states the rate, the P1/P2 count, and the missing-test follow-ups.
- The dashboard trends the rate across releases, so a rising trend triggers a review of the test
  strategy even while each release is under target.
- An escape that a lower layer could have caught becomes a new case in the owning test plan under
  `06/02-*`.

**Notes.** This is the only metric that closes the loop between verification and production. It is the
reason the other three are tracked: coverage, determinism, and speed exist to move defects to the left
of the release boundary.

---

## Where the Metrics Are Reported

| Surface | What it shows | Consumer |
|---|---|---|
| Pull request | Merged coverage summary, required-check status, per-stage duration | Author and reviewer |
| CI artifacts | Raw coverage, Playwright traces, Lighthouse reports | Debugging and audits |
| Quality dashboard (`08/02-02`) | All four metrics trended over their windows, against targets | QA lead, engineering lead |
| Weekly triage | Flakiest specs and quarantine list | QA lead and spec owners |
| Per-release report | Coverage, escape rate, and P1/P2 escapes for the release | Release owner and stakeholders |

The dashboard (`08/02-02`) reads from the telemetry backend (`08/02-01`) and the CI provider; it is the
one place the four metrics are read together. The exact dashboard layout and alert wiring belong to
`08/02-02` and `08/02-03`.

---

## Ownership and Cadence

- **Owner.** The QA lead owns the metrics and the weekly triage; each spec owner owns the flakes and
  quarantines for their specs.
- **Cadence.** Flaky-test rate and build duration are read weekly; coverage and defect escape rate are
  read per release and reported in the release notes.
- **Ratchet.** Every target is a floor that improves over time. Lowering a target requires an explicit
  decision recorded here and in review, exactly as a coverage-floor change does in `06/01-05`.

---

## Open Questions

1. The exact dashboard panels, alert thresholds, and retention window are owned by `08/02-02` and
   `08/02-03`; this file fixes the definitions and targets those surfaces read.
2. The severity taxonomy used for the P1/P2 sub-target is owned by the issue-tracker configuration;
   the labels are the input, and the sub-target here assumes P1 and P2 are defined there.
3. Whether build duration includes the documentation stage 6 and the nightly browser matrix in one
   number or in separate series is a dashboard decision; the PR and nightly targets above are fixed
   regardless.
