# Payment Gateway Runbook

Symptom, diagnosis, and mitigation for a payment gateway outage or degradation: degrading to an alternate method, the retry policy, and customer communication.

**Links:** [On-call and incident response](./01-oncall.md) · [Database runbook](./02-runbook-db.md) · [Double-charge runbook](./04-runbook-double-charge.md) · [Payment gateways](../../03-architecture/04-integration/05-payment-gateways.md) · [Alerts](../02-monitoring/03-alerts.md) · [Dashboards](../02-monitoring/02-dashboards.md) · [Telemetry](../02-monitoring/01-telemetry.md) · [Topology](../01-infrastructure/01-topology.md) · [Capacity planning](../04-scaling/01-capacity.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

A failing payment gateway must degrade checkout safely, never corrupt it. This runbook is
`08/03-03`: it takes a gateway alert, confirms whether the fault is provider-side or local, and
restores a working payment path — first by retrying transient failures under the existing policy,
then by routing new checkouts to an alternate method. The gateway contract, idempotency rules, and
retry classification are owned by `03/04-05`; the customer-facing guarantee is NFR-AVAIL-005
(`01/03-03`); declaration and comms are `08/03-01`.

The single rule: **no order is ever marked paid without a confirmed capture.** When the provider's
outcome is unknown, the payment is reconciled before any order advances (`FR-PAY-004`,
`FR-CHK-004`).

---

## Entry Alerts

| Alert (`08/02-03`) | Signal | First runbook section |
|---|---|---|
| `PaymentSuccessRateLow` | Captured+authorized ÷ total < 90% over 5 min for a gateway. | [Gateway outage](#symptom-gateway-outage). |
| `PaymentSuccessRateDegraded` | Same signal < 97% over 15 min. | [Gateway degradation](#symptom-gateway-degradation). |
| `GatewayCircuitOpen` | Circuit open for > 1 min. | [Gateway outage](#symptom-gateway-outage). |
| `GatewayLatencyHigh` | Gateway p95 above the configured timeout budget for 10 min. | [Gateway degradation](#symptom-gateway-degradation). |
| `GatewayRetryableErrorSpike` | Retryable errors > 5× the trailing 1 h baseline for 10 min. | [Gateway degradation](#symptom-gateway-degradation). |
| `ReconciliationBacklog` | Oldest unresolved unknown-outcome payment > 30 min. | [Unknown-outcome reconciliation](#unknown-outcome-reconciliation). |

Related runbooks: `08/03-01` (declaration, escalation, comms), `08/03-02` (database), and
`08/03-04` (duplicate charge). The Payment Success board in `08/02-02` is the shared view.

---

## Triage — First Five Minutes

1. **Confirm the gateway.** Use the `gateway` filter on the Payment Success board to separate a
   single provider from a platform-wide failure. One gateway failing is a provider incident; all
   gateways failing usually points at the network path or the API host, not the providers.
2. **Check the circuit.** `resys.payment.circuit.open` tells you whether the adapter has already
   tripped its breaker. An open circuit means calls are being rejected fast, not hanging.
3. **Classify the failure.** Distinguish retryable (timeout, provider 5xx, rate limit) from
   terminal (decline, invalid token) using `resys.payment.gateway.errors{retryable}` (`03/04-05`).
   A retryable spike is an outage; a decline spike is a shopper or issuer problem.
4. **Check the provider status page** and the provider's own incident feed before assuming a local
   fault.
5. **Check platform health.** Rule out a local cause: API saturation, a database problem
   (`08/03-02`), or a DNS/egress failure would fail every outbound call.
6. **Declare.** A single gateway outage that blocks checkout is Sev1 under `08/03-01`; degraded
   success with a working alternate method is Sev2.

---

## Symptom: Gateway Outage

**Looks like.** `GatewayCircuitOpen` and `PaymentSuccessRateLow` fire for one provider; the
adapter's circuit is open and calls fail fast; checkout returns a recoverable error to the shopper
and preserves the cart (`FR-CHK-007`).

**Diagnosis.**

1. Confirm the provider is down (status page, provider incident feed, and error codes) rather than
   a local credential or endpoint problem.
2. Verify credentials and endpoint configuration are unchanged: a rotated or expired key fails all
   calls and looks like an outage (`05/01-02`).
3. Confirm the circuit breaker is behaving as designed: open means fast-fail and protection, not a
   second bug (`03/04-05`, NFR-AVAIL-008).
4. Check whether other gateways are healthy and enabled for the affected stores, so an alternate
   path exists.

**Mitigation.**

1. **Keep the circuit open.** Do not disable the breaker to force calls into a down provider; that
   exhausts request threads and turns a payment outage into a platform outage (NFR-AVAIL-008).
2. **Degrade to an alternate method** for new checkouts (see
   [Degrade to an alternate method](#degrade-to-an-alternate-method)).
3. **Preserve the cart.** A failed attempt leaves the order in its current state with line items
   and addresses intact; the shopper retries with the alternate method (`FR-CHK-007`).
4. **Do not mark paid on an unknown outcome.** If a call timed out after the provider may have
   charged, reconcile before advancing the order (`FR-PAY-004`); this is the case `08/03-04`
   covers.
5. **Engage the provider** at escalation L5 (`08/03-01`) when the fault is provider-side, and
   record the provider incident reference on the timeline.
6. **Verify** success rate recovers above 97% and the circuit closes before restoring the primary
   method.

---

## Symptom: Gateway Degradation

**Looks like.** `PaymentSuccessRateDegraded`, `GatewayLatencyHigh`, or
`GatewayRetryableErrorSpike` fires; some attempts succeed, latency is elevated, and the retry
budget is being consumed.

**Diagnosis.**

1. Confirm whether latency is provider-side or local: compare `resys.payment.gateway.duration` with
   the API's own processing time and rule out API saturation (`08/04-01`).
2. Check the retryable-error mix: a rate-limit spike is handled by backoff; a timeout spike may
   require reducing concurrency against the provider.
3. Check the reconciliation backlog (`ReconciliationBacklog`); a growing backlog of unknown
   outcomes means the degradation is producing ambiguous results, not clean failures.
4. Confirm the failure is bounded and classified; an unclassified error is a defect in the adapter
   (`03/04-05`).

**Mitigation.**

1. **Let the retry policy work.** Transient failures retry with bounded exponential backoff and
   jitter, reusing the same idempotency key (`03/04-05`); do not manually re-drive individual
   payments.
2. **Reduce load on the provider** if latency is driven by concurrency: lower the outbound
   concurrency or shed non-essential calls, without changing the payment state machine.
3. **Watch the circuit.** If retryable errors keep climbing, the circuit will open and the incident
   becomes an outage; prepare the alternate-method step.
4. **Reconcile the backlog** continuously so unknown outcomes do not age past the Sev2 threshold
   (`08/03-03`, `03/04-05`).
5. **Verify** p95 returns inside the timeout budget and the success rate returns above 97%.

---

## Degrade to an Alternate Method

The platform ships six gateways: `stripe` (default online), `paypal` (second online),
`bank-transfer`, `check`, `store-credit`, and `cash-on-delivery` (`03/04-05`). Degrading means
routing new checkouts to a healthy enabled method while the failing one is unavailable.

1. **Pick the alternate.** Prefer the second online gateway (`paypal`) when Stripe is failing; when
   both online gateways are down, fall back to an offline method (bank transfer, check,
   cash-on-delivery) or store credit where the store and the customer are eligible
   (`FR-PAY-003`, `FR-PAY-007`, `FR-PAY-008`).
2. **Disable the failing method per store.** A payment method is store-scoped and can be disabled
   by an admin (`FR-PAY-001`); disabling it removes it from the offered methods. This is a per-store
   setting and applies to new checkouts only, not in-flight orders (`FR-CHK-011`).
3. **Confirm only enabled methods are offered.** Checkout resolves available methods from the store
   registry before the payment step (`FR-PAY-008`); verify the failing method is gone from the
   storefront payment step.
4. **Keep the cart and the shopper informed.** The payment step returns a recoverable error and the
   shopper selects the alternate method; the order is not placed while payment is incomplete
   (`FR-CHK-007`).
5. **Record the change.** Note the store, the disabled method, and the time on the incident
   timeline; re-enable the method only after the provider recovers and is verified.
6. **Watch for a double charge** when re-enabling or retrying: a shopper may have a timed-out
   attempt on the failing gateway plus a new attempt on the alternate. The same idempotency key
   returns the original outcome (`FR-PAY-005`); an actual duplicate is handled by `08/03-04`.

---

## Retry Policy

Retry behavior is fixed by `03/04-05` and is not improvised during an incident:

- **Transient** (`Retryable = true`: network timeout, provider 5xx, rate limit). Retry with
  exponential backoff and jitter for a bounded number of attempts, reusing the **same idempotency
  key** so the provider cannot create a second charge. When the budget is exhausted the attempt is
  recorded `failed` and surfaced to the caller.
- **Terminal** (`Retryable = false`: decline, invalid token, insufficient funds). No retry; the
  attempt is recorded `failed`, the order does not advance, and the shopper may start a new attempt
  with a new key (`FR-PAY-004`).
- **Unknown outcome** (timeout after the provider may have charged). Reconcile with
  `GetStatusAsync` before deciding; never assume failure and never assume success (`03/04-05`).
- **Isolation.** A failed attempt records a transaction row but advances no order state and emits
  no transition event (`03/02-03`). Notification failures never roll back a payment transition.

Do not raise retry counts or timeouts during an incident without a reviewed change: an unbounded
retry turns a provider slowdown into thread-pool exhaustion (NFR-AVAIL-008).

---

## Unknown-Outcome Reconciliation

1. Identify unknown-outcome payments from the reconciliation job and the `ReconciliationBacklog`
   signal (`08/02-03`).
2. For each, call `GetStatusAsync` against the originating gateway and compare the provider state
   with the local payment state (`03/04-05`).
3. If the provider confirms a charge, record it as captured and let the order advance normally; if
   the provider has no charge, treat the attempt as failed and let the shopper retry.
4. If the provider confirms **two** charges for one payment, stop and run `08/03-04`.
5. Keep the backlog age under the Sev2 threshold; a growing backlog is itself an incident signal.

---

## Customer Communication

- **One message.** The Communications Lead owns the status page and the checkout copy; both carry
  the same wording (`08/03-01`).
- **State the impact and the workaround.** "Card payments with <provider> are temporarily
  unavailable; choose <alternate> at payment." Browsing, cart, and account features remain
  available (NFR-AVAIL-005).
- **No order is marked paid without a confirmed capture.** Tell shoppers whose payment is pending
  that their cart is saved and no charge is confirmed until it is verified (`FR-PAY-004`).
- **No sensitive data.** Never publish or log PAN, CVV, tokens, gateway payloads, or customer PII
  (`03/04-05`, NFR-SEC-007); reference payments by id or provider reference only.
- **Resolution notice.** State that the gateway is restored, whether any pending payments were
  reconciled, and the postmortem commitment for Sev1/Sev2.

---

## Verification and Exit Criteria

- Success rate is above 97% for every enabled gateway and the circuit is closed.
- Gateway p95 is inside the configured timeout budget; retryable errors are back at baseline.
- The reconciliation backlog is below the alert threshold and no unknown outcome is unresolved.
- The disabled method has been re-enabled per store and appears in the checkout payment step.
- No order was marked paid without a confirmed capture; any duplicate is being handled by
  `08/03-04`.
- The incident is downgraded or resolved under `08/03-01`, with a postmortem for Sev1/Sev2.

---

## Escalation and Communication

- Declare per `08/03-01`: a gateway outage blocking checkout is Sev1; degraded success with a
  working alternate is Sev2.
- Escalate to L5 and engage the provider when the fault is provider-side; record the provider
  incident reference.
- Escalate to L3 (payments owner) for a credential, endpoint, or adapter-classification defect
  (`03/04-05`).

---

## Open Questions

1. Provider-side retry windows and dispute webhook coverage are finalized with `08/03-04`; the
   local retry and reconciliation rules here and in `03/04-05` are unaffected.
2. Which stores pre-enable which alternate methods is a per-store admin setting (`FR-PAY-001`);
   this runbook assumes at least one alternate is enabled for the affected stores and escalates if
   none is.
3. The exact checkout error copy and status-page templates are owned with the storefront/admin
   design docs; the requirement that the cart is preserved and the error is recoverable is fixed by
   `FR-CHK-007` and NFR-AVAIL-005.
4. The outbound-concurrency limit against a provider under degradation is a deployment value; the
   bounded-timeout and circuit-breaker requirements are fixed by NFR-AVAIL-008.
