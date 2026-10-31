# 02 — Planning

The delivery roadmap, the module backlog, and the risk register for resys-eshop.

**Links:** [Roadmap](./01-roadmap.md) · [Backlog](./02-backlog.md) · [Risks](./03-risks.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area turns the requirements (`01`) into an executable plan. It is split into three
single-concern files: how work is sequenced (`02/01`), what work exists (`02/02`), and what could
go wrong (`02/03`). All three trace back to the [design spec](../11-spec/README.md),
the single source of truth for the project.

Planning here is deliberately lightweight: the spec and the architecture (`03`) hold the durable
decisions, while this area holds the ordering, the inventory of work, and the register of risks.
When a planning detail conflicts with the spec, the spec wins and the planning file is corrected.

---

## Files

| File | Purpose | Identifier |
|---|---|---|
| [01-roadmap.md](./01-roadmap.md) | Eight delivery phases from documentation to hardening, with milestones (M0–M7) and exit criteria per phase. | `02/01` |
| [02-backlog.md](./02-backlog.md) | Candidate work items for all twelve modules, each traced to a functional requirement and prioritized with MoSCoW. | `02/02` |
| [03-risks.md](./03-risks.md) | The scored risk register, covering payment-gateway variance, tax complexity, multi-store isolation, i18n scope, and Nuxt upgrades, among others. | `02/03` |

---

## Planning Model

The three files answer three different questions and are kept independent on purpose:

- **Sequence** — `02/01` orders capabilities into phases P0–P7. It does not enumerate work; it
  states dependencies and the exit criteria that gate each phase.
- **Inventory** — `02/02` enumerates candidate work per module and assigns MoSCoW priority. It
  does not schedule; it feeds the phases with scope.
- **Uncertainty** — `02/03` records what could delay or degrade the work, with a mitigation and
  an accountable role for each risk.

Read together, a phase is "the backlog items for the modules in that phase, gated by the exit
criteria, with the active risks mitigated". The roadmap table is the join point between the
three.

---

## How to Use This Area

1. Start with the **roadmap** (`02/01`) to see the order of delivery and the exit criteria that
   gate each phase.
2. Use the **backlog** (`02/02`) to find candidate work per module and its priority; module
   requirements live in `01/02-01` … `01/02-12`.
3. Consult the **risk register** (`02/03`) before committing to a phase, and re-score risks at
   each milestone review.
4. Every doc in this set exists; cite another doc by its identifier (`AA/BB-CC`, or `AA/NN` for a
   flat area) and link it when a jump is useful.

---

## Functional-Area Index

This is a **functional-area** view, not the locked twelve-module list (that list is `01/01-03`; the
module contracts are `03/02-02`). `Checkout` is a functional area within the Orders module, and
Wishlists, Reviews, Stores, and i18n group here by requirement file rather than by module project.
Each area's functional requirements and backlog sections are:

| Functional area | Requirements | Backlog section |
|---|---|---|
| Catalog | `01/02-01` | `02/02` Catalog |
| Orders | `01/02-02` | `02/02` Orders |
| Checkout | `01/02-03` | `02/02` Orders |
| Shipping | `01/02-04` | `02/02` Shipping |
| Payments | `01/02-05` | `02/02` Payments |
| Tax | `01/02-06` | `02/02` Tax |
| Promotions | `01/02-07` | `02/02` Promotions |
| Inventory | `01/02-08` | `02/02` Inventory |
| Returns | `01/02-09` | `02/02` Returns |
| Accounts | `01/02-10` | `02/02` Accounts |
| Wishlists, Reviews | `01/02-11` | `02/02` Wishlists, Reviews |
| Stores, i18n | `01/02-12` | `02/02` Stores |

---

## Related Areas

- `01` (requirements) defines the behavior the roadmap delivers and the backlog enumerates.
- `03` (architecture) specifies how each module is built; module contracts live in `03/02-02`.
- `06` (verification) holds the test plans that prove each phase's exit criteria.
- `07` (release) freezes scope and produces the release notes and rollback plan.
- `10/01` (technical debt) records deferred work drawn from the backlog's Could items.

---

## Keeping This Area Current

- Update the roadmap when a phase's exit criteria or dependencies change; do not renumber phases
  without correcting the milestone table.
- Update the backlog when a module's requirements change in `01/02-*`, keeping item-to-requirement
  traceability.
- Re-score the register at every milestone boundary (M0–M7), as described in `02/03`.
- Any decision that changes a locked choice is recorded as an ADR in `00/01`, not edited here.

## Open Questions

None at the area level. Unresolved details are stated within the owning file under its
`## Open Questions` section.
