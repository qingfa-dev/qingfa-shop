#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"

trace_exec dotnet build "$ROOT/QingFa.Shop.slnx" -c "${CONFIGURATION:-Release}" --no-restore
