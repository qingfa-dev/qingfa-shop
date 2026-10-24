---
name: folder-readme-guide
description: Folder-level README files for project folders. Use when documenting a folder for humans and agents — navigation, summary, assumptions, boundaries, ownership. Excludes the repository root README.
---

# Folder README — File-Centric Style Guide (Extended)

**Use when:** adding, updating, or reviewing a `README.md` **inside a project folder** (not the repository root).  
**Goal:** a folder's `README.md` lets a human or agent understand what lives here, why, how it fits, what must be assumed, and where the boundaries are — without opening every file.  
**Rule:** read the folder README first; it should answer *navigation*, *summary*, *assumptions*, *boundaries*, and *ownership*.  
**Out of scope:** root `README.md` (project onboarding, install, license, contribution) — that has its own convention.

---

## 1. Core Principle

A folder `README.md` is a **local map**, not a project brochure.

It should answer five questions in order:

```text
1. What is this folder?         → Identity & Summary
2. What is inside it?           → Navigation
3. How does it fit?             → Context & Relationships
4. What must be assumed?        → Assumptions, Invariants, Trust
5. What should I not do here?   → Boundaries, Constraints, Anti-patterns
```

It is not:

- a duplicate of the root README;
- a changelog;
- a scratchpad for TODOs;
- a place to narrate every file;
- a substitute for ADRs, inline API docs, or code comments.

> **The folder README is the smallest useful introduction to a folder that a human or agent can read before touching anything inside it.**

---

## 2. Depth & Scope — Ask the User First

Folder READMEs can be added at any level. **Do not decide silently.** Ask before generating.

### 2.1 Depth question (mandatory)

```text
How deep should folder READMEs be generated?

[ ] Root only                 (out of scope for this guide)
[ ] Level 1 only              (top-level folders under root)
[ ] Level 1–2                 (recommended default)
[ ] Level 1–3                 (feature/domain granularity)
[ ] Leaf-only                 (only folders that contain files, no further subfolders)
[ ] Every folder              (may be noisy; not recommended)
[ ] Custom depth: <N>
[ ] Custom rule: <describe>
```

### 2.2 Depth guidance

| Depth choice | When appropriate | Cost |
|---|---|---|
| Level 1 only | Monorepo with a few top-level areas | Low coverage |
| Level 1–2 | Most projects; domain + subdomain | Balanced |
| Level 1–3 | Large projects with feature folders | Higher maintenance |
| Leaf-only | Deep trees where only leaves matter | Skips mid-level context |
| Every folder | Small documentation-heavy repos | Noisy; drift risk |
| Custom `<N>` | Explicit user request | User-managed |

### 2.3 Scope & ownership questions

Ask only if not answered by the user:

1. **Exclusions** — which folders should be skipped?  
   Default exclusions: `node_modules/`, `dist/`, `build/`, `bin/`, `obj/`, `.git/`, `coverage/`, `vendor/`.
2. **Generated folders** — should generated folders get a README, a stub, or nothing?
3. **Overwrite policy** — overwrite existing folder READMEs, merge, or skip?
4. **Ownership policy** — should each README declare an owning team/person?
5. **Language** — same language as the root README, or per-folder?

### 2.4 Content-shape questions

6. **Length budget** — target maximum lines per README (default: 60–120).
7. **Audience** — optimize for humans, for agents, or balanced?
8. **Diagrams** — should READMEs include Mermaid diagrams for folder relationships?
9. **Child policy** — link to child READMEs, inline-summarize them, or both?
10. **Code snippets** — include short usage examples, or link only?
11. **Reading order** — include a suggested reading order for newcomers?
12. **Cross-cutting emphasis** — should any concern be highlighted project-wide (security, performance, compliance)?
13. **Change-hint policy** — should READMEs point to where change is likely (e.g. “extend via `handlers/`”), or stay purely descriptive?
14. **Stale policy** — what should happen when a README conflicts with code? (default: code wins; README updated or flagged)

Do not proceed until depth, exclusions, overwrite, and audience are confirmed.

---

## 3. Folder Vocabulary

Before writing a README, classify the folder. Classification drives section emphasis.

### 3.1 Folder kind (what the folder *is*)

