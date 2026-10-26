#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"

trace_exec dotnet test --project "$ROOT/tests/Integration/AppHost.Integration.Tests/AppHost.Integration.Tests.csproj" \
  -c "${CONFIGURATION:-Release}" --no-build
