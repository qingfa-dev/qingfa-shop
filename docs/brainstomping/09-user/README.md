# 09 — User Documentation

Guides for the people who use resys-eshop: storefront customers and back-office administrators, plus the published API reference and support material.

**Links:** [Admin onboarding](./01-admin-guide/01-onboarding.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area is the reader-facing side of the documentation set. It turns the requirements, design, and
architecture produced elsewhere in `docs/` into task-oriented guides: how to sign in and work in the
admin back-office, how customers shop, how to call the public and admin APIs, and where to find
answers when something goes wrong.

Every guide follows the same shape — a one-line purpose, a links block, concentrated how-to sections,
and an `Open Questions` section for details that are genuinely undecided. Guides cite the owning
architecture or design file so a reader can go deeper; when this area and the spec disagree, the spec
wins.

The design spec is at `11-spec/README.md`; the admin front-end is
Spec §9, the API surface is Spec §7. The admin design is `04/02-01` … `04/02-03`; the admin HTTP
contract is `03/04-02`; the customer design is `04/01-01` … `04/01-04`.

---

## Subfolder Index

| Subfolder | Purpose | Files |
|---|---|---|
| `01-admin-guide` | Back-office operator guide: onboarding and session, catalog, orders, fulfillment and returns, settings. | [01-onboarding.md](./01-admin-guide/01-onboarding.md) · [02-catalog.md](./01-admin-guide/02-catalog.md) · [03-orders.md](./01-admin-guide/03-orders.md) · [04-fulfillment-returns.md](./01-admin-guide/04-fulfillment-returns.md) · [05-settings.md](./01-admin-guide/05-settings.md) |
| `02-customer` | Storefront customer guide: register/login, browse, cart, checkout, orders, wishlist, reviews. | [01-guide.md](./02-customer/01-guide.md) |
| `03-api` | Published REST reference mirroring the public and admin contracts. | [01-auth.md](./03-api/01-auth.md) · [02-catalog.md](./03-api/02-catalog.md) · [03-cart-checkout.md](./03-api/03-cart-checkout.md) · [04-account.md](./03-api/04-account.md) · [05-admin-api.md](./03-api/05-admin-api.md) · [06-errors.md](./03-api/06-errors.md) |
| `04-support` | FAQ and troubleshooting for common questions and failures. | [01-faq.md](./04-support/01-faq.md) · [02-troubleshooting.md](./04-support/02-troubleshooting.md) |

Every file in this area is linked from the subfolder index above; prose elsewhere cites it by its
identifier (for example `09/02-01`, `09/03-01` … `09/03-06`, `09/04-01`, `09/04-02`).

---

## How to Read This Area

1. **Administrators start at `09/01-01`.** It explains the cookie session, the store switcher, and the
   `customer`/`admin` roles before any feature work.
2. **Then follow the task.** Catalog is `09/01-02`; orders are `09/01-03`; fulfillment, inventory, and
   returns are `09/01-04`; shipping, tax, promotions, stores, and users are `09/01-05`.
3. **Customers use `09/02-01`.** It follows the browse → product → cart → three-step checkout →
   confirmation journey and the account area.
4. **API consumers use `09/03-01` … `09/03-06`.** These mirror the public `/api/v1` and admin
   `/api/admin` contracts endpoint for endpoint.
5. **Support uses `09/04-01` and `09/04-02`.** The FAQ answers policy questions; troubleshooting maps
   common failures to resolutions.

---

## Admin Guide at a Glance

The five admin-guide files are split by concern so a reader loads only what they need.

| File | Answers |
|---|---|
| [01-onboarding.md](./01-admin-guide/01-onboarding.md) | How do I sign in? Where is the token stored? How do I pick a store? What can each role do? |
| [02-catalog.md](./01-admin-guide/02-catalog.md) | How do I create a product, variants, options, and images? How do I price per store and currency? How does the category tree work? |
| [03-orders.md](./01-admin-guide/03-orders.md) | How do I find an order? What does the detail view show? Which status transitions are legal? How do refunds and manual adjustments work? |
| [04-fulfillment-returns.md](./01-admin-guide/04-fulfillment-returns.md) | How do I fulfill and ship? How do I adjust stock and transfer between locations? How do I authorize, receive, restock, and settle a return or exchange? |
| [05-settings.md](./01-admin-guide/05-settings.md) | How do I configure shipping, tax, and promotions? How do I set store currencies and locales? How do I manage users, roles, and store assignments? |

The full journeys behind these steps are `04/02-01` (flows) and `04/02-02` (screens); the HTTP calls
are `03/04-02`.

---

## Conventions

- **Admin calls `/api/admin`.** The admin is a headless HTTP client, not an embedded UI (ADR 0004).
  Its JWT lives in an httpOnly cookie, and the selected store travels in the `X-Store` header.
- **Roles are `customer` and `admin`**, plus per-store `AdminStoreAssignment`. A store admin acts only
  inside assigned stores; a platform admin spans all stores and owns platform-level routes.
- **Money is typed.** Every amount is a `{ amount, currency }` value in the store currency; the admin
  formats but never converts.
- **State changes go through state machines.** An illegal transition is `409` and changes nothing.
- **Open details live in `Open Questions`**, never in placeholder markers.

---

## Related Areas

- `01` (requirements) — the functional requirements and admin stories the guides realize; the admin
  stories are `01/04-03`.
- `03` (architecture) — the REST contracts (`03/04-01`, `03/04-02`), authentication (`03/04-03`),
  order lifecycle (`03/02-03`), inventory (`03/02-06`), tax (`03/02-05`), and promotions
  (`03/02-04`).
- `04` (design) — the storefront and admin journeys, screens, and components the guides describe.
- `00` (governance) — ADR 0004 locks the headless, JWT-auth front-end architecture.

---

## Open Questions

None at the area level. Unresolved details are stated in the owning guide under its `## Open
Questions` section. The entries that affect this index are the admin store-switcher persistence and
the possible third role, owned by `09/01-01` and `09/01-05`.
