# Convention Gate — Design

**Date:** 2026-10-31
**Status:** Implemented (2026-10-31)
**Owners:** Platform

## Problem

Three conventions are already followed by most of the codebase but are only
partially enforced:

1. **No inline magic values** — literals that carry domain meaning belong in a
   `*Constant.cs` file, not in a service body.
2. **No inline outcomes** — `Error.*` construction belongs in `*Outcome.cs`
   files that declare stable `Codes`, never at the call site.
3. **Monadic results** — the public surface of services, interfaces, and
   abstract classes returns `Result` / `Result<T>` instead of `void`,
   `Task`, or `Task<T>`.

Today only rule 2 is enforced, and only by
`tests/Architecture.Tests/InlineErrorUsage.Tests.cs`, which requires a build and
runs as part of the test suite rather than the lint gate. Rules 1 and 3 have no
automated enforcement at all.

The 2026-10-31 code-quality sweep (`docs/changelogs/2026-10-31-code-quality-sweep.md`)
normalised outcomes and constants across 81 files, so the codebase is close to
compliant. The remaining gaps are small and enumerable, which makes a ratcheted
gate practical now.

## Goals

- One build-free check that verifies all three conventions.
- Wired into `make lint` and the pull-request CI job.
- Green on the current tree without a mass rewrite, via a checked-in baseline.
- The baseline shrinks over time and cannot hide new violations.
- `AGENTS.md` documents the rules so contributors and agents apply them while
  writing code, not only after a failure.
- A skill-style guide in `guide/convention-gate-guide.md` carries the full
  rule text, examples, and the fix procedure, matching the existing guides.

## Non-goals

- Replacing `InlineErrorUsage.Tests`; it stays as the reflection-level check.
- Enforcing every literal, message, SQL fragment, or log template.
- Enforcing monadic results on handlers, endpoints, or EF overrides.
- Auto-fixing violations. The gate reports; the author moves the value.
- Style or formatting rules already owned by `dotnet format`.

## Rules

All rules scan C# under `src/` and `app/`. Tests (`tests/**`, `*.Test.cs`,
`*.Spec.cs`, web `__tests__`) are out of scope: fixtures may use literal data.

Comments, XML docs, and string contents are stripped before the numeric scan so
prose such as `Threat TMT-AUTH-001` is never a finding.

### R1 — `magic-number`

Flag an integer or decimal literal whose absolute integer part is **10 or
greater**.

Excluded paths:

- `**/obj/**`, `**/bin/**`, `**/Migrations/**`
- file name contains `Constant` (`*.Constant.cs`, `RepositoryConstant.cs`,
  `QueryCapabilityConstant.cs`, `*.Constants.cs`)
- file name ends with `Outcome.cs`, `Codes.cs`, `Enum.cs`, `Option.cs`,
  `Options.cs`, `Setting.cs`, `Settings.cs`, `Config.cs`, `Model.cs`,
  `Logger.cs`
- generated files (`*.Designer.cs`, `*.g.cs`)

Excluded values:

- `0`–`9` and their negatives, so index arithmetic and small counts stay legal.
- hex (`0x…`) and binary (`0b…`) literals.
- literals inside `#region` names, comments, and string contents.

Baseline key: `path` + the literal text (`30`, `256`, `1000`).

### R2 — `magic-string`

Flag a string literal that looks like a wire contract, in the same file scope
as R1. Three shapes only:

- dotted identifier: `^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9_]+)+$`
  (`todo.completed`, `payments.initiated`, `Microsoft.EntityFrameworkCore.InMemory`)
- hyphenated identifier: `^[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)+$`
  (`X-Correlation-Id`, `content-hash`, `api-payments`)
- colon key: `^[a-z][a-z0-9]*(?::[A-Za-z0-9_*{}.-]+)+$`
  (`seeder:todos`, `todo:{message.TodoId}`)

Excluded literals:

- URIs and connection strings: `http://`, `https://`, `amqp://`, `redis://`,
  `postgres://`, `unix://`, `Host=`, `Server=`, `Data Source=`, `Endpoint=`
- MIME types: `application/…`, `text/…`, `image/…`, `audio/…`, `video/…`
- prose: any literal containing whitespace
- format strings passed to `string.Format`, interpolation holes, and literals
  inside `nameof(...)`

Baseline key: `path` + the literal text.

### R3 — `outcome-shape`

For every `*Outcome.cs` file under `src/`:

1. The root declaration is a `static class` whose name ends in `Outcome`
   (`public` or `internal`).
