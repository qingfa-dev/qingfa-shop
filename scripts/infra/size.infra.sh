#!/usr/bin/env bash
set -euo pipefail

REGISTRY="${REGISTRY:-qingfa-shop}"
TAG="${TAG:-local}"

for name in gateway api shopfront admin postgres redis rabbitmq; do
  bytes="$(docker image inspect "$REGISTRY/$name:$TAG" --format '{{.Size}}' 2>/dev/null || echo 0)"
  awk -v name="$name" -v bytes="$bytes" \
    'BEGIN { printf "%-12s %6.1f MB\n", name, bytes / 1024 / 1024 }'
done
