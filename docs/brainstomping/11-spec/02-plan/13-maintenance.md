# Plan — Maintenance

Task 31: technical debt, dependency policy, and performance tuning.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](12-user-docs.md) · [Next →](14-root-readme-audit.md) · [Root README](../../../README.md)

---

### Task 31: Maintenance


**Files:**
- Modify: `docs/10-maintenance/README.md`
- Create: `docs/10-maintenance/01-technical-debt.md`
- Create: `docs/10-maintenance/02-dependency-policy.md`
- Create: `docs/10-maintenance/03-performance-tuning.md`

**Interfaces:**
- Consumes: `02/03`, `07/01`; Spec §4.
- Produces: `10/01` … `10/03`.

- [ ] **Step 1: Write the three maintenance files and the index**

- `01-technical-debt.md` — Debt register table (`ID`, `Area`, `Description`, `Impact`, `Proposed remedy`, `Linked risk`) seeded with known trade-offs (module boundaries, gateway abstractions, i18n coverage).
- `02-dependency-policy.md` — Update cadence; .NET LTS upgrade rule (support to 2028-11-14); Nuxt/Vue upgrade policy (Nuxt 3 EOL 2026-07-31 is why v4 is required; Vue pinned 3.5 until 3.6 GA); security-patch SLA.
- `03-performance-tuning.md` — Known hot paths (catalog search, checkout totals, promotion application), indexing strategy, caching, and measurement pointers to `06/03-02` and `08/02-*`.

Area README becomes an index.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/10-maintenance && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/10-maintenance
git commit -m "docs(maintenance): add technical debt, dependency policy, performance tuning"
```