2. A nested `Codes` class exists.
3. A nested `Failure` and/or `Success` class exists.
4. Every **code-taking** `Error.<Factory>(` call inside the file receives a
   code that is `Codes.<Member>`, a member declared in that file's `Codes`, or
   a builder method declared inside `Codes` (for example
   `Codes.For(operation, reason)`). A bare or interpolated literal is a
   violation. Factories that take an exception instead of a code
   (`Error.Unexpected(exception)`, `Error.FromException(exception)`) are not
   subject to this rule.

For every other file under `src/` and `app/`:

5. `Error.<Factory>(` must not appear, except in the two documented exceptions
   already carried by `InlineErrorUsage.Tests`:
   - `src/Core/BuildingBlocks/Functional/Results/Result.Generic.cs`
   - `src/Application/BuildingBlocks/Security/Authorization/Domain/PermissionMetadata.cs`

Dynamic code builders are legitimate — `CachingOutcome.Code(operation, reason)`
and `$"Channel.{channelName}.Section.Required"` express codes that cannot be
constants. Rule 4 requires such a builder to live inside `Codes`; legacy files
that do not yet follow that shape are baselined.

Baseline key: `path` + the missing element (`static`, `Codes`, `grouping`,
`code-ref`, `inline-error-<file>`).

### R4 — `monadic-result`

Scope: `*.Service.cs`, `*.Interface.cs`, `*.Abstract.cs` under `src/` and
`app/`.

Members: public and protected members, plus all interface members (implicitly
public). Constructors, properties, events, operators, indexers, `Dispose`,
`DisposeAsync`, `Deconstruct`, and framework `override` members are exempt
(the base class fixes their signature).

A member is a violation when its declared return type is exactly:

- `void`
- `Task`
- `ValueTask`
- `Task<T>` or `ValueTask<T>` where `T` is not `Result` or `Result<…>`

Allowed returns:

- `Result`, `Result<T>`
- `Task<Result>`, `Task<Result<T>>`, `ValueTask<Result>`, `ValueTask<Result<T>>`
- any non-void, non-`Task` type: pure functions such as `bool IsExpired(...)`,
  `string Serialize(...)`, `Type Resolve(...)`, `IReadOnlyList<T> ByDomain(...)`

Baseline key: `path` + member name.

### Current state

| Rule | Findings | Baseline rows |
|---|---|---|
| `magic-number` | 0 | 0 |
| `magic-string` | 0 | 0 |
| `outcome-shape` | 0 | 0 |
| `monadic-result` | 9 | 9 |

Total: 9 findings suppressed by 9 baseline rows. The original 211 findings /
194 rows fell in four passes: the Payments constant and monadic-result pass,
the EF Core configuration and seeder extraction, the provider, event,
messaging, storage, gateway, and AppHost extractions, and the outcome-shape
pass (query codes, notification builders, metadata, permissions, identity, and
the persistence mapper rename). No row was ever added.

Three rules are fully cleared. The remaining baseline is deliberate:

- `monadic-result` (9): module composition hooks (`Configure`,
  `RegisterEventTypes`), aggregate state mutators (`LoadFromHistory`,
  `RestoreSnapshot`, `RemoveAllEvents`, `ClearUncommittedEvents`), and the
  storage anti-forgery `Task<bool>` contract. Changing those signatures would
  ripple through the composition root and callers.

The outcome-shape pass moved every code into its owning outcome: `QueryErrorCodes`
became `QueryOutcome.Codes`, notification and permission code builders moved
inside `Codes`, `StorageOutcome` became a static class, and the persistence
failure mapper was renamed from `Persistence.Outcome.cs` to
`Persistence.Mapper.cs` because it maps failures rather than declaring them.

## Architecture

```
scripts/common/
├── convention-check.common.sh   # wrapper: ROOT, exec python
├── convention-check.common.py   # rules, baseline, reporting
└── convention-baseline.tsv      # checked-in exceptions
guide/
└── convention-gate-guide.md     # rule text, examples, fix procedure
```

The split mirrors `common/coverage-check.common.sh` + `.py`: the shell entry
point exists so the Make target and CI step look like every other script, and
the logic is Python because the checks need comment stripping, depth-aware
generics, and stable sorting. Only the Python standard library is used.

The checker is a single pass over the file list:

1. Resolve `ROOT` from the script location.
2. Enumerate candidate files once.
3. For each file, strip comments and string contents into a scan buffer while
   retaining byte offsets for `path:line` reporting.
