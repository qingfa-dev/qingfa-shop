#!/usr/bin/env bash
# Sourced by coverage scripts. Keeps output paths, thresholds, and the
# gated-assembly list in one place so Make and CI cannot drift.

COVERAGE_ROOT="${COVERAGE_ROOT:-$ROOT/artifacts/coverage}"
COVERAGE_DOTNET_DIR="$COVERAGE_ROOT/dotnet"
COVERAGE_DOTNET_RAW="$COVERAGE_DOTNET_DIR/raw"

# Defaults are overridable from Make (`COVERAGE_LINES=…`) and from CI env.
COVERAGE_LINES="${COVERAGE_LINES:-90}"
COVERAGE_BRANCHES="${COVERAGE_BRANCHES:-80}"

# Validate: only production assemblies owned by this repository are gated.
# ServiceDefaults, Gateway, and AppHost are orchestration glue and stay
# report-only; test assemblies never appear here.
COVERAGE_GATED_UNIT="Core Application Contract Infrastructure"
COVERAGE_GATED_ALL="$COVERAGE_GATED_UNIT Web.Api"

# Contract is a records/DTO assembly: its four compiler-generated branch points
# are not meaningful behaviour. Gate its lines, but not its branches.
COVERAGE_BRANCH_EXEMPT="${COVERAGE_BRANCH_EXEMPT-Contract}"
export COVERAGE_BRANCH_EXEMPT

# Run a test suite under dotnet-coverage and keep the raw Cobertura file.
# Usage: coverage_collect <name> <command> [args...]
coverage_collect() {
  local name="$1"
  shift
  mkdir -p "$COVERAGE_DOTNET_RAW"
  trace dotnet tool run dotnet-coverage collect \
    -f cobertura \
    -o "$COVERAGE_DOTNET_RAW/$name.xml" \
    "$@"
}

# Merge raw files into one report and enforce per-assembly thresholds.
# Usage: coverage_finalize <report-name> <gated-assemblies> <raw-file>...
coverage_finalize() {
  local name="$1"
  local gated="$2"
  shift 2
  local output="$COVERAGE_DOTNET_DIR/$name.cobertura.xml"

  mkdir -p "$COVERAGE_DOTNET_DIR"
  trace dotnet tool run dotnet-coverage merge \
    -f cobertura \
    -o "$output" \
    "$@"

  COVERAGE_GATED="$gated" \
    "$ROOT/scripts/common/coverage-check.common.sh" "$output" \
    | tee "$COVERAGE_DOTNET_DIR/$name.summary.txt"
}
