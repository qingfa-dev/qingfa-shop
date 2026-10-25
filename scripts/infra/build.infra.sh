#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
REGISTRY="${REGISTRY:-qingfa-shop}"
TAG="${TAG:-local}"
SERVICES="${SERVICES:-gateway api shopfront admin postgres redis}"

build() {
  local name="$1" dockerfile="$2"
  echo "==> $REGISTRY/$name:$TAG"
  trace docker build --load -f "$ROOT/$dockerfile" -t "$REGISTRY/$name:$TAG" "$ROOT"
}

for service in $SERVICES; do
  case "$service" in
    gateway)    build gateway    app/Gateway/Dockerfile ;;
    api)        build api        app/Web.Api/Dockerfile ;;
    shopfront)  build shopfront  app/Web.Storefront/Dockerfile ;;
    admin)      build admin      app/Web.Admin/Dockerfile ;;
    postgres)   build postgres   app/Infrastructure/Postgres/Dockerfile ;;
    redis)      build redis      app/Infrastructure/Redis/Dockerfile ;;
    *)
      echo "build: unknown service '$service' (want: gateway api shopfront admin postgres redis)" >&2
      exit 1
      ;;
  esac
done
