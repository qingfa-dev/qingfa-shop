# Implementation Guide — User Experience, Edge, and Operations Layers

**Version:** 1.0
**Author model:** DeepSeek V4 Pro (high effort)
**Repository:** `QingFa.Shop`
**Date:** 2026-10-31
**Scope:** U1–U10 as defined: storefront, admin, edge/BFF contract, API contract discipline, frontend testing, developer experience, CI/CD, operations, performance, documentation.
**Audience:** developers executing the next 90-day remediation and feature plan.
**Prerequisites:** `docs/architecture/software-quality-audit.pro.md` (especially Q1, Q18, Q27–Q32, Q45) and `docs/architecture/roadmap-implementation-audit.pro.md` (Week 7 frontend, G1–G8, P0/P1 gaps).

**What NOT to build (and why):**

| Omission | Reason |
|---|---|
| BFF (Backend-For-Frontend) | Same-origin through the gateway; no cross-service aggregation needed with a modular monolith. Re-evaluate when storefront needs data from ≥3 services. |
| Micro-frontends | Single team, single SPA per persona. Adds build complexity and orchestration overhead with no current benefit. |
| SSR (Nuxt/Next) | Admin is behind auth and needs no SEO. Storefront SEO is addressable via prerendering + meta tags at build time without hydration cost. Re-evaluate when organic search drives >20% of traffic. |
| Separate worker process | All workers are `BackgroundService` inside the API. Extract `app/Web.Worker` only when scaling requires independent CPU/memory. |
| Saga / Process Manager | One database owns all state. Revisit when checkout spans independent transactional systems. |
| Pinia for server state | Server state belongs in composables wrapping typed API clients. Pinia is reserved for shared client-side state (auth session, cart contents, UI preferences). |
| CORS policy | Same-origin through the gateway. Add `AddCors` only when a second origin is explicitly justified. |
| CDC / Sharding | Outbox polling latency is acceptable at current volume. Revisit when outbox age exceeds 5 s p99. |

---

## U1. Storefront Feature Architecture

### Current State

- **`app/Web.Storefront/src/`** — `App.vue:1-29` renders a header with placeholder nav links ("Catalog", "Cart"), a `<RouterView/>`, and a footer health chip.
- **`app/Web.Storefront/src/router/index.ts:1-20`** — two routes: `/` → `HomeView`, catch-all → `NotFoundView`. No guards, no lazy-loaded feature routes.
- **`app/Web.Storefront/src/composables/useApiHealth.ts:1-23`** — single composable; calls `fetch('/health')` which reaches the **gateway's** own health endpoint, not the API (`docs/architecture/software-quality-audit.pro.md:Q45`).
- **`app/Web.Storefront/src/api/health.ts:1-6`** — hand-rolled `fetch`; no typed client, no `Result` envelope parsing, no auth header.
- **No Pinia**, no forms, no loading/error/empty states, no accessibility attributes.

### Target Design

```
src/
  api/
    client.ts              # Typed API client with Result envelope + auth interceptor
    catalog.api.ts         # Catalog endpoints typed
    cart.api.ts            # Cart endpoints typed
    ...
  composables/
    useApiHealth.ts        # Fixed: probes API through gateway readiness endpoint
    useAuth.ts             # Auth session + login/register/logout/refresh
    useCatalog.ts          # Product listing + filtering + pagination
    useCart.ts             # Cart operations
    useCheckout.ts         # Order creation + confirmation
  features/
    catalog/
      components/
        ProductCard.vue
        ProductList.vue
        ProductFilter.vue
        ProductDetail.vue
      views/
        CatalogView.vue
        ProductView.vue
    cart/
      components/
        CartItem.vue
        CartSummary.vue
      views/
        CartView.vue
    checkout/
      components/
        CheckoutForm.vue
        OrderConfirmation.vue
      views/
        CheckoutView.vue
    auth/
      components/
        LoginForm.vue
        RegisterForm.vue
      views/
        LoginView.vue
        RegisterView.vue
  router/
    index.ts              # Lazy-loaded feature routes + auth guard
  stores/
    auth.store.ts         # Pinia: auth session state (shared client state only)
    cart.store.ts          # Pinia: cart item count/badge (shared UI state)
    ui.store.ts            # Pinia: theme, sidebar, preferences
  App.vue                 # Layout shell with accessible nav + dynamic health indicator
```

### Rationale

- **Feature folders** group components, composables, types, and tests that change together. This is the standard Vue 3 convention and avoids monolithic `components/` directories.
- **Typed API client** wraps `fetch` (or `ofetch` from `ohmyfetch`) with `Result<T>` parsing, JWT attachment, and 401 → refresh → retry logic.
- **Composables as primary data layer.** Each composable owns a query/mutation lifecycle: `data`, `isLoading`, `error`, `isEmpty`. Server state is _not_ placed in Pinia — it lives in the composable's reactive refs.
- **Pinia only for shared client state** that multiple components need (auth session, cart count, UI preferences). This avoids the "two sources of truth" problem where a Pinia store and a composable both cache the same `/api/products` response.
- **Loading/error/empty states** are a first-class pattern: every feature view handles all four states (`AsyncState<'loading' | 'error' | 'empty' | 'data'>`).

### Files to Add/Modify

| Action | Path | Purpose |
|---|---|---|
| **Create** | `app/Web.Storefront/src/api/client.ts` | Typed `qingfaFetch<T>` with `Result<T>` parsing, JWT interceptor, 401 refresh |
| **Create** | `app/Web.Storefront/src/api/catalog.api.ts` | `getProducts`, `getProduct`, `searchProducts` |
| **Create** | `app/Web.Storefront/src/api/cart.api.ts` | `getCart`, `addToCart`, `updateLineItem`, `removeFromCart` |
| **Create** | `app/Web.Storefront/src/api/auth.api.ts` | `login`, `register`, `refresh`, `logout`, `getMe` |
| **Create** | `app/Web.Storefront/src/api/checkout.api.ts` | `createOrder`, `getOrder`, `confirmOrder` |
| **Modify** | `app/Web.Storefront/src/api/health.ts` | Change URL from `/health` to `/api/health` and parse structured readiness response |
| **Create** | `app/Web.Storefront/src/composables/useAuth.ts` | Login/register/logout/refresh flow; stores token in memory + `localStorage` |
| **Create** | `app/Web.Storefront/src/composables/useCatalog.ts` | Product list query with pagination, filtering, search debounce |
| **Create** | `app/Web.Storefront/src/composables/useCart.ts` | Cart CRUD with optimistic updates |
| **Modify** | `app/Web.Storefront/src/composables/useApiHealth.ts` | Use new typed health client; return structured `{ healthy: boolean, degraded: string[] }` |
| **Create** | `app/Web.Storefront/src/stores/auth.store.ts` | Pinia: `token`, `user`, `isAuthenticated`, `login`, `logout` |
| **Create** | `app/Web.Storefront/src/stores/cart.store.ts` | Pinia: `itemCount`, `badgeVisible` |
| **Modify** | `app/Web.Storefront/src/router/index.ts` | Add lazy routes for `/catalog`, `/product/:id`, `/cart`, `/checkout`, `/login`, `/register`; add `beforeEach` auth guard |
| **Create** | `app/Web.Storefront/src/features/catalog/views/CatalogView.vue` | Product grid with filter sidebar |
| **Create** | `app/Web.Storefront/src/features/catalog/views/ProductView.vue` | Product detail page |
| **Create** | `app/Web.Storefront/src/features/catalog/components/ProductCard.vue` | Card with image, name, price, "Add to Cart" |
| **Create** | `app/Web.Storefront/src/features/catalog/components/ProductFilter.vue` | Category, price range, search |
| **Create** | `app/Web.Storefront/src/features/cart/views/CartView.vue` | Cart line items + summary |
| **Create** | `app/Web.Storefront/src/features/checkout/views/CheckoutView.vue` | Shipping/payment form + order review |
| **Create** | `app/Web.Storefront/src/features/auth/views/LoginView.vue` | Login form |
| **Create** | `app/Web.Storefront/src/features/auth/views/RegisterView.vue` | Registration form |
| **Modify** | `app/Web.Storefront/src/App.vue` | Replace placeholder nav with real links; add `aria-*` attributes; add `<Suspense>` for async views |
| **Create** | `app/Web.Storefront/index.html` title/meta | SEO meta tags, structured data for products |

