#!/usr/bin/env python3
"""Enforce the repository's value, outcome, and Result conventions.

Usage:
    convention-check.common.py [--baseline PATH] [--write-baseline] [--self-test]

Rules:
    magic-number    Numeric literals with an absolute value of 10 or more must
                    live in a constant file.
    magic-string    Dotted, hyphenated, or colon-key literals must live in a
                    constant file.
    outcome-shape   Error construction belongs in *Outcome.cs files that declare
                    Codes and Failure/Success grouping.
    monadic-result  Public/protected service, interface, and abstract members
                    must return Result / Result<T> instead of void or Task<T>.

Baseline rows are ``rule<TAB>path<TAB>key<TAB>count<TAB>reason``. A row
suppresses that many occurrences; excess occurrences fail, stale rows fail, and
duplicate rows fail. The list only ever shrinks.
"""

from __future__ import annotations

import argparse
import bisect
import os
import re
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator, Sequence

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_BASELINE = ROOT / "scripts" / "common" / "convention-baseline.tsv"

SCAN_ROOTS = ("src", "app")
GENERATED_SEGMENTS = ("/obj/", "/bin/", "/node_modules/", "/Migrations/")

LITERAL_EXEMPT_SUFFIXES = (
    "Outcome.cs",
    "Codes.cs",
    "Enum.cs",
    "Option.cs",
    "Options.cs",
    "Setting.cs",
    "Settings.cs",
    "Config.cs",
    "Model.cs",
    "Logger.cs",
    "Designer.cs",
    ".g.cs",
)

INLINE_ERROR_EXEMPT_PATHS = {
    "src/Core/BuildingBlocks/Functional/Results/Result.Generic.cs",
    "src/Application/BuildingBlocks/Security/Authorization/Domain/PermissionMetadata.cs",
}

CODE_TAKING_FACTORIES = {
    "BadRequest",
    "Unauthorized",
    "Forbidden",
    "NotFound",
    "Conflict",
    "UnprocessableEntity",
    "Validation",
    "InternalServerError",
    "ServiceUnavailable",
    "Custom",
}

ANY_ERROR_FACTORY = re.compile(
    r"\bError\.(?:Custom|BadRequest|Unauthorized|Forbidden|NotFound|Conflict|"
    r"UnprocessableEntity|Validation|InternalServerError|Unexpected|"
    r"ServiceUnavailable|FromException)\s*\(",
)

NUMBER = re.compile(r"(?<![\w.])(\d[\d_]*)")

WIRE_STRING_SHAPES = (
    re.compile(r"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9_]+)+$"),
    re.compile(r"^[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)+$"),
    re.compile(r"^[a-z][a-z0-9]*(?::[A-Za-z0-9_*{}.-]+)+$"),
)

STRING_EXCLUDED_PREFIXES = (
    "http://",
    "https://",
    "amqp://",
    "redis://",
    "postgres://",
    "unix://",
    "Host=",
    "Server=",
    "Data Source=",
    "Endpoint=",
    "application/",
    "text/",
    "image/",
    "audio/",
    "video/",
)

ROLE_SUFFIXES = (".Service.cs", ".Interface.cs", ".Abstract.cs")
MEMBER_EXEMPT_NAMES = {"Dispose", "DisposeAsync", "Deconstruct"}
TYPE_KEYWORDS = {
    "class",
    "record",
    "struct",
    "interface",
    "enum",
    "delegate",
    "event",
    "namespace",
    "new",
    "return",
    "var",
    "await",
    "throw",
    "else",
    "if",
    "for",
    "foreach",
    "while",
    "switch",
    "catch",
    "using",
    "lock",
    "operator",
    "this",
    "public",
    "private",
    "protected",
    "internal",
    "static",
    "readonly",
    "const",
}