| Kind | Meaning | README emphasis |
|---|---|---|
| `module` | Cohesive feature/domain unit | Boundaries, contracts, invariants |
| `layer` | Architectural layer (application, domain, infra) | Allowed dependencies, forbidden directions |
| `aggregate` | Consistency boundary in a domain model | Invariants, lifecycle, events |
| `feature` | User-facing capability slice | Entry points, flows, feature flags |
| `package` | Publishable/consumable unit | Public API, versioning, stability |
| `namespace` | Logical grouping of related symbols | Naming rules, cohesion |
| `collection` | Heterogeneous set of related items | Selection rules, order |
| `barrel` | Re-export surface | What is public vs internal |
| `entrypoint` | Application/process entry | Start-up sequence, config |
| `resource` | Non-code assets (migrations, seeds, templates, i18n) | Format, ownership, regeneration |
| `generated` | Produced output | Generator, regeneration command, do-not-edit |
| `test` | Test-scoped code | Scope, fixtures, data ownership |
| `docs` | Documentation | Audience, structure, upkeep |
| `scripts` | Tooling and automation | Preconditions, side effects |
| `vendor` | Third-party code checked in | Provenance, update policy |
| `meta` | Repo-level concerns (CI, ADR, tooling) | Purpose, ownership |

### 3.2 Folder role (what the folder *does* in the architecture)

| Role | Meaning |
|---|---|
| `api` | Public surface consumed by other modules/services |
| `internal` | Implementation detail; not for external use |
| `shared` | Cross-cutting, dependency-light utilities |
| `kernel` | Core abstractions depended on by everything |
| `adapter` | Boundary translation to external systems |
| `port` | Interface to be implemented by adapters |
| `orchestrator` | Coordinates multiple modules |
| `worker` | Background processing |
| `scheduler` | Time/period-driven triggers |
| `plugin` | Extendable, discoverable behavior |
| `provider` | Supplies a capability |
| `consumer` | Subscribes to events/messages |

### 3.3 Relationship vocabulary (how the folder connects)

| Relation | Meaning |
|---|---|
| `parent` | Containing folder |
| `sibling` | Same-level folder |
| `child` | Immediate subfolder |
| `upstream` | Depends on this folder |
| `downstream` | This folder depends on it |
| `peer` | Symmetric relationship |
| `boundary` | Trust/ownership change point |
| `contract` | Interface or schema shared with another module |
| `invariant` | Rule that must always hold across folders |
| `lifecycle` | Ordered state transitions the folder participates in |

Use these terms in READMEs so vocabulary stays consistent across folders.

### 3.4 Documentation vocabulary

Use this vocabulary in the README so it is greppable:

```text
Summary · Contains · Boundaries · Assumptions · Invariants · Contracts
Conventions · Related · Ownership · Reading Order · Flows · Diagrams
Notes · Gaps · Temporal markers (TODO-/TEMP:/DEADLINE-)
```

---

## 4. Scope Rules

- One `README.md` per documented folder.
- Never overwrite a tooling-owned or generated README.
- Never place a folder README at the repository root under this guide; the root README follows its own convention.
- A folder README documents **its own folder**, not its children in depth. Children are summarized and linked.
- If a child folder has its own README, link to it — do not duplicate its content.
- If a child folder does not have its own README (due to depth limit), summarize it inline and mark it `_(no README)_`.
- If a README conflicts with code, **code wins**; update the README or flag it in `Notes`.

---

## 5. Required & Optional Sections

Use this order. Required sections must appear unless genuinely empty. Optional sections are marked.

| # | Section | Required | Purpose |
|---|---|---|---|
| 1 | `# <Folder Name>` | Required | Title + one-line purpose |
| 2 | `## Summary` | Required | What this folder is, in 2–4 sentences |
| 3 | `## Contains` | Required | Navigation table of files/subfolders |
| 4 | `## Boundaries` | Required | What belongs here and what does not |
| 5 | `## Assumptions` | Required | Preconditions, dependencies |
| 6 | `## Invariants` | Optional | Rules that must always hold |
| 7 | `## Contracts` | Optional | Public API / schema / interfaces exposed |
| 8 | `## Conventions` | Optional | Local rules refining project-wide rules |
| 9 | `## Flows` | Optional | Key end-to-end sequences inside the folder |
| 10 | `## Reading Order` | Optional | Suggested reading for newcomers |
| 11 | `## Related` | Required | Parent, siblings, children, ADRs |
| 12 | `## Ownership` | Optional | Owning team/person, review requirements |
| 13 | `## Notes` | Optional | Temporal markers, gaps, open questions |