### Typed API Client Sketch

```typescript
// app/Web.Storefront/src/api/client.ts
import { useAuthStore } from '@/stores/auth.store'

// Shape: the backend's ApiResult<T> envelope
interface ApiResult<T> {
  isSuccess: boolean
  data?: T
  error?: { code: string; description: string }
}

// Shape: paginated response convention
interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

let refreshPromise: Promise<void> | null = null

async function qingfaFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<ApiResult<T>> {
  const auth = useAuthStore()

  // Attach JWT when authenticated
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> | undefined),
  }
  if (auth.token) {
    headers['Authorization'] = `Bearer ${auth.token}`
  }

  let res = await fetch(path, { ...options, headers })

  // 401 → refresh → retry once
  if (res.status === 401 && auth.token && !path.includes('/api/auth/refresh')) {
    if (!refreshPromise) {
      refreshPromise = auth.refresh().finally(() => { refreshPromise = null })
    }
    await refreshPromise
    if (auth.token) {
      headers['Authorization'] = `Bearer ${auth.token}`
      res = await fetch(path, { ...options, headers })
    }
  }

  const body = await res.json()
  // Normalize the backend's Result envelope
  return {
    isSuccess: body.isSuccess ?? res.ok,
    data: body.data ?? body.value ?? body,
    error: body.error ?? (res.ok ? undefined : { code: String(res.status), description: res.statusText }),
  }
}

export { qingfaFetch }
export type { ApiResult, PagedResult }
```

### Routing and Guards

```typescript
// app/Web.Storefront/src/router/index.ts
import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'home', component: () => import('@/views/HomeView.vue') },
    { path: '/catalog', name: 'catalog', component: () => import('@/features/catalog/views/CatalogView.vue') },
    { path: '/product/:id', name: 'product', component: () => import('@/features/catalog/views/ProductView.vue'), props: true },
    { path: '/cart', name: 'cart', component: () => import('@/features/cart/views/CartView.vue') },
    { path: '/checkout', name: 'checkout', component: () => import('@/features/checkout/views/CheckoutView.vue'), meta: { requiresAuth: true } },
    { path: '/login', name: 'login', component: () => import('@/features/auth/views/LoginView.vue'), meta: { guestOnly: true } },
    { path: '/register', name: 'register', component: () => import('@/features/auth/views/RegisterView.vue'), meta: { guestOnly: true } },
    { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue') },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) return { name: 'login', query: { redirect: to.fullPath } }
  if (to.meta.guestOnly && auth.isAuthenticated) return { name: 'home' }
})

export default router
```

### Sequence (U1)

1. **Fix Q1 first** (U3): gateway routes must preserve `/api` for all API groups before any frontend feature code can work through the edge.
2. **Build typed API client** (`api/client.ts`) with `Result<T>` parsing and 401 refresh.
3. **Add auth composable + Pinia auth store** so the client has a token source.
4. **Add auth feature views** (login/register) as the first user-facing feature; these are needed to test the full auth → API → token → refresh flow.
5. **Build catalog feature** with composable-driven data fetching and loading/error/empty states.
6. **Add cart composable + Pinia cart store** for the badge count.
7. **Add checkout view** with auth guard.
8. **Add accessibility**: `aria-label` on nav, `role="main"` on content, `aria-live="polite"` for dynamic health state, focus management on route change.
9. **Add SEO basics**: `<title>`, `<meta description>`, `robots.txt`, JSON-LD structured data on product pages.

### Verification (U1)

- `make store.test.unit` — every new component has a spec testing all four async states and user interactions.
- `make store.test.integration` — API-flow specs with MSW (see U5).
- `make store.build` — production bundle succeeds without warnings.
- Manual: navigate `/` → `/catalog` → `/product/1` → add to cart → `/cart` → `/checkout` (redirects to login) → login → back to checkout.
- Accessibility: `npx axe-core` or `pa11y` on each view.

---

## U2. Admin Feature Architecture

### Current State

- **`app/Web.Admin/src/`** — identical shell structure to storefront: `App.vue`, `HomeView.vue`, `NotFoundView.vue`, one router, one `useApiHealth` composable.
- **No permission-aware UI**, no data tables, no forms, no optimistic updates.

### Target Design

```
src/
  features/
    products/
      views/ProductListView.vue      # Data table + search + pagination
      views/ProductEditView.vue       # Create/edit form
    orders/
      views/OrderListView.vue         # Order table with status filters
      views/OrderDetailView.vue       # Order detail + timeline
    payments/
      views/PaymentListView.vue       # Payment table
      views/PaymentDetailView.vue     # Payment detail + refund action
    dashboard/
      views/DashboardView.vue         # KPI cards (revenue, orders, users)
  composables/
    useAdminProducts.ts
    useAdminOrders.ts
    useAdminPayments.ts
    usePermissions.ts                 # Reactive permission check
  stores/
    auth.store.ts                     # Admin auth session
    ui.store.ts                       # Sidebar, theme
```

### Permission-Aware UI Pattern

Every admin action must check the current user's permissions before rendering:

```typescript
// app/Web.Admin/src/composables/usePermissions.ts
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth.store'

export function usePermissions() {
  const auth = useAuthStore()

  const can = computed(() => ({
    readProducts: auth.hasPermission('Catalog.Products.Read'),
    manageProducts: auth.hasPermission('Catalog.Products.Manage'),
    readOrders: auth.hasPermission('Orders.Read'),
    manageOrders: auth.hasPermission('Orders.Manage'),
    readPayments: auth.hasPermission('Payments.Read'),
    managePayments: auth.hasPermission('Payments.Manage'),
    refundPayments: auth.hasPermission('Payments.Refund'),
    manageUsers: auth.hasPermission('Identity.Users.Manage'),
    manageRoles: auth.hasPermission('Identity.Roles.Manage'),
  }))

  return { can }
}
```

### Optimistic Updates and Invalidation

Mutations follow this pattern:
1. Immediately update local state (optimistic).
2. Send API request.
3. On success: no-op (already updated).
4. On failure: roll back local state + show error toast.
5. Invalidate related queries (e.g. after creating a product, refetch the product list).

### Sequence (U2)

1. **Add admin auth store** (separate from storefront auth — admin tokens may have different claims).
2. **Add permissions composable** → gate sidebar nav items and action buttons.
3. **Build product management** (list, create, edit, delete) — first screen because it exercises the full CRUD + permission pattern.
4. **Build order management** (list with status filters, detail view).
5. **Build payment management** (list, detail, refund action behind `Payments.Refund` permission).
6. **Add dashboard** with KPI cards (read from projections, not live aggregates).

### Verification (U2)

