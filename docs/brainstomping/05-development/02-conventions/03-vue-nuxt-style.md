# Vue and Nuxt Style

The front-end conventions for both Nuxt 4 apps: directory layout, `<script setup lang="ts">`, the Composition API, Pinia store patterns, typed `useFetch`/`$fetch` calls, Tailwind utility usage, and PrimeVue usage in the admin.

**Links:** [.NET style](./01-dotnet-style.md) · [Naming](./04-naming.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Storefront tokens](../../04-design/01-storefront/04-tokens.md) · [Admin components](../../04-design/02-admin/03-components.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the front-end style contract for `web/storefront`, `web/admin`, and `web/shared`. It
fixes the Nuxt 4 directory model, the component authoring rules, state ownership, the data-fetch
path, and the styling stack. It assumes the versions in ADR 0001 and spec §4: Nuxt 4.5.x, Vue
3.5.43, TypeScript strict, Pinia, Tailwind CSS v4, PrimeVue 4.

The guiding rule: **the server owns computed values, the client renders them.** Prices, tax,
totals, stock state, and eligibility are never recomputed in the browser; a component displays what
the API returns (`03/04-01`, `03/04-02`).

---

## Nuxt 4 Directory Conventions

Nuxt 4 uses an `app/` source directory by default. Both apps follow it, and the shared workspace
package sits beside them.

```
web/
├─ storefront/
│  ├─ app/
│  │  ├─ pages/            # route components, file-based routing
│  │  ├─ components/       # presentational + route-local components
│  │  ├─ layouts/          # default.vue and checkout.vue
│  │  ├─ composables/      # useMoney, useApiV1, useCart, …
│  │  ├─ stores/           # Pinia setup stores
│  │  ├─ middleware/       # route guards (auth, store resolution)
│  │  └─ assets/css/       # main.css importing the shared Tailwind theme
│  ├─ server/              # Nitro routes / SSR data loaders that call /api/v1
│  ├─ nuxt.config.ts
│  └─ package.json
├─ admin/                  # same shape, PrimeVue 4, /api/admin
└─ shared/
   ├─ src/api/             # generated OpenAPI client + typed wrappers
   ├─ src/types/           # DTO mirrors of Resys.Shared
   ├─ src/format/          # money/currency and i18n helpers
   └─ package.json
```

- **`app/` holds UI; `server/` holds server-only code.** A client component never imports from
  `server/`.
- **Pages are thin.** A page in `app/pages/` composes components and owns the route's data fetch;
  presentational components take props and emit events (`04/01-03`, `04/02-03`).
- **Shared code lives in `web/shared`**, imported by both apps. A component reused by both
  front-ends lives there; a one-app component stays in that app.
- **`web/shared` is a build-time workspace package**, not a deployable (`03/02-01`).
- **`nuxt.config.ts` holds runtime config only** — API base URLs, per-store defaults — read through
  `useRuntimeConfig`. Secrets are never exposed to the client; only `public` keys reach the bundle
  (`05/01-02`).

---

## Components and the Composition API

- **`<script setup lang="ts">` on every component.** No Options API and no `defineComponent`
  wrapper in application code.
- **Typed props and events.** Use `defineProps<Props>()` and `defineEmits<Emits>()` with an
  interface or type; props are camelCase, events are kebab-case (`04/01-03`).
- **`withDefaults` for optional props**; a prop has a sensible default or is required, never
  undefined-by-accident.
- **One component per file**, PascalCase filename matching the component name (`ProductCard.vue`).
- **Template order is `script` then `template`** (options may precede `template` only when a
  component genuinely needs them); a second `<style>` block is rare because styling is utility
  classes.
- **Composables are `use*` functions** in `app/composables/`, returning refs and functions. Shared
  stateful logic is a composable; only cross-page state becomes a store.

```vue
<script setup lang="ts">
import type { ProductSummary } from '@resys/shared'

interface Props {
  product: ProductSummary
  currency: string
  quickAdd?: boolean
}

const props = withDefaults(defineProps<Props>(), { quickAdd: false })
const emit = defineEmits<{
  select: [slug: string]
  'quick-add': [variantId: string]
}>()

const { format } = useMoney()
</script>

<template>
  <article class="bg-surface-raised border border-border rounded-lg shadow-card">
    <button type="button" @click="emit('select', props.product.slug)">
      <span class="text-lg font-semibold tabular-nums">{{ format(props.product.price) }}</span>
    </button>
  </article>
</template>
```

- **Never recompute a server value.** A component renders `product.price` as returned; it does not
  add tax, apply a discount, or estimate shipping.
- **Format money through `useMoney`**, never by concatenating a currency symbol and a number
  (`03/03-03`).

---

## Pinia Store Patterns

Pinia owns cross-page client state: cart, auth/session, UI, and store context.

- **Setup stores only** — `defineStore('cart', () => { ... })` with refs, computed, and functions.
  Options stores are not used.
- **Stores live in `app/stores/`**, one file per store named `use<Thing>.ts`, exporting the matching
  `use<Thing>` function (`stores/useCart.ts` exports `useCart`).
- **State is private by default.** A store exposes what a component needs; raw refs are not
  mutated from outside.
- **The server remains the source of truth** for cart contents and session validity. A store caches
  the latest server response and triggers a refetch; it never derives an authoritative total.
- **Store actions call the typed API client** (below) and update state from the response. A store
  never talks to `$fetch` directly.
- **Admin store context** (`useStoreContext`) owns the selected store id and the `X-Store` header;
  the `useApiAdmin` wrapper attaches it (`04/02-03`).

```ts
import { defineStore } from 'pinia'
import type { CartDto } from '@resys/shared'

export const useCart = defineStore('cart', () => {
  const cart = ref<CartDto | null>(null)
  const itemCount = computed(() =>
    cart.value?.line_items.reduce((sum, line) => sum + line.quantity, 0) ?? 0)

  async function refresh() {
    cart.value = await api.get<CartDto>('/api/v1/cart')
  }

  return { cart, itemCount, refresh }
})
```

---

## Data Fetching with Typed Contracts

- **The generated client is the only HTTP path.** `web/shared/src/api/` is generated from the
  service's OpenAPI document (`03/04-04`); application code imports typed wrappers, not raw fetch
  calls.
- **DTO types come from `web/shared/src/types/`**, which mirror `Resys.Shared` (`03/02-02`). A
  hand-written response interface is a defect.
- **`useFetch` for route data that must be present for SSR.** It runs during server rendering,
  dedupes by key, and exposes `data`, `pending`, and `error` to the page. Use it for a product
  page, a listing, or a cart read.
- **`$fetch` inside actions, event handlers, and stores** — after a mutation, or on user intent. It
  is not used at the top level of a setup function for initial data; that is `useFetch`'s job.
- **Always pass a `key`** to `useFetch` when the URL can vary, so client and server cache
  consistently.

```ts
const route = useRoute()
const { data: product, error } = await useFetch(
  () => `/api/v1/products/${route.params.slug}`,
  { key: () => `product-${route.params.slug}`, transform: (raw) => raw as ProductDetailDto },
)
```

- **Admin calls carry credentials and headers** through `useApiAdmin`: `credentials: 'include'`,
  `X-Store`, `X-CSRF-Token` on mutations, and `If-Match` on order and stock updates (`04/02-03`,
  `03/04-02`).
- **Errors are problem details.** Map a non-2xx response to a form or inline error with
  `useProblem`; do not show a raw status code (`03/04-01`).
- **Never trust the client with a computed amount.** A `POST` sends a variant id and quantity; the
  server computes the price.

---

## Tailwind Utility Usage (Storefront)

Tailwind CSS v4 is CSS-first: there is no `tailwind.config.js`. The theme is declared with
`@theme` in `web/shared` and imported by the storefront's `app/assets/css/main.css`
(`04/01-04`).

- **Utilities only; no literal values.** Colors, spacing, radii, and shadows come from tokens or
  their generated utilities. A hex or `oklch()` value in a component is a defect (`04/01-04`).
- **Prefer semantic tokens for chrome** (`bg-surface`, `text-text-muted`, `border-border`) and the
  brand/semantic scale for intentional emphasis.
- **Mobile-first.** Write base styles for the smallest breakpoint and layer `sm:`/`md:`/`lg:` up.
- **Dark mode is a token swap.** Components never branch on theme; `[data-theme="dark"]` overrides
  the semantic variables.
- **Focus is always visible.** Interactive elements carry
  `focus-visible:ring-2 focus-visible:ring-focus-ring`.
- **Class order** is a stable convention — layout, spacing, size, typography, color, state — so
  diffs stay readable. No `@apply` in components; extract a component instead of a CSS class.

---

## PrimeVue Usage (Admin)

The admin uses PrimeVue 4 for data-dense tables and forms (`04/02-03`).

- **PrimeVue is an admin dependency, not a storefront one.** The storefront uses Tailwind and
  plain components.
- **Theming follows the shared token preset** derived from `04/01-04`; admin components do not
  invent a second palette.
- **Tables use `DataTable` in lazy/server mode.** A wrapper emits the page, sort, and filter it
  needs; the page re-reads the endpoint. Sorting and filtering are never applied client-side over a
  single server page.
- **Forms use `Form` + `FormField`.** Validation errors map from the server's problem details onto
  the matching field; the admin does not duplicate domain validation.
- **Destructive actions route through `ConfirmDialog`** and `useConfirmAction`; a mutation carries
  CSRF and concurrency headers and a `409` keeps the dialog open (`04/02-03`).
- **PrimeVue components are used as documented** — no wrapper that re-implements `DataTable`
  behavior, and no direct DOM manipulation of a PrimeVue internal.

---

## Linting and Type Checking

- **TypeScript strict** is on in both apps; `any` is avoided, and an unavoidable boundary cast is
  localized and commented.
- **Run the checks before commit** and in CI (`05/03-03`):

  ```bash
  pnpm --filter storefront lint
  pnpm --filter storefront typecheck
  pnpm --filter admin lint
  pnpm --filter admin typecheck
  ```

- **`eslint` with the Nuxt and Vue TypeScript configs owns rules** the formatter does not; a
  disabled rule is scoped to one line with a reason.
- **Prettier (or the Nuxt default formatter) owns whitespace and quotes.** The config is committed;
  formatting is not debated in review.

## Open Questions

1. The exact ESLint preset (flat config composition) is finalized when the apps are scaffolded in
   `05/03-01`; the strict-TypeScript and no-`any` rules are fixed here.
2. Whether the storefront ships a user-facing theme toggle is a store-setting detail owned by
   `01/02-12`; the token layer supports both modes (`04/01-04`).
3. Whether `useFetch` payloads are transformed through generated client functions or raw response
   shapes is a scaffold decision in `web/shared`; DTO types come from `Resys.Shared` either way.

## Links

- [.NET style](./01-dotnet-style.md)
- [Naming](./04-naming.md)
- [Solution layout](../../03-architecture/02-components/01-solution-layout.md)
- [Storefront tokens](../../04-design/01-storefront/04-tokens.md)
- [Admin components](../../04-design/02-admin/03-components.md)
- [Design spec](../../11-spec/README.md) — §4 (stack),
  §9 (front-ends).
- Storefront API: `03/04-01`; admin API: `03/04-02`; auth: `03/04-03`; OpenAPI: `03/04-04`;
  storefront components: `04/01-03`; build and run: `05/03-01`.
