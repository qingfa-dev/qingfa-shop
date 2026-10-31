# Plan — Development

Tasks 18–20: setup, conventions, and workflow.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](07-design.md) · [Next →](09-verification.md) · [Root README](../../../README.md)

---

### Task 18: Development — setup and area index


**Files:**
- Modify: `docs/05-development/README.md`
- Create: `docs/05-development/01-setup/01-prerequisites.md`
- Create: `docs/05-development/01-setup/02-environment.md`
- Create: `docs/05-development/01-setup/03-docker-compose.md`
- Create: `docs/05-development/01-setup/04-seed-data.md`

**Interfaces:**
- Consumes: Spec §4, §5, §13.
- Produces: `05/01-01` … `05/01-04`, cited by `00/02-01` and `05/03-01`.

- [ ] **Step 1: Write the four setup files and the index**

- `01-prerequisites.md` — Exact tool versions: .NET SDK 10.0.x, Node.js (LTS compatible with Nuxt 4.5), pnpm/npm, Docker + Compose, PostgreSQL 18 (container), Git.
- `02-environment.md` — Environment variables and `appsettings` for API (DB connection, JWT, gateways, email, storage); Nuxt runtime config (`API_BASE_URL`, per-store branding); **resolve Spec §13 Open Question 2** — local disk default for dev, S3-compatible for production.
- `03-docker-compose.md` — A complete `docker-compose.yml` (PostgreSQL 18 + optional pgAdmin) with ports, volumes, healthcheck, and the exact `docker compose up` command.
- `04-seed-data.md` — Seed dataset description (stores, taxonomies, products/variants, options, shipping/tax/promotion configs, admin user) and the seed/apply command.

Area README becomes an index.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/05-development && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/05-development
git commit -m "docs(development): add setup (prerequisites, environment, compose, seed)"
```

### Task 19: Development — conventions


**Files:**
- Create: `docs/05-development/02-conventions/01-dotnet-style.md`
- Create: `docs/05-development/02-conventions/02-module-conventions.md`
- Create: `docs/05-development/02-conventions/03-vue-nuxt-style.md`
- Create: `docs/05-development/02-conventions/04-naming.md`

**Interfaces:**
- Consumes: `03/02-01`, `03/02-02`; Global Constraints.
- Produces: `05/02-01` … `05/02-04`, cited by `00/02-03` (review policy).

- [ ] **Step 1: Write the four convention files**

- `01-dotnet-style.md` — C# conventions (nullable enabled, file-scoped namespaces, records for DTOs, `async`/`await` rules, cancellation tokens, analyzers, formatting command).
- `02-module-conventions.md` — How to add a module (folder shape, EF configuration placement, endpoint registration, DI extension method), dependency rules, and the contract-only rule from `03/02-02`.
- `03-vue-nuxt-style.md` — Nuxt 4 directory conventions, `<script setup lang="ts">`, Composition API, Pinia store patterns, `useFetch`/`$fetch` with typed contracts, Tailwind utility usage, PrimeVue usage in admin.
- `04-naming.md` — Names for projects, namespaces, modules, API routes, DB tables/columns, Vue components, stores, and doc files (`AA/BB-CC` citation rule).

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/05-development/02-conventions && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/05-development/02-conventions
git commit -m "docs(development): add .NET, module, Vue/Nuxt, naming conventions"
```

### Task 20: Development — workflow


**Files:**
- Create: `docs/05-development/03-workflow/01-build-run.md`
- Create: `docs/05-development/03-workflow/02-branching.md`
- Create: `docs/05-development/03-workflow/03-ci.md`

**Interfaces:**
- Consumes: Tasks 18–19; `00/02-01`.
- Produces: `05/03-01` … `05/03-03`, cited by contributing guidelines.

- [ ] **Step 1: Write the three workflow files**

- `01-build-run.md` — Exact commands: restore/build/test the solution, run `Resys.Api`, run each Nuxt app, run migrations and seed, and the URLs/ports each serves.
- `02-branching.md` — Branch naming, Conventional Commits, PR flow, required approvals (link `00/02-03`).
- `03-ci.md` — Pipeline stages: build → unit → integration (Testcontainers/Postgres) → front-end lint/typecheck → E2E (Playwright) → docs checks (placeholder scan + link check from Verification Helpers). Include the commands per stage.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/05-development/03-workflow && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/05-development/03-workflow
git commit -m "docs(development): add build/run, branching, CI"
```