4. Run the four rules, collecting `(rule, path, line, key)` findings.
5. Load the baseline, suppress matching findings, and report:
   - unsuppressed findings → exit 1
   - stale baseline entries → exit 1
   - duplicate baseline entries → exit 1
6. Print `convention check passed` with the suppressed count on success.

### Baseline format

Tab-separated, one exception per line, `#` comments allowed:

```
# rule            path                                                        key        count  reason
magic-number      src/Infrastructure/.../Todo.Seeder.cs                       30         3      legacy: unnamed numeric literal
magic-string      app/Gateway/Gateway.Router.cs                              api-time   1      legacy: unnamed wire literal
outcome-shape     src/Application/BuildingBlocks/Caching/Caching.Outcome.cs  Codes      1      legacy: outcome shape
monadic-result    src/Application/Modules/Payments/Persistence/IPaymentReadStore.Interface.cs  FindAsync  7  legacy: non-monadic signature
```

Semantics:

- An entry suppresses `count` occurrences of the same `(rule, path, key)`.
- More occurrences than `count` fail: adding another baselined literal to the
  file is a new violation.
- An entry that matches nothing is **stale** and fails the run. The list can
  only shrink.
- Duplicate `(rule, path, key)` entries fail the run.
- `--write-baseline` regenerates the file from the current tree, preserving the
  reason of an existing entry and writing a rule-specific `legacy:` reason for
  new ones. Reviewers see the growth in the diff.
- `CONVENTION_BASELINE` (or `make convention.check BASELINE=<path>`) overrides
  the file location.

### Make and CI

The `##@ Quality` section in `make/common.mk` defines:

```make
convention.check: ## Verify magic values, outcome shape, and monadic Results
	$(call run,$@,$(ROOT)/scripts/common/convention-check.common.sh)
```

`lint` gains `convention.check` as a prerequisite, so `make lint` and any local
lint run cover it. The backend CI job calls the group lints individually rather
than `make lint`, so the workflow gets an explicit step:

```yaml
- run: make convention.check
```

The step runs before `make lib.build`, so doc/constant violations fail in
seconds rather than after a build.

### Guide

`guide/convention-gate-guide.md` is the long-form reference. It follows the
existing guide format exactly:

- YAML frontmatter with `name: convention-gate` and a `description` that names
  the trigger (adding literals, errors, or service members; fixing the gate);
- `# Convention Gate — Optimized Skill Guide`, then `**Use when:**`,
  `**Goal:**`, and `**Rule:**`;
- numbered `##` sections with tables and C# examples, separated by `---`;
- closing `Agent Procedure`, `Human Review Checklist`, `Compact Reference`, and
  `One-Sentence Rule` sections, as in `code-comment-style-guide.md` and
  `dotted-file-naming-guide.md`.

Section plan:

| Section | Content |
|---|---|
| 1. Why the Gate Exists | the three conventions and their cost when unenforced |
| 2. Rule Summary | one table mapping rule → failure → fix |
| 3. Magic Numbers | threshold, exclusions, where each value kind lives |
| 4. Magic Strings | the three wire shapes and their regexes |
| 5. Outcome Shape | good/bad examples, required structure, exceptions |
| 6. Monadic Results | allowed/violation return types, exemptions |
| 7. Baseline Policy | TSV shape, stale/duplicate rules, no new rows |
| 8. Agent Procedure | the seven-step decision list |
| 9. Human Review Checklist | the review boxes |
| 10. Compact Reference | value/error/contract → file mapping plus commands |
| 11. One-Sentence Rule | the closing rule |

`guide/README.md` gains the guide in its `Contains` table and notes that it is
the one guide enforced mechanically by `make convention.check`.

### AGENTS.md

A new `## Convention gate` section documents:

- the four rules and their exemptions;
- where constants (`*Constant.cs`) and outcomes (`*Outcome.cs`) live;
- the `Error.*` → outcome and `Result` return requirements;
- `make convention.check` as the local repro;
- the baseline policy: entries need a reason, stale entries fail, and new code
  must not add entries;
- that `InlineErrorUsage.Tests` remains the deeper check and the script is its
  fast mirror.

`make/README.md`, `scripts/README.md`, and the root `README.md` lists get the
new target and script.

## Rollout

1. Add `convention-check.common.py` and `.sh`.
2. Generate `convention-baseline.tsv` from the current tree and review it.
3. Add the Make target and wire it into `lint`.
4. Add the CI step.
5. Add `guide/convention-gate-guide.md` and index it in `guide/README.md`.
6. Update `AGENTS.md`, `make/README.md`, `scripts/README.md`, `README.md`.
7. Verify positively and negatively (see below).
8. Follow-up (separate change): shrink the baseline, starting with
   `CommandTimeout = 30`, `HasMaxLength(256)`, and the payments read store.

