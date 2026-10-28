#!/usr/bin/env python3
"""Enforce per-assembly line and branch coverage from a Cobertura report.

Usage: coverage-check.common.py <report.xml> <lines-floor> <branches-floor> <assembly>...

Assemblies are read from the Cobertura <package name="..."> entries. A missing
gated assembly fails the check so a renamed project cannot silently escape the
gate. Floors are skipped when the report has no lines or no branch points for
that assembly (for example a contract project made only of records).

Generated code is excluded from the measured totals: EF Core migrations,
design-time factories, and compiler/source-generator output under obj/.
"""

from __future__ import annotations

import os
import sys
import xml.etree.ElementTree as ET

# Exclude: generated or tool-authored files are not behaviour this gate owns.
EXCLUDED_SOURCE_FRAGMENTS = (
    "/Migrations/",
    ".DesignTime.Factory.cs",
    "/obj/",
)


def is_excluded(filename: str) -> bool:
    normalized = filename.replace("\\", "/")
    return any(fragment in normalized for fragment in EXCLUDED_SOURCE_FRAGMENTS)


def totals(package: ET.Element) -> tuple[int, int, int, int]:
    """Count authored line and branch points for one assembly.

    Cobertura repeats lines under <methods>; only the <class>/<lines> block is
    authoritative. Branch totals come from the
    `condition-coverage="50% (1/2)"` attribute.
    """
    lines = covered_lines = branches = covered_branches = 0

    for cls in package.findall("classes/class"):
        if is_excluded(cls.get("filename", "")):
            continue

        class_lines = cls.find("lines")
        if class_lines is None:
            continue

        for line in class_lines.findall("line"):
            lines += 1
            if int(line.get("hits", "0")) > 0:
                covered_lines += 1

            condition = line.get("condition-coverage", "")
            if "(" not in condition:
                continue

            fraction = condition.rsplit("(", 1)[1].rstrip(")")
            covered, total = fraction.split("/", 1)
            branches += int(total)
            covered_branches += int(covered)

    return covered_lines, lines, covered_branches, branches


def rate(covered: int, valid: int) -> float:
    return 100.0 * covered / valid if valid else 100.0


def main(argv: list[str]) -> int:
    if len(argv) < 4:
        print(
            "usage: coverage-check.common.py <report.xml> <lines-floor> "
            "<branches-floor> <assembly>...",
            file=sys.stderr,
        )
        return 2

    report, lines_floor_raw, branches_floor_raw, *gated = argv[1:]
    lines_floor = float(lines_floor_raw)
    branches_floor = float(branches_floor_raw)

    try:
        root = ET.parse(report).getroot()
    except (OSError, ET.ParseError) as error:
        print(f"coverage: cannot read {report}: {error}", file=sys.stderr)
        return 2

    packages = {
        package.get("name", ""): package
        for package in root.findall("packages/package")
    }

    print(f"Coverage thresholds: line {lines_floor:.0f}% / branch {branches_floor:.0f}%")
    print("Excluded from measurement: EF migrations, design-time factories, obj/ generated code")

    branch_exempt = set(
        filter(None, os.environ.get("COVERAGE_BRANCH_EXEMPT", "").split())
    )
    print(f"{'Assembly':32} {'Line':>8} {'Branch':>8}  Result")

    failures: list[str] = []
    for name in gated:
        package = packages.get(name)
        if package is None:
            failures.append(f"{name}: no coverage data")
            print(f"{name:32} {'-':>8} {'-':>8}  MISSING")
            continue

        line_covered, line_valid, branch_covered, branch_valid = totals(package)
        line = rate(line_covered, line_valid)
        branch = rate(branch_covered, branch_valid)

        line_ok = line_valid == 0 or line + 1e-9 >= lines_floor
        exempt_from_branch = name in branch_exempt
        branch_ok = exempt_from_branch or branch_valid == 0 or branch + 1e-9 >= branches_floor
        line_text = f"{line:7.1f}%" if line_valid else "     n/a"
        branch_text = (
            "     n/a"
            if branch_valid == 0 or exempt_from_branch
            else f"{branch:7.1f}%"
        )

        if line_ok and branch_ok:
            print(f"{name:32} {line_text:>8} {branch_text:>8}  PASS")
            continue

        failures.append(name)
        reasons = []
        if not line_ok:
            reasons.append(f"line {line:.1f}% < {lines_floor:.0f}%")
        if not branch_ok:
            reasons.append(f"branch {branch:.1f}% < {branches_floor:.0f}%")
        print(f"{name:32} {line_text:>8} {branch_text:>8}  FAIL ({', '.join(reasons)})")

    total_line_covered, total_line_valid, total_branch_covered, total_branch_valid = (
        0,
        0,
        0,
        0,
    )
    for package in packages.values():
        line_covered, line_valid, branch_covered, branch_valid = totals(package)
        total_line_covered += line_covered
        total_line_valid += line_valid
        total_branch_covered += branch_covered
        total_branch_valid += branch_valid

    total_line = rate(total_line_covered, total_line_valid)
    total_branch = rate(total_branch_covered, total_branch_valid)
    print(
        f"\nTotal (all assemblies, report only): "
        f"line {total_line:.1f}% / branch {total_branch:.1f}%"
    )

    if failures:
        print(f"\ncoverage: FAILED for {len(failures)} gated assemblies")
        return 1

    print("\ncoverage: ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