MEMBER_DECLARATION = re.compile(
    r"^(?P<mods>(?:(?:public|protected|internal|private|static|abstract|virtual|"
    r"override|async|sealed|partial|new|readonly|unsafe|extern)\s+)*)"
    r"(?P<type>[A-Za-z_][\w.]*(?:<[^;{}()]*>)?(?:\[\])?(?:\?)?)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\(",
)


@dataclass(frozen=True)
class Finding:
    """One rule violation."""

    rule: str
    path: str
    line: int
    key: str


class LineMap:
    """Map byte offsets back to 1-based line numbers."""

    def __init__(self, text: str) -> None:
        self._starts = [0]
        self._starts.extend(index + 1 for index, char in enumerate(text) if char == "\n")

    def line(self, offset: int) -> int:
        return bisect.bisect_right(self._starts, offset)


def _literal_start(text: str, index: int) -> tuple[str, int] | None:
    """Classify a string literal starting at or before ``index``."""
    cursor = index
    verbatim = False
    length = len(text)

    while cursor < length and text[cursor] in "$@":
        if text[cursor] == "@":
            verbatim = True
        cursor += 1

    if cursor >= length or text[cursor] != '"':
        return None

    if text.startswith('"""', cursor):
        return "raw", cursor

    return ("verbatim" if verbatim else "normal"), cursor


def mask_source(text: str) -> tuple[str, list[tuple[int, str]]]:
    """Blank comments and string contents, returning visible offsets of literals."""
    output = list(text)
    literals: list[tuple[int, str]] = []
    index = 0
    length = len(text)

    def blank(start: int, end: int) -> None:
        for position in range(start, min(end, length)):
            if output[position] != "\n":
                output[position] = " "

    while index < length:
        if text.startswith("//", index):
            end = text.find("\n", index)
            end = length if end < 0 else end
            blank(index, end)
            index = end
            continue

        if text.startswith("/*", index):
            end = text.find("*/", index + 2)
            end = length if end < 0 else end + 2
            blank(index, end)
            index = end
            continue

        start = _literal_start(text, index)
        if start is not None:
            kind, quote = start

            if kind == "raw":
                closing = text.find('"""', quote + 3)
                closing = length if closing < 0 else closing
                literals.append((index, text[quote + 3 : closing]))
                end = min(closing + 3, length)
            elif kind == "verbatim":
                cursor = quote + 1
                while cursor < length:
                    if text[cursor] == '"':
                        if cursor + 1 < length and text[cursor + 1] == '"':
                            cursor += 2
                            continue
                        break
                    cursor += 1
                literals.append(
                    (index, text[quote + 1 : cursor].replace('""', '"'))
                )
                end = min(cursor + 1, length)
            else:
                cursor = quote + 1
                value: list[str] = []
                while cursor < length:
                    if text[cursor] == "\\" and cursor + 1 < length:
                        value.append(text[cursor + 1])
                        cursor += 2
                        continue
                    if text[cursor] == '"':
                        break
                    value.append(text[cursor])
                    cursor += 1
                literals.append((index, "".join(value)))
                end = min(cursor + 1, length)

            blank(index, end)
            index = end
            continue

        index += 1

    return "".join(output), literals


def strip_comments(text: str) -> str:
    """Remove comments while keeping string literals intact."""
    output: list[str] = []
    index = 0
    length = len(text)

    while index < length:
        if text.startswith("//", index):
            end = text.find("\n", index)
            index = length if end < 0 else end
            continue

        if text.startswith("/*", index):
            end = text.find("*/", index + 2)
            index = length if end < 0 else end + 2
            output.append("\n")
            continue

        start = _literal_start(text, index)
        if start is not None:
            kind, quote = start
            if kind == "raw":
                closing = text.find('"""', quote + 3)
                end = length if closing < 0 else min(closing + 3, length)
            elif kind == "verbatim":
                cursor = quote + 1
                while cursor < length:
                    if text[cursor] == '"':
                        if cursor + 1 < length and text[cursor + 1] == '"':
                            cursor += 2
                            continue
                        break
                    cursor += 1
                end = min(cursor + 1, length)
            else:
                cursor = quote + 1
                while cursor < length:
                    if text[cursor] == "\\" and cursor + 1 < length:
                        cursor += 2
                        continue
                    if text[cursor] == '"':
                        break
                    cursor += 1
                end = min(cursor + 1, length)
            output.append(text[quote:end])
            index = end
            continue

        output.append(text[index])
        index += 1

    return "".join(output)


