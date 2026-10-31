# Application Topology

The deployable units of resys-eshop, the networks and ports they bind, the CORS origins they trust, and how a request resolves to a store.

**Links:** [C4 model](./01-c4.md) · [Data flow](./03-data-flow.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file describes how the containers from `03/01-01` are deployed and connected: the three
application deployables, the network paths between them and their dependencies, the ports and
CORS origins they use, and the mechanism that maps an inbound request to a `Store`. It is the
bridge between the conceptual view in `03/01-01` and the environment/deployment detail owned by
`05/01-02` (environment) and `08/01-01` (infrastructure topology).

The topology is **three deployables plus managed dependencies**: one .NET API host, two Nuxt 4
front-ends, one PostgreSQL database, one object-storage backend, and one email transport. The API
is stateless and horizontally scalable; the database is the only stateful service.

---

## Deployables

| Deployable | Source path | Runtime | Build output | Primary role |
|---|---|---|---|---|
| API | `src/Resys.Api` | .NET 10 / ASP.NET Core Kestrel | Published service binary/container | Serves `/api/v1` and `/api/admin`, runs modules and the outbox relay. |
| Storefront | `web/storefront` | Nuxt 4.5.x (Vue 3.5.43) | Node server bundle (SSR) | Public storefront; calls `/api/v1` only. |
| Admin | `web/admin` | Nuxt 4.5.x + PrimeVue 4 | Node server bundle or static SPA assets | Back-office; calls `/api/admin` only, JWT in httpOnly cookie. |
| Database | n/a (managed/container) | PostgreSQL 18 | n/a | All commerce state, Identity tables, and the outbox. |
| Object storage | n/a (managed/container) | S3-compatible (MinIO/dev disk) | n/a | Product images and uploaded assets. |
| Email transport | n/a | SES or SMTP relay | n/a | Transactional email delivery. |

`web/shared` and `Resys.Shared` are compile-time libraries and are not deployed on their own.
The API host is the only deployable with database and provider credentials.

---

## Network Topology

```
             Internet / browser
                    │  HTTPS
        ┌───────────┴────────────┐
        │        Edge / TLS      │
        └───┬────────────────┬───┘
            │                │
   ┌────────▼──────┐  ┌──────▼────────┐
   │ storefront    │  │ admin         │
   │ :3000 (dev)   │  │ :3001 (dev)   │
   └────────┬──────┘  └──────┬────────┘
            │  /api/v1       │  /api/admin
            └───────┬────────┘
                    ▼
            ┌───────────────┐
            │   Resys.Api   │  :8080 HTTP / :8443 HTTPS
            └──┬────┬────┬──┘
               │    │    │
      ┌────────┘    │    └───────────┐
      ▼             ▼                ▼
┌───────────┐ ┌────────────┐ ┌───────────────┐
│ PostgreSQL│ │  Object    │ │ Email (SES/   │
│    :5432  │ │  storage   │ │ SMTP)         │
└───────────┘ └────────────┘ └───────────────┘
                    ▲
                    │ HTTPS (outbound)
            ┌───────┴────────┐
            │ Payment gateways│
            │ Stripe · PayPal │
            └────────────────┘
```

**Network rules**

- Browsers reach the storefront and admin apps over HTTPS at the edge. Those apps proxy or call
  the API server-side and client-side as configured.
- The API reaches PostgreSQL, object storage, email, and payment gateways on the private or
  outbound network. Only the API holds those credentials.
- The browser may talk directly to payment providers (Stripe Elements / PayPal JS SDK) from the
  checkout page; the platform never proxies or stores PAN/CVV.
- No deployable opens a connection to another front-end; there is no server-to-server link
  between `storefront` and `admin`.

---

## Ports

The values below are the documented development defaults; production values are supplied through
environment configuration and are recorded in `05/01-02` and `08/01-01`.

| Port | Service | Protocol | Notes |
|---|---|---|---|
| 3000 | `web/storefront` (dev) | HTTP | Nuxt dev server; SSR render and client calls. |
| 3001 | `web/admin` (dev) | HTTP | Nuxt dev server; back-office shell. |
| 8080 | `Resys.Api` (dev) | HTTP | Primary API listener (`/api/v1`, `/api/admin`). |
| 8443 | `Resys.Api` (dev) | HTTPS | TLS listener when local certificates are configured. |
| 5432 | PostgreSQL 18 | TCP | Reachable by the API only. |
| 9000 | Object storage (dev, S3 API) | HTTP/HTTPS | MinIO or local-disk emulation; API only. |
| 1025 | SMTP (dev) | TCP | Mail-catcher transport replacing SES in development. |

**API health endpoints**

| Endpoint | Meaning |
|---|---|
| `/health/live` | Process liveness; no dependency checks. |
| `/health/ready` | Readiness; verifies the database and other required dependencies. |

The API supports vertical and horizontal scale. Horizontally, every instance binds the same
listener ports; the outbox relay must be concurrency-safe so only one instance consumes a given
outbox row (relay design is `03/02-03`).

---

## CORS Origins

The API allows browser requests only from the two front-end origins. Origins are an allow-list,
never a wildcard, and are supplied per environment.

| Client | Development origin | Production origin (example) |
|---|---|---|
| Storefront | `http://localhost:3000` | `https://shop.example.com` |
| Admin | `http://localhost:3001` | `https://admin.example.com` |

**CORS and security rules**

- The allow-list contains exactly the two configured front-end origins; unlisted origins are
  rejected.
- Credentials are allowed for the admin origin so the httpOnly JWT cookie is sent. A wildcard
  origin is invalid when credentials are enabled.
- Only the methods and headers the front-ends actually use are exposed; state-changing requests
  enforce CSRF protection in addition to CORS.
- The public API is rate-limited separately from origins; rate-limit policy is owned by
  `03/04-03`.
- Actual production hostnames are environment configuration and are confirmed in `08/01-01`.

---

## Store Resolution

Every catalog, inventory, payment, and order row is scoped to a `Store` (ADR 0002, spec §6). A
request is mapped to exactly one store before any store-scoped query runs. Resolution is
deterministic and applies to both surfaces.

**Resolution order**

1. **Explicit store id** — an authenticated admin request may name a store directly (the admin
   store switcher sends the store id). Admin access is additionally checked against the caller's
   per-store assignment.
2. **`X-Store` header (development, opt-in)** — in non-production environments a client may send
   an `X-Store` header naming a store by code or id. This exists to test multi-store behaviour
   without DNS; it is disabled in production.
3. **Request domain** — the storefront resolves the store from the request host: a mapped custom
   domain or subdomain is looked up against the `Stores` module's domain mapping. The first
   matching store wins.
4. **Default store** — when no mapping matches in a development environment, a configured default
   store is used. In production an unmapped domain is an error rather than a silent fallback.

```
request ──► explicit store id? ──yes──► validate + use
                 │no
                 ▼
           X-Store header (dev only)? ──yes──► look up by code/id
                 │no
                 ▼
           Host/domain mapping? ──yes──► look up store by domain
                 │no
                 ▼
           default store (dev only) / 404 unknown store (prod)
```

**Rules**

- Store resolution happens in middleware before endpoints execute; the resolved store is attached
  to the request scope.
- A store-scoped request never falls through to another store's data; queries carry the resolved
  store key.
- Storefront content (branding, supported locales, enabled currencies, checkout options) is
  resolved from the same store.
- The admin's store switcher re-resolves the store per request; changing stores does not require
  re-authentication, but per-store authorization is re-checked.
- Unknown or disabled stores return a not-found error in production; the development default
  fallback exists only to keep local setup friction low.

---

## Open Questions

1. Whether object storage is fronted by the API (signed URLs) or served directly from a CDN is a
   deployment decision owned by `08/01-01`; the storage abstraction is unaffected either way.
2. The exact admin store-switcher transport (header vs. route segment) is finalized with the admin
   API contract in `04-integration/02-rest-admin.md`.
3. Production port and TLS termination details (edge vs. per-service listeners) are owned by
   `08/01-01`.

## Links

- [C4 model](./01-c4.md)
- [Data flow](./03-data-flow.md)
- [Design spec](../../11-spec/README.md) — §6 (data
  architecture), §7 (API surface), §10 (cross-cutting).
- [ADR 0002 — Modular monolith](../../00-governance/01-adr/02-modular-monolith.md)
- [ADR 0004 — Headless API and JWT auth](../../00-governance/01-adr/04-headless-jwt.md)
- Infrastructure topology and environments: `08/01-01`, `08/01-02`; authentication and CORS:
  `03/04-03`; order lifecycle: `03/02-03`.
