#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"
trace docker compose exec -T postgres pg_isready -U qingfa -d db
trace docker compose exec -T redis redis-cli ping
trace docker compose exec -T rabbitmq rabbitmq-diagnostics -q check_port_connectivity