def iter_sources() -> Iterator[tuple[Path, str]]:
    """Yield candidate production files as (absolute path, relative path)."""
    for root_name in SCAN_ROOTS:
        root = ROOT / root_name
        if not root.is_dir():
            continue
        for path in sorted(root.rglob("*.cs")):
            relative = path.relative_to(ROOT).as_posix()
            lowered = f"/{relative}"
            if any(segment in lowered for segment in GENERATED_SEGMENTS):
                continue
            if relative.startswith("tests/"):
                continue
            yield path, relative


def literal_exempt(relative: str) -> bool:
    name = Path(relative).name
    if "Constant" in name:
        return True
    return name.endswith(LITERAL_EXEMPT_SUFFIXES)


def check_magic_number(relative: str, masked: str, lines: LineMap) -> list[Finding]:
    """Flag numeric literals with an absolute value of 10 or more."""
    if literal_exempt(relative):
        return []

    findings: list[Finding] = []
    for match in NUMBER.finditer(masked):
        digits = match.group(1)
        value = int(digits.replace("_", ""))
        if value < 10:
            continue
        findings.append(Finding("magic-number", relative, lines.line(match.start()), digits))

    return findings


def check_magic_string(
    relative: str,
    literals: Sequence[tuple[int, str]],
    lines: LineMap,
) -> list[Finding]:
    """Flag wire-shaped string literals outside constant files."""
    if literal_exempt(relative):
        return []

    findings: list[Finding] = []
    for offset, value in literals:
        if not value or any(char.isspace() for char in value):
            continue
        if value.startswith(STRING_EXCLUDED_PREFIXES) or "/" in value:
            continue
        if not any(shape.match(value) for shape in WIRE_STRING_SHAPES):
            continue
        findings.append(Finding("magic-string", relative, lines.line(offset), value))

    return findings


def _first_argument(text: str, open_index: int) -> str:
    depth = 1
    index = open_index + 1
    start = index

    while index < len(text) and depth:
        char = text[index]
        if char in "([{<":
            depth += 1
        elif char in ")]}>":
            depth -= 1
            if depth == 0:
                break
        elif char == "," and depth == 1:
            break
        index += 1

    argument = text[start:index].strip()
    if argument.startswith("code:"):
        argument = argument[len("code:") :].strip()
    return argument


def check_outcome_shape(
    relative: str,
    text: str,
    masked: str,
    lines: LineMap,
) -> list[Finding]:
    """Require the outcome shape and forbid inline Error construction."""
    findings: list[Finding] = []
    is_outcome = Path(relative).name.endswith("Outcome.cs")

    if is_outcome:
        code = strip_comments(text)
        if not re.search(r"\bstatic\s+class\s+\w+Outcome\b", code):
            findings.append(Finding("outcome-shape", relative, 1, "static"))
        if not re.search(r"\bclass\s+Codes\b", code):
            findings.append(Finding("outcome-shape", relative, 1, "Codes"))
        if not re.search(r"\bclass\s+(?:Failure|Success)\b", code):
            findings.append(Finding("outcome-shape", relative, 1, "grouping"))

        for match in re.finditer(r"\bError\.(\w+)\s*\(", code):
            if match.group(1) not in CODE_TAKING_FACTORIES:
                continue
            argument = _first_argument(code, match.end() - 1)
            if argument.startswith(("Codes.", "Code(")):
                continue
            findings.append(
                Finding(
                    "outcome-shape",
                    relative,
                    lines.line(match.start()),
                    f"code-ref:{argument[:60]}",
                )
            )
        return findings

    if relative in INLINE_ERROR_EXEMPT_PATHS:
        return findings

    for match in ANY_ERROR_FACTORY.finditer(masked):
        findings.append(
            Finding("outcome-shape", relative, lines.line(match.start()), "inline-error")
        )

    return findings


