#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"

# Initialize: both SPAs are members of one pnpm workspace, so a single frozen
# install at the workspace root covers both and keeps one committed lock file.
cd "$ROOT/app"
trace_exec pnpm install --frozen-lockfile
