#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"

trace docker compose config >/dev/null
trace docker compose -f compose.yaml -f compose.deploy.yaml config >/dev/null
echo "infra.config: ok"
