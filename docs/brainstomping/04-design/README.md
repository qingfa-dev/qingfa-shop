# 04 — Design

UI/UX design for resys-eshop: the storefront journeys, pages, components, and design tokens, and the index to the admin design.

**Links:** [Storefront Flows](./01-storefront/01-flows.md) · [Storefront Wireframes & Pages](./01-storefront/02-wireframes-pages.md) · [Storefront Components](./01-storefront/03-components.md) · [Storefront Design Tokens](./01-storefront/04-tokens.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area turns the front-end decisions in the design spec (Spec §9) and the behavioral contracts
in areas `01` and `03` into implementation-grade design: the user journeys each front-end must
support, the screens that realize them, the components those screens compose, and the shared
Tailwind v4 token system that styles them.

There are two front-ends, each with its own concern subfolder and numbered files (~100–300 lines
each):

- `web/storefront` — Nuxt 4.5.x (Vue 3.5.43) SSR, Tailwind CSS v4, Pinia, `@nuxtjs/i18n`; consumes
  `/api/v1` only.
- `web/admin` — Nuxt 4 + PrimeVue; consumes `/api/admin` with a JWT in an httpOnly cookie.

When a statement here conflicts with the spec, the spec wins and the design file is corrected.

---

## Subfolder Index

| Subfolder | Purpose | Files |
|---|---|---|
| `01-storefront` | Public storefront design: journeys, page wireframes, component inventory, and design tokens. | [01-flows.md](./01-storefront/01-flows.md) · [02-wireframes-pages.md](./01-storefront/02-wireframes-pages.md) · [03-components.md](./01-storefront/03-components.md) · [04-tokens.md](./01-storefront/04-tokens.md) |
| `02-admin` | Back-office design: admin journeys, screen inventory, and PrimeVue component usage. | [01-flows.md](./02-admin/01-flows.md) · [02-screens.md](./02-admin/02-screens.md) · [03-components.md](./02-admin/03-components.md) |

Every file in this area is linked from the subfolder index above; prose elsewhere cites it by its
identifier (for example `04/02-01`, `04/02-02`, `04/02-03`) and links it when a jump is useful.

---

## How to Read This Area

1. **Start with the journeys.** `04/01-01` maps browse → product → cart → three-step checkout →
   confirmation, plus account and review submission, to routes and `/api/v1` calls.
2. **See the pages.** `04/01-02` gives an ASCII wireframe per route, each labelled with its Nuxt
   path and its server-rendered data reads.
3. **Build from the inventory.** `04/01-03` lists every component with its props, events, and the
   API calls it owns or triggers, plus the Pinia stores and composables.
4. **Style with tokens.** `04/01-04` defines the Tailwind v4 CSS-first theme — color, typography,
   spacing, radii, and breakpoints — and how each token maps to a utility class.
5. **Then the admin.** `04/02-01` … `04/02-03` cover the back-office flows, screens, and
   components, and consume the shared tokens from `04/01-04`.
6. Cite a file by its identifier (`AA/BB-CC`) in prose and link it from the index above when a
   reader should jump to it.

---

## Design Stance

- **Two decoupled front-ends.** No UI lives in the API service; both apps are HTTP clients of the
  headless platform (ADR 0004).
- **Storefront is REST-only.** Every storefront call is under `/api/v1`; prices, tax, totals,
  stock, and eligibility are server-computed and never posted by the client (`03/04-01`).
- **SSR for the storefront.** Pages render on the server for SEO and first paint; interactive
  islands hydrate on the client (Spec §4, §9).
- **One token system.** Tailwind v4 CSS-first tokens live in `web/shared` and style both apps; a
  token edit is the only way a brand color or type step changes (`04/01-04`).
- **Accessible by default.** Visible focus, WCAG AA contrast, and mobile-first breakpoints are
  baseline rules, not enhancements (`04/01-04`).
- **i18n and money are typed.** Locale falls back to the store default; every amount is a `Money`
  value in the store currency (`03/03-03`).

---

## Related Areas

- `01` (requirements) — the user stories and functional requirements the designs realize;
  `01/04-01` and `01/04-02` are the storefront story sources.
- `03` (architecture) — the `/api/v1` and `/api/admin` contracts, order lifecycle, and i18n/money
  model the designs consume.
- `05` (development) — the Vue/Nuxt conventions and environment the designs are implemented against.
- `06` (verification) — the E2E plan `06/01-04` drives the journeys defined in `04/01-01` and
  `04/02-01`.
- `09` (user) — the customer guide `09/02-01` follows the storefront journeys; the admin guide
  follows the admin flows.
- `00` (governance) — ADR 0004 locks the headless, JWT-auth front-end architecture.

---

## Keeping This Area Current

- Update `04/01-01` when a journey step, route, or state transition changes, and `04/01-02` when a
  page or region is added or removed.
- Update `04/01-03` when a component's props, events, or API calls change, and `04/01-04` when a
  token is added, renamed, or remapped.
- Keep the storefront files consistent with the REST contract in `03/04-01`; a new endpoint that a
  page consumes is reflected in the component's API-call column.
- Keep `04/02-01` … `04/02-03` consistent with the shared tokens in `04/01-04` and the admin
  contract in `03/04-02`.

---

## Open Questions

None at the area level. Unresolved details are stated in the owning file under its
`## Open Questions` section; the ones that affect this index are the storefront currency/locale
switching behavior and the admin rendering mode, owned by `04/01-01` and `04/02-01` respectively.
