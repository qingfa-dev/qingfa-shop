# Plan — Release

Task 24: versioning, release notes, deployment, and rollback.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](09-verification.md) · [Next →](11-operations.md) · [Root README](../../../README.md)

---

### Task 24: Release — versioning, notes, deployment, rollback


**Files:**
- Modify: `docs/07-release/README.md`
- Create: `docs/07-release/01-versioning.md`
- Create: `docs/07-release/02-release-notes.md`
- Create: `docs/07-release/03-deployment.md`
- Create: `docs/07-release/04-rollback.md`

**Interfaces:**
- Consumes: `05/03-01`, `06/01-01`.
- Produces: `07/01` … `07/04`, cited by `08/01-02`, `10/02`.

- [ ] **Step 1: Write the four release files and the index**

- `01-versioning.md` — Semantic versioning, API versioning (`/api/v1` additive until a break), DB migration versioning, changelog rules.
- `02-release-notes.md` — Template plus a complete `v0.1.0` entry (initial platform foundation).
- `03-deployment.md` — Environments (dev/staging/prod), container build/publish for `Resys.Api` and the two Nuxt apps, PostgreSQL migration step, env-var matrix, K8s notes.
- `04-rollback.md` — App rollback, migration rollback/forward-fix, feature-flag kill switch.

Area README becomes an index.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/07-release && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/07-release
git commit -m "docs(release): add versioning, notes, deployment, rollback"
```
