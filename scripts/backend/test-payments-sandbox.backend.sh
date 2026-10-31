#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
PROJECT_DIR="$ROOT/tests/Integration/Api.Integration.Tests"

# Validate: the sandbox suite is opt-in. Without a provider secret it skips cleanly
# instead of failing, so CI never depends on live credentials.
if [ -z "${STRIPE__SECRETKEY:-}" ] && [ -z "${PAYPAL__CLIENTSECRET:-}" ]; then
  echo "skipped: set STRIPE__SECRETKEY or PAYPAL__CLIENTSECRET to run the provider sandbox checks" >&2
  exit 0
fi

trace_exec dotnet test \
  --project "$PROJECT_DIR/Api.Integration.Tests.csproj" \
  -c Release \
  --filter-class '*PaymentsSandboxTest'
