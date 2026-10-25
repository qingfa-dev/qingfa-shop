#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"

# Initialize: gateway routing lives in the shared AppHost suite because those
# specs need the full orchestration. Filter to the class instead of duplicating
# the fixture in a second test project.
trace_exec dotnet test --project "$ROOT/tests/Integration/AppHost.Tests/AppHost.Tests.csproj" \
  -c "${CONFIGURATION:-Release}" --no-build \
  --filter-class AppHost.Tests.GatewayRoutingSpec