Do not add ad-hoc sections. If a section is needed repeatedly, extend this guide deliberately.

---

## 6. Section Content Rules

### 6.1 Title

```markdown
# orders

Order lifecycle, checkout orchestration, and persistence for customer orders.
```

- One line, no trailing period.
- Say what the folder *is*, not what it *does mechanically*.
- Optionally include the folder kind/role in a subtitle: `_module · boundary_`.

### 6.2 Summary

- 2–4 sentences.
- Answer: what, why, who depends on it.
- No bullet sprawl; no restating the folder name.

Good:

```markdown
## Summary

This folder owns the order aggregate and everything that mutates it.
It exposes application services used by the checkout and fulfillment
modules. Persistence is via `IOrderRepository`; direct database access
is not allowed outside this folder.
```

Bad:

```markdown
## Summary

- Contains orders.
- Files are here.
- Used by other parts of the app.
```

### 6.3 Contains (Navigation)

A table. One row per meaningful file or subfolder.

```markdown
## Contains

| Path | Role | Notes |
|---|---|---|
| `order.service.ts` | Application service | Orchestrates place/cancel |
| `order.repository.ts` | Persistence | Only reader/writer of `orders` table |
| `order.guard.ts` | Access control | Blocks mutation of settled orders |
| `order.spec.ts` | Tests | Covers place/cancel/refund |
| `handlers/` | Event handlers | See [`handlers/README.md`](./handlers/README.md) |
| `projections/` | Read models | _(no README)_ |
```

Rules:

- Use backticks for paths.
- Use the role vocabulary from §3.2 and the Dotted File Naming guide (§2).
- Link to child READMEs when they exist.
- Mark undocumented child folders as `_(no README)_`.
- Skip generated artifacts unless meaningful to the reader.

### 6.4 Boundaries

```markdown
## Boundaries

In scope:
- Order aggregate and its invariants.
- Persistence and query access for orders.

Out of scope:
- Payment capture → see [`../payments/`](../payments/).
- Fulfillment decisions → see [`../fulfillment/`](../fulfillment/).
- Cross-module reporting → see [`../../analytics/`](../../analytics/).
```

Boundaries prevent accidental coupling and duplicated responsibility.

### 6.5 Assumptions

```markdown
## Assumptions

- Authentication has already run upstream; `userId` is trusted.
- `IOrderRepository` is registered in DI before this module loads.
- Currency is always ISO-4217; no conversion happens here.
- Order IDs are ULIDs and unique across tenants.
```

Use for anything a reader could plausibly get wrong.

### 6.6 Invariants (optional)

```markdown
## Invariants

- An order is always owned by exactly one customer.
- An order in `Settled` never returns to `Draft`.
- Line items are never removed after payment authorization.
```

### 6.7 Contracts (optional)

```markdown
## Contracts

- `IOrderRepository` — consumed by checkout, fulfillment.
- `OrderPlacedEvent` — published to `orders.events`.
- `POST /orders` — external API surface (see `../../api/README.md`).
```

### 6.8 Conventions (optional)

```markdown
## Conventions

- One aggregate per file; do not merge `Order` and `OrderItem`.
- Handlers are idempotent; see [`../README.md#eventing`](../README.md#eventing).
- No `await` inside repository methods; callers own timing.
```

Only refine or differ from project-wide rules; do not repeat them.

### 6.9 Flows (optional)

```markdown
## Flows

### Place order
1. `order.controller` validates input.
2. `order.service.place` creates the aggregate.
3. `OrderPlacedEvent` is raised.
4. `handlers/` persists and publishes.

### Cancel order
- Blocked if order is `Settled`; see `order.guard.ts`.
```

### 6.10 Reading Order (optional)

```markdown
## Reading Order

1. `order.entity.ts` — the aggregate.
2. `order.service.ts` — the use cases.
3. `order.repository.ts` — persistence.
4. `handlers/` — event handling.
```

### 6.11 Related

```markdown
## Related

- Parent: [`../README.md`](../README.md)
- Sibling: [`../payments/README.md`](../payments/README.md)
- Child: [`handlers/README.md`](./handlers/README.md)
- ADR: [`../../docs/adr/0007-order-state-machine.md`](../../docs/adr/0007-order-state-machine.md)
```

### 6.12 Ownership (optional)

```markdown
## Ownership