def _looks_like_member(relative: str, line: str) -> re.Match[str] | None:
    match = MEMBER_DECLARATION.match(line.strip())
    if match is None:
        return None

    return_type = " ".join(match.group("type").split())
    name = match.group("name")

    if return_type in TYPE_KEYWORDS:
        return None
    if name in MEMBER_EXEMPT_NAMES:
        return None
    if "." in return_type:
        return None

    modifiers = match.group("mods")

    # Skip: framework overrides cannot change their signature to return Result.
    if "override" in modifiers.split():
        return None

    if Path(relative).name.endswith(".Interface.cs"):
        return match
    if "public" not in modifiers and "protected" not in modifiers:
        return None

    return match


def _is_monadic(return_type: str) -> bool:
    compact = return_type.replace(" ", "")

    if compact in ("Result", "Task", "ValueTask", "void"):
        return compact == "Result"

    for wrapper in ("Task", "ValueTask"):
        prefix = f"{wrapper}<"
        if compact.startswith(prefix) and compact.endswith(">"):
            inner = compact[len(prefix) : -1]
            return inner == "Result" or inner.startswith("Result<")

    return True


def check_monadic_result(relative: str, masked: str, lines: LineMap) -> list[Finding]:
    """Require Result returns on the service/interface/abstract public surface."""
    if not Path(relative).name.endswith(ROLE_SUFFIXES):
        return []

    findings: list[Finding] = []
    for number, line in enumerate(masked.splitlines(), start=1):
        if "(" not in line:
            continue
        match = _looks_like_member(relative, line)
        if match is None:
            continue

        return_type = " ".join(match.group("type").split())
        if _is_monadic(return_type):
            continue

        findings.append(
            Finding("monadic-result", relative, number, match.group("name"))
        )

    return findings


def scan_text(relative: str, text: str) -> list[Finding]:
    """Run every rule against one file body."""
    masked, literals = mask_source(text)
    lines = LineMap(text)

    findings: list[Finding] = []
    findings.extend(check_magic_number(relative, masked, lines))
    findings.extend(check_magic_string(relative, literals, lines))
    findings.extend(check_outcome_shape(relative, text, masked, lines))
    findings.extend(check_monadic_result(relative, masked, lines))
    return findings


def scan_repository() -> list[Finding]:
    """Scan every candidate file under the configured roots."""
    findings: list[Finding] = []
    for path, relative in iter_sources():
        findings.extend(scan_text(relative, path.read_text(encoding="utf-8")))
    return findings


def load_baseline(
    path: Path,
) -> tuple[dict[tuple[str, str, str], tuple[int, str]], list[str]]:
    """Parse the baseline, returning rows and parse errors."""
    rows: dict[tuple[str, str, str], tuple[int, str]] = {}
    errors: list[str] = []

    if not path.exists():
        return rows, errors

    for number, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue

        parts = raw.split("\t")
        if len(parts) < 5:
            errors.append(f"baseline parse error: {path}:{number} needs 5 tab-separated columns")
            continue

        rule, relative, key, count_text, reason = (
            parts[0].strip(),
            parts[1].strip(),
            parts[2].strip(),
            parts[3].strip(),
            parts[4].strip(),
        )

        try:
            count = int(count_text)
        except ValueError:
            errors.append(f"baseline parse error: {path}:{number} count is not an integer")
            continue

        identity = (rule, relative, key)
        if identity in rows:
            errors.append(f"duplicate baseline entry: {path}:{number} {rule} {relative} {key}")
            continue

        rows[identity] = (count, reason)

    return rows, errors


