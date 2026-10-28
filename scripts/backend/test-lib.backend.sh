#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
CONFIG="${CONFIGURATION:-Release}"

# Initialize: the .NET 10 SDK runs MTP projects through `dotnet test --project`
# (see the `test.runner` entry in global.json). Order is base layer first, with
# architecture rules last; test.dotnet.unit mirrors this list.
projects=(
  "Unit/Core.Unit.Tests/Core.Unit.Tests.csproj"
  "Unit/Contract.Unit.Tests/Contract.Unit.Tests.csproj"
  "Unit/Application.Unit.Tests/Application.Unit.Tests.csproj"
  "Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj"
  "Architecture.Tests/Architecture.Tests.csproj"
)

# Validate: MTP exits 8 when a project contains zero tests. A scaffolded suite
# is skipped by inspecting its sources, so a real test failure is never masked.
has_tests() {
  local project_dir="$1"
  grep -rqE '\[(Fact|Theory)' "$project_dir" \
    --include='*.cs' --exclude-dir=bin --exclude-dir=obj
}

for project in "${projects[@]}"; do
  echo "==> $project"
  project_dir="$ROOT/tests/$(dirname "$project")"
  if ! has_tests "$project_dir"; then
    echo "  no tests found; skipped"
    continue
  fi

  trace dotnet test --project "$ROOT/tests/$project" -c "$CONFIG" --no-build
done
