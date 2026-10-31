# Coverage

The coverage target per test layer, how coverage is collected from each tool, the exclusions, and how the result is reported and gated in CI.

**Links:** [Test matrix](./01-matrix.md) · [Unit tests](./02-unit.md) · [Integration tests](./03-integration.md) · [End-to-end tests](./04-e2e.md) · [CI pipeline](../../05-development/03-workflow/03-ci.md) · [Review policy](../../00-governance/02-policies/03-review-policy.md) · [Performance NFRs](../../01-requirements/03-nonfunctional/01-performance.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Coverage is a **signal, not a goal**. This file fixes the target per layer, the tool that produces
the number, the code that is deliberately excluded, and the CI behavior when a target is missed. The
layer scope is `06/01-01`; the tests that produce the number are `06/01-02`, `06/01-03`, and
`06/01-04`. The review policy (`00/02-03`) still requires a test at the correct layer for a change
regardless of any percentage.

The guiding rule: **a layer is measured against the code it is responsible for, and a threshold is a
floor that ratchets up, never a ceiling that lets an uncovered module hide behind a global average.**

---

## Coverage Targets per Layer

| Layer | Measured code | Target | Why |
|---|---|---|---|
| Unit (`06/01-02`) | `Resys.Core` and each module's `Domain/` and `Services/` | **Line ≥ 90%**, **branch ≥ 80%** | Engines and invariants are cheap to test and expensive to get wrong; branch coverage matters most on the promotion, tax, shipping, and state-machine paths. |
| Integration (`06/01-03`) | Module `Persistence/`, `Endpoints/`, and `Infrastructure/` (outbox, identity) | **Line ≥ 70%** | Persistence and HTTP are broad; the target is a floor, and every module's persistence and every endpoint group must have at least one integration test. |
| Front-end component (`06/01-02`) | `web/shared` and each app's stores and composables with logic | **Line ≥ 80%** | Formatting, cart, auth, and store-context logic is the front-end's risk surface; presentational components without behavior are not the target. |
| End-to-end (`06/01-04`) | Journeys, not lines | **100% of the critical journeys pass** | A journey is either proven or not; a line percentage on a browser run is noise. |

The unit target applies to the **module assemblies under test**, not to the test project. A module's
`Domain/` and `Services/` are the units; a module's migrations and DTO records are excluded below.

---

## Per-Assembly Thresholds

A single solution-wide percentage is not a gate: it lets one well-covered project mask an untested
module. Thresholds are therefore enforced **per assembly**, and every module assembly carries its own
floor:

- `Resys.Core` — line ≥ 90%, branch ≥ 80%.
- Each `Resys.Modules.<Name>` — line ≥ 85% overall, and line ≥ 90% on the assembly's `Domain/` and
  `Services/` namespaces. The lower overall floor accounts for persistence scaffolding; the namespace
  floor keeps the engines honest.
- `Resys.Infrastructure` — line ≥ 70%.
- `Resys.Shared` — excluded as DTO records with no behavior (below).

When a new module is added, it inherits the module floor; the build fails if it is added without
tests. The thresholds are the initial floor and ratchet up as the codebase matures; they are lowered
only with an explicit decision recorded here and in the review.

---

## How Coverage Is Measured

### .NET layers

Coverage is collected by **Coverlet** through the standard test command, so no extra stage is added
and the CI commands in `05/03-03` stay the contract. The collection flags are additive:

```bash
dotnet test tests/Resys.UnitTests -c Release --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory artifacts/coverage/unit

dotnet test tests/Resys.IntegrationTests -c Release --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory artifacts/coverage/integration
```

- `coverlet.collector` is referenced by both test projects; the output is a Cobertura XML file per test
  assembly.
- A `coverlet.runsettings` file at the repository root carries the include/exclude filters and the
  per-assembly thresholds, so the gate is versioned with the code rather than typed into the pipeline.
- The XML files are merged and rendered by **ReportGenerator** into an HTML report and a merged
  Cobertura artifact.

### Front-end component layer

The app test scripts use the Vitest coverage provider with the `v8` engine:

```bash
pnpm --filter storefront test --coverage
pnpm --filter admin test --coverage
```

The output is `lcov` plus a console summary, written under `artifacts/coverage/frontend/`.

### End-to-end layer

E2E reports **journey coverage**, not line coverage. The Playwright JSON report lists every spec and
its result; a missing or skipped critical journey is a failure. The journey list is the one in
`06/01-04`.

---

## How Coverage Is Reported and Gated in CI

- **Each layer job uploads its coverage artifact.** The unit, integration, and front-end jobs publish
  `artifacts/coverage/<layer>/` as a build artifact, so a reviewer can open the HTML report without
  reproducing the run.
- **The merged report is posted to the pull request.** The pipeline publishes the per-assembly summary
  as a PR comment, so a drop is visible in review.
- **A threshold miss fails the layer job.** The `coverlet.runsettings` thresholds are evaluated by
  Coverlet; a run below the floor returns a non-zero exit and the job is red. The front-end job fails
  on a Vitest coverage threshold miss.
- **The documentation checks stay separate.** Coverage is not part of stage 6; the forbidden-token and
  link checks in `05/03-03` are independent gates.
- **The metric is tracked over time.** Coverage, build duration, flaky-test rate, and defect escape
  rate are defined and trended in `06/03-01`; coverage is one input to that report, not the whole of
  it.

The CI provider and workflow layout are owned by `05/03-03`; the collection commands and thresholds
above are the test-strategy contract the pipeline implements.

---

## Exclusions

Exclusions are declared in `coverlet.runsettings` and in the Vitest configuration, never inline per
file. Excluded code:

- **EF Core migrations and model snapshots** — generated, not hand-written (`03/03-04`).
- **`Program.cs` and the host composition root** — startup wiring, exercised by every integration and
  E2E run rather than by a unit assertion.
- **`Resys.Shared` DTO records** — positional records with no behavior; their contract is asserted by
  the OpenAPI parity check in `06/01-03`.
- **Generated OpenAPI client code in `web/shared`** — generated from the service document
  (`03/04-04`).
- **Design tokens and static assets** — not executable logic.

An exclusion is reviewed like code: a new entry names what it excludes and why, and a class is not
excluded merely because it is hard to test.

---

## What Coverage Does Not Replace

- **A test at the right layer.** Review requires a change to carry a test at the layer mapped in
  `06/01-01` (`00/02-03`); a high percentage does not excuse a missing integration or E2E case.
- **A meaningful assertion.** A covered line with no assertion is worthless. Coverage measures
  execution, not correctness; the assertions are the test.
- **The critical journeys.** A 95% line number with a broken checkout is a failing release. The E2E
  journey list is the user-facing gate.
- **The performance budgets.** Coverage says nothing about latency; the budgets in `01/03-01` are
  measured separately by `06/03-02`.

---

## Open Questions

1. The exact numeric floor per new module after the first release is confirmed with `06/03-01`; the
   initial floors above are the contract until then.
2. Whether the merged HTML report is published to a static site or only as a build artifact is a
   pipeline-scaffold decision owned by `05/03-03`.
3. Whether branch coverage is enforced on the front-end layer as well as line coverage is settled when
   the Vitest thresholds are configured; the line floor is fixed here.
