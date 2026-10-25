#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
app="${1:?usage: format-check.web.sh <workspace-app-directory>}"

cd "$ROOT/app"
trace_exec pnpm exec prettier --check "$app"
