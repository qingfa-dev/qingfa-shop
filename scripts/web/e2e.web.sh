#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
app="${1:?usage: e2e.web.sh <workspace-app-directory> <workspace-package-name>}"
package="${2:?usage: e2e.web.sh <workspace-app-directory> <workspace-package-name>}"
e2e_dir="$ROOT/app/$app/e2e"

# Validate: e2e is opt-in. Without a runner config and at least one spec the
# target is a no-op, so `make test.all` stays green until a browser suite lands.
spec="$(find "$e2e_dir" -type f \( -name '*.spec.*' -o -name '*.test.*' \) -print -quit 2>/dev/null || true)"
if ! compgen -G "$ROOT/app/$app/playwright.config.*" >/dev/null || [ -z "$spec" ]; then
  echo "e2e.web: no Playwright suite for $app; skipped"
  exit 0
fi

cd "$ROOT/app"
trace_exec pnpm --filter "$package" exec playwright test