## Verification

| Check | Expected |
|---|---|
| `make convention.check` on the current tree | passes, reports suppressed count |
| Add `TimeSpan.FromSeconds(1234)` to a `*.Service.cs` | fails with `magic-number path:line 1234` |
| Add `cache.RemoveAsync("todo:magic")` to a service | fails with `magic-string` |
| Add `Error.NotFound(code: "X", message: "y")` to a handler | fails with `outcome-shape` |
| Add `public Task<string> LoadAsync()` to a `*.Interface.cs` | fails with `monadic-result` |
| Delete a baseline row whose finding still exists | fails as stale |
| `make lint` / `make ci` | include the gate and pass |
| `make structure.check` | unchanged |
| `guide/convention-gate-guide.md` | frontmatter, `**Use when:**` block, 11 sections, checklist, and compact reference present; `guide/README.md` lists it |
| `AGENTS.md` | links the guide and states the baseline policy |

The negative cases are run manually during implementation, then removed; they
are not committed as fixtures. The checker's own parsing behaviour (comment
stripping, nested generics, raw strings) is covered by embedded `--self-test`
cases in the Python file, which run `make convention.check SELF_TEST=1`.

## Risks and trade-offs

- **Regex over C#.** Multi-line generic signatures and raw string literals can
  confuse a scanner. Mitigation: strip comments and strings with a small state
  machine, parse return types with bracket-depth tracking, and baseline the
  remainder. The gate is a lint, not a compiler.
- **Large baseline.** 211 findings across 194 rows at introduction
  (70 + 56 + 68 + 17). Acceptable for a ratchet; every row carries a reason and
  must remain live. Fixing the top clusters shrinks it quickly.
- **Duplication with the architecture test.** Intentional: the test runs with
  the suite and can use reflection; the script runs in lint without a build.
  Rule text lives in `guide/convention-gate-guide.md`; this spec records the
  design and the measurements behind it.
- **Dynamic outcome codes.** Rule R3.4 forces builders into `Codes`. This is a
  real refactor for the 11 flagged outcome files, so they are baselined rather
  than rewritten now.
- **Numbers that are not domain values.** Event IDs, EF limits, and byte
  conversions are still literals. Baselining them is honest; moving them to
  constants is a mechanical follow-up.

## Acceptance criteria

- `make convention.check` exits 0 on a clean tree and prints the suppressed
  count.
- Each rule fails on a fresh violation with `rule path:line key`.
- A stale or duplicate baseline entry fails the run.
- `make lint` and the PR CI job both run the gate.
- `AGENTS.md` states the rules, exemptions, file locations, and baseline
  policy, and links the guide.
- `guide/convention-gate-guide.md` follows the existing guide format and is
  indexed in `guide/README.md`.
- No C# production file changes are required to land the gate.

## Implementation record

| Path | Purpose |
|---|---|
| `scripts/common/convention-check.common.py` | four rules, masking, baseline ratchet, `--self-test` |
| `scripts/common/convention-check.common.sh` | Make/CI entry point |
| `scripts/common/convention-baseline.tsv` | 9 rows suppressing 9 findings |
| `make/common.mk` | `convention.check` target; `lint` prerequisite |
| `.github/workflows/ci.yml` | explicit `make convention.check` step before build |
| `AGENTS.md` | `## Convention gate` rules and baseline policy |
| `guide/convention-gate-guide.md` | long-form guide |

Verification performed:

| Command | Result |
|---|---|
| `python3 … --self-test` | 23 fixtures pass |
| `make convention.check` | pass, 9 baselined |
| Scratch `1234` + `Task<string>` file | fails with `magic-number` and `monadic-result` |
| Extra `100` in a baselined file | fails with `new occurrence(s) of baselined value` |
| Fake baseline row | fails with `stale baseline entry` |
| `make lint` | pass, includes the gate |
| `make convention.check SELF_TEST=1` | pass through the Make target |
| `make ci` | pass, including the gate, Api, and RabbitMQ suites |

The checker initially found 70 numeric, 56 wire-string, 68 outcome, and 17
monadic findings. Successive passes removed every numeric, wire-string, and
outcome finding plus 8 monadic findings without adding a row, leaving 9
findings across 9 rows. What remains is intentional legacy: composition-hook
and aggregate-mutator signatures, plus the storage anti-forgery `Task<bool>`.
