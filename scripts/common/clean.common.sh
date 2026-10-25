#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"

trace find src app tests \
  -type d -name node_modules -prune -o \
  -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
trace rm -rf TestResults artifacts
trace rm -rf app/Web.Storefront/dist app/Web.Admin/dist

if [[ "${ALL:-0}" == "1" ]]; then
  trace rm -rf app/node_modules app/Web.Storefront/node_modules app/Web.Admin/node_modules
fi

echo "clean: build output removed"
