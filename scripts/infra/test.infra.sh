#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
PROJECT_DIR="$ROOT/tests/Integration/Infrastructure.Integration.Tests"

# Validate: MTP exits 8 when a project contains zero tests. The scaffold has no
# infrastructure specs yet, so skip it by source inspection rather than by
# ignoring an exit code that could also hide a real failure.
if ! grep -rqE '\[(Fact|Theory)' "$PROJECT_DIR" \
  --include='*.cs' --exclude-dir=bin --exclude-dir=obj; then
  echo "infra.test: no tests found; skipped"
  exit 0
fi

trace_exec dotnet test --project "$PROJECT_DIR/Infrastructure.Integration.Tests.csproj" \
  -c "${CONFIGURATION:-Release}" --no-build