- `make admin.test.unit` — permission-gating specs: assert action buttons are absent when permission is missing.
- `make admin.test.integration` — CRUD flow specs with MSW.
- Manual: login as user-manager → verify product/order/payment management buttons are absent. Login as admin → all screens functional.

---

## U3. Edge/BFF Contract — Gateway Route Table

### Current State (Q1 — Critical)

**`app/Gateway/Gateway.Router.cs:68-74`** — the `/api/{**remainder}` catch-all strips `/api` via `StripPrefix("/{remainder}")`, so a request to `/api/todos` is proxied to the API as `/todos`. But the API maps every endpoint under `/api/*`:

| API Endpoint | Base Path | Gateway Receives |
|---|---|---|
| `Todo.Endpoint.cs:21` | `/api/todos` | `/todos` ❌ (prefix stripped) |
| `TodoList.Endpoint.cs:19` | `/api/todo-lists` | `/todo-lists` ❌ |
| `TodoCategory.Endpoint.cs:19` | `/api/todo-categories` | `/todo-categories` ❌ |
| `Identity.Endpoint.cs:32` | `/api/auth` | `/auth` ❌ |
| `Identity.*.Endpoint.cs` | `/api/identity/*` | `/identity/*` ❌ |
| `PaymentEndpoint.cs:29` | `/api/payments` | `/api/payments` ✅ (explicit route) |
| `PaymentAdminEndpoint.cs` | `/api/admin/payments` | `/api/admin/payments` ✅ (explicit route) |
| `PaymentWebhookEndpoint.cs:22` | `/api/payments/webhooks` | `/api/payments/webhooks` ✅ (explicit route) |

Additionally, **`Gateway.Router.cs:74`** applies `ProtectedApiPolicy` to the catch-all, so `/api/auth/register`, `/api/auth/login`, and all anonymous identity routes receive 401 at the edge before reaching the API. Only `/api/time` and `/api/payments/webhooks` are explicitly anonymous.

### Corrected Gateway Route Table

The route order is load-bearing. Narrow routes must precede broad ones.

```csharp
// app/Gateway/Gateway.Router.cs (corrected)
public static IReadOnlyList<RouteConfig> Routes() =>
[
    // 1. Anonymous liveness probe
    new RouteConfig {
        RouteId = "api-time",
        ClusterId = ApiCluster,
        Match = new RouteMatch { Path = "/api/time" },
        Transforms = StripPrefix("/time"),
        AuthorizationPolicy = AnonymousPolicy,
    },

    // 2. Anonymous auth surface — MUST come before the /api catch-all
    new RouteConfig {
        RouteId = "api-auth",
        ClusterId = ApiCluster,
        Match = new RouteMatch { Path = "/api/auth/{**remainder}" },
        AuthorizationPolicy = AnonymousPolicy,  // <-- was ProtectedApiPolicy
    },

    // 3. Anonymous webhook surface
    new RouteConfig {
        RouteId = "api-payments-webhook",
        ClusterId = ApiCluster,
        Match = new RouteMatch { Path = "/api/payments/webhooks/{**remainder}" },
        AuthorizationPolicy = AnonymousPolicy,
    },

    // 4. Admin payment surface (protected, permission-checked at API)
    new RouteConfig {
        RouteId = "api-admin-payments",
        ClusterId = ApiCluster,
        Match = new RouteMatch { Path = "/api/admin/payments/{**remainder}" },
        AuthorizationPolicy = ProtectedApiPolicy,
    },

    // 5. Customer payment surface (protected)
    new RouteConfig {
        RouteId = "api-payments",
        ClusterId = ApiCluster,
        Match = new RouteMatch { Path = "/api/payments/{**remainder}" },
        AuthorizationPolicy = ProtectedApiPolicy,
    },

    // 6. All other API — prefix preserved, authenticated-only
    //    NOTE: StripPrefix REMOVED. API endpoints are mapped at /api/*.
    new RouteConfig {
        RouteId = "api",
        ClusterId = ApiCluster,
        Match = new RouteMatch { Path = "/api/{**remainder}" },
        // Transforms = StripPrefix("/{remainder}"),  // <-- REMOVED
        AuthorizationPolicy = ProtectedApiPolicy,
    },

    // 7. Admin SPA — no path transform (base is /admin/)
    new RouteConfig {
        RouteId = "admin",
        ClusterId = AdminCluster,
        Match = new RouteMatch { Path = "/admin/{**remainder}" },
    },

    // 8. Storefront catch-all — MUST be last
    new RouteConfig {
        RouteId = "shopfront",
        ClusterId = ShopFrontCluster,
        Match = new RouteMatch { Path = "/{**remainder}" },
        Transforms = StripPrefix("/{remainder}"),
    },
];

// Replace StripPrefix helper — the path arg must NOT be prepended with "/":
private static IReadOnlyList<IReadOnlyDictionary<string, string>> StripPrefix(string pathPattern) =>
    [new Dictionary<string, string> { ["PathRemovePrefix"] = pathPattern }];
```

**Key decisions:**

| Decision | Why |
|---|---|
| `PathRemovePrefix` instead of `PathPattern` on the catch-all | The `/{**remainder}` catch-all strips the leading `/` so the SPA receives root-relative paths. Using `PathRemovePrefix` with `"/"` is semantically clearer. |
| Anonymous auth route is explicitly matched at `/api/auth/{**remainder}` | Identity endpoints mix anonymous (register, login, refresh, forgot-password) and authenticated (logout, `/me`). The edge cannot distinguish them, so the edge authenticates the token _if present_ but does not reject anonymous requests. The API enforces per-endpoint authorization. |
| No per-route rate limits yet | The current per-IP fixed window (100 req/10 s) applies to all proxied requests. Per-route limits (e.g., stricter on `/api/auth/login`) should be added as a follow-up. |
| No CORS headers | Same-origin: browser → gateway → API. The gateway does not serve cross-origin requests. |

### Security Headers

Add a response header middleware to the gateway:

```csharp
// app/Gateway/SecurityHeadersMiddleware.cs
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});
```

The `Strict-Transport-Security` header is deferred to the reverse proxy / load balancer in front of the gateway.

### Correlation

The existing `CorrelationIdMiddleware` (`app/Gateway/Program.cs:62`) generates or propagates an `X-Correlation-Id` header. The API consumes it. Ensure the gateway forwards it to the downstream API (YARP does this by default for proxied requests).

### Rate Limits Per Identity

Upgrade from per-IP to per-identity rate limiting:

```csharp
// In app/Gateway/Program.cs, replace the FixedWindowLimiter partition key:
options.AddPolicy(GatewayRouter.RateLimitPolicy, context =>
{
    // Prefer authenticated user ID over IP address
    var userId = context.User?.FindFirstValue("sub")
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 100,
        Window = TimeSpan.FromSeconds(10),
        QueueLimit = 0,
    });
});
```

### Health/Readiness Surface Exposed to the Browser

- **`app/Gateway/Program.cs:69`** maps `MapDefaultEndpoints()` which exists only in `Development` (`ServiceDefaults.Extension.cs:116-131`). In non-Development environments, only `/health/live` and `/health/ready` exist.
- **Fix:** for browser consumption, expose a lightweight `/api/health` endpoint on the API that returns structured readiness status (self + db + redis + rabbitmq + outbox age). The browser health chip calls this, not the gateway's own `/health` endpoint.

### Sequence (U3)

