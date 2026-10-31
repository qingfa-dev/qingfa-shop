# Plan — Planning

Task 9: the roadmap, backlog, and risk register.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](04-requirements.md) · [Next →](06-architecture.md) · [Root README](../../../README.md)

---

### Task 9: Planning — roadmap, backlog, risks


**Files:**
- Modify: `docs/02-planning/README.md`
- Create: `docs/02-planning/01-roadmap.md`
- Create: `docs/02-planning/02-backlog.md`
- Create: `docs/02-planning/03-risks.md`

**Interfaces:**
- Consumes: Tasks 1–8.
- Produces: `02/01` … `02/03`, cited by `10/01` (technical debt).

- [ ] **Step 1: Write the three planning files and the index**

- `01-roadmap.md` sections: `Phases` (docs → platform core → catalog/order → checkout/shipping/tax/payment → promotions/inventory/returns → admin → storefront → hardening), `Milestones`, `Exit Criteria per phase`.
- `02-backlog.md` sections: `By Module` (one subsection per module with candidate work items), `Prioritization` (MoSCoW).
- `03-risks.md` sections: `Risk Register` table (`ID`, `Risk`, `Likelihood`, `Impact`, `Mitigation`, `Owner`) covering payment-gateway variance, tax-rule complexity, multi-store data isolation, i18n scope creep, Nuxt major upgrades.

Area README becomes an index.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/02-planning && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/02-planning
git commit -m "docs(planning): add roadmap, backlog, risks"
```
