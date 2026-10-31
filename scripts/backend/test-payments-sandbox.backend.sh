#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
PROJECT_DIR="$ROOT/tests/Integration/Api.Integration.Tests"

# Validate: the sandbox suite is opt-in. Without a provider secret it skips cleanly
# instead of failing, so CI never depends on live credentials.
if [ -z "${STRIPE__SECRETKEY:-}" ] && [ -z "${PAYPAL__CLIENTSECRET:-}" ]; then
  echo "skipped: set STRIPE__SECRETKEY (+ STRIPE__WEBHOOKSECRET) or" >&2
  echo "         PAYPAL__CLIENTID + PAYPAL__CLIENTSECRET + PAYPAL__WEBHOOKID" >&2
  echo "         to run the provider sandbox checks" >&2
  exit 0
fi

if [ -n "${STRIPE__SECRETKEY:-}" ]; then
  # Validate: an enabled provider must have every secret, or the host fails at start-up.
  if [ -z "${STRIPE__WEBHOOKSECRET:-}" ]; then
    echo "STRIPE__WEBHOOKSECRET is required when STRIPE__SECRETKEY is set (from 'stripe listen')." >&2
    exit 2
  fi

  export Payments__Providers__Stripe__Enabled=true
  export Payments__Providers__Stripe__SecretKey="$STRIPE__SECRETKEY"
  export Payments__Providers__Stripe__WebhookSecret="$STRIPE__WEBHOOKSECRET"
fi

if [ -n "${PAYPAL__CLIENTSECRET:-}" ]; then
  # Validate: PayPal verifies webhooks against a configured id, so all three are required.
  if [ -z "${PAYPAL__CLIENTID:-}" ] || [ -z "${PAYPAL__WEBHOOKID:-}" ]; then
    echo "PAYPAL__CLIENTID and PAYPAL__WEBHOOKID are required when PAYPAL__CLIENTSECRET is set." >&2
    exit 2
  fi

  export Payments__Providers__PayPal__Enabled=true
  export Payments__Providers__PayPal__ClientId="$PAYPAL__CLIENTID"
  export Payments__Providers__PayPal__ClientSecret="$PAYPAL__CLIENTSECRET"
  export Payments__Providers__PayPal__WebhookId="$PAYPAL__WEBHOOKID"
  export Payments__Providers__PayPal__Environment=Sandbox
fi

trace_exec dotnet test \
  --project "$PROJECT_DIR/Api.Integration.Tests.csproj" \
  -c Release \
  --filter-class '*PaymentsSandboxTest'