1. **Fix gateway routes** — add explicit `/api/auth/{**remainder}` anonymous route; remove `StripPrefix` from the `/api` catch-all. This is the single most blocking issue (Q1).
2. **Add security headers middleware** to the gateway.
3. **Add per-identity rate limiting** (can share the existing `edge` policy with an upgraded partition key).
4. **Add `/api/health` endpoint** on the API with structured readiness JSON.
5. **Update Vite proxy configurations** — both `app/Web.Storefront/vite.config.ts:27-35` and `app/Web.Admin/vite.config.ts:29-37` proxy `/health` and `/api` to `API_BASE_URL`. After the fix, the storefront should proxy `/api` to the gateway and call `/api/health` for the health chip.
6. **Add edge regression tests** (see U5).

### Verification (U3)

- `curl http://localhost:5050/api/auth/login -d '{"email":"x","password":"x"}'` → 400 (not 401). Gateway passes anonymous auth routes through.
- `curl http://localhost:5050/api/todos -H "Authorization: Bearer <valid-token>"` → 200 (not 404). Gateway preserves `/api` prefix.
- `curl -v http://localhost:5050/api/health` → JSON body with `{ healthy: true, checks: { db: "Healthy", ... } }`.
- `curl -v http://localhost:5050/` → HTML with `X-Content-Type-Options: nosniff` and `X-Frame-Options: DENY`.
- `for i in $(seq 110); do curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5050/api/time; done | sort | uniq -c` → 429s appear after 100 requests.

---

## U4. API Contract Discipline

### Current State

- **OpenAPI** is wired via `OpenApi.Composition.cs` (`src/Infrastructure/BuildingBlocks/Adapters/OpenApi/`), serves at `/openapi/v1.json`, and renders with Scalar UI.
- **No typed client generation** — the frontend uses hand-written `fetch` calls (`health.ts:3-5`).
- **No API versioning** — no `Asp.Versioning` package, no route-based version strategy.
- **No contract testing in CI** — no OpenAPI diff, no breaking-change detection.

### Target Design

1. **OpenAPI as source of truth.** The backend's `/openapi/v1.json` is authoritative. The frontend typed client is generated from it.
2. **Versioning strategy: URL path prefix.** `/api/v1/todos`, `/api/v1/products`, etc. This is the simplest strategy for a single-consumer API and avoids header-based version negotiation.
3. **Breaking-change detection in CI.** A CI step diffs the generated OpenAPI spec against the previous `main` version and fails on breaking changes (removed endpoints, changed request/response shapes, changed required fields).
4. **Typed client generation.** Use `openapi-typescript` + `openapi-fetch` (or `@hey-api/openapi-ts`) to generate TypeScript types and a typed `fetch` wrapper from the OpenAPI spec.
5. **Pagination/filtering conventions.** All list endpoints use: `GET /api/v1/{resource}?page=1&pageSize=20&sort=name&order=asc&filter=status:active`.

### Breaking-Change Detection in CI

Add a `make contract.check` target:

```bash
# scripts/backend/contract-check.backend.sh
# 1. Start API (or use a pre-built OpenAPI spec committed to the repo)
# 2. Fetch /openapi/v1.json
# 3. Compare against committed spec using openapi-diff or oasdiff
# 4. Fail on breaking changes
```

```yaml
# In .github/workflows/ci.yml, add to the backend job:
- name: Contract check
  run: make contract.check
```

### Typed Client Generation

Add to `app/package.json`:

```json
{
  "scripts": {
    "generate:api-client": "openapi-typescript http://localhost:8080/openapi/v1.json -o app/Web.Storefront/src/api/generated/schema.d.ts"
  }
}
```

The frontend's `api/client.ts` wraps the generated typed fetch with auth handling:

```typescript
import createClient from 'openapi-fetch'
import type { paths } from './generated/schema'
import { useAuthStore } from '@/stores/auth.store'

const { GET, POST, PUT, DELETE } = createClient<paths>({
  baseUrl: '/api/v1',
})

// Auth middleware
const authHeaders = () => {
  const auth = useAuthStore()
  return auth.token ? { Authorization: `Bearer ${auth.token}` } : {}
}

export const api = { GET, POST, PUT, DELETE, authHeaders }
```

### Sequence (U4)

1. **Add API versioning** — install `Asp.Versioning.Mvc` / `Asp.Versioning.Http`, add `AddApiVersioning()` to `Adapters.Composition.cs`, set default version to `1.0`.
2. **Update all Carter modules** to mount under `/api/v1/*` (or use a global route prefix).
3. **Commit the OpenAPI spec** to `docs/contracts/openapi-v1.json` as the CI baseline.
4. **Add `make contract.check`** and wire into CI.
5. **Add `openapi-typescript` to `app/package.json`** devDependencies.
6. **Generate typed client** and replace `api/health.ts` + `api/client.ts` with generated types.

### Verification (U4)

- `make contract.check` passes on a clean `main`.
- Deliberately remove a required field from a response DTO → `make contract.check` fails.
- Frontend `make store.build` uses generated types with zero type errors.

---

## U5. Frontend Testing

### Current State

- **`app/Web.Storefront/src/__tests__/unit/App.spec.ts:1-34`** — 2 tests: renders heading + health chip, shows 404. Uses `vi.stubGlobal('fetch')`.
- **`app/Web.Storefront/src/__tests__/integration/health.integration.spec.ts`** — health API integration spec.
- **`app/Web.Storefront/src/__tests__/integration/navigation.integration.spec.ts`** — navigation spec.
- **No MSW**, no Playwright config, no accessibility checks, no visual regression.

### Target Design

| Layer | Tool | Scope |
|---|---|---|
| Unit | Vitest + `@vue/test-utils` | Component rendering, user interactions, composable logic. Mock API at the `qingfaFetch` layer. |
| Integration | Vitest + MSW (`msw`) | Full component tree with real router + mocked API. Exercises auth flow, catalog listing, cart add, checkout. |
| Smoke (E2E) | Playwright | Critical user paths through the gateway: login → browse catalog → add to cart → checkout. Runs on `main`/nightly only. |
| Contract | `make contract.check` | OpenAPI diff (see U4). |
| Accessibility | `axe-playwright` | Run against the Playwright smoke test pages. |
| Visual regression | Optional: `@chromatic-com/playwright` or Percy | Gated behind a feature flag; only on `main`. |

### MSW Setup

```typescript
// app/Web.Storefront/src/__tests__/mocks/handlers.ts
import { http, HttpResponse } from 'msw'

export const handlers = [
  // Health
  http.get('/api/health', () =>
    HttpResponse.json({ healthy: true, checks: { db: 'Healthy' } })),

  // Auth
  http.post('/api/auth/login', () =>
    HttpResponse.json({ isSuccess: true, data: { accessToken: 'test-token', refreshToken: 'test-refresh' } })),

  // Catalog
  http.get('/api/v1/catalog/products', () =>
    HttpResponse.json({
      isSuccess: true,
      data: {
        items: [
          { id: '1', name: 'Test Product', price: { amount: 1999, currency: 'USD' } },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      },
    })),
]
```

### Playwright Smoke Spec Outline

