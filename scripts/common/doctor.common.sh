#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

fail=0

report() {
  printf '  %-8s %-14s (want %s)\n' "$1" "$3" "$2"
}

missing() {
  printf '  %-8s MISSING        (want %s)\n' "$1" "$2"
  fail=1
}

command -v dotnet >/dev/null 2>&1 && report dotnet "10.0.401" "$(dotnet --version)" || missing dotnet "10.0.401"
command -v node   >/dev/null 2>&1 && report node   ">=24"     "$(node --version)"   || missing node ">=24"
command -v pnpm   >/dev/null 2>&1 && report pnpm   "12.8.1"   "$(pnpm --version)"   || missing pnpm "12.8.1"

if docker compose version >/dev/null 2>&1; then
  report docker "compose v2" "$(docker compose version --short 2>/dev/null || echo present)"
else
  printf '  %-8s WARN           (docker compose unavailable; up/down/smoke disabled)\n' docker
fi

exit "$fail"
