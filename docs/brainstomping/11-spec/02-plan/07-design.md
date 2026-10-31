# Plan — Design

Tasks 16–17: storefront and admin design.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](06-architecture.md) · [Next →](08-development.md) · [Root README](../../../README.md)

---

### Task 16: Design — storefront and area index


**Files:**
- Modify: `docs/04-design/README.md`
- Create: `docs/04-design/01-storefront/01-flows.md`
- Create: `docs/04-design/01-storefront/02-wireframes-pages.md`
- Create: `docs/04-design/01-storefront/03-components.md`
- Create: `docs/04-design/01-storefront/04-tokens.md`

**Interfaces:**
- Consumes: Task 8; Spec §9; `03/04-01`.
- Produces: `04/01-01` … `04/01-04`, cited by `06/01-04` (E2E) and `09/02-01`.

- [ ] **Step 1: Write the four storefront-design files and the index**

- `01-flows.md` — Journey maps: browse → product → cart → 3-step checkout → confirmation; account; review submission. Link `01/04-01`.
- `02-wireframes-pages.md` — ASCII wireframe per page: home, category/listing (filters + pagination), product detail (variations, reviews, stock), search, cart, checkout delivery/payment/confirm, account pages, auth. Each wireframe labelled with its route.
- `03-components.md` — Component inventory (e.g. `ProductCard`, `VariantPicker`, `CartLine`, `CheckoutStepper`, `ReviewList`), each with props/events and the API calls it makes. Link `03/04-01`.
- `04-tokens.md` — Tailwind v4 CSS-first theme tokens: color scale, typography, spacing, radii, breakpoints; how tokens map to utility classes.

Area README becomes an index.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/04-design && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/04-design
git commit -m "docs(design): add storefront flows, wireframes, components, tokens"
```

### Task 17: Design — admin


**Files:**
- Create: `docs/04-design/02-admin/01-flows.md`
- Create: `docs/04-design/02-admin/02-screens.md`
- Create: `docs/04-design/02-admin/03-components.md`

**Interfaces:**
- Consumes: `04/01-04` (tokens); Spec §9, §13; `03/04-02`.
- Produces: `04/02-01` … `04/02-03`, cited by `09/01-*`.

- [ ] **Step 1: Write the three admin-design files**

- `01-flows.md` — Admin journeys: product create/edit, order fulfillment (list → detail → fulfill/ship → refund/return), inventory adjustment, shipping/tax/promotion configuration, store switch, user management. **Resolve Spec §13 Open Question 1** with an explicit decision (SSR initial shell) and rationale.
- `02-screens.md` — Screen inventory with layout notes for dashboard, catalog, orders, inventory, settings, users, reports; multi-store switcher in the shell.
- `03-components.md` — PrimeVue-based component inventory (`DataTable` wrappers, `FormLayout` patterns, `ConfirmDialog` usage) with props/events and the `/api/admin` calls each makes.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/04-design && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/04-design
git commit -m "docs(design): add admin flows, screens, components"
```