- Owning team: **Order Platform**.
- Review required from: Payments, Fulfillment.
- Deprecated: `v1` handlers are read-only; contact Order Platform before change.
```

### 6.13 Notes (optional)

```markdown
## Notes

- `TODO-2026Q2:` Split refund logic out of `order.service.ts`.
- `TEMP:` Fallback for legacy `v1` orders until migration completes.
- Known gap: no test coverage for tenant-scoped queries.
```

Use the same temporal markers as the Code Comment Labels guide.

---

## 7. Folder README vs Other Docs

| Document | Scope | Owner |
|---|---|---|
| Root `README.md` | Project onboarding, install, license, contribution | Project maintainers |
| Folder `README.md` | Local map: summary, navigation, assumptions, boundaries | Folder owners |
| ADR | Durable architectural decisions | Architecture group |
| Code comment (`// Label:`) | Single non-obvious intent at a code site | Code author |
| Inline API docs (JSDoc/XML) | API-level contract for a symbol | Symbol author |
| Diagrams | Structural or flow visualization | Diagram owner |

A folder README is **not** a substitute for any of the others.

---

## 8. Writing Rules

- **Be local.** Describe this folder, not the project.
- **Be terse.** Target 60–120 lines. Split or prune if longer.
- **Be stable.** Avoid detail that will drift.
- **Be directional.** Link rather than duplicate.
- **Be honest.** Mark gaps as gaps.
- **Be greppable.** Use the fixed section names.
- **No free-text sections.** If a new section is needed repeatedly, extend this guide.
- **No changelog.** Git history owns that.
- **No root-level content.** Install, license, contribution belong to the root README.
- **No dead links.** Every relative link must resolve.
- **No mixed languages** unless the root README establishes it.

---

## 9. Agent Procedure

### Step 1 — Confirm depth and policy

Ask the questions in §2.1 and §2.3.  
Do not proceed until the user confirms depth, exclusions, overwrite policy, and audience.

### Step 2 — Walk the tree

- Enumerate folders up to the confirmed depth.
- Apply exclusions.
- Detect existing folder READMEs and ownership.
- Apply overwrite policy.

### Step 3 — Classify each folder

For each folder, determine:

- kind (§3.1);
- role (§3.2);
- relationships (§3.3) — parent, siblings, children, contracts.

### Step 4 — Gather evidence

- Immediate children (files + subfolders).
- Apparent roles from filenames (Dotted File Naming vocabulary).
- Existing inline docs indicating intent.
- DI registrations, exports, or barrels that reveal public surface.
- Tests that reveal invariants and flows.

### Step 5 — Draft using the fixed sections

Use §5 order. Include required sections. Include optional sections where they add real value.

### Step 6 — Validate against constraints

- Length within budget.
- No duplication of root README.
- Child READMEs linked, not duplicated.
- Temporal markers used for temporary notes.
- Assumptions present (or explicitly omitted when none apply).
- Every relative link resolves.

### Step 7 — Present for review

- Show the tree of folders that will receive READMEs.
- Show a preview of one leaf and one mid-level README.
- Do not write files until the user approves.

### Step 8 — Write and link

- Write the README files.
- Update parent READMEs’ `Contains` tables to link to new child READMEs.
- Do not touch the root README.

---

## 10. Human Review Checklist

```text
Depth & scope
[ ] Depth matches the user's chosen depth?
[ ] Exclusions respected (node_modules, dist, generated, etc.)?
[ ] Overwrite policy respected?
[ ] No root README modified by this guide?

Content
[ ] Fixed section order respected?
[ ] Summary is 2–4 sentences and says what, why, who?
[ ] Contains is a table of files/subfolders with roles?
[ ] Boundaries state in-scope and out-of-scope with links?
[ ] Assumptions capture preconditions and trust boundaries?
[ ] Invariants/Contracts/Flows present where meaningful?
[ ] Conventions only refine or differ from project-wide rules?
[ ] Reading Order helpful for newcomers?
[ ] Related links parent, siblings, children, ADRs?
[ ] Ownership declared when project requires it?
[ ] Notes use temporal markers where applicable?

Quality
[ ] Child READMEs linked, not duplicated?
[ ] No changelog, no install instructions, no license text?
[ ] Length within the agreed budget?
[ ] No free-text section names invented?
[ ] Every relative link resolves?
[ ] No stale assumptions or dead references?
```

---

## 11. Anti-Patterns