```typescript
// app/Web.Storefront/e2e/smoke.spec.ts
import { test, expect } from '@playwright/test'

test.describe('Storefront smoke', () => {
  test('health chip shows healthy', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByTestId('health-chip')).toContainText('healthy')
  })

  test('catalog loads products', async ({ page }) => {
    await page.goto('/catalog')
    await expect(page.getByTestId('product-card')).toHaveCount(1)
  })

  test('full purchase flow', async ({ page }) => {
    // Register
    await page.goto('/register')
    await page.fill('[data-test="email"]', 'test@example.com')
    await page.fill('[data-test="password"]', 'Password123!')
    await page.click('[data-test="register-submit"]')
    await expect(page).toHaveURL('/')

    // Browse → add to cart
    await page.goto('/catalog')
    await page.click('[data-test="add-to-cart"]:first-child')
    await expect(page.getByTestId('cart-badge')).toContainText('1')

    // Checkout
    await page.goto('/cart')
    await page.click('[data-test="checkout-button"]')
    await expect(page.getByTestId('checkout-form')).toBeVisible()
  })

  test('login required for checkout', async ({ page }) => {
    await page.goto('/checkout')
    await expect(page).toHaveURL(/\/login/)
  })

  test('admin route returns 200', async ({ page }) => {
    await page.goto('/admin')
    await expect(page.getByTestId('app-title')).toBeVisible()
  })
})
```

### Test Data Strategy

- **MSW handlers return scenario-based data** — "empty catalog", "single product", "paginated list", "error".
- **Seeded demo data** (see U6) provides deterministic IDs for Playwright tests.
- **No test against production database** — Playwright tests use the seeded Compose stack.

### Sequence (U5)

1. **Add MSW** to `app/Web.Storefront/package.json` and `app/Web.Admin/package.json` devDependencies.
2. **Create `__tests__/mocks/server.ts` + `handlers.ts`** in both apps.
3. **Add Vitest setup file** (`vitest.setup.ts`) that starts MSW server before all tests.
4. **Migrate existing integration specs** from `vi.stubGlobal('fetch')` to MSW.
5. **Write new integration specs** for catalog, cart, checkout, auth flows.
6. **Add Playwright** for smoke tests. Install `@playwright/test`. Add `app/Web.Storefront/e2e/` and `app/Web.Admin/e2e/`.
7. **Add axe accessibility checks** in Playwright smoke tests.
8. **Wire `make test.web.e2e`** to actually install Playwright browsers and run.

### Verification (U5)

- `make store.test.integration` — MSW-based API flow specs pass.
- `make test.web.e2e` — Playwright smoke passes through the gateway without skips.
- `pnpm --filter @qingfa/storefront exec vitest run --coverage` — line coverage ≥90%.

---

## U6. Developer Experience

### Current State

- **`make help`** is the public command surface. Dotted targets (`make store.test`) work.
- **`make install`** — one .NET restore + one `app/` pnpm install.
- **`make doctor`** — verify dotnet/node/pnpm/Docker.
- **AppHost** (`app/AppHost/AppHost.cs`) wires the full stack for `F5` development.
- **Compose** (`compose.yaml`) provides a containerized equivalent.
- **No seeded demo data** beyond the EF migration seeders (identity roles/permissions).
- **Doc drift** — storefront/admin READMEs describe product screens that do not exist.

### Target Design

1. **One-command onboarding:** `make doctor && make install && make build && make dev`. `make dev` starts AppHost with hot reload for both SPAs.
2. **Seeded demo data:** `make seed` populates the database with demo catalog (10 products, 3 categories), a demo user (demo@qingfa.shop / Demo123!), and sample orders. Runs against a running API.
3. **Local Aspire/Compose parity:** both `make dev` (AppHost) and `make infra.up` (Compose) produce a working storefront at `http://localhost:5050`. The `ConnectionStrings__*` and `Messaging__Transport` values match.
4. **Doc accuracy:** every `app/*/README.md` describes only what is implemented. Add a `doc-check` CI step that verifies route tables match the actual code.
5. **Conventional commits:** add `commitlint` and a `.commitlintrc.yaml` to enforce `type(scope): description` format.

### Make Targets to Add

| Target | Purpose |
|---|---|
| `make dev` | Start AppHost with `dotnet run --project app/AppHost` (hot reload for .NET + Vite) |
| `make seed` | Run `POST /api/seed/demo` against the running API |
| `make doc.check` | Verify READMEs don't claim unimplemented features |
| `make contract.check` | OpenAPI diff against committed baseline |

### Seeded Demo Data

Add a `SeedEndpoint` (Development-only, gated behind `ASPNETCORE_ENVIRONMENT=Development`):

```csharp
// app/Web.Api/Endpoints/Seed/SeedEndpoint.cs
// POST /api/seed/demo
// Creates: demo user (demo@qingfa.shop), 10 products across 3 categories, 2 sample orders
// Idempotent: skips if demo user already exists
```

### Sequence (U6)

1. **Add `make dev`** target that runs AppHost.
2. **Create seed endpoint** with demo data.
3. **Add `make seed`** target.
4. **Update storefront/admin READMEs** to remove claims about unimplemented screens.
5. **Add `commitlint`** config and `make commit.lint` target.
6. **Add doc accuracy check** to `make lint`.

### Verification (U6)

- Clone repo fresh → `make doctor && make install && make build && make dev` → storefront at `http://localhost:5050` shows catalog with products.
- `make seed` → `curl http://localhost:5050/api/v1/catalog/products` returns 10 products.
- `make lint` includes `make doc.check` and fails on stale README claims.

---

## U7. CI/CD

### Current State

- **`.github/workflows/ci.yml`** — PR runs `backend` (lint, build, unit coverage, api.test, test.infra.broker) + `web` (format, lint, coverage, build). Main/nightly adds `apphost`, `infra`, `coverage-report`, `compose` smoke.
- **`.github/workflows/cd.yml`** — verifies (`make ci`), then builds and pushes 4 images to `ghcr.io`.
- **Fail-open broker gate** (`test-broker.backend.sh:9-19`) — exits 0 if test class names change or Docker is absent.
- **No Playwright** — `make test.web.e2e` skips.
- **No SBOM/signing** — images are not signed.

### Target Job Graph

```
PR:
  changes ─────────────────────────────────────────────┐
  backend ──┐                                          │
    lint    │                                          │
    build   │                                          │
    coverage.unit                                      │
    api.test                                           │
    test.infra.broker (FAIL-CLOSED)                     │
    contract.check  ← NEW                              │
  web ──────┤                                          │
    format.check                                       │
    lint (vue-tsc)                                     │
    coverage.web                                       │
    build                                              │
  gate ← depends on [backend, web]                     │
    └─ requires success or skip for all deps            │

main/nightly (adds):
  backend also runs: coverage.api, coverage.infra      │
  apphost: coverage.apphost + Playwright smoke ← NEW    │
  infra: coverage.infra                                 │
  coverage-report: merge all .NET coverage              │
  compose: smoke test (conditional on Compose changes)  │
```

### Fail-Closed Requirements

| Gate | Current | Target |
|---|---|---|
| Broker tests | Skips if Docker absent or test class absent (`scripts/backend/test-broker.backend.sh:9-19`) | **Fail** if Docker is available but no tests matched (class name change). Skip only if no Docker runtime. |
| Playwright | Skips (no config) | Install Playwright; fail if tests are absent but config exists. |
| Contract check | Absent | Fail on breaking OpenAPI changes. |
| Architecture tests | Partly vacuous (`tests/Architecture.Tests/LayerArchitecture.Tests.cs:57-67`) | Replace placeholders with real reflection-based rules. |

### Artifact Immutability

- Images are tagged with `sha-{short}` (immutable) + `latest` (mutable). CD already does this.
- **Add image signing** with `cosign` (keyless via GitHub Actions OIDC):

