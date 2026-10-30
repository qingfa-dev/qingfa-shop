#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
PROJECT_DIR="$ROOT/tests/Integration/Infrastructure.Integration.Tests"
PROJECT="$PROJECT_DIR/Infrastructure.Integration.Tests.csproj"

# Validate: MTP exits 8 when a filter matches no tests. Skip only when the broker
# specs are absent, so a renamed suite cannot silently disable the CI gate.
if ! grep -rqE 'class RabbitMq(Composition|Messaging)Test' "$PROJECT_DIR" \
  --include='*.cs' --exclude-dir=bin --exclude-dir=obj; then
  echo "test.infra.broker: no RabbitMQ tests found; skipped"
  exit 0
fi

trace_exec dotnet test --project "$PROJECT" \
  -c "${CONFIGURATION:-Release}" --no-build \
  --filter-class "*RabbitMq*"
