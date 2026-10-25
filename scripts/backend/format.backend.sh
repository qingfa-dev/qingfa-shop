#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"

# Initialize: the solution is the formatting workspace; callers pass --include
# paths so each app group formats only its own projects.
if [ "$#" -eq 0 ]; then
  trace_exec dotnet format "$ROOT/QingFa.Shop.slnx" --verbosity minimal
fi

trace_exec dotnet format "$ROOT/QingFa.Shop.slnx" --verbosity minimal --include "$@"
