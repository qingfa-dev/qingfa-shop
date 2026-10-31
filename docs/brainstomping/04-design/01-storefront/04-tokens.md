# Storefront Design Tokens

Tailwind CSS v4 CSS-first theme tokens for the storefront — color, typography, spacing, radii, and breakpoints — and how each token maps to a utility class.

**Links:** [Flows](./01-flows.md) · [Wireframes & Pages](./02-wireframes-pages.md) · [Components](./03-components.md) · [i18n & money](../../03-architecture/03-data/03-i18n-money.md) · [Design index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the single source of truth for the storefront visual system. It targets **Tailwind CSS
v4**, which is **CSS-first**: there is no `tailwind.config.js`; the theme is declared with
`@theme` in one stylesheet and Tailwind generates the matching utilities. Tokens are the only
place raw color and type values are written; components use utilities or semantic tokens, never
literal values.

The theme entry point is `web/storefront/app/assets/css/main.css`:

```css
@import "tailwindcss";
@import "./theme.css";      /* the @theme blocks below */
```

`theme.css` is owned by `web/shared` so the admin app can reuse the same scale with its own
PrimeVue theme. Tokens are named for their role, not their value, so a rebrand is a token edit, not
a component sweep.

---

## Color

Colors use the `oklch()` color space for perceptually even steps. Three families ship: `brand`
(interactive/identity), `neutral` (surface and text), and four semantic families (`success`,
`warning`, `danger`, `info`).

```css
@theme {
  /* Brand — identity and interactive emphasis */
  --color-brand-50:  oklch(0.97 0.02 275);
  --color-brand-100: oklch(0.94 0.04 275);
  --color-brand-200: oklch(0.89 0.07 275);
  --color-brand-300: oklch(0.82 0.11 275);
  --color-brand-400: oklch(0.72 0.15 275);
  --color-brand-500: oklch(0.62 0.18 275);
  --color-brand-600: oklch(0.54 0.19 275);  /* primary action */
  --color-brand-700: oklch(0.47 0.17 275);
  --color-brand-800: oklch(0.40 0.14 275);
  --color-brand-900: oklch(0.35 0.11 275);
  --color-brand-950: oklch(0.25 0.08 275);

  /* Neutral — surfaces, borders, text */
  --color-neutral-50:  oklch(0.985 0.002 250);
  --color-neutral-100: oklch(0.967 0.003 250);
  --color-neutral-200: oklch(0.929 0.006 250);
  --color-neutral-300: oklch(0.869 0.010 250);
  --color-neutral-400: oklch(0.704 0.020 250);
  --color-neutral-500: oklch(0.554 0.024 250);
  --color-neutral-600: oklch(0.446 0.022 250);
  --color-neutral-700: oklch(0.372 0.020 250);
  --color-neutral-800: oklch(0.279 0.016 250);
  --color-neutral-900: oklch(0.208 0.014 250);
  --color-neutral-950: oklch(0.129 0.012 250);

  /* Semantic — state and feedback */
  --color-success-500: oklch(0.65 0.16 150);
  --color-success-600: oklch(0.55 0.14 150);
  --color-warning-500: oklch(0.78 0.15 75);
  --color-warning-600: oklch(0.68 0.15 60);
  --color-danger-500:  oklch(0.63 0.21 25);
  --color-danger-600:  oklch(0.55 0.20 25);
  --color-info-500:    oklch(0.65 0.13 230);
  --color-info-600:    oklch(0.56 0.12 230);
}
```

`brand-600` is the primary action color; `success` marks in-stock and placed orders, `warning`
marks backordered/low stock, `danger` marks declines, validation errors, and out-of-stock, and
`info` marks neutral notices such as a pending review.

### Semantic color layer

Components consume **semantic** tokens so dark mode is a token override. The semantic layer is
declared as CSS variables in `:root`, overridden under `[data-theme="dark"]`, and exposed to
Tailwind with `@theme inline`.

```css
:root {
  --surface: var(--color-neutral-50);
  --surface-raised: #ffffff;
  --surface-sunken: var(--color-neutral-100);
  --text: var(--color-neutral-900);
  --text-muted: var(--color-neutral-600);
  --text-on-brand: #ffffff;
  --border: var(--color-neutral-200);
  --primary: var(--color-brand-600);
  --primary-hover: var(--color-brand-700);
  --focus-ring: var(--color-brand-500);
}
[data-theme="dark"] {
  --surface: var(--color-neutral-950);
  --surface-raised: var(--color-neutral-900);
  --surface-sunken: oklch(0.16 0.012 250);
  --text: var(--color-neutral-100);
  --text-muted: var(--color-neutral-400);
  --border: var(--color-neutral-800);
  --primary: var(--color-brand-400);
  --primary-hover: var(--color-brand-300);
  --focus-ring: var(--color-brand-300);
}
@theme inline {
  --color-surface: var(--surface);
  --color-surface-raised: var(--surface-raised);
  --color-surface-sunken: var(--surface-sunken);
  --color-text: var(--text);
  --color-text-muted: var(--text-muted);
  --color-text-on-brand: var(--text-on-brand);
  --color-border: var(--border);
  --color-primary: var(--primary);
  --color-primary-hover: var(--primary-hover);
  --color-focus-ring: var(--focus-ring);
}
```

---

## Typography

```css
@theme {
  --font-sans: "Inter var", system-ui, -apple-system, "Segoe UI", sans-serif;
  --font-display: "Fraunces", Georgia, "Times New Roman", serif;
  --font-mono: "JetBrains Mono", ui-monospace, "SFMono-Regular", monospace;

  --text-xs: 0.75rem;    --text-xs--line-height: 1rem;
  --text-sm: 0.875rem;   --text-sm--line-height: 1.25rem;
  --text-base: 1rem;     --text-base--line-height: 1.5rem;
  --text-lg: 1.125rem;   --text-lg--line-height: 1.75rem;
  --text-xl: 1.25rem;    --text-xl--line-height: 1.75rem;
  --text-2xl: 1.5rem;    --text-2xl--line-height: 2rem;
  --text-3xl: 1.875rem;  --text-3xl--line-height: 2.25rem;
  --text-4xl: 2.25rem;   --text-4xl--line-height: 2.5rem;

  --font-weight-regular: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;

  --tracking-tight: -0.015em;
  --tracking-normal: 0em;
  --tracking-wide: 0.025em;
}
```

- `font-display` is reserved for page and section headings; body copy uses `font-sans`.
- Product SKUs, order numbers, and coupon codes use `font-mono`.
- Prices use `font-sans` with `tabular-nums` so digits align in carts and order summaries.

---

## Spacing, Radii, Shadows, and Containers

```css
@theme {
  --spacing: 0.25rem;               /* base step; p-4 = 1rem */
  --spacing-section: 4rem;          /* vertical rhythm between page sections */
  --spacing-gutter: 1rem;           /* grid gutter */

  --radius-sm: 0.25rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;             /* cards, inputs, buttons */
  --radius-xl: 1rem;
  --radius-2xl: 1.5rem;
  --radius-full: 9999px;            /* pills, avatars, badges */

  --shadow-sm: 0 1px 2px 0 oklch(0 0 0 / 0.05);
  --shadow-card: 0 1px 3px 0 oklch(0 0 0 / 0.08), 0 1px 2px -1px oklch(0 0 0 / 0.08);
  --shadow-overlay: 0 10px 30px -10px oklch(0 0 0 / 0.25);

  --container-content: 72rem;       /* default page width */
  --container-wide: 90rem;          /* full-bleed listing width */
}
```

---

## Breakpoints

Tailwind v4 breakpoints are declared with `--breakpoint-*` and generate `sm:` … `2xl:` variants.
The system is **mobile-first**: base styles target phones, and each variant widens the layout.

```css
@theme {
  --breakpoint-sm: 40rem;   /* 640px  — large phone / small tablet */
  --breakpoint-md: 48rem;   /* 768px  — tablet; FilterSidebar appears inline */
  --breakpoint-lg: 64rem;   /* 1024px — desktop; 3–4 up product grid */
  --breakpoint-xl: 80rem;   /* 1280px — wide desktop */
  --breakpoint-2xl: 96rem;  /* 1536px — max content width */
}
```

Layout rules tied to breakpoints: the product grid is `grid-cols-2` at base, `md:grid-cols-3`, and
`lg:grid-cols-4`; `FilterSidebar` is a drawer below `md` and inline at `md` and up; the cart and
checkout two-column summary collapses to a single column below `lg`.

---

## Token → Utility Mapping

Tailwind generates a utility family for every token namespace. Components reference the utility,
never the raw token or a literal value.

| Token | Example value | Generated utilities |
|---|---|---|
| `--color-brand-600` | `oklch(0.54 0.19 275)` | `bg-brand-600`, `text-brand-600`, `border-brand-600`, `ring-brand-600`, `fill-brand-600` |
| `--color-primary` | `var(--primary)` | `bg-primary`, `text-primary`, `border-primary`, `hover:bg-primary-hover` |
| `--color-text-muted` | `var(--text-muted)` | `text-text-muted` |
| `--font-display` | `"Fraunces", …` | `font-display` |
| `--text-lg` | `1.125rem` | `text-lg` (with the paired line height) |
| `--font-weight-semibold` | `600` | `font-semibold` |
| `--spacing` | `0.25rem` | `p-4`, `m-2`, `gap-6`, `w-64` (all multiples of the step) |
| `--spacing-section` | `4rem` | `py-section`, `mt-section` |
| `--radius-lg` | `0.75rem` | `rounded-lg` |
| `--shadow-card` | two-layer shadow | `shadow-card` |
| `--container-content` | `72rem` | `max-w-content` |
| `--breakpoint-md` | `48rem` | `md:` variant on any utility |

### Worked examples

| Wireframe need | Utility expression |
|---|---|
| Primary button | `bg-primary text-text-on-brand rounded-lg px-4 py-2 font-medium hover:bg-primary-hover focus-visible:ring-2 focus-visible:ring-focus-ring` |
| Product card surface | `bg-surface-raised border border-border rounded-lg shadow-card` |
| Muted caption | `text-sm text-text-muted` |
| In-stock badge | `bg-success-500/10 text-success-600 rounded-full px-2 py-0.5 text-xs font-semibold` |
| Out-of-stock badge | `bg-danger-500/10 text-danger-600 rounded-full px-2 py-0.5 text-xs font-semibold` |
| Price | `text-lg font-semibold tabular-nums` |
| Page section | `max-w-content mx-auto px-4 py-section` |

---

## Usage Rules

1. **No literal values in components.** Colors, sizes, radii, and shadows come from tokens or
   utilities; a hex or `oklch()` in a component is a defect.
2. **Prefer semantic over scale in components.** Use `bg-surface`/`text-text-muted`/`border-border`
   for chrome; use `brand`/semantic scale steps only for intentional emphasis.
3. **Dark mode is a token swap.** Components never branch on theme; `[data-theme="dark"]` overrides
   the semantic variables.
4. **Mobile-first.** Write base styles for the smallest breakpoint and layer `sm:`/`md:`/`lg:` up.
5. **Focus is visible.** Every interactive element carries a `focus-visible:ring-2` ring in
   `focus-ring`; never remove the outline without a replacement.
6. **Contrast.** Body text (`neutral-900` on `neutral-50`) and primary actions (`#ffffff` on
   `brand-600`) meet WCAG AA; a new color pairing is checked before use.
7. **Shared with admin.** `theme.css` lives in `web/shared`; the admin app consumes the same tokens
   with PrimeVue, so a token change affects both front-ends by design.

---

## Open Questions

1. Whether the storefront ships a user-facing theme toggle or follows `prefers-color-scheme` only is
   a store-setting detail owned by `01/02-12`; the token layer supports both.
2. The display and body font choices are placeholders for the first store's branding and are
   finalized with the store branding config; the token names are stable regardless of family.
3. The exact focus-ring offset and motion/easing tokens are added during storefront implementation
   if accessibility or animation needs require them; no component depends on a missing token.