def apply_baseline(
    findings: Sequence[Finding],
    baseline: dict[tuple[str, str, str], tuple[int, str]],
) -> tuple[list[Finding], list[str], int]:
    """Split findings into new violations and baseline errors."""
    grouped: dict[tuple[str, str, str], list[Finding]] = defaultdict(list)
    for finding in findings:
        grouped[(finding.rule, finding.path, finding.key)].append(finding)

    new_findings: list[Finding] = []
    errors: list[str] = []
    suppressed = 0

    for identity, items in grouped.items():
        items = sorted(items, key=lambda item: item.line)
        if identity not in baseline:
            new_findings.extend(items)
            continue

        allowed, _reason = baseline[identity]
        suppressed += min(allowed, len(items))

        if len(items) > allowed:
            errors.append(
                f"new occurrence(s) of baselined value: {identity[0]} {identity[1]} {identity[2]} "
                f"({len(items)} now, baseline allows {allowed})"
            )
            new_findings.extend(items[allowed:])

    for identity in baseline:
        if identity not in grouped:
            errors.append(
                f"stale baseline entry: {identity[0]} {identity[1]} {identity[2]}"
            )

    return new_findings, errors, suppressed


def write_baseline(path: Path, findings: Sequence[Finding]) -> None:
    """Rewrite the baseline, preserving reasons for surviving identities."""
    existing, _ = load_baseline(path)
    grouped: dict[tuple[str, str, str], int] = defaultdict(int)
    for finding in findings:
        grouped[(finding.rule, finding.path, finding.key)] += 1

    lines = [
        "# Convention gate baseline: reviewed legacy exceptions.",
        "# Format: rule<TAB>path<TAB>key<TAB>count<TAB>reason",
        "# The list only shrinks: fix the finding and delete its row.",
    ]

    default_reasons = {
        "magic-number": "legacy: unnamed numeric literal",
        "magic-string": "legacy: unnamed wire literal",
        "outcome-shape": "legacy: outcome shape",
        "monadic-result": "legacy: non-monadic signature",
    }

    for identity in sorted(grouped):
        rule, relative, key = identity
        reason = existing.get(identity, (0, default_reasons[rule]))[1]
        lines.append(f"{rule}\t{relative}\t{key}\t{grouped[identity]}\t{reason}")

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _self_test_mask() -> list[str]:
    failures: list[str] = []
    cases = [
        ("line comment", "var x = 30; // 99\n", "99", False),
        ("block comment", "/* 99 */ var x = 30;\n", "99", False),
        ("string content", 'var s = "todo.completed";\n', "todo.completed", False),
        ("verbatim string", 'var s = @"todo.completed";\n', "todo.completed", False),
        ("raw string", 'var s = """todo.completed""";\n', "todo.completed", False),
        ("code visible", "var x = 30;\n", "30", True),
    ]

    for name, source, needle, visible in cases:
        masked, _ = mask_source(source)
        if (needle in masked) != visible:
            failures.append(f"self-test mask: {name}")

    return failures