```yaml
# In .github/workflows/cd.yml, after docker/build-push-action:
- uses: sigstore/cosign-installer@v3
- run: cosign sign --yes ghcr.io/${{ steps.image.outputs.prefix }}/${{ matrix.name }}@${{ steps.build.outputs.digest }}
```

### SBOM

- **Generate SBOM** at build time:

```yaml
- uses: anchore/sbom-action@v0
  with:
    image: ghcr.io/${{ steps.image.outputs.prefix }}/${{ matrix.name }}@${{ steps.build.outputs.digest }}
```

### Environment Promotion

| Environment | Trigger | Config Source |
|---|---|---|
| Dev | Every push to non-main branch | AppHost / Compose with `Development` env |
| Staging | `main` push | Compose with `Staging` env, fake payments |
| Production | Tag `v*.*.*` | Compose with `Production` env, real payment providers |

### Migration Job

- **Currently:** AppHost sets `Persistence__AutoMigrate=true` (`AppHost.cs:28`), Compose does not (`compose.yaml` — no `Persistence__AutoMigrate` env var).
- **Target:** Add a dedicated migration job that runs `dotnet ef database update` before the API starts. In Compose, add an `init` container or a `depends_on` condition. In Kubernetes, use a Job.

### Rollback

- **Image rollback:** redeploy with previous `sha-{short}` tag. The CD workflow keeps all tagged images.
- **Database rollback:** EF migrations are forward-only. For rollback, deploy a previous image that expects the pre-migration schema, or apply a down migration.
- **Feature flags:** use `appsettings.json` feature toggles for new UI features so a rollback can disable them without a code revert.

### Sequence (U7)

1. **Fix broker test fail-open** → `scripts/backend/test-broker.backend.sh` must exit non-zero if Docker is available and zero tests matched.
2. **Add contract check** to `make contract.check` and CI backend job.
3. **Add Playwright** — install `@playwright/test` with browsers, add smoke specs, wire `make test.web.e2e`.
4. **Fix architecture test placeholders** → replace `true.Should().BeTrue(...)` with real reflection rules.
5. **Add image signing + SBOM** to CD workflow.
6. **Add migration job** to Compose (init container pattern).
7. **Add feature flag support** using `Microsoft.FeatureManagement`.

### Verification (U7)

- PR with a breaking OpenAPI change → `contract.check` fails CI.
- PR with renamed broker test class → `test.infra.broker` fails CI (not skips).
- Tag push `v1.0.0` → CD builds, signs, and pushes 4 images with SBOMs.
- `docker compose -f compose.yaml up` → migration runs before API starts.

---

## U8. Operations

### Current State

- **No SLOs/SLIs defined.**
- **No dashboards** — OTLP export is conditional on `OTEL_EXPORTER_OTLP_ENDPOINT` being set (`ServiceDefaults.Extension.cs:86-88`), which is not configured in Compose.
- **No alerts.** Payment metrics meter (`PaymentMetrics.cs:12`) exists but is never subscribed (`ServiceDefaults.Extension.cs:59-63` — only `AspNetCore`, `HttpClient`, `Runtime` instrumentations).
- **No runbooks, incident checklist, backup/restore drill.**

### SLO/Alert Definitions

| SLI | SLO | Alert Threshold | Metric |
|---|---|---|---|
| API availability | 99.9% monthly | <99.5% over 5 min | `http_server_duration_count{status=~"2.."}` / total |
| API latency p95 | <500 ms | >1 s over 5 min | `http_server_duration_seconds` (p95) |
| Outbox dispatch lag | <30 s p95 | >120 s over 5 min | Custom meter: `outbox_age_seconds` |
| Outbox dead-letter rate | <0.1% of publishes | >1% over 15 min | Custom meter: `outbox_dead_letter_total` |
| Projection lag | <10 s p95 | >60 s over 5 min | Custom meter: `projection_lag_seconds` (checkpoint age) |
| Webhook processing age | <60 s p95 | >300 s over 5 min | Custom meter: `webhook_age_seconds` (event received → settled) |
| Payment failure rate | <1% of attempts | >5% over 15 min | Custom meter: `payment_failure_total` / `payment_attempt_total` |
| Notification delivery failure rate | <2% of attempts | >10% over 15 min | Custom meter: `notification_failure_total` |

### Observability Wiring

```csharp
// src/Infrastructure/BuildingBlocks/Adapters/Observability/Observability.Extension.cs
public static TBuilder AddBusinessMetrics<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            // Custom meters
            metrics.AddMeter("QingFa.Payments");
            metrics.AddMeter("QingFa.Outbox");
            metrics.AddMeter("QingFa.Projections");
            metrics.AddMeter("QingFa.Notifications");
            metrics.AddMeter("QingFa.Webhooks");
        });
    return builder;
}
```

### Committed Metrics (as code)

Each metric follows the existing `PaymentMetrics` pattern (`src/Application/Modules/Payments/PaymentMetrics.cs:12`):

| Metric | Type | Labels | Location |
|---|---|---|---|
| `outbox_age_seconds` | Histogram | `event_type` | `src/Infrastructure/.../Outbox/OutboxMetrics.cs` |
| `outbox_dead_letter_total` | Counter | `event_type` | Same |
| `projection_lag_seconds` | Histogram | `projection_name` | `src/Infrastructure/.../Projections/ProjectionMetrics.cs` |
| `webhook_age_seconds` | Histogram | `provider` | `src/Application/Modules/Payments/Webhooks/WebhookMetrics.cs` |
| `payment_attempt_total` | Counter | `provider`, `status` | Extend existing `PaymentMetrics.cs` |
| `payment_failure_total` | Counter | `provider`, `reason` | Extend existing `PaymentMetrics.cs` |
| `notification_failure_total` | Counter | `channel`, `provider`, `reason` | `src/Infrastructure/.../Notifications/NotificationMetrics.cs` |

### Dashboards

Minimum viable dashboard (Grafana or Aspire dashboard):

1. **RED metrics** (Rate, Errors, Duration) per endpoint group.
2. **Outbox health** — pending count, dispatch rate, oldest message age, dead-letter count.
3. **Projection health** — lag per projection, checkpoint age, rebuild status.
4. **Payment health** — success/failure rate, refund rate, webhook processing age.
5. **Infrastructure** — CPU, memory, connection pool, disk (Postgres WAL).

### Runbooks

Create `docs/runbooks/` with:

| Runbook | Content |
|---|---|
| `outbox-dead-letter.md` | How to detect, inspect, and replay dead-lettered outbox messages. |
| `projection-rebuild.md` | How to trigger a projection rebuild, estimate duration, monitor progress. |
| `payment-reconciliation.md` | How to run reconciliation, interpret drift, and manually resolve. |
| `database-restore.md` | How to restore from backup, verify, and resume operations. |
| `incident-response.md` | Severity levels, escalation path, communication template, postmortem template. |

### Backup/Restore

```bash
# scripts/ops/backup.sh
pg_dump -h $PGHOST -U $PGUSER -d $PGDATABASE \
  --format=custom \
  --compress=9 \
  --file="backups/qingfa-$(date -u +%Y%m%dT%H%M%SZ).dump"

# scripts/ops/restore.sh
pg_restore -h $PGHOST -U $PGUSER -d $PGDATABASE \
  --clean --if-exists \
  "$BACKUP_FILE"
```

Backup drill: restore to a temporary database, verify row counts, verify projection replay, then discard.

### Sequence (U8)

