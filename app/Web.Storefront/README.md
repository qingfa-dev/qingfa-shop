# Web.Storefront

Customer-facing storefront SPA. _feature · api consumer_

## Summary

Web.Storefront is the public storefront: product browsing and checkout entry
points for customers. It is a Vue 3 + Vite + TypeScript app styled with
StyleX, and it reaches the backend exclusively through the API base URL
provided by the environment — it never talks to the database or to other
services directly.

## Contains

| Path                                  | Role       | Notes                                                   |
| ------------------------------------- | ---------- | ------------------------------------------------------- |
| `src/App.vue`                         | entrypoint | Root component; `src/App.styles.ts` holds StyleX tokens |
| `src/main.ts`                         | entrypoint | Bootstraps the app and router                           |
| `src/router/`                         | internal   | Route table (`HomeView`, `NotFoundView`)                |
| `src/views/`                          | feature    | Route-level components                                  |
| `src/api/`                            | adapter    | HTTP layer — `health.ts` demonstrates the pattern       |
| `src/composables/`                    | shared     | Reusable Vue composables                                |
| `src/__tests__/`                      | test       | Vitest + `@vue/test-utils` specs                        |
| `vite.config.ts` / `vitest.config.ts` | meta       | Port **5173**, StyleX unplugin, `@` alias               |
| `public/`                             | resource   | Static assets                                           |

## Boundaries

In scope: storefront UI, client-side routing, API calls for storefront data.

Out of scope:

- Admin-only screens → [`../Web.Admin/`](../Web.Admin/README.md)
- Backend endpoints and business rules → [`../Web.Api/`](../Web.Api/)
- Workspace-level scripts → [`../package.json`](../package.json)

## Assumptions

- Node >= 24 and pnpm 12.8.1; install from the workspace root
  (`make store.install` or `pnpm install` in `app/`), not inside this folder.
- `API_BASE_URL` is injected by [`../AppHost/`](../AppHost/);
  without it, Vite proxies to `http://localhost:5080`.
- Dev server runs on **:5173**.

## Conventions

- API access goes through `src/api/`; components do not fetch ad hoc.
- Styles are StyleX (`*.styles.ts`), not scoped CSS blocks.

## Related

- Parent: [`../../README.md`](../../README.md)
- Sibling: [`../Web.Admin/README.md`](../Web.Admin/README.md)
- Tests: `make store.test`

## Notes

- Sourced from the create-vue template; README rewritten to the folder
  convention in [`../../guide/folder-readme-guide.md`](../../guide/folder-readme-guide.md).
