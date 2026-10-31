# Plan — Requirements

Tasks 3–8: business context, functional and non-functional requirements, and user stories.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](03-governance.md) · [Next →](05-planning.md) · [Root README](../../../README.md)

---

### Task 3: Requirements — business context and area index


**Files:**
- Modify: `docs/01-requirements/README.md`
- Create: `docs/01-requirements/01-business/01-goals.md`
- Create: `docs/01-requirements/01-business/02-actors.md`
- Create: `docs/01-requirements/01-business/03-constraints.md`

**Interfaces:**
- Consumes: Spec §1, §2, §6, Appendix A.
- Produces: `01/01-01` … `01/01-03`, cited by the functional requirements (Tasks 4–6) and roadmap (`02/01`).

- [ ] **Step 1: Write the three business files**

- `01-goals.md` sections: `Purpose`, `Business Goals` (multi-store commerce, Spree parity minus CMS), `Success Criteria`, `Non-Goals` (CMS, GraphQL, Blazor, recommendation engine, mobile apps).
- `02-actors.md` sections: `Customer`, `Store Admin`, `Platform Admin`, `Guest`, `Payment/Shipping Providers` — each with responsibilities and the surfaces they use.
- `03-constraints.md` sections: `Technical` (versions from Global Constraints), `Regulatory/Payment` (no PAN storage), `Operational`, `Documentation` (docs-first, spec is canonical).

Area README becomes an index of all 4 subfolders (business, functional, nonfunctional, user-stories).

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/01-requirements && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/01-requirements
git commit -m "docs(requirements): add business goals, actors, constraints"
```

### Task 4: Requirements — functional: catalog, orders, checkout


**Files:**
- Create: `docs/01-requirements/02-functional/01-catalog.md`
- Create: `docs/01-requirements/02-functional/02-orders.md`
- Create: `docs/01-requirements/02-functional/03-checkout.md`

**Interfaces:**
- Consumes: `01/01-01` … `01/01-03`; Spec §6.
- Produces: FRs cited by `03/02-03` (order lifecycle) and `09/03-02` / `09/03-03`.

- [ ] **Step 1: Write the three functional files**

Format each as numbered FRs (`FR-CAT-001`, `FR-ORD-001`, …) with `Actor`, `Description`, `Rules`, `Acceptance Criteria`.

- `01-catalog.md` — Products (master) and variants; option types/values; taxonomy (categories); images; product search with facets; per-store price sets. Link `03/02-01`, `03/03-01`.
- `02-orders.md` — Order aggregate; states `cart → awaiting_payment → awaiting_fulfillment → shipped → completed` plus `canceled`/`returnable`; line items; ship/bill addresses; adjustments; totals computed from source + adjustments + tax; events (`OrderPlaced`, `PaymentCompleted`, `OrderShipped`). Link `03/02-03`.
- `03-checkout.md` — Three steps (delivery → payment → confirm); guest checkout; stock reservation on confirm; failure/retry; coupon entry. Link `03/02-04`, `09/03-03`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/01-requirements/02-functional && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/01-requirements/02-functional
git commit -m "docs(requirements): add catalog, orders, checkout FRs"
```

### Task 5: Requirements — functional: shipping, payments, tax, promotions


**Files:**
- Create: `docs/01-requirements/02-functional/04-shipping.md`
- Create: `docs/01-requirements/02-functional/05-payments.md`
- Create: `docs/01-requirements/02-functional/06-tax.md`
- Create: `docs/01-requirements/02-functional/07-promotions.md`

**Interfaces:**
- Consumes: `01/02-01` … `01/02-03`; Spec §6, §8.
- Produces: FRs cited by `03/02-04`, `03/02-05`, `03/04-05`.

- [ ] **Step 1: Write the four functional files**

Numbered FRs with `Actor`, `Description`, `Rules`, `Acceptance Criteria`.

