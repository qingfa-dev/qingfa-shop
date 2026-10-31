# Customer FAQ

Answers to the questions shoppers ask most about payment, shipping, returns, accounts, coupons, and multiple currencies.

**Links:** [Checkout Requirements](../../01-requirements/02-functional/03-checkout.md) · [Payments](../../01-requirements/02-functional/05-payments.md) · [Shipping](../../01-requirements/02-functional/04-shipping.md) · [Returns](../../01-requirements/02-functional/09-returns.md) · [Accounts](../../01-requirements/02-functional/10-accounts.md) · [Promotions](../../01-requirements/02-functional/07-promotions.md) · [Stores & i18n](../../01-requirements/02-functional/12-stores-i18n.md) · [Public REST API](../../03-architecture/04-integration/01-rest-public.md) · [User docs index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file answers policy and "how does it work" questions about shopping on the storefront. It
covers payment, shipping, returns, accounts, coupons, and multi-currency behavior. For a failure
you are actively hitting — a declined card, a cart that will not update, a missing email, or a
rejected coupon — see `09/04-02`, which maps each symptom to resolution steps.

Answers describe the platform rules in `01/02-03`, `01/02-04`, `01/02-05`, and `01/02-09`. Where
a store setting can differ, the answer says so and points to the store policy.

---

## Payment

**Which payment methods can I use?**
Only the methods the store has enabled. The platform supports Stripe, PayPal, bank transfer,
check, store credit, and cash-on-delivery, but a given store offers a subset (`FR-PAY-001`,
`FR-PAY-003`). The enabled methods appear on the payment step of checkout.

**Is my card safe?**
Yes. Card details are entered only in the payment provider's hosted interface (Stripe Elements or
the PayPal JS SDK). The platform never receives or stores the card number or CVV — only a
provider reference is recorded (`FR-CHK-003`, `FR-PAY-010`).

**When am I charged?**
The amount charged is the server-computed order total in the order currency. A card payment is
authorized and captured through the provider; store credit, bank transfer, check, and
cash-on-delivery are offline methods. An offline method may leave the order awaiting payment
confirmation until an admin marks it paid (`FR-PAY-004`).

**Can I be charged twice if I click twice?**
No. Payments and order confirmation are idempotent: each attempt carries a unique key, and
replaying it returns the original result instead of charging again (`FR-PAY-005`, `FR-CHK-010`).

**Why was my payment declined?**
A decline comes from the provider, not the platform. The reason is shown on the payment step.
Your cart, address, and delivery selection are kept and you can retry with the same method or a
different one; each retry is a new attempt (`FR-CHK-007`).

**How do I get a refund?**
Refunds are issued by support against the gateway that captured the payment and never exceed the
captured amount minus earlier refunds. A full refund marks the payment `refunded`; a partial one
`partially_refunded` (`FR-PAY-006`). See Returns below and `09/04-02` if you are waiting on one.

---

## Shipping

**How is shipping cost calculated?**
Shipping methods are defined per store and grouped into categories such as Standard and Express.
The delivery step validates your address, quotes the methods available for that destination, and
records the method you pick as a shipping adjustment on the order (`FR-SHP-001`, `FR-CHK-002`).
Rates are `Money` values in the order currency.

**Why did the shipping price change?**
Changing the address or the cart items re-quotes the available methods and clears a previous
selection, so the price you see at the delivery step always matches your current destination and
items (`FR-CHK-002`).

**Can shipping be free?**
Yes. A free-shipping promotion can reduce the selected cost to zero. It does so through a
promotion adjustment rather than by deleting the shipping charge, so the order still records the
method that was chosen (`FR-SHP-006`).

**Do you ship to my country?**
Availability is determined by the store's shipping zones. If your address is outside a served
zone, or the store has no method for it, no rates are returned for that destination.

---

## Returns

**Can I return an item?**
A return can be requested against an order that is `shipped` or `completed`. You may return only
your own orders, and the return references the original order and lines without editing them
(`FR-RET-001`).

**How much can I return?**
One return item per order line, with a quantity that never exceeds what you bought and have not
already returned. Requesting more than remains is rejected (`FR-RET-002`).

**Do I need a reason?**
Yes. Each returned item carries a reason, which the store uses to route the return and to decide
whether the stock is restocked or written off (`FR-RET-003`).

