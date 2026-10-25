#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
GATEWAY_URL="${GATEWAY_URL:-http://localhost:5050}"

wait_for() {
  local name="$1" url="$2"
  echo "==> waiting for $name at $url"
  for _ in $(seq 1 90); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      return 0
    fi
    sleep 2
  done
  echo "smoke: $name did not become ready at $url" >&2
  (cd "$ROOT" && docker compose logs --tail=80 "$name") >&2 || true
  return 1
}

check() {
  local url="$1" want="$2" code
  code="$(curl -s -o /dev/null -w '%{http_code}' "$url")"
  printf '  %-40s %s\n' "$url" "$code"
  if [[ "$code" != "$want" ]]; then
    echo "smoke: $url returned $code, want $want" >&2
    exit 1
  fi
}

check_body() {
  local url="$1" needle="$2" body
  body="$(curl -fsS "$url")"
  if [[ "$body" != *"$needle"* ]]; then
    echo "smoke: $url did not contain '$needle'" >&2
    exit 1
  fi
  printf '  %-40s contains %s\n' "$url" "$needle"
}

wait_for gateway "$GATEWAY_URL/alive"

check "$GATEWAY_URL/alive"       200
check "$GATEWAY_URL/api/time"    200
check "$GATEWAY_URL/"            200
check "$GATEWAY_URL/admin/"      200

check_body "$GATEWAY_URL/"       "QingFa.Shop · Storefront"
check_body "$GATEWAY_URL/admin/" "QingFa.Shop · Admin"

echo "smoke: ok"
