# Convention Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a build-free lint gate that rejects new inline magic values, malformed or inline outcomes, and non-monadic service/interface/abstract contracts, wired into `make lint` and PR CI.

**Architecture:** One stdlib-only Python checker under `scripts/common/` with a thin bash wrapper, plus a checked-in TSV baseline. The checker scans `src/` and `app/`, masks comments and string contents, runs four rules, suppresses reviewed legacy findings, and fails on new findings or stale baseline rows.

**Tech Stack:** bash 4+, Python 3 standard library, GNU Make, GitHub Actions, existing `trace.common.sh` conventions.

**Spec:** `docs/changelogs/2026-10-31-convention-gate.md`

## Global Constraints

- Python standard library only; no third-party packages, no virtualenv.
- Shell entry point follows `scripts/common/*.sh`: `set -euo pipefail`, `ROOT` from `BASH_SOURCE`, `trace`/`trace_exec` from `trace.common.sh` where a command is echoed.
- Make targets dispatch through `$(call run,$@,...)` and document with `##`.
- Rules, thresholds, and exemptions are exactly those in the spec: numeric abs ≥ 10; three wire-string shapes; static `*Outcome` with `Codes` and `Failure`/`Success`; `Result` returns for public/protected service/interface/abstract members.
- Scan roots: `src/` and `app/`. Never scan `tests/`, `bin/`, `obj/`, `node_modules/`, or migrations.
- Baseline rows: `rule ⇥ path ⇥ key ⇥ count ⇥ reason`. A row suppresses that many occurrences; excess occurrences fail. Stale or duplicate rows fail.
- `make convention.check` must pass on the current tree before wiring it into CI.
- No production C# changes in this plan.

---

### Task 1: Checker scaffolding, masking, and self-tests

**Files:**
- Create: `scripts/common/convention-check.common.py`
- Create: `scripts/common/convention-check.common.sh`

**Interfaces:**
- Produces: `Finding` dataclass (`rule`, `path`, `line`, `key`), `mask_source(text) -> tuple[str, list[tuple[int, str]]]`, `iter_sources() -> Iterator[tuple[Path, str]]`, `main(argv) -> int`.
- Produces: CLI flags `--baseline PATH`, `--write-baseline`, `--self-test`.

- [ ] **Step 1: Write the masking self-test fixtures**

The fixture list lives in the Python file and runs with `--self-test`. It asserts that comments and string contents disappear from the scan buffer while code stays visible, and that raw/verbatim strings are handled.

```python
SELF_TESTS = [
    ("line comment", "var x = 30; // 99\n", "99", False),
    ("block comment", "/* 99 */ var x = 30;\n", "99", False),
    ("string content", 'var s = "todo.completed";\n', "todo.completed", False),
    ("verbatim string", 'var s = @"todo.completed";\n', "todo.completed", False),
    ("raw string", 'var s = """todo.completed""";\n', "todo.completed", False),
    ("code visible", "var x = 30;\n", "30", True),
]
```

- [ ] **Step 2: Run the self-test to verify it fails**

Run: `python3 scripts/common/convention-check.common.py --self-test`
Expected: FAIL — file or `--self-test` not implemented.

- [ ] **Step 3: Implement masking and file discovery**

