# Seed Data

The development seed dataset, how it is layered over migrations, and the commands that apply and reset it.

**Links:** [Prerequisites](./01-prerequisites.md) · [Environment](./02-environment.md) · [Docker Compose](./03-docker-compose.md) · [Migrations](../../03-architecture/03-data/04-migrations.md) · [Entity model](../../03-architecture/03-data/01-entities.md) · [Multi-store](../../03-architecture/03-data/02-multi-store.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

A freshly migrated database is structurally correct but empty of commerce data. This file describes
the **development seed dataset** — the stores, taxonomy, catalog, options, inventory, shipping, tax,
promotion, user, and sample-order records that make the platform immediately usable — and the exact
commands that apply it. The layering between migration-carried reference data and the runtime demo
seeder is fixed by `03/03-04`; the connection and environment it runs against are `05/01-02`; the
database container is `05/01-03`.

The dataset exists for development and tests only. It is never applied to production.

---

## Seed Layers

Seeding is split into two layers so that every environment is functional while only development
carries demo content.

| Layer | Mechanism | Contents | Environments |
|---|---|---|---|
| **Reference data** | EF Core `HasData` inside module migrations | `customer` and `admin` roles, ISO currency rows, the default store with its currency/locale, the default tax category. | All, including production |
| **Demo dataset** | Runtime idempotent seeder | Stores, taxonomy, catalog, options, inventory, shipping/tax/promotion configuration, admin and customer users, sample orders. | Development and Test only |

Rules carried from `03/03-04`:

- Reference rows use **fixed ids** so later migrations can update them deterministically.
- The runtime seeder is **keyed by stable natural identifiers** (store slug, product slug, variant
  SKU, user email) and upserts, so running it twice does not duplicate data.
- **Mutable business state is never seeded in a migration.** Orders, payments, and stock movements
  are created through domain operations by the runtime seeder so their invariants and events hold.
- The seeder is **disabled outside Development and Test** and refuses to run in `Production`.

---

## Apply Commands

Run everything from the repository root. The order is: start the database, apply migrations, then
seed.

```bash
# 1. start PostgreSQL 18 (05/01-03)
docker compose up -d

# 2. apply every module migration and create the schemas
dotnet run --project src/Resys.Api -- migrate

# 3. load the development demo dataset (idempotent)
dotnet run --project src/Resys.Api -- seed --profile demo
```

Step 2 applies each module's migration history in turn (`03/03-04`); `Resys.Api` is the startup
project so the host's module registration and connection configuration are used. Step 3 runs the
runtime seeder against `appsettings.Development.json` (`05/01-02`).

Variants:

```bash
dotnet run --project src/Resys.Api -- seed --profile demo --verify   # seed, then print row counts
dotnet run --project src/Resys.Api -- seed --profile demo --dry-run  # report planned writes, change nothing
dotnet run --project src/Resys.Api -- seed --reset-demo             # remove demo rows, keep reference data
```

## Reset

To return to a clean, freshly seeded state, drop the data volume and repeat the apply steps:

```bash
docker compose down -v          # destructive: deletes the local database volume
docker compose up -d
dotnet run --project src/Resys.Api -- migrate
dotnet run --project src/Resys.Api -- seed --profile demo
```

`--reset-demo` is the lighter option when only the demo data is wrong: it deletes rows the seeder
owns and re-creates them, leaving reference data and the schema untouched. The seeder never deletes
rows it did not create.

---

## Dataset

### Stores

| Slug | Name | Domain (dev) | Default currency | Locales |
|---|---|---|---|---|
| `resys-main` | Resys Main | `localhost:3000` | `USD` | `en-US` (default) |
| `resys-eu` | Resys EU | `eu.localhost:3000` | `EUR` | `de-DE` (default), `en-US` |

Each store carries checkout options, its default tax category, an email from-address, and branding
(`05/01-02`). Store resolution in development uses the `X-Store` header, so both stores are
reachable from one host.

### Users and Roles

| Email | Role | Store scope | Purpose |
|---|---|---|---|
| `admin@resys.test` | `admin` | platform admin (all stores) | Back-office administration and gateway/promotion config. |
| `eu-admin@resys.test` | `admin` | `resys-eu` only | Exercises store-scoped authorization (`03/04-03`). |
| `shopper@resys.test` | `customer` | n/a | Account, address book, wishlist, and order history. |
| `guest@resys.test` | `customer` | n/a | A second customer for review and order samples. |

Seeded passwords are development-only and read from `Seed__AdminPassword` and `Seed__CustomerPassword`
(default `Passw0rd!` locally); the seeder refuses to run in `Production`.

### Taxonomy

A category tree, translated per store locale (`03/03-03`):

- **Clothing** — Tops (T-Shirts, Hoodies), Bottoms (Jeans, Shorts)
- **Accessories** — Bags, Hats
- **Home & Garden** — Kitchen, Outdoor

### Catalog, Options, and Variants

Twelve products span the categories so listing filters, facets, and pagination are exercised:

| Product | Slug | Category | Option type(s) | Variants | Notes |
|---|---|---|---|---|---|
| Classic Tee | `classic-tee` | Clothing / T-Shirts | Size (S, M, L, XL), Color (Black, White) | 8 | Per-store prices; one variant backorderable. |
| Pima Crew Tee | `pima-crew-tee` | Clothing / T-Shirts | Size (S, M, L) | 3 | Second T-Shirts product for listing filters. |
| Heavy Hoodie | `heavy-hoodie` | Clothing / Hoodies | Size (S, M, L, XL) | 4 | Weight set for shipping calculation. |
| Rain Shell | `rain-shell` | Clothing / Tops | Size (S, M, L, XL), Color (Black, Moss) | 8 | Two option types on a non-tee top. |
| Slim Jeans | `slim-jeans` | Clothing / Jeans | Waist (30, 32, 34), Length (30, 32) | 6 | Tall/weight for shipping. |
| Denim Shorts | `denim-shorts` | Clothing / Shorts | Waist (30, 32, 34) | 3 | Warm-weather sibling of Slim Jeans. |
| Canvas Tote | `canvas-tote` | Accessories / Bags | — | 1 | Single-variant product. |
| Leather Belt | `leather-belt` | Accessories | Size (S, M, L) | 3 | Non-apparel sizing option. |
| Wool Hat | `wool-hat` | Accessories / Hats | Color (Charcoal, Navy) | 2 | Reduced-tax category. |
| Chef Knife | `chef-knife` | Home & Garden / Kitchen | — | 1 | Non-clothing category. |
| Cast-Iron Pan | `cast-iron-pan` | Home & Garden / Kitchen | — | 1 | Heavy item for shipping-weight cases. |
| Garden Trowel | `garden-trowel` | Home & Garden / Outdoor | — | 1 | Outdoor category for facet coverage. |

- **Option types** — `Size`, `Color`, `Waist`, `Length`; option values are the lists above.
- **Prices** — every variant carries a price per store currency (`USD` and `EUR`), expressed as
  `Money` (`03/03-03`), so multi-currency display is seeded.
- **Images** — the seeded products reference local placeholder objects created through
  `IFileStorage`, so the image pipeline works with the `local` backend (`05/01-02`).

### Inventory

| Location | Store scope | Purpose |
|---|---|---|
| `main-warehouse` | both | Primary stock; backorders allowed. |
| `retail-store` | `resys-main` | Walk-in stock; backorders disallowed. |

`StockItem` rows cover a representative subset of variants at each location, with a `StockMovement`
ledger of receipts and adjustments so the inventory views (`03/02-06`) render history. At least one
variant is seeded at zero available with backorders disabled to exercise the out-of-stock path.

### Shipping

- **Zones** — `Domestic` (US) and `EU`.
- **Methods** — `Flat Rate` (fixed), `Free Shipping` (order total threshold), `Expedited`
  (per-item plus base). Each is attached to a zone and store and backed by a shipping calculator
  (`03/02-02`).

### Tax

- **Jurisdictions** — a country/region hierarchy: `US > CA` and `DE`.
- **Rates** — `US-CA` 8.5% on the default category; `DE` 19% VAT on the default category and a
  reduced rate on the `Clothing` tax category.
- Inclusive vs exclusive pricing is set per store so both modes are represented (`03/02-05`).

### Promotions

| Name | Rule | Action | Window |
|---|---|---|---|
| `WELCOME10` | Coupon `WELCOME10` | 10% off the order | open |
| `FREESHIP-75` | Order total ≥ 75 | Free shipping | open |
| `SUMMER-CLOTHING` | Category = Clothing | 15% off matched line items | a fixed seasonal window |

Each promotion exercises a different rule/action pair from the promotion engine (`03/02-04`), so
adjustments, per-line clamping, and coupon validation are all visible with seeded data.

### Sample Orders

A small set of orders, created through the order state machine (`03/02-03`) so each emits its domain
events and outbox rows:

| State | Count | Purpose |
|---|---|---|
| `cart` | 1 | Abandoned cart for the customer account. |
| `awaiting_payment` | 1 | Checkout at the payment step. |
| `awaiting_fulfillment` | 1 | Paid order ready to ship. |
| `shipped` | 1 | Fulfilled order with a shipment. |
| `completed` | 1 | Completed order for reports and history. |
| `returnable` | 1 | Completed order inside the return window. |
| `canceled` | 1 | Canceled order for state-history tests. |

Reviews and two wishlist entries are seeded for the customer accounts so those surfaces render
without manual setup.

---

## Idempotency and Safety

- **Keyed upserts.** Every demo row is matched on a stable key — store slug, product/variant slug or
  SKU, option value name, user email, coupon code. Re-running the seeder updates the row instead of
  inserting a duplicate.
- **Owned rows only.** The seeder records and deletes only rows it created; reference data and any
  hand-made records are never removed by `--reset-demo`.
- **Environment guard.** The seeder refuses to run when `ASPNETCORE_ENVIRONMENT=Production`, and it
  is not invoked automatically on host start.
- **No real credentials.** Seeded gateway settings are test-mode values or omitted entirely; no live
  key, PAN, or personal data is ever seeded (`05/01-02`).

## Verify

After seeding, `--verify` prints the counts below; the expected values are the dataset above.

| Check | Expected |
|---|---|
| Stores | 2 |
| Products | 12 |
| Variants | 41 |
| Stock locations | 2 |
| Shipping methods | 3 |
| Tax rates | 3 |
| Promotions | 3 |
| Admin users | 2 |
| Customer users | 2 |
| Sample orders | 7 (one per state) |

The API `/health/ready` endpoint and the admin dashboard KPIs (`04/02-01`) are the end-to-end smoke
check that the seeded data is queryable.

## Open Questions

1. The exact placeholder image assets and their object keys are confirmed when the catalog admin
   flow is implemented (`03/04-06`); the seeder writes through `IFileStorage` either way.
2. Whether integration and E2E tests consume this dataset directly or build their own fixtures is a
   test-strategy choice owned by `06/01-01`; the dataset remains the shared development default.
