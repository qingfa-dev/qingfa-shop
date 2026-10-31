# Solution Layout

The repository and solution project layout.

**Links:** [Spec index](../README.md) · [← Prev](04-tech-stack.md) · [Next →](06-domain-data.md) · [Root README](../../../README.md)

---

```
resys-eshop/
├─ src/
│  ├─ Resys.Core/               # shared kernel: Money, identities, store scoping,
│  │                            #   domain events, extension contracts
│  ├─ Resys.Modules.Catalog/    # products (master + variations), option types/values,
│  ├─ Resys.Modules.*           #   taxonomy. Modules: Catalog, Inventory, Shipping,
│  │                            #   Tax, Promotions, Payments, Returns, Wishlists,
│  │                            #   Reviews, Accounts, Stores
│  ├─ Resys.Infrastructure/     # EF Core + Postgres, Identity, file storage,
│  │                            #   email, gateway adapters, outbox relay
│  ├─ Resys.Api/                # Minimal API host: /api/v1 + /api/admin, OpenAPI
│  └─ Resys.Shared/             # DTOs / contracts shared with API clients
├─ web/
│  ├─ storefront/               # Nuxt 4 SSR storefront
│  ├─ admin/                    # Nuxt 4 + PrimeVue admin
│  └─ shared/                   # shared TS types, API client, components
├─ tests/
│  ├─ Resys.UnitTests/
│  ├─ Resys.IntegrationTests/   # Testcontainers (Postgres)
│  └─ Resys.E2E/                # Playwright (storefront + admin)
└─ docs/                         # the deliverable (this spec + 00–11 areas)
```

Each module project owns its entities + services + extension points. `Resys.Api` wires
modules via DI and exposes endpoints. `web/*` are fully decoupled HTTP clients.
