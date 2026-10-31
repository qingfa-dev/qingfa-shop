#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
. "$ROOT/scripts/common/coverage.common.sh"

# Report-only here: the authoritative Infrastructure gate runs in
# coverage.report, where unit and integration raw files are merged.
coverage_collect infra "$ROOT/scripts/infra/test.infra.sh"
coverage_finalize infra "" "$COVERAGE_DOTNET_RAW/infra.xml"
