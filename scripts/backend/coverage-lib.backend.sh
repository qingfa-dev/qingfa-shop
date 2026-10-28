#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
. "$ROOT/scripts/common/coverage.common.sh"

# Reuse the suite definition so coverage and plain tests cannot drift.
coverage_collect lib "$ROOT/scripts/backend/test-lib.backend.sh"
coverage_finalize unit "$COVERAGE_GATED_UNIT" "$COVERAGE_DOTNET_RAW/lib.xml"
