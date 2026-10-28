#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
report="${1:?usage: coverage-check.common.sh <cobertura.xml>}"

# Validate: keep the parser dependency-free and available on every runner that
# already builds this repo (ubuntu-latest and the dev container ship python3).
export COVERAGE_BRANCH_EXEMPT="${COVERAGE_BRANCH_EXEMPT-Contract}"
read -ra gated <<<"${COVERAGE_GATED-Core Application Contract Infrastructure}"
exec python3 "$ROOT/scripts/common/coverage-check.common.py" \
  "$report" \
  "${COVERAGE_LINES:-90}" \
  "${COVERAGE_BRANCHES:-80}" \
  "${gated[@]}"
