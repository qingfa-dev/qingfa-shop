#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"
# Initialize: wait for every healthcheck so a following test or migration never
# races a broker that is still booting.
trace_exec docker compose up -d --wait --wait-timeout 120 postgres redis rabbitmq
