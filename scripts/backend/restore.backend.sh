#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"

# Enforce: CI restores against the committed lock files so a dependency change
# cannot slip through a local-only restore.
if [[ "${CI:-}" == "true" ]]; then
  trace_exec dotnet restore "$ROOT/QingFa.Shop.slnx" --locked-mode
fi

trace_exec dotnet restore "$ROOT/QingFa.Shop.slnx"