```python
@dataclass(frozen=True)
class Finding:
    rule: str
    path: str
    line: int
    key: str


def mask_source(text: str) -> tuple[str, list[tuple[int, str]]]:
    """Return (code_with_literals_blanked, [(offset, literal_value), ...])."""
    out = list(text)
    literals: list[tuple[int, str]] = []
    i, n = 0, len(text)
    while i < n:
        two = text[i : i + 2]
        if two == "//":
            j = text.find("\n", i)
            j = n if j < 0 else j
            for k in range(i, j):
                out[k] = " "
            i = j
            continue
        if two == "/*":
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
            for k in range(i, j):
                if out[k] != "\n":
                    out[k] = " "
            i = j
            continue
        if two == '@"':
            j = i + 2
            while j < n:
                if text[j] == '"':
                    if j + 1 < n and text[j + 1] == '"':
                        j += 2
                        continue
                    break
                j += 1
            literals.append((i, text[i + 2 : j].replace('""', '"')))
            for k in range(i, min(j + 1, n)):
                if out[k] != "\n":
                    out[k] = " "
            i = j + 1
            continue
        if text.startswith('"""', i):
            j = text.find('"""', i + 3)
            end = n if j < 0 else j + 3
            literals.append((i, text[i + 3 : (n if j < 0 else j)]))
            for k in range(i, end):
                if out[k] != "\n":
                    out[k] = " "
            i = end
            continue
        if text[i] == '"':
            j = i + 1
            value = []
            while j < n:
                if text[j] == "\\" and j + 1 < n:
                    value.append(text[j + 1])
                    j += 2
                    continue
                if text[j] == '"':
                    break
                value.append(text[j])
                j += 1
            literals.append((i, "".join(value)))
            for k in range(i, min(j + 1, n)):
                if out[k] != "\n":
                    out[k] = " "
            i = j + 1
            continue
        i += 1
    return "".join(out), literals
```

Discovery excludes `/obj/`, `/bin/`, `/node_modules/`, `/Migrations/`, and any path under `tests/`.

- [ ] **Step 4: Run the self-test to verify it passes**

Run: `python3 scripts/common/convention-check.common.py --self-test`
Expected: `self-test: 6 passed`.

- [ ] **Step 5: Add the bash wrapper**

```bash
#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
exec python3 "$ROOT/scripts/common/convention-check.common.py" "$@"
```

`chmod +x` both files.

---

### Task 2: Rules R1 and R2 — magic numbers and magic strings

**Files:**
- Modify: `scripts/common/convention-check.common.py`
- Test: embedded `SELF_TESTS`

**Interfaces:**
- Consumes: `mask_source`, `iter_sources`, `Finding`.
- Produces: `check_magic_number(path, masked, line_of) -> list[Finding]`, `check_magic_string(path, literals, line_of) -> list[Finding]`.

- [ ] **Step 1: Add failing fixtures for both rules**

```python
RULE_CASES = [
    ("magic-number", "src/X.Service.cs", "var t = 30;\n", 1),
    ("magic-number", "src/X.Service.cs", "var t = 9;\n", 0),
    ("magic-number", "src/X.Constant.cs", "var t = 30;\n", 0),
    ("magic-number", "src/X.Service.cs", "// 30\n", 0),
    ("magic-number", "src/X.Service.cs", "var t = 0x1F;\n", 0),
    ("magic-string", "src/X.Service.cs", 'x.RemoveAsync("todo:magic");\n', 1),
    ("magic-string", "src/X.Service.cs", 'x.Send("hello world");\n', 0),
    ("magic-string", "src/X.Service.cs", 'x.Get("https://a.b");\n', 0),
    ("magic-string", "src/X.Constant.cs", 'const string K = "todo:magic";\n', 0),
]
```

- [ ] **Step 2: Run and confirm the fixtures fail**

Run: `python3 scripts/common/convention-check.common.py --self-test`
Expected: FAIL — rule functions missing.

- [ ] **Step 3: Implement both rules**

Numeric pattern: `(?<![\w.])(\d[\d_]*)`; skip when preceded by `0x`/`0b` (detected by checking the two characters before the match), and when `abs(int(digits)) < 10`.

String shapes and exclusions are exactly the spec's three regexes; skip literals containing whitespace, starting with `http://`, `https://`, `amqp://`, `redis://`, `postgres://`, `unix://`, `Host=`, `Server=`, `Data Source=`, `Endpoint=`, `application/`, `text/`, `image/`, `audio/`, `video/`, or containing `/`.

File-name exemptions for both rules: name contains `Constant`, or ends with `Outcome.cs`, `Codes.cs`, `Enum.cs`, `Option.cs`, `Options.cs`, `Setting.cs`, `Settings.cs`, `Config.cs`, `Model.cs`, `Logger.cs`, `Designer.cs`, `.g.cs`.

- [ ] **Step 4: Run the self-test**

Run: `python3 scripts/common/convention-check.common.py --self-test`
Expected: all masking and rule cases pass.

---

### Task 3: Rules R3 and R4 — outcome shape and monadic results

**Files:**
- Modify: `scripts/common/convention-check.common.py`

