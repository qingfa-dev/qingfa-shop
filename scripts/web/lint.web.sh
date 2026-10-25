#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
package="${1:?usage: lint.web.sh <workspace-package-name>}"

cd "$ROOT/app"

# Enforce: these are Vue SFCs, so type-checking must run vue-tsc (the app's
# type-check script) rather than plain tsc against a project-references config.
trace_exec pnpm --filter "$package" type-check
