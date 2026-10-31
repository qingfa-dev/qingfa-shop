#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
. "$ROOT/scripts/common/coverage.common.sh"

# Merge every raw report collected by the backend, apphost, and infra jobs and
# enforce the full gated set. Runs after the CI artifacts are downloaded.
shopt -s nullglob
raw_files=("$COVERAGE_DOTNET_RAW"/*.xml)
if [ ${#raw_files[@]} -eq 0 ]; then
  echo "coverage.report: no raw reports under $COVERAGE_DOTNET_RAW" >&2
  exit 1
fi

coverage_finalize coverage "$COVERAGE_GATED_ALL" "${raw_files[@]}"
