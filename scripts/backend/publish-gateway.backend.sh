#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
CONFIG="${CONFIGURATION:-Release}"
OUT="$ROOT/artifacts/publish"

trace dotnet publish "$ROOT/app/Gateway/Gateway.csproj" -c "$CONFIG" -o "$OUT/gateway" --no-build

echo "publish: $OUT/gateway"
