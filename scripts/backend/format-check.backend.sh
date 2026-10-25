#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"

# Validate: callers pass --include paths so a group checks only its own projects.
if [ "$#" -eq 0 ]; then
  trace_exec dotnet format "$ROOT/QingFa.Shop.slnx" --verify-no-changes --verbosity minimal
fi

trace_exec dotnet format "$ROOT/QingFa.Shop.slnx" --verify-no-changes --verbosity minimal --include "$@"
