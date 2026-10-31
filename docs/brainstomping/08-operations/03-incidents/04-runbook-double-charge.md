# Double-Charge Runbook

Detect a duplicate charge by idempotency key, verify it with the gateway, refund the duplicate, correct the ledger, and prevent recurrence.

**Links:** [On-call and incident response](./01-oncall.md) · [Database runbook](./02-runbook-db.md) · [Gateway runbook](./03-runbook-gateway.md) · [Payment gateways](../../03-architecture/04-integration/05-payment-gateways.md) · [Order lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Payments requirements](../../01-requirements/02-functional/05-payments.md) · [Checkout requirements](../../01-requirements/02-functional/03-checkout.md) · [Alerts](../02-monitoring/03-alerts.md) · [Dashboards](../02-monitoring/02-dashboards.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

A duplicate charge is a data-integrity and trust incident: the customer paid twice for one order.
This runbook is `08/03-04`: it takes a `DuplicateChargeSuspected` alert or a customer report and
drives it through detection, verification, refund, ledger correction, and prevention. The
idempotency rules it relies on are owned by `03/04-05`; the payment state machine and refund
guarantees are `01/02-05` and `FR-PAY-005`/`FR-PAY-006`; declaration and comms are `08/03-01`.

The single rule: **a duplicate is refunded, never hidden.** The duplicate is verified against the
provider, refunded through the originating gateway, recorded as a transaction, and the cause is
fixed so it cannot recur. The customer is told, without exposing card or gateway data.

---

## Entry

| Trigger | Signal | Severity |
|---|---|---|
| Reconciliation job flags a suspected duplicate. | `resys.payment.duplicate_suspected` (`DuplicateChargeSuspected`). | Sev1. |
| Customer or support report of two charges for one order. | Support ticket or account complaint. | Sev1 if verified. |
| Reconciliation finds two provider charges for one payment. | Reconciliation job output. | Sev1. |

Related runbooks: `08/03-01` (declaration, escalation, comms), `08/03-02` (database), and
`08/03-03` (gateway). The Payment Success board in `08/02-02` shows the duplicate-suspicion stat and
the reconciliation backlog.

---

## Step 1 — Detect: Idempotency Key Lookup

The platform is designed so a retried or concurrent request returns the original outcome rather
than charging again (`FR-PAY-005`, `FR-CHK-010`). Detection starts from the keys that guarantee
this.

1. **Get the order and payment ids** from the alert, the customer report, or the reconciliation
   job output.
2. **Look up the idempotency key** on the payment: the key is derived from the checkout
   `Idempotency-Key` header and stored on the `Payment` row (`03/04-05`).
3. **Check the unique index.** The index `(StoreId, IdempotencyKey)` on the payment table rejects a
   duplicate insert (`03/03-01`). Two payment rows with the same key means the guard was bypassed
   or the key was not propagated — record that immediately as a defect candidate.
4. **Check for two distinct keys.** A legitimate retry uses the same key; a shopper-initiated new
   attempt after a terminal decline uses a **new** key (`FR-PAY-005`). Two keys for one order can
   still be a genuine double charge if the first attempt's outcome was unknown, not terminal.
5. **List the payment transactions.** The append-only `PaymentTransaction` rows (`03/03-01`) show
   every authorize, capture, refund, and void with its provider reference. Two capture rows with
   distinct provider references for one payment is the duplicate signature.
6. **Record the facts** on the incident timeline: order id, payment id(s), idempotency key(s),
   provider reference(s), amount(s), and timestamp(s).

---

## Step 2 — Verify via Gateway

Never refund from the local record alone: confirm what the provider actually charged.

1. **Call `GetStatusAsync`** for the payment's provider reference and read the provider's captured
   amount and refunded amount (`03/04-05`).
2. **Cross-check the provider dashboard or API** for all charges on the customer, the payment
   method token, or the order reference to find any charge the local record does not know about.
3. **Classify the finding:**
   - **One provider charge, two local rows.** A local bookkeeping defect; no customer money is
     duplicated. Correct the local rows and close the refund path.
   - **Two provider charges for one order.** A real duplicate; proceed to Step 3.
   - **One provider charge, one local capture.** No duplicate; close as a false positive and record
     why the reconciliation job flagged it.
4. **Confirm the amount and currency** of each charge against the order total; a partial duplicate
   is refunded for the duplicated amount, not necessarily the full order.
5. **Do not void a settled capture.** A captured charge is refunded, not voided; void applies only
   to an uncaptured authorization (`FR-PAY-006`).

---

## Step 3 — Refund Path

1. **Refund through the originating gateway.** A refund must use the gateway that captured the
   payment (`FR-PAY-006`); never route a Stripe capture through PayPal or vice versa.
2. **Refund only the duplicate amount.** A refund may be full or partial but never exceeds the
   captured amount minus prior refunds; the transaction re-checks that remainder inside the
   transaction (`FR-PAY-006`).
3. **Use a fresh idempotency key per refund.** Refunds use a new key, not the original charge key,
   so the refund itself is idempotent and cannot double-refund (`03/04-05`).
4. **Record the refund transaction.** A refund writes a new `PaymentTransaction` row referencing
   the original payment; it never mutates the original capture row (`FR-PAY-006`, `03/03-01`).
5. **Offline and store-credit duplicates** are handled per their method: an offline duplicate is
   recorded manually with an audit entry (`FR-PAY-006`); a store-credit duplicate is corrected by
   the ledger entry in Step 4 (`FR-PAY-007`).
6. **Write the audit entry.** Refund operations require the `admin` role plus store scope and are
   written to the audit log (`FR-PAY-009`); the incident reference goes in the audit reason.
7. **Confirm the refund settled** with the provider (`GetStatusAsync` / provider dashboard) before
   telling the customer the duplicate is returned.

---

## Step 4 — Ledger Correction

The refund returns the money; the ledger must then agree with it.

1. **Payment state.** After a full refund the payment moves to `refunded`; a partial refund moves
   it to `partially_refunded` (`FR-PAY-004`). The state change goes through the payment state
   machine, never a direct write.
2. **Order state.** A duplicate charge does not change the order's status: the order was paid once,
   and it remains in its correct state (`03/02-03`). Do not cancel or re-place an order to correct a
   payment duplicate.
3. **Store-credit ledger.** The store-credit ledger is append-only and its balance is derived
   (`FR-PAY-007`). If the duplicate was a store-credit debit, write a credit entry referencing the
   refund and the order; never edit or delete the original debit.
4. **Totals are not patched.** Order totals are computed from source + adjustments + tax and are
   never cached in one column (`FR-ORD-006`); a duplicate charge is a payment-layer fact, not an
   order-total adjustment.
5. **Reconcile the books.** Confirm the provider refund, the refund transaction, the payment state,
   and (where applicable) the ledger credit all reference the same payment and amount. A mismatch
   is not closed.

---

## Step 5 — Prevent Recurrence

A verified duplicate means a guard failed. Find and fix it before closing the incident.

1. **Idempotency key propagation.** Confirm the checkout `Idempotency-Key` header is required and
   forwarded unchanged to the provider (Stripe `Idempotency-Key`, PayPal `PayPal-Request-Id`) and
   stored on the payment row (`03/04-05`, `FR-PAY-005`). A dropped header is the most common cause.
2. **Unique index intact.** Confirm `(StoreId, IdempotencyKey)` is enforced and no path inserts a
   payment outside it (`03/03-01`).
3. **Concurrency guard.** Confirm concurrent captures for one order serialize on the payment row
   (row lock plus the unique index) so exactly one capture succeeds (`03/04-05`).
4. **Unknown-outcome handling.** Confirm a timeout reconciles through `GetStatusAsync` before a
   retry, so a possibly-successful first attempt is not re-driven (`03/04-05`).
5. **Webhook deduplication.** Confirm provider events are deduplicated by `EventId` so a redelivered
   `payment_intent.succeeded` cannot create a second capture (`03/04-05`).
6. **Reconciliation and alert.** Confirm the reconciliation job still emits `duplicate_suspected`
   and that `DuplicateChargeSuspected` pages (`08/02-03`). If the duplicate was found by a customer
   before the job, that is a detection gap to fix.
7. **Test the guard.** Add or extend an integration/chaos test that replays a confirm and a capture
   with the same key and asserts one charge (`01/03-03` measurement, `06/01-03`).
8. **Feed the postmortem.** Record the failed guard, the fix, and the detection gap as action items
   under `08/03-01`; update `08/02-02`/`08/02-03` if a panel or threshold missed it.

---

## Customer Communication

- **Own the error plainly.** Tell the customer a duplicate charge was identified and is being
  refunded, with the expected timeline from the provider.
- **State the amount and the method.** Confirm the refunded amount and that it returns to the
  original payment method; do not promise a bank posting date the provider controls.
- **No sensitive data.** Never include PAN, CVV, tokens, or gateway payloads; reference payments by
  id or provider reference (`FR-PAY-010`, NFR-SEC-007).
- **One message.** Support, the status page, and the incident channel carry the same wording from
  the Communications Lead (`08/03-01`).
- **Close the loop.** Confirm when the refund has settled and the ledger agrees.

---

## Verification and Exit Criteria

- The provider confirms exactly one net charge for the order after the refund settles.
- The refund transaction exists, references the original payment, and uses a fresh idempotency key.
- The payment state is `refunded` or `partially_refunded`; the order state is unchanged and correct.
- Any store-credit debit has a matching credit entry; the derived balance is correct.
- The failed guard is identified, fixed or ticketed with an owner, and covered by a test.
- The customer has been informed and the refund confirmed settled.
- The incident is resolved under `08/03-01`, with a postmortem for this Sev1.

---

## Escalation and Communication

- Declare a Sev1 under `08/03-01` on any confirmed or suspected duplicate charge.
- Escalate to L3 (payments owner) for adapter, idempotency, or reconciliation defects
  (`03/04-05`); escalate to L5 when the provider must act (dispute, reversal, or a provider-side
  duplicate).
- If duplicates are systemic (multiple orders, one root cause), treat it as a platform incident and
  freeze the affected checkout path until the guard is fixed.

---

## Open Questions

1. Provider-side retry windows and dispute webhook coverage are finalized with `03/04-05`; the
   local idempotency, refund, and reconciliation rules here are unaffected.
2. Whether a duplicate is detected by the reconciliation job or by a customer first is an
   operational outcome; the detection gap feeds `08/02-02`/`08/02-03` and `08/03-01`.
3. The exact support script and refund messaging template are owned with the support docs; the
   requirement to refund the verified duplicate and correct the ledger is fixed here and by
   `FR-PAY-006`.
4. Whether store credit is per store or shared across a customer's stores is owned by `03/03-02`;
   the ledger-correction rule here applies to whichever ledger the duplicate debited.