| Anti-pattern | Bad | Good |
|---|---|---|
| Root-level content in folder README | Install steps, license, contributing | Link to root README |
| Narrating every file | “This file does X” for generated code | List only meaningful entries |
| Duplicating child README | Copy of child’s Summary | Link to child README |
| Changelog in README | “v1.2 added retries” | Git history |
| Free-text sections | `## Random Thoughts` | Use the fixed section set |
| Silent depth choice | Agent picks depth alone | Ask the user (§2) |
| Silent overwrite | Agent replaces existing README | Ask overwrite policy (§2.3) |
| Stale assumptions | Outdated preconditions | Revisit when interfaces change |
| Mixed languages | Sections in different languages | One language per repo |
| Tooling-owned README overwritten | Regenerating a managed file | Respect ownership |
| Undocumented children silently listed | `handlers/` with no marker | `handlers/ _(no README)_` |
| Dead links | `../../moved-folder/` | Verify every path |
| Orphan README | No parent link | Link parent in `Related` |

---

## 12. Templates

### 12.1 Leaf folder (has files, no subfolders)

```markdown
# <folder-name>

One-line purpose.

## Summary
2–4 sentences.

## Contains
| Path | Role | Notes |
|---|---|---|

## Boundaries
In scope / Out of scope.

## Assumptions
Preconditions, dependencies.

## Related
Parent · siblings · ADRs.
```

### 12.2 Mid-level folder (has subfolders)

```markdown
# <folder-name>

One-line purpose. _<kind> · <role>_

## Summary
2–4 sentences.

## Contains
| Path | Role | Notes |
|---|---|---|
| `subfolder/` | <role> | See `subfolder/README.md` |
| `file.ts` | <role> | <notes> |

## Boundaries
In scope / Out of scope (with links).

## Assumptions
Preconditions, dependencies.

## Invariants
Rules that must always hold.

## Contracts
Public APIs / events / schemas.

## Related
Parent · siblings · children · ADRs.

## Ownership
Owning team, review requirements.
```

### 12.3 Boundary folder (module/package)

```markdown
# <folder-name>

One-line purpose. _module · boundary_

## Summary
What this module owns and exposes.

## Contains
Table.

## Boundaries
In scope / Out of scope.

## Assumptions
Trust, DI, config.

## Invariants
Cross-cutting rules.

## Contracts
Public surface: interfaces, events, HTTP.

## Conventions
Local refinements.

## Flows
Key end-to-end sequences.

## Reading Order
For newcomers.

## Related
Parent · siblings · children · ADRs.

## Ownership
Owning team, review requirements, deprecations.

## Notes
Temporal markers, gaps.
```

---

## 13. Compact Reference

### Folder README shape

```text
# <folder-name>
one-line purpose

## Summary          required
## Contains         required
## Boundaries       required
## Assumptions      required
## Invariants       optional
## Contracts        optional
## Conventions      optional
## Flows            optional
## Reading Order    optional
## Related          required
## Ownership        optional
## Notes            optional
```

### Depth question (ask first)

```text
How deep should folder READMEs be generated?
[ ] Root only  [ ] Level 1  [ ] Level 1–2 (default)  [ ] Level 1–3
[ ] Leaf-only  [ ] Every folder  [ ] Custom: <N>  [ ] Custom rule: <describe>
```

### Default exclusions

```text
node_modules/  dist/  build/  bin/  obj/  .git/  coverage/  vendor/
```

### Folder vocabulary

```text
Kind:   module · layer · aggregate · feature · package · namespace · collection ·
        barrel · entrypoint · resource · generated · test · docs · scripts · vendor · meta
Role:   api · internal · shared · kernel · adapter · port · orchestrator · worker ·
        scheduler · plugin · provider · consumer
Rel:    parent · sibling · child · upstream · downstream · peer · boundary ·
        contract · invariant · lifecycle
```

### Documentation vocabulary

```text
Summary · Contains · Boundaries · Assumptions · Invariants · Contracts ·
Conventions · Flows · Reading Order · Related · Ownership · Notes · Gaps
Temporal: TODO-<period>: · TEMP: · DEADLINE-<date>:
```

---

## 14. One-Sentence Rule

> **Give every documented folder a `README.md` that says what the folder is, what it contains, what must be assumed, what must always hold, where its boundaries are, and who owns it — at the depth the user chose, in a fixed section order, using the shared folder vocabulary, and linking to children instead of duplicating them.**