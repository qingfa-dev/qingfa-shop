# Plan — Root README & Final Audit

Task 32: the root README, the cross-area audit, and the author self-review notes.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](13-maintenance.md) · [Root README](../../../README.md)

---

### Task 32: Root README and final cross-area audit


**Files:**
- Modify: `README.md` (replace the stale scaffold text)
- Modify: `docs/11-spec/README.md` (index the spec and this plan)

**Interfaces:**
- Consumes: all prior tasks; Spec §11, §12.
- Produces: the entry point for the whole doc set.

- [ ] **Step 1: Rewrite the root README and spec index**

- `README.md` sections: `resys-eshop` (one-paragraph description — Spree-like .NET e-commerce, docs-first), `Architecture at a glance` (API + two Nuxt apps + PostgreSQL), `Repository Layout` (`src/`, `web/`, `tests/`, `docs/`), `Documentation Map` (table linking every `docs/00`–`docs/11` area index), `Design Spec` (link to `docs/11-spec/README.md`), `Implementation Plan` (link to `docs/11-spec/README.md`), `License`.
- `docs/11-spec/README.md` — index `01-design/` and `02-plan/` with the actual filenames.

- [ ] **Step 2: Full placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/00-governance docs/01-requirements docs/02-planning docs/03-architecture docs/04-design docs/05-development docs/06-verification docs/07-release docs/08-operations docs/09-user docs/10-maintenance && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Full link check**

Run the Link check from Verification Helpers across the deliverable areas (`docs/00`–`docs/10`; skip
`docs/11-spec/`, which records these commands and illustrative path examples).
Expected: no `BROKEN:` lines.

- [ ] **Step 4: Cross-area consistency audit**

Check and fix inline if any fail:
- Versions stated in `docs/05-development/01-setup/01-prerequisites.md`, `docs/10-maintenance/02-dependency-policy.md`, and ADR `00/01-01` match Global Constraints exactly.
- Every ADR `00/01-*` is linked from `docs/00-governance/README.md`.
- Every area README lists every file in its subfolders: verify with
  `for a in docs/0* docs/10*; do echo "== $a"; find "$a" -mindepth 2 -name '*.md' | sort; done`
  and compare against the README index tables by hand.
- The three Spec §13 open questions are each resolved with a decision in the file named by the spec (`04/02-01`, `05/01-02`, `03/04-05`).

- [ ] **Step 5: Commit**

```bash
git add README.md docs
git commit -m "docs: add root README, spec index, final consistency audit"
```

### Self-Review Notes (author checklist, completed)


- **Spec coverage:** §1–§13 each map to tasks — governance §2/§4/§7/§8 (T1–2), scope/non-goals §1 (T3), domain §6 (T4–8, T12–13), stack/versions §4 (T18, T31), layout §5 (T11), API §7 (T14, T30), extensions §8 (T11, T15), front-ends §9 (T16–17, T28–29), cross-cutting §10 (T15, T26), docs map §11 (all), verification §12 (T21–23, T30, T32), open questions §13 (resolved T15, T17, T18).
- **Placeholder scan of this plan:** no `TBD`/`TODO`/"implement later" tokens; every step carries concrete content or an exact command.
- **Type/name consistency:** file paths, `AA/BB-CC` citations, module names, and version strings are identical across tasks and match the spec's §11 tree verbatim.