**Interfaces:**
- Produces: `check_outcome_shape(path, text, masked, line_of) -> list[Finding]`, `check_monadic_result(path, masked, line_of) -> list[Finding]`.

- [ ] **Step 1: Add fixtures**

```python
OUTCOME_GOOD = '''
public static class XOutcome
{
    public static class Failure
    {
        public static Error Nope => Error.NotFound(code: Codes.Nope, message: "n");
    }
    public static class Codes { public const string Nope = "X.Nope"; }
}
'''
OUTCOME_BAD_CODES = "public static class XOutcome { public static class Failure { } }\n"
INLINE_ERROR = 'var e = Error.NotFound(code: "X.Nope", message: "n");\n'
SERVICE_BAD = "public Task<string> LoadAsync() => Task.FromResult(\"x\");\n"
SERVICE_GOOD = "public Task<Result<string>> LoadAsync() => Task.FromResult(Result<string>.Ok(\"x\"));\n"
```

- [ ] **Step 2: Run and confirm they fail**

Run: `python3 scripts/common/convention-check.common.py --self-test`
Expected: FAIL — functions missing.

- [ ] **Step 3: Implement R3**

For `*Outcome.cs`: require `static class \w+Outcome`, `class Codes\b`, `class (Failure|Success)\b`. For code-taking factories (`BadRequest`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`, `UnprocessableEntity`, `Validation`, `InternalServerError`, `ServiceUnavailable`, `Custom`), extract the first argument (or `code:` value) with a depth-aware scan; allow `Codes.` and `Code(` prefixes. Key is `static`, `Codes`, `grouping`, or `code-ref:<expression>`.

For every other file: `Error\.(?:...)\s*\(` outside the two documented exceptions is a finding with key = path.

- [ ] **Step 4: Implement R4**

Only `.Service.cs`, `.Interface.cs`, `.Abstract.cs`. Parse `public`/`protected`/interface members with a depth-aware return type; skip constructors, properties, events, operators, `Dispose`, `DisposeAsync`, `Deconstruct`, and type declarations. Violation when the type is `void`, `Task`, `ValueTask`, or `Task<T>`/`ValueTask<T>` whose inner type is not `Result`/`Result<…>`. Key is the member name.

- [ ] **Step 5: Run the self-test**

Expected: all cases pass.

---

### Task 4: Baseline ratchet and CLI

**Files:**
- Modify: `scripts/common/convention-check.common.py`

**Interfaces:**
- Produces: `load_baseline(path) -> dict[(rule, path, key), (count, reason)]`, `apply_baseline(findings, baseline) -> (kept, used, errors)`.

- [ ] **Step 1: Add fixtures**

```python
BASELINE_CASES = [
    ("suppresses", [Finding("magic-number", "a.cs", 1, "30")], "magic-number\ta.cs\t30\t1\tlegacy"),
    ("excess fails", [Finding("magic-number", "a.cs", 1, "30"), Finding("magic-number", "a.cs", 2, "30")], "magic-number\ta.cs\t30\t1\tlegacy"),
    ("stale fails", [], "magic-number\ta.cs\t30\t1\tlegacy"),
]
```

- [ ] **Step 2: Run and confirm failure**

Run: `python3 scripts/common/convention-check.common.py --self-test`
Expected: FAIL — baseline loader missing.

- [ ] **Step 3: Implement baseline parsing and ratchet**

Rules:

- Parse lines, skip blanks and `#` lines, require 5 tab-separated columns.
- Duplicate `(rule, path, key)` rows fail with both line numbers.
- Group findings by `(rule, path, key)`; a group is covered when `len(group) <= count`.
- Excess occurrences are reported as new findings at their own lines.
- Rows with no matching finding are stale errors.
- `--write-baseline` writes sorted groups with `count` and preserves the reason of an existing row with the same key, else `legacy`.

- [ ] **Step 4: Implement the CLI**

```text
python3 convention-check.common.py [--baseline PATH] [--write-baseline] [--self-test]
```

Exit codes: `0` pass, `1` violations or baseline errors, `2` usage/parse error.

On success print:

```text
convention check passed (0 new findings, 211 baselined)
```

On failure print one line per problem:

```text
convention check failed
magic-number  src/X.Service.cs:12  1234
outcome-shape src/X.Handler.cs:8   inline-error
stale baseline entry: scripts/common/convention-baseline.tsv:42 magic-number a.cs 30
```

- [ ] **Step 5: Run the self-test**

Expected: all cases pass.

---

### Task 5: Make target, baseline generation, and CI

**Files:**
- Modify: `make/common.mk`
- Modify: `.github/workflows/ci.yml`
- Create: `scripts/common/convention-baseline.tsv` (generated)

**Interfaces:**
- Consumes: `scripts/common/convention-check.common.sh`.
- Produces: `make convention.check`, `make convention.check SELF_TEST=1`, `make convention.check BASELINE=<path>`.

- [ ] **Step 1: Add the Make target**

```make
.PHONY: install format format.check structure.check convention.check lint build

convention.check: ## Verify magic values, outcome shape, and monadic Results
	$(call run,$@,$(ROOT)/scripts/common/convention-check.common.sh $(if $(SELF_TEST),--self-test))
```

`lint` gains `convention.check`:

```make
lint: lib.lint api.lint gate.lint host.lint store.lint admin.lint convention.check
```

- [ ] **Step 2: Generate the baseline**

Run: `python3 scripts/common/convention-check.common.py --write-baseline`
Expected: `scripts/common/convention-baseline.tsv` created with the current findings, sorted.

- [ ] **Step 3: Confirm the gate passes with the baseline**

Run: `make convention.check`
Expected: `convention check passed (0 new findings, <N> baselined)`.

- [ ] **Step 4: Add the CI step**

In `.github/workflows/ci.yml`, backend job, after the lint step and before the build step:

```yaml
- run: make convention.check
```

- [ ] **Step 5: Prove each rule fails on a fresh violation**

Create a scratch file `src/Scratch.Service.cs` containing `public Task<string> M() => Task.FromResult("x");` and `var n = 1234;`, run the checker, confirm three findings (`monadic-result`, `magic-number`, and no baseline growth), then delete the file.

- [ ] **Step 6: Prove stale rows fail**

Delete one baseline row, run `make convention.check`, confirm `stale baseline entry`, then restore it.

- [ ] **Step 7: Run the tree gate**

Run: `make convention.check` and `make lint`
Expected: both pass.

---

### Task 6: Documentation

**Files:**
- Modify: `AGENTS.md`
- Modify: `make/README.md`
- Modify: `scripts/README.md`
- Modify: `README.md`
- Modify: `guide/convention-gate-guide.md`
- Modify: `docs/changelogs/2026-10-31-convention-gate.md`

**Interfaces:**
- Consumes: the target and flags from Task 5.

- [ ] **Step 1: Add the AGENTS.md section**

`## Convention gate` covering the four rules, exemptions, file locations, `make convention.check`, and the baseline policy, linking `guide/convention-gate-guide.md`.

- [ ] **Step 2: Update the command docs**

- `make/README.md`: `common.mk` row mentions `convention.check`; invariants note `lint` includes it.
- `scripts/README.md`: `common/` row and env table gain the checker and `CONVENTION_BASELINE`; notes explain the ratchet.
- `README.md`: command list gains `make convention.check`.

- [ ] **Step 3: Align the guide and spec with the 5-column baseline**

Both documents describe `rule ⇥ path ⇥ key ⇥ count ⇥ reason` and the excess-occurrence rule.

- [ ] **Step 4: Mark the spec implemented**

Change the spec status to `Implemented` and record the final baseline size.

- [ ] **Step 5: Run the final gate**

Run: `make convention.check`, `make lint`, `make structure.check`, `git diff --check`.
Expected: all pass.

---

## Self-review notes

- Every spec rule (R1–R4), the baseline ratchet, Make/CI wiring, and the guide/spec docs map to a task.
- No task changes production C#, matching the spec's "no C# changes to land the gate".
- The baseline format is 5 columns in this plan; the spec and guide are updated in Task 6 to match.
- `make ci` inherits the gate through `lint`, and CI also calls it explicitly because the workflow invokes group lints rather than `make lint`.
