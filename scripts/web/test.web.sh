#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
package="${1:?usage: test.web.sh <workspace-package-name>}"

cd "$ROOT/app"

# Enforce: run once and exit. The app's test:unit script starts Vitest in watch
# mode for local development, which would hang a CI or Make invocation.
trace_exec pnpm --filter "$package" exec vitest run