1. **Add `AddBusinessMetrics()`** to `ServiceDefaults.Extension.cs` and subscribe all custom meters.
2. **Create metric classes** for outbox, projections, notifications, webhooks following the `PaymentMetrics` pattern.
3. **Configure OTLP export** in Compose (`OTEL_EXPORTER_OTLP_ENDPOINT` pointing to a local collector or Aspire dashboard).
4. **Create Grafana dashboard JSON** in `docs/operations/dashboards/`.
5. **Write runbooks** for the top 5 incident scenarios.
6. **Create backup/restore scripts** and document the drill procedure.
7. **Add alert rules** as PrometheusRule YAML in `docs/operations/alerts/`.

### Verification (U8)

- `curl http://localhost:8080/metrics` (or OTLP endpoint) shows `payment_attempt_total`, `outbox_age_seconds`, etc.
- Run the backup script → restore to a fresh database → projections replay correctly → `make test` passes against restored data.
- Simulate outbox dead-letter → alert fires → runbook resolves it.

---

## U9. Performance

### Current State

- **No bundle budgets**, no Core Web Vitals measurement, no API p95 targets beyond standard resilience handler defaults, no load test scenarios, no CDN strategy.

### Bundle Budgets

| App | JS (gzip) | CSS (gzip) | Total (gzip) |
|---|---|---|---|
| Storefront | <150 KB | <30 KB | <200 KB |
| Admin | <200 KB | <40 KB | <250 KB |

Enforce with `vite-plugin-bundle-budget` or a custom script in `make store.build` / `make admin.build` that parses the build output.

### Core Web Vitals Targets

| Metric | Target | Measurement |
|---|---|---|
| LCP (Largest Contentful Paint) | <2.5 s p75 | Playwright trace or Lighthouse CI |
| INP (Interaction to Next Paint) | <200 ms p75 | Playwright trace |
| CLS (Cumulative Layout Shift) | <0.1 p75 | Playwright trace or Lighthouse CI |

### API p95 Targets

| Endpoint | Target |
|---|---|
| GET `/api/v1/catalog/products` (list) | <200 ms |
| GET `/api/v1/catalog/products/{id}` (detail) | <100 ms |
| POST `/api/v1/cart/items` (add to cart) | <300 ms |
| POST `/api/v1/orders` (checkout) | <500 ms |
| POST `/api/auth/login` | <300 ms |

These are measured server-side via the existing HTTP instrumentation and validated in load tests.

### Load Test Scenarios

Use `k6`:

```javascript
// tests/Load/baseline.js
import { check, sleep } from 'k6'
import http from 'k6/http'

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Ramp up
    { duration: '1m', target: 50 },    // Steady state
    { duration: '30s', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
}

export default function () {
  const healthRes = http.get('http://localhost:8080/api/health')
  check(healthRes, { 'health is 200': (r) => r.status === 200 })
  sleep(1)
}
```

Add `make load.baseline` target.

### Caching/CDN Strategy

| Layer | What | TTL |
|---|---|---|
| Browser | Static assets (JS, CSS, fonts) | 1 year (immutable with content-hash filenames) |
| Gateway | `GET /api/v1/catalog/products` | 30 s (stale-while-revalidate) |
| Redis | Product read models | 5 min (invalidated on product update) |
| CDN | Storefront static assets (`/assets/*`) | 1 year; deploy to CDN in front of gateway |

No CDN for the admin SPA (behind auth, low traffic).

### Sequence (U9)

1. **Add bundle budget check** to `make store.build` and `make admin.build`.
2. **Add `rollup-plugin-visualizer`** to Vite configs for bundle analysis.
3. **Add Lighthouse CI** (or `@lhci/cli`) to `make test.web.e2e` for Core Web Vitals.
4. **Add `k6` load test** for baseline scenario.
5. **Add response caching** to the gateway for catalog list endpoint.
6. **Add Redis caching** to `ProductReadStore` queries.

### Verification (U9)

- `make store.build` fails if bundle exceeds budget.
- `make load.baseline` reports p95 < 500 ms at 50 concurrent users.
- Lighthouse CI reports LCP < 2.5 s on `/catalog`.

---

## U10. Documentation

### Current State

- **No C4 diagrams**, no ADR process, module READMEs claim unimplemented features, docs link to archived paths, onboarding guide incomplete.

### Target

| Document | Path | Purpose |
|---|---|---|
| C4 Context | `docs/architecture/c4/context.puml` | System context: users, external systems (Stripe, PayPal, SendGrid) |
| C4 Container | `docs/architecture/c4/container.puml` | Gateway, API, PostgreSQL, Redis, RabbitMQ, Storefront, Admin |
| ADR index | `docs/architecture/adr/README.md` | Index of architectural decisions with status (Proposed/Accepted/Deprecated/Superseded) |
| ADR-001 | `docs/architecture/adr/001-event-sourcing.md` | Why event sourcing for Payments and Todos |
| ADR-002 | `docs/architecture/adr/002-outbox.md` | Why transactional outbox; boundaries of the guarantee |
| ADR-003 | `docs/architecture/adr/003-no-bff.md` | Why no BFF yet; re-evaluation trigger |
| ADR-004 | `docs/architecture/adr/004-same-origin.md` | Why same-origin through gateway; when to add CORS |
| Module READMEs | `src/*/Modules/*/README.md` | What the module does, its API surface, its persistence schema, its integration events |
| API usage | `docs/api/quickstart.md` | Authentication, pagination, filtering, error handling, rate limits |
| Onboarding | `docs/onboarding.md` | `make doctor && make install && make build && make dev` |
| Authorization matrix | `docs/architecture/authorization-matrix.md` | (Already identified as a gap in the roadmap audit) |

### ADR Process

- ADRs are numbered sequentially, never renumbered.
- Each ADR has: Title, Status, Context, Decision, Consequences.
- ADRs are immutable once merged (supersede with a new ADR, do not edit).
- New ADRs are proposed in PRs; the `docs/architecture/adr/README.md` index is updated.

### Sequence (U10)

1. **Create C4 context and container diagrams** (PlantUML — rendered in PRs).
2. **Write ADR-001 through ADR-004** for decisions already made.
3. **Update source module READMEs** to reflect only what is implemented.
4. **Write API quickstart** with curl examples.
5. **Update onboarding guide** to be verifiable (follow the steps → working storefront at localhost).
6. **Write authorization matrix** from the derived table in the roadmap audit.

### Verification (U10)

- A new developer follows `docs/onboarding.md` → `make dev` → sees a storefront with catalog.
- `make lint` includes doc accuracy check — no stale claims.
- Every module README matches the actual endpoints in `app/Web.Api/Endpoints/`.

---

## Dependency Graph

```
U3 (Gateway fix) ──► U1 (Storefront) ──► U2 (Admin)
                   ──► U4 (API contracts) ──► U1, U2 (typed clients)
                   ──► U5 (testing) ──► U1, U2 (test coverage)
U6 (DX) ──► independent (can start immediately)
U7 (CI/CD) ──► depends on U5 (Playwright), U4 (contract check)
U8 (Ops) ──► depends on U3 (correct edge), U7 (deployment artifacts)
U9 (Perf) ──► depends on U1, U2 (UI to measure)
U10 (Docs) ──► parallel with all; updated continuously
```

**Recommended execution order:**

1. **U3** (Gateway route fix — Q1 blocking issue — 1 day)
2. **U4** (API versioning + contract check — 2 days)
3. **U7** (Fail-closed CI gates — 1 day)
4. **U1** (Storefront features — 2 weeks)
5. **U5** (Frontend testing — parallel with U1, 1 week)
6. **U6** (Developer experience — 2 days)
7. **U2** (Admin features — 2 weeks)
8. **U8** (Operations — 1 week)
9. **U9** (Performance — 1 week)
10. **U10** (Documentation — ongoing)

