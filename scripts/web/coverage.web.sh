#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
package="${1:?usage: coverage.web.sh <workspace-package-name>}"

cd "$ROOT/app"

# Enforce: `vitest run --coverage` uses each app's vitest.config.ts, which owns
# the include/exclude list and the line/branch thresholds.
trace_exec pnpm --filter "$package" exec vitest run --coverage
