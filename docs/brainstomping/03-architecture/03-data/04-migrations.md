# Migrations

The EF Core migration workflow per module, migration naming, the seed strategy, and the rollback expectations for schema change.

**Links:** [Entity model](./01-entities.md) · [Multi-store scoping](./02-multi-store.md) · [i18n & money](./03-i18n-money.md) · [Solution layout](../02-components/01-solution-layout.md) · [Module contracts](../02-components/02-module-contracts.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Schema change is managed with **EF Core migrations**, one history per module. This file states where
migrations live, the command workflow, the naming convention, how reference and demo data are
seeded, and what rollback means for a production database. The entity shapes being migrated are
`03/03-01`; the project layout that owns the `DbContext`s is `03/02-01`; the seed dataset itself is
documented in `05/01-04`.

The guiding rule: **migrations are append-only and every module owns its own migration history, so
one module's schema evolves without coordinating with another's.**

---

## Ownership and Placement

- Each `Resys.Modules.<Name>` project owns its `DbContext`, its `IEntityTypeConfiguration<T>`
  classes, and its migrations under `Persistence/` (`03/02-01`).
- Each module writes to its own PostgreSQL schema (`catalog`, `orders`, `inventory`, `shipping`,
  `tax`, `promotions`, `payments`, `returns`, `wishlists`, `reviews`, `accounts`, `stores`) inside
  the single resys-eshop database, with its own `__EFMigrationsHistory` table (`03/02-01`).
- Cross-module foreign keys are **not** created; a module references another module's aggregate by
  typed id only, and integrity across the boundary is maintained through contracts and domain
  events (`03/02-02`). Within a module, FKs and cascades are declared in the module's own
  configuration.
- A module's migration never edits another module's schema; a change that appears to need that is
  a boundary violation to resolve with a contract or event, not a migration.

---

## Workflow

Migrations are generated and applied with the `dotnet ef` tooling against `Resys.Api` as the
startup project, so the host's module registration and connection configuration are used.

```bash
# add a migration to one module (run from the repository root)
dotnet ef migrations add <Name> \
  --project src/Resys.Modules.<Name> \
  --startup-project src/Resys.Api \
  --context <Name>DbContext

# review the generated migration before applying it
dotnet ef migrations script \
  --project src/Resys.Modules.<Name> \
  --startup-project src/Resys.Api \
  --context <Name>DbContext \
  --idempotent -o /tmp/<name>.sql

# apply locally
dotnet ef database update \
  --project src/Resys.Modules.<Name> \
  --startup-project src/Resys.Api \
  --context <Name>DbContext
```

Steps for any schema change:

1. Change the entity or its `IEntityTypeConfiguration<T>` in the owning module.
2. Add the migration with a descriptive name (below) and review the generated `Up`/`Down`.
3. Apply it locally and run the module's integration tests, which apply migrations to a
   Testcontainers PostgreSQL instance (`03/02-01`).
4. Commit the entity change and the migration together; a schema change without its migration is
   incomplete.
5. Production applies migrations at deploy time from a migration bundle, or via a single migrator
   job; the API never migrates at startup.

Because each module has an independent history, migrations from different modules are applied
independently and are not ordered relative to one another.

---

## Naming

- EF prefixes the file and class with a UTC timestamp; the **name** is the part you choose.
- Use `Initial<Module>` for a module's first migration and a short PascalCase description of the
  change thereafter: `InitialCatalog`, `AddVariantWeight`, `AddStockItemRowVersion`,
  `AddPromotionUsageLimit`.
- Name for the schema effect, not the ticket: `AddOrderNumberIndex` rather than `Ticket1234`.
- Avoid generic names (`Update`, `Changes`, `Migration1`); the timestamp already orders migrations,
  so the name carries the intent.
- The migration file, the snapshot, and the designer file are all committed; never hand-edit the
  snapshot.

---

## Seed Strategy

Seed data is split into **migration-carried reference data** and a **runtime demo dataset**; the
full dataset is documented in `05/01-04`.

- **Reference data in migrations.** Data every environment needs to function is seeded with
  `modelBuilder.Entity<T>().HasData(...)` inside the owning module's migration: the `customer` and
  `admin` roles, ISO currency rows, a default store with its default currency and locale, and the
  default tax category. `HasData` rows use fixed ids so later migrations can update them
  deterministically.
- **Demo data at runtime.** A development-only seeder populates products, variants, stock,
  promotions, and sample orders after migrations run. It is idempotent, keyed by stable
  identifiers, and invoked explicitly (`05/01-04`), not on every host start.
- **No production demo data.** The runtime seeder is disabled outside development and test; the
  production database receives schema plus reference rows only.
- **Never seed mutable business state in a migration.** Orders, payments, and stock movements are
  never `HasData` rows; they are created through domain operations so their invariants hold.

---

## Rollback Expectations

- **Every migration ships a `Down`.** The generated `Down` is reviewed and kept accurate for the
  change; a migration that cannot be reversed must say so in its class comment and in the pull
  request.
- **Forward-only in production.** Production rollback is a **new compensating migration**, not
  `dotnet ef database update <PreviousMigration>`. Applying an old migration on a live database
  risks data loss the `Down` cannot restore.
- **Expand and contract.** A breaking column change is staged: add the new column, backfill,
  switch reads and writes, then remove the old column in a later release. This keeps a rolling
  deploy safe and makes the rollback path a code revert rather than a schema revert.
- **Destructive changes need a backup.** Dropping a table or column is allowed only with a
  verified backup and an explicit release note; the compensating migration restores the shape but
  not the lost rows.
- **Idempotent scripts.** Deployment uses the `--idempotent` script or a bundle so re-running an
  apply is safe; the migrator runs as a single instance to avoid concurrent `database update`
  calls (`03/02-01`).
- **Restore is the last resort.** Point-in-time restore of PostgreSQL is the documented recovery
  path for a bad production migration; the runbook lives in `08/01-01`.

## Open Questions

1. Whether production applies migrations in a dedicated deploy job or a migration bundle invoked
   by the release pipeline is owned by `08/01-01`; the append-only, single-migrator rule is fixed.
2. The exact boundary between `HasData` reference rows and the runtime seeder is confirmed with the
   seed dataset in `05/01-04`.
3. Whether integration tests apply migrations or use `EnsureCreated` for speed is a test-strategy
   choice owned by `06/01-01`; migrations are the source of truth either way.

## Links

- [Entity model](./01-entities.md)
- [Multi-store scoping](./02-multi-store.md)
- [i18n & money](./03-i18n-money.md)
- [Solution layout](../02-components/01-solution-layout.md)
- [Module contracts](../02-components/02-module-contracts.md)
- [Design spec](../../11-spec/README.md) — §6 (data
  architecture), §5 (solution layout).
- Seed dataset: `05/01-04`; operations runbook: `08/01-01`; test strategy: `06/01-01`.