---

## Appendix A: Complete File Inventory

### Files to Create

```
app/Gateway/SecurityHeadersMiddleware.cs
app/Web.Api/Endpoints/Health/HealthEndpoint.cs
app/Web.Api/Endpoints/Seed/SeedEndpoint.cs
app/Web.Storefront/src/api/client.ts
app/Web.Storefront/src/api/auth.api.ts
app/Web.Storefront/src/api/catalog.api.ts
app/Web.Storefront/src/api/cart.api.ts
app/Web.Storefront/src/api/checkout.api.ts
app/Web.Storefront/src/api/generated/schema.d.ts
app/Web.Storefront/src/composables/useAuth.ts
app/Web.Storefront/src/composables/useCatalog.ts
app/Web.Storefront/src/composables/useCart.ts
app/Web.Storefront/src/composables/useCheckout.ts
app/Web.Storefront/src/stores/auth.store.ts
app/Web.Storefront/src/stores/cart.store.ts
app/Web.Storefront/src/stores/ui.store.ts
app/Web.Storefront/src/features/catalog/components/ProductCard.vue
app/Web.Storefront/src/features/catalog/components/ProductCard.styles.ts
app/Web.Storefront/src/features/catalog/components/ProductFilter.vue
app/Web.Storefront/src/features/catalog/components/ProductFilter.styles.ts
app/Web.Storefront/src/features/catalog/views/CatalogView.vue
app/Web.Storefront/src/features/catalog/views/CatalogView.styles.ts
app/Web.Storefront/src/features/catalog/views/ProductView.vue
app/Web.Storefront/src/features/catalog/views/ProductView.styles.ts
app/Web.Storefront/src/features/cart/views/CartView.vue
app/Web.Storefront/src/features/cart/views/CartView.styles.ts
app/Web.Storefront/src/features/cart/components/CartItem.vue
app/Web.Storefront/src/features/cart/components/CartSummary.vue
app/Web.Storefront/src/features/checkout/views/CheckoutView.vue
app/Web.Storefront/src/features/auth/views/LoginView.vue
app/Web.Storefront/src/features/auth/views/RegisterView.vue
app/Web.Storefront/src/__tests__/mocks/server.ts
app/Web.Storefront/src/__tests__/mocks/handlers.ts
app/Web.Storefront/e2e/smoke.spec.ts
app/Web.Storefront/e2e/accessibility.spec.ts
app/Web.Admin/src/composables/usePermissions.ts
app/Web.Admin/src/composables/useAdminProducts.ts
app/Web.Admin/src/composables/useAdminOrders.ts
app/Web.Admin/src/composables/useAdminPayments.ts
app/Web.Admin/src/stores/auth.store.ts
app/Web.Admin/src/stores/ui.store.ts
app/Web.Admin/src/features/products/views/ProductListView.vue
app/Web.Admin/src/features/products/views/ProductEditView.vue
app/Web.Admin/src/features/orders/views/OrderListView.vue
app/Web.Admin/src/features/orders/views/OrderDetailView.vue
app/Web.Admin/src/features/payments/views/PaymentListView.vue
app/Web.Admin/src/features/payments/views/PaymentDetailView.vue
app/Web.Admin/src/features/dashboard/views/DashboardView.vue
app/Web.Admin/e2e/smoke.spec.ts
app/Web.Admin/src/__tests__/mocks/server.ts
app/Web.Admin/src/__tests__/mocks/handlers.ts
src/Infrastructure/BuildingBlocks/Adapters/Observability/Observability.Extension.cs
src/Infrastructure/BuildingBlocks/Adapters/Outbox/OutboxMetrics.cs
src/Infrastructure/BuildingBlocks/Adapters/Projections/ProjectionMetrics.cs
src/Infrastructure/BuildingBlocks/Adapters/Notifications/NotificationMetrics.cs
src/Application/Modules/Payments/Webhooks/WebhookMetrics.cs
docs/architecture/c4/context.puml
docs/architecture/c4/container.puml
docs/architecture/adr/README.md
docs/architecture/adr/001-event-sourcing.md
docs/architecture/adr/002-outbox.md
docs/architecture/adr/003-no-bff.md
docs/architecture/adr/004-same-origin.md
docs/architecture/authorization-matrix.md
docs/api/quickstart.md
docs/onboarding.md
docs/runbooks/outbox-dead-letter.md
docs/runbooks/projection-rebuild.md
docs/runbooks/payment-reconciliation.md
docs/runbooks/database-restore.md
docs/runbooks/incident-response.md
docs/operations/dashboards/grafana-overview.json
docs/operations/alerts/prometheus-rules.yaml
scripts/backend/contract-check.backend.sh
scripts/ops/backup.sh
scripts/ops/restore.sh
tests/Load/baseline.js
```

### Files to Modify

```
app/Gateway/Gateway.Router.cs              # Fix route table (Q1)
app/Gateway/Program.cs                     # Security headers, rate-limit by identity
app/Web.Api/Endpoints/Health/              # Add structured /api/health endpoint
app/Web.Storefront/vite.config.ts          # Proxy /api to gateway
app/Web.Admin/vite.config.ts               # Proxy /api to gateway
app/Web.Storefront/src/api/health.ts       # Use typed client, call /api/health
app/Web.Storefront/src/composables/useApiHealth.ts  # Structured readiness
app/Web.Storefront/src/router/index.ts     # Feature routes + guards
app/Web.Storefront/src/App.vue             # Real nav + accessibility
app/Web.Storefront/src/views/HomeView.vue  # Link to catalog
app/Web.Admin/src/router/index.ts          # Admin feature routes + guards
app/Web.Admin/src/App.vue                  # Real admin nav + permissions
src/Infrastructure/BuildingBlocks/Adapters/Adapters.Composition.cs  # AddBusinessMetrics
src/ServiceDefaults/ServiceDefaults.Extension.cs                     # Subscribe meters
.github/workflows/ci.yml                   # Contract check, fail-closed broker, Playwright
.github/workflows/cd.yml                   # Image signing, SBOM
compose.yaml                               # JWT key, environment, migration job
compose.deploy.yaml                        # Production defaults
make/store.mk                              # New targets
make/admin.mk                              # New targets
make/test.mk                               # add load.baseline, e2e fixes
scripts/backend/test-broker.backend.sh     # Fail-closed
app/package.json                           # openapi-typescript, msw, @playwright/test
app/Web.Storefront/README.md               # Remove unimplemented claims
app/Web.Admin/README.md                    # Remove unimplemented claims
```

---

## Appendix B: Feature Flag Strategy

All new UI features should be gated behind a flag so they can be disabled without a code revert:

```typescript
// app/Web.Storefront/src/composables/useFeatureFlags.ts
import { ref, onMounted } from 'vue'

interface FeatureFlags {
  catalog: boolean
  cart: boolean
  checkout: boolean
  search: boolean
}

const flags = ref<FeatureFlags>({
  catalog: false,
  cart: false,
  checkout: false,
  search: false,
})

export function useFeatureFlags() {
  onMounted(async () => {
    const res = await fetch('/api/features')
    if (res.ok) flags.value = await res.json()
  })
  return { flags }
}
```

Backend: `GET /api/features` returns JSON from `appsettings.json` feature section. Flags default to `false` for new features; enable in staging, then production after verification.

---

*End of implementation guide.*