- `04-shipping.md` — Shipping methods, categories, zones; pluggable calculators (flat, per-item, per-order, weight); rate selection at delivery step; free-shipping interaction with promotions. Link `03/02-02`, `03/04-05`.
- `05-payments.md` — Payment method registry; gateways (Stripe, PayPal, bank transfer, check, store credit, cash-on-delivery); payment states; refunds; store-credit ledger; idempotent charges. Link `03/04-05`, `08/03-04`.
- `06-tax.md` — Jurisdictions (country > region > city); `TaxRate` + `TaxCategory`; inclusive/exclusive per store; computed on post-promotion amounts. Link `03/02-05`.
- `07-promotions.md` — Promotions (channels, window, usage limits); rules (coupon, user, product/variant, order total, shipping method); actions (percent/amount off order or item, fixed-price item, free shipping, Nth-item free); application order; per-line clamping. Link `03/02-04`, `06/02-02`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/01-requirements/02-functional && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/01-requirements/02-functional
git commit -m "docs(requirements): add shipping, payments, tax, promotions FRs"
```

### Task 6: Requirements — functional: inventory through stores/i18n


**Files:**
- Create: `docs/01-requirements/02-functional/08-inventory.md`
- Create: `docs/01-requirements/02-functional/09-returns.md`
- Create: `docs/01-requirements/02-functional/10-accounts.md`
- Create: `docs/01-requirements/02-functional/11-wishlists-reviews.md`
- Create: `docs/01-requirements/02-functional/12-stores-i18n.md`

**Interfaces:**
- Consumes: `01/02-01` … `01/02-07`; Spec §6.
- Produces: FRs cited by `03/02-06`, `03/03-02`, `03/03-03`.

- [ ] **Step 1: Write the five functional files**

Numbered FRs with `Actor`, `Description`, `Rules`, `Acceptance Criteria`.

- `08-inventory.md` — `StockItem` per (variant, location); `available` vs `on_hand`; backorder policy; `StockMovement` ledger; reservation/shipment/return movements; no negative stock unless backorders allowed. Link `03/02-06`, `06/02-04`.
- `09-returns.md` — Return items, reasons; refund vs store credit; exchanges create new order lines; restocking. Link `03/02-03`, `06/02-05`.
- `10-accounts.md` — Identity users; roles `customer`/`admin`; per-store admin assignment; addresses; profile. Link `03/04-03`.
- `11-wishlists-reviews.md` — Wishlist entities; product reviews with rating and moderation. Link `09/02-01`.
- `12-stores-i18n.md` — `Store` entity; domain resolution; enabled currencies; supported locales; `Translatable` pattern. Link `03/03-02`, `03/03-03`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/01-requirements/02-functional && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/01-requirements/02-functional
git commit -m "docs(requirements): add inventory, returns, accounts, wishlists/reviews, stores/i18n FRs"
```

### Task 7: Requirements — non-functional


**Files:**
- Create: `docs/01-requirements/03-nonfunctional/01-performance.md`
- Create: `docs/01-requirements/03-nonfunctional/02-security.md`
- Create: `docs/01-requirements/03-nonfunctional/03-availability.md`
- Create: `docs/01-requirements/03-nonfunctional/04-i18n.md`

**Interfaces:**
- Consumes: Spec §7, §10, §12; Global Constraints.
- Produces: NFRs cited by `06/03-02` (performance budgets), `08/02-03` (alerts), `03/04-03`.

- [ ] **Step 1: Write the four NFR files**

Numbered NFRs (`NFR-PERF-001`, `NFR-SEC-001`, …) with `Description`, `Target`, `Measurement`.

- `01-performance.md` — API p95 targets, storefront LCP target, SSR budgets, catalog query limits; link `06/03-02`.
- `02-security.md` — Identity + JWT, httpOnly cookie for admin, CSRF, CORS allow-list, rate limiting, no PAN/PII in logs, audit log for admin mutations; link `03/04-03`.
- `03-availability.md` — health endpoints `/health/live` and `/health/ready`, backup/RPO/RTO targets, graceful gateway-degradation; link `08/03-02`.
- `04-i18n.md` — per-store locales, fallback to default locale, currency display, RTL note; link `03/03-03`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/01-requirements/03-nonfunctional && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/01-requirements/03-nonfunctional
git commit -m "docs(requirements): add non-functional requirements"
```

### Task 8: Requirements — user stories


**Files:**
- Create: `docs/01-requirements/04-user-stories/01-browse-buy.md`
- Create: `docs/01-requirements/04-user-stories/02-account.md`
- Create: `docs/01-requirements/04-user-stories/03-admin.md`

**Interfaces:**
- Consumes: Tasks 3–7.
- Produces: `01/04-01` … `01/04-03`, cited by `04/01-01`, `04/02-01`.

- [ ] **Step 1: Write the three story files**

Use `US-###` with `As a… I want… so that…`, `FR references`, `Acceptance Criteria`.

- `01-browse-buy.md` — browse, search/filter, view product, choose variant, add to cart, checkout, pay, receive confirmation. Link `01/02-01`, `01/02-03`.
- `02-account.md` — register/login, manage addresses, view orders, wishlist, submit review. Link `01/02-10`, `01/02-11`.
- `03-admin.md` — manage catalog, fulfill orders, adjust inventory, configure shipping/tax/promotions, manage stores/users, view reports. Link `01/02-02`, `01/02-04`–`01/02-07`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/01-requirements/04-user-stories && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/01-requirements/04-user-stories
git commit -m "docs(requirements): add user stories"
```