def _self_test_rules() -> list[str]:
    failures: list[str] = []
    cases = [
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

    for rule, relative, source, expected in cases:
        findings = [item for item in scan_text(relative, source) if item.rule == rule]
        if len(findings) != expected:
            failures.append(
                f"self-test rule: {rule} {source.strip()!r} expected {expected}, "
                f"got {len(findings)}"
            )

    return failures


def _self_test_outcome_and_members() -> list[str]:
    failures: list[str] = []

    good_outcome = """public static class XOutcome
{
    public static class Failure
    {
        public static Error Nope => Error.NotFound(code: Codes.Nope, message: "n");
    }

    public static class Codes
    {
        public const string Nope = "X.Nope";
    }
}
"""
    bad_outcome = "public static class XOutcome { public static class Failure { } }\n"
    inline_error = 'var e = Error.NotFound(code: "X.Nope", message: "n");\n'
    bad_service = 'public Task<string> LoadAsync() => Task.FromResult("x");\n'
    good_service = 'public Task<Result<string>> LoadAsync() => Task.FromResult(Result<string>.Ok("x"));\n'

    checks = [
        ("good outcome", "src/XOutcome.cs", good_outcome, "outcome-shape", 0),
        ("bad outcome", "src/XOutcome.cs", bad_outcome, "outcome-shape", 1),
        ("inline error", "src/X.Handler.cs", inline_error, "outcome-shape", 1),
        ("bad service", "src/X.Service.cs", bad_service, "monadic-result", 1),
        ("good service", "src/X.Service.cs", good_service, "monadic-result", 0),
    ]

    for name, relative, source, rule, expected in checks:
        findings = [item for item in scan_text(relative, source) if item.rule == rule]
        if len(findings) != expected:
            failures.append(
                f"self-test shape: {name} expected {expected} {rule}, got {len(findings)} "
                f"({[item.key for item in findings]})"
            )

    return failures


def _self_test_baseline() -> list[str]:
    failures: list[str] = []

    suppress = (("magic-number", "a.cs", "30"), (1, "legacy"))
    baseline = {suppress[0]: suppress[1]}
    findings = [Finding("magic-number", "a.cs", 1, "30")]
    new, errors, _ = apply_baseline(findings, baseline)
    if new or errors:
        failures.append("self-test baseline: suppression failed")

    findings = [
        Finding("magic-number", "a.cs", 1, "30"),
        Finding("magic-number", "a.cs", 2, "30"),
    ]
    new, errors, _ = apply_baseline(findings, baseline)
    if len(new) != 1 or not any("new occurrence" in error for error in errors):
        failures.append("self-test baseline: excess occurrence not reported")

    new, errors, _ = apply_baseline([], baseline)
    if not any("stale baseline entry" in error for error in errors):
        failures.append("self-test baseline: stale row not reported")

    return failures


def run_self_tests() -> int:
    """Run the embedded parsing and rule fixtures."""
    failures: list[str] = []
    failures.extend(_self_test_mask())
    failures.extend(_self_test_rules())
    failures.extend(_self_test_outcome_and_members())
    failures.extend(_self_test_baseline())

    if failures:
        for failure in failures:
            print(failure, file=sys.stderr)
        print(f"self-test: {len(failures)} failed", file=sys.stderr)
        return 1

    print("self-test: 23 passed")
    return 0


def main(argv: Sequence[str] | None = None) -> int:
    """Run the checker."""
    parser = argparse.ArgumentParser(description="Enforce repository conventions.")
    parser.add_argument("--baseline", type=Path, default=None)
    parser.add_argument("--write-baseline", action="store_true")
    parser.add_argument("--self-test", action="store_true")
    arguments = parser.parse_args(argv)

    if arguments.self_test:
        return run_self_tests()

    baseline_path = arguments.baseline
    if baseline_path is None:
        override = os.environ.get("CONVENTION_BASELINE")
        baseline_path = Path(override) if override else DEFAULT_BASELINE

    findings = scan_repository()

    if arguments.write_baseline:
        write_baseline(baseline_path, findings)
        print(f"convention baseline written: {os.path.relpath(baseline_path, ROOT)} ({len(findings)} findings)")
        return 0

    baseline, parse_errors = load_baseline(baseline_path)
    new_findings, ratchet_errors, suppressed = apply_baseline(findings, baseline)
    errors = parse_errors + ratchet_errors

    if new_findings or errors:
        print("convention check failed", file=sys.stderr)
        for finding in sorted(new_findings, key=lambda item: (item.path, item.line, item.rule)):
            print(
                f"  {finding.rule:15} {finding.path}:{finding.line}  {finding.key}",
                file=sys.stderr,
            )
        for error in errors:
            print(f"  {error}", file=sys.stderr)
        return 1

    print(f"convention check passed (0 new findings, {suppressed} baselined)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
