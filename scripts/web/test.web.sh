#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
package="${1:?usage: test.web.sh <workspace-package-name> [unit|integration|all]}"
scope="${2:-all}"

case "$scope" in
  all) targets=() ;;
  unit) targets=("src/__tests__/unit") ;;
  integration) targets=("src/__tests__/integration") ;;
  *)
    echo "test.web.sh: unknown scope '$scope' (expected unit, integration, or all)" >&2
    exit 2
    ;;
esac

cd "$ROOT/app"

# Enforce: run once and exit. The app's test:unit script starts Vitest in watch
# mode for local development, which would hang a CI or Make invocation.
trace_exec pnpm --filter "$package" exec vitest run "${targets[@]}"