**How does the refund work for a return?**
Accepted goods are refunded through the originating payment or, if you prefer, as store credit in
the ledger. Refunds never exceed the captured amount minus prior refunds, and an exchange does not
change the original order — it creates new order lines and settles any price difference
(`FR-RET-001`, `FR-PAY-007`).

**When does the return count as complete?**
A return starts `requested` and is authorized by an admin before goods are accepted. After the
store receives and inspects the items, it restocks what is sellable and settles the refund or
credit. The status is visible under your order.

---

## Account

**Do I need an account to buy?**
No. Checkout is guest-capable when the store allows it; you supply an email for the confirmation
and receipt. A guest order can be looked up with the order number plus that email
(`FR-CHK-005`).

**Can I create an account during checkout?**
Yes. You may register on the confirm step; the in-flight order is linked to the new account
without resetting the cart or totals (`FR-ACC-005`).

**Why does my guest order not appear in my history?**
Guest orders are not listed until claimed. Sign in with the same verified email used at checkout
and the order is claimed idempotently without changing its contents or totals (`FR-ACC-008`,
`FR-ORD-011`).

**I forgot my password or my account is locked.**
Repeated failed sign-ins trigger lockout or throttling. A locked account returns `423` on
sign-in; wait for the lockout window or contact support to reset access (`FR-ACC-010`).

**Can I change the email on my account?**
Yes, but the new email must be unique and is re-verified. Changing your profile never rewrites
historical orders or the addresses captured on them (`FR-ACC-006`).

**Can I have someone else's orders in my account?**
No. A customer sees only their own orders, and an order already linked to a different account is
not claimable by another (`FR-ORD-011`, `FR-ACC-008`).

---

## Coupons

**How do I apply a coupon?**
Enter the code on the cart or at checkout. The server validates it and, if valid, applies the
promotion as a discount adjustment (`FR-CHK-008`). The browser never decides the discount amount.

**Why was my coupon rejected?**
A coupon is rejected if it is unknown, expired, outside its active window, not eligible for your
items or the channel, or has reached its total or per-user usage limit. The specific reason is
returned (`FR-CHK-008`, `FR-PRM-001`).

**Can I stack several coupons?**
Eligibility is evaluated per promotion on the current order. Each promotion may carry its own
rules and usage limits, so whether two coupons combine depends on how the store configured them
(`FR-PRM-002`).

**The discount disappeared at checkout.**
A coupon entered in the cart is a preview and is revalidated when the order is confirmed. If it
expired or its usage limit was reached in between, it is dropped rather than silently applied
(`FR-CHK-008`).

**Does a coupon change the tax?**
Tax is computed on the post-promotion amount, so applying or removing a coupon recomputes tax and
the grand total (`FR-CHK-009`, `FR-TAX-004`).

---

## Multi-Currency

**Which currency will I be charged in?**
An order is created in the store's default currency and keeps that currency for its entire
lifecycle (`FR-ORD-012`). Every amount is a `Money` value with an ISO-4217 code.

**Can I shop in a different currency?**
A store may declare several enabled currencies and a currency selector appears when it does.
Prices are returned in the resolved currency; the browser formats but never converts
(`FR-STO-003`).

**Why is a product priced in one currency but not another?**
A variant is sellable only in the currencies it has a price for. A variant priced in an enabled
currency can be bought; one priced only in a disabled currency is not sellable
(`FR-STO-003`).

**Will changing the store currency change my existing orders?**
No. Changing a store's currency does not rewrite existing orders, which stay in the currency they
were placed in. A store cannot change its currency while it has open orders in that currency
(`FR-STO-003`, `FR-ORD-012`).

**Why does my total differ from a converted amount I calculated?**
The charged total is the server-computed order total in the order currency, including adjustments
and tax. Reporting conversions use the `CurrencyRate` table and are not what you are charged
(`03/03-03`).

---

## Open Questions

1. Whether the storefront supports switching currency within a session or is fixed to the store
   default is a store-setting detail owned by `01/02-12`; the selector appears only when the store
   enables more than one currency.
2. The exact rating scale and review formatting shown next to returns and reviews is finalized in
   `04/01`; this FAQ does not restate it.
3. Whether guest order lookup returns shipment tracking in the same response is fixed by the
   account API in `03/04-01`; the confirmation page renders tracking when present.
