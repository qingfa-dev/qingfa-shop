# Plan — User Docs

Tasks 28–30: admin guide, customer guide, and API reference.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](11-operations.md) · [Next →](13-maintenance.md) · [Root README](../../../README.md)

---

### Task 28: User docs — admin guide and area index


**Files:**
- Modify: `docs/09-user/README.md`
- Create: `docs/09-user/01-admin-guide/01-onboarding.md`
- Create: `docs/09-user/01-admin-guide/02-catalog.md`
- Create: `docs/09-user/01-admin-guide/03-orders.md`
- Create: `docs/09-user/01-admin-guide/04-fulfillment-returns.md`
- Create: `docs/09-user/01-admin-guide/05-settings.md`

**Interfaces:**
- Consumes: `04/02-01` … `04/02-03`; `03/04-02`.
- Produces: `09/01-01` … `09/01-05`.

- [ ] **Step 1: Write the five admin-guide files and the index**

- `01-onboarding.md` — Login, JWT/httpOnly-cookie session, store switcher, role overview.
- `02-catalog.md` — Create/edit products, variants, options, images, taxonomy, per-store pricing.
- `03-orders.md` — Find/filter orders, read the detail view, status transitions, refunds.
- `04-fulfillment-returns.md` — Fulfill/ship, inventory adjustments, process returns/exchanges, restock.
- `05-settings.md` — Shipping methods/zones, tax rates, promotions, store currencies/locales, users/roles.

Area README becomes an index of the 4 subfolders.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/09-user && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/09-user
git commit -m "docs(user): add admin guide"
```

### Task 29: User docs — customer guide and support


**Files:**
- Create: `docs/09-user/02-customer/01-guide.md`
- Create: `docs/09-user/04-support/01-faq.md`
- Create: `docs/09-user/04-support/02-troubleshooting.md`

**Interfaces:**
- Consumes: `04/01-01`; `01/02-11`.
- Produces: `09/02-01`, `09/04-01`, `09/04-02`.

- [ ] **Step 1: Write the three files**

- `02-customer/01-guide.md` — Register/login, browse/search, product/variant selection, cart, checkout (delivery → payment → confirm), order tracking, wishlist, reviews.
- `04-support/01-faq.md` — Payment, shipping, returns, account, coupon, multi-currency questions.
- `04-support/02-troubleshooting.md` — Common issues → resolution steps (failed payment, cart not updating, missing order email, coupon rejected).

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/09-user && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/09-user
git commit -m "docs(user): add customer guide, FAQ, troubleshooting"
```

### Task 30: User docs — API reference (must mirror architecture contracts)


**Files:**
- Create: `docs/09-user/03-api/01-auth.md`
- Create: `docs/09-user/03-api/02-catalog.md`
- Create: `docs/09-user/03-api/03-cart-checkout.md`
- Create: `docs/09-user/03-api/04-account.md`
- Create: `docs/09-user/03-api/05-admin-api.md`
- Create: `docs/09-user/03-api/06-errors.md`

**Interfaces:**
- Consumes: `03/04-01`, `03/04-02`, `03/04-03`, `03/04-04` — **the source of truth**.
- Produces: `09/03-01` … `09/03-06`. Must match Task 14 exactly (Spec §12.4 API parity).

- [ ] **Step 1: Write the six API-reference files**

- `01-auth.md` — Register/login/refresh, JWT bearer header format, admin httpOnly-cookie session.
- `02-catalog.md` — Endpoints for products, categories, search/facets (public).
- `03-cart-checkout.md` — Cart create/mutate; checkout delivery/payment/confirm; coupon.
- `04-account.md` — Orders, addresses, wishlists, profile.
- `05-admin-api.md` — `/api/admin` endpoint groups (catalog CRUD, orders, inventory, shipping/tax/promotions, stores, users, reports).
- `06-errors.md` — Error envelope, status-code table, common error codes, rate-limit responses.

Each file includes runnable `curl` examples and JSON request/response bodies copied to match `03/04-*`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/09-user/03-api && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check + API parity check**

Run the Link check; expect no `BROKEN:` lines.
Then compare each endpoint/field in `09/03-*` against `03/04-01`, `03/04-02`, `03/04-03`:
```bash
grep -rhoE '(GET|POST|PATCH|PUT|DELETE) /api/[a-z0-9/{}_-]+' docs/03-architecture/04-integration | sort -u > /tmp/api-arch.txt
grep -rhoE '(GET|POST|PATCH|PUT|DELETE) /api/[a-z0-9/{}_-]+' docs/09-user/03-api | sort -u > /tmp/api-user.txt
diff /tmp/api-arch.txt /tmp/api-user.txt && echo "API PARITY OK"
```
Expected: no diff, `API PARITY OK`. If the diff shows paths, add the missing endpoint to the correct `09/03-*` file and re-run.

- [ ] **Step 4: Commit**

```bash
git add docs/09-user/03-api
git commit -m "docs(user): add API reference mirroring architecture contracts"
```
