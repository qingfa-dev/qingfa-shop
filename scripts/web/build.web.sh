#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
package="${1:?usage: build.web.sh <workspace-package-name>}"

cd "$ROOT/app"
trace_exec pnpm --filter "$package" build
