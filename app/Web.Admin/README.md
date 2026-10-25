# Web.Admin

Operations and merchandising SPA. _feature · api consumer_

## Summary

Web.Admin is the internal console for running the shop: catalogue, orders, and
configuration screens for operators. It shares the same stack and layout as
[`../Web.Storefront/`](../Web.Storefront/README.md) — Vue 3, Vite, TypeScript,
StyleX — but ships as a separate app with its own port and router so the two
surfaces can evolve independently.

## Contains

| Path                                  | Role       | Notes                                             |
| ------------------------------------- | ---------- | ------------------------------------------------- |
| `src/App.vue`                         | entrypoint | Root component                                    |
| `src/main.ts`                         | entrypoint | Bootstraps the app and router                     |
| `src/router/`                         | internal   | Route table (`HomeView`, `NotFoundView`)          |
| `src/views/`                          | feature    | Route-level components                            |
| `src/api/`                            | adapter    | HTTP layer — `health.ts` demonstrates the pattern |
| `src/composables/`                    | shared     | Reusable Vue composables                          |
| `src/__tests__/`                      | test       | Vitest + `@vue/test-utils` specs                  |
| `vite.config.ts` / `vitest.config.ts` | meta       | Port **5174**, StyleX unplugin, `@` alias         |
| `public/`                             | resource   | Static assets                                     |

## Boundaries

In scope: operator-facing UI, client-side routing, admin API calls.

Out of scope:

- Customer storefront → [`../Web.Storefront/`](../Web.Storefront/README.md)
- Backend endpoints and authorization decisions → [`../Web.Api/`](../Web.Api/)
- Workspace-level scripts → [`../package.json`](../package.json)

## Assumptions

- Node >= 24 and pnpm 12.8.1; install from the workspace root
  (`make admin.install` or `pnpm install` in `app/`).
- `API_BASE_URL` is injected by [`../AppHost/`](../AppHost/).
- Dev server runs on **:5174**; do not collide with Web.Storefront.

## Conventions

- API access goes through `src/api/`; components do not fetch ad hoc.
- Styles are StyleX (`*.styles.ts`), not scoped CSS blocks.

## Related

- Parent: [`../../README.md`](../../README.md)
- Sibling: [`../Web.Storefront/README.md`](../Web.Storefront/README.md)
- Tests: `make admin.test`

## Notes

- Sourced from the create-vue template; README rewritten to the folder
  convention in [`../../guide/folder-readme-guide.md`](../../guide/folder-readme-guide.md).
