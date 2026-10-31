#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
. "$ROOT/scripts/common/coverage.common.sh"

# Report-only: AppHost is orchestration glue and stays outside the gated set.
coverage_collect apphost "$ROOT/scripts/backend/test-apphost.backend.sh"
coverage_finalize apphost "" "$COVERAGE_DOTNET_RAW/apphost.xml"
