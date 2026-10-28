#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
. "$ROOT/scripts/common/coverage.common.sh"

coverage_collect api "$ROOT/scripts/backend/test-api.backend.sh"
coverage_finalize api "Web.Api" "$COVERAGE_DOTNET_RAW/api.xml"
