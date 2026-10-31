# Plan — Governance

Tasks 1–2: the ADRs and contribution policies.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](02-verification-helpers.md) · [Next →](04-requirements.md) · [Root README](../../../README.md)

---

### Task 1: Governance — ADRs and area index


**Files:**
- Modify: `docs/00-governance/README.md` (turn skeleton into an index)
- Create: `docs/00-governance/01-adr/01-stack.md`
- Create: `docs/00-governance/01-adr/02-modular-monolith.md`
- Create: `docs/00-governance/01-adr/03-no-cms-no-gql.md`
- Create: `docs/00-governance/01-adr/04-headless-jwt.md`

**Interfaces:**
- Consumes: Spec §2, §4, §5, §7, §8, §13.
- Produces: the decision record that all later docs cite (`00/01-01` … `00/01-04`).

- [ ] **Step 1: Write the four ADRs and the index**

Each ADR uses sections: `Status`, `Context`, `Decision`, `Consequences`, `Alternatives Considered`, `Links`.

- `01-stack.md` — Decision: .NET 10.0 LTS (10.0.12) + ASP.NET Core Minimal API (REST only) + EF Core 10/Npgsql/PostgreSQL 18 + two Nuxt 4 (Vue 3.5.43) apps. Alternatives: Blazor, GraphQL, microservices, .NET 8 (out of maintenance window by plan date).
- `02-modular-monolith.md` — Decision: one deployable .NET service, internal module projects (`Catalog`, `Inventory`, `Shipping`, `Tax`, `Promotions`, `Payments`, `Returns`, `Wishlists`, `Reviews`, `Accounts`, `Stores`) with DI contracts; not microservices. Link `03/02-01`, `03/02-02`.
- `03-no-cms-no-gql.md` — Decision: no CMS, no GraphQL, no Blazor, no recommendation engine. GraphQL/Blazor/rec-engine listed under Alternatives.
- `04-headless-jwt.md` — Decision: headless API; admin consumes `/api/admin` over HTTP like the storefront; JWT Bearer for API; admin stores JWT in httpOnly cookie; roles `customer`/`admin` + per-store assignment. Link `03/04-03`.

Area README becomes an index table (File · Purpose · Links) listing the 4 ADRs and the 3 policies in `02-policies/`, plus a link to the spec.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/00-governance && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check + cross-reference spot check**

Run the Link check from Verification Helpers.
Expected: no `BROKEN:` lines. Confirm each ADR links to the spec and each README entry resolves.

- [ ] **Step 4: Commit**

```bash
git add docs/00-governance
git commit -m "docs(governance): add ADRs and area index"
```

### Task 2: Governance — policies


**Files:**
- Create: `docs/00-governance/02-policies/01-contributing.md`
- Create: `docs/00-governance/02-policies/02-code-of-conduct.md`
- Create: `docs/00-governance/02-policies/03-review-policy.md`
- Modify: `docs/00-governance/README.md` (ensure the three policy links resolve)

**Interfaces:**
- Consumes: Task 1 (`00/01-01`); Spec §11, §12.
- Produces: `00/02-01` … `00/02-03`, referenced by `05/03-02` (branching) and `05/03-03` (CI).

- [ ] **Step 1: Write the three policies**

- `01-contributing.md` sections: `Getting Started` (link `05/01-01`), `Branching` (link `05/03-02`), `Commit Messages` (Conventional Commits), `Pull Requests`, `Definition of Done` (tests per `06/01-01`), `Documentation Rules` (repeat the file-format and no-placeholder rules from Global Constraints).
- `02-code-of-conduct.md` sections: `Our Pledge`, `Standards`, `Scope`, `Enforcement`, `Attribution` (Contributor Covenant).
- `03-review-policy.md` sections: `Reviewer Responsibilities`, `Required Approvals`, `What Reviewers Check` (spec traceability, link integrity, no placeholders), `Verification Evidence Required`.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/00-governance/02-policies && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/00-governance
git commit -m "docs(governance): add contributing, CoC, review policy"
```
