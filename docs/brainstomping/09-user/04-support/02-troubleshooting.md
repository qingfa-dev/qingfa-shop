# Troubleshooting

Resolve the failures shoppers hit most on the storefront: a failed payment, a cart that will not update, a missing order email, and a rejected coupon.

**Links:** [Customer Guide](../02-customer/01-guide.md) · [Checkout Requirements](../../01-requirements/02-functional/03-checkout.md) · [Payments](../../01-requirements/02-functional/05-payments.md) · [Public REST API](../../03-architecture/04-integration/01-rest-public.md) · [Email & Files](../../03-architecture/04-integration/06-email-files.md) · [Double-Charge Runbook](../../08-operations/03-incidents/04-runbook-double-charge.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This guide maps four common storefront symptoms to resolution steps: a payment that fails, a
cart that does not reflect a change, an order confirmation email that never arrives, and a coupon
that is rejected. Every fix is expressed in shopper actions first and support actions second. The
underlying rules are in `01/02-03` (checkout), `01/02-05` (payments), and `03/04-01` (public
API), and the step-by-step happy path is in `09/02-01`.

The storefront is a REST client of `/api/v1`; failures come back as RFC 9457 problem details. The
status code usually tells you which layer to look at before you read the body.

| Status | Usually means | First thing to check |
|---|---|---|
| `401` | Session or cart token missing or expired. | Sign in again; let the cart token be re-issued. |
| `402` | The payment gateway declined or errored. | Read the reason on the payment step; retry. |
| `409` | A step was attempted out of order or a stale state was submitted. | Revisit the previous checkout step. |
| `422` | A field failed server validation. | Correct the highlighted input. |
| `429` | Too many attempts from this address. | Wait for the `Retry-After` window. |

---

## Failed Payment

**Symptom.** The payment step shows a decline or an error, or the order is not placed after
submitting payment.

**Causes to rule out**

1. The card was declined by the issuer (insufficient funds, wrong expiry, blocked card).
2. The postal code or 3-D Secure step failed.
3. The gateway itself returned an error rather than a clean decline.
4. The amount changed because the cart or a coupon was edited after the payment step was opened.

**Resolution**

1. Confirm the storefront stayed on `/checkout/payment` with the cart, address, and delivery
   selection intact. A decline must not empty the cart (`FR-CHK-007`).
2. Enter the card details again in the provider-hosted form; do not paste a saved card that has
   expired.
3. Retry the same method once. Each retry creates a new payment attempt; the failed attempt is
   never mutated, so a support agent can inspect both (`FR-CHK-007`).
4. If it fails again, choose a different enabled method (for example PayPal or a different card).
5. If the cart or coupon was edited, return to the delivery step, re-select a method, and re-enter
   payment so the amount matches the freshly computed total (`FR-CHK-009`).
6. If the provider shows no charge but the storefront shows a placed order, do not retry blindly;
   open the order in history and confirm its payment state first. Duplicate-charge handling is in
   `08/03-04`.

**Do not** ask the shopper to submit payment repeatedly for the same cart. Confirmation and
charges are idempotent, but repeated new attempts against a live order should be avoided
(`FR-CHK-010`).

---

## Cart Not Updating

**Symptom.** A quantity change, line removal, or coupon addition does not stick, or the totals
look stale.

**Causes to rule out**

1. A change was made in one tab while another tab held an older cart view.
2. The anonymous cart token was lost after clearing cookies or switching devices.
3. The line points at a variant that became unavailable, so the change was rejected.
4. A coupon change was applied but totals were not re-read.

**Resolution**

1. Reload `/cart` so the storefront re-reads `GET /api/v1/cart` from the server. Totals are
   always re-derived server-side and are never cached from the client (`FR-CHK-009`).
2. Make the change in a single tab. If two tabs are open, close the stale one and reload the
   other.
3. If lines vanished after clearing cookies, the cart token was lost and a new empty cart was
   created. There is no way to recover an anonymous cart without its token; sign in and re-add the
   items.
4. If a line cannot be changed because the variant is now out of stock and not backorderable,
   remove it or choose a different variant (`FR-INV-009`).
5. For a coupon, remove and re-enter it on the cart (`DELETE` then `POST /api/v1/cart/coupon`)
   and confirm the discount adjustment appears before proceeding (`FR-CHK-008`).
6. If a `409` is returned, the cart was modified concurrently; reload and repeat the change once.

---

## Missing Order Email

**Symptom.** The order is placed and visible, but the confirmation or receipt email never arrives.

**Causes to rule out**

1. The email address was mistyped on the order, or a guest used an address they do not read.
2. The message was filtered to spam, promotions, or a bulk folder.
3. The store's mail sending is delayed or retrying.
4. The order is still `awaiting_payment` (an offline method not yet confirmed), so no confirmation
   has been sent yet.

**Resolution**

1. Confirm the order was actually placed. Open `/account/orders` (signed in) or use
   `/orders/lookup` with the order number and the email used (`FR-CHK-005`).
2. Check the order's email address on the order detail. If it is wrong, support must correct the
   contact address; the order email is not editable by the shopper.
3. Ask the shopper to check spam, promotions, and any provider-level filters, and to search for
   the sender by name.
4. If the order is `awaiting_payment`, wait for the offline payment to be marked paid; the
   confirmation is queued on the fulfillment transition (`FR-PAY-003`, `FR-PAY-004`).
5. If delivery software is suspect, support checks the email queue and delivery logs; the
   transactional templates and sender configuration are described in `03/04-06`.
6. The order itself is never dependent on the email. Even if the message is lost, the order,
   payment, and shipment all continue normally.

---

## Coupon Rejected

**Symptom.** A code that should work returns an error and no discount appears.

**Causes to rule out**

1. The coupon is unknown or mistyped (codes are matched exactly).
2. It is outside its start/end window, or has reached its total or per-user usage limit.
3. It is not eligible for the current items, shipping method, or channel.
4. It was applied in the cart but expired or hit its limit before confirm, so it was revalidated
   and dropped at checkout.

**Resolution**

1. Re-enter the code exactly, with no leading or trailing spaces, on the cart or at checkout
   (`POST /api/v1/cart/coupon`). Validation is server-side; the browser never decides the discount
   (`FR-CHK-008`).
2. Read the rejection reason. An expired or usage-exhausted coupon is rejected without changing
   the total (`FR-PRM-001`).
3. Confirm the coupon's eligibility rules against the cart: a coupon rule needs a matching code, a
   product rule needs matching lines, an order-total rule needs the minimum, and a shipping-method
   rule constrains delivery (`FR-PRM-002`).
4. If the discount disappeared at the confirm step, the coupon was invalidated after being
   previewed. Remove it, re-apply if it is still valid, and re-check the recomputed total
   (`FR-CHK-009`).
5. If the shopper believes the coupon is valid and in-window, support verifies the promotion's
   window and usage counters in the admin back-office before escalating.
6. Removing the coupon restores the total to its pre-coupon value; the shopper can continue
   without it (`FR-CHK-008`).

---

## When to Escalate

Escalate to support rather than troubleshooting further when:

- The provider dashboard shows a capture but the order has no successful payment record.
- Two orders or two charges exist for one cart (start from `08/03-04`).
- A signed-in customer cannot complete checkout because every enabled method fails with `402`.
- The email queue shows the confirmation as sent but it is absent across multiple orders for
  different recipients.

Include the order number, the email used, the payment method, the approximate time, and the
status code and response body from the failing request so support can trace it.

---

## Open Questions

1. Whether guest order lookup returns shipment tracking in the same response is fixed by the
   account API in `03/04-01`; this guide tells the shopper to check the confirmation page for
   tracking when it is present.
2. The exact transporter and retry policy for transactional email is owned by `03/04-06`; this
   guide assumes the confirmation is queued at confirm and re-driven from the outbox.
3. Whether the storefront surfaces the provider's raw decline text or a normalized message is a
   storefront detail owned by `04/01-01`; the resolution steps apply either way.
