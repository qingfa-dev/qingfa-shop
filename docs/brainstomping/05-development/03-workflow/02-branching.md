# Branching and Commits

The branch naming, commit message, pull request, and approval rules a contributor follows from first commit to merge.

**Links:** [Contributing](../../00-governance/02-policies/01-contributing.md) · [Review policy](../../00-governance/02-policies/03-review-policy.md) · [Build and run](./01-build-run.md) · [CI](./03-ci.md) · [Naming](../02-conventions/04-naming.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file turns the governance policies into the concrete mechanics of a change: how a branch is
named, how its commits are written, how a pull request is opened and reviewed, how many approvals it
needs, and when it may be merged. It is the workflow companion to `05/03-01` (local commands) and
`05/03-03` (the automated gates).

The source of truth is the [contributing guide](../../00-governance/02-policies/01-contributing.md)
(`00/02-01`), which owns the Definition of Done, and the
[review policy](../../00-governance/02-policies/03-review-policy.md) (`00/02-03`), which owns the
approval counts. If this file ever conflicts with either, those policies win and this file is
corrected.

---

## Branch Model

- `main` is the only long-lived branch and is always deployable.
- Work happens on short-lived branches that branch from an up-to-date `main` and are deleted after
  merge.
- **Never commit directly to `main`.** A merge into `main` goes through a pull request that passes
  CI (`05/03-03`) and review.
- Keep one concern per branch. A branch that mixes a refactor with a behaviour change is hard to
  review and hard to revert.
- Rebase the branch on `main` before opening or updating a pull request. Do not merge `main` into a
  feature branch just to catch up.

---

## Branch Naming

The pattern is `<type>/<short-description>`:

```
feat/catalog-variations
fix/orders-store-scope
docs/review-policy
ci/docs-checks
```

- The description is lower case, hyphen-separated, and short — it names the change, not the ticket.
- `<type>` is one of `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `perf`, `ci`; the same
  vocabulary opens a Conventional Commit (below).
- One branch carries one concern and, ideally, one reviewable change set.

---

## Conventional Commits

Every commit message uses [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

| Part | Rule |
|---|---|
| `<type>` | `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, or `chore`. |
| `<scope>` | The module or area touched — `catalog`, `orders`, `api`, `admin`, `governance`. Optional for repository-wide changes. |
| `<subject>` | Imperative mood, lower case, no trailing period, at most about 72 characters. Write "add tax rate endpoint", not "added" or "adds". |
| `<body>` | What changed and why, wrapped at 72 columns. The subject says what; the body says why. |
| `<footer>` | References the issue and notes a breaking change with `BREAKING CHANGE:`. |

Examples:

```
feat(catalog): add variant option type endpoints
fix(orders): scope stock reservation to the order store
docs(governance): add contributing, CoC, review policy
refactor(tax)!: drop the legacy jurisdiction cache

BREAKING CHANGE: TaxRate now requires an explicit jurisdiction id.
```

- A commit should build and pass tests on its own.
- Avoid work-in-progress commits on a branch under review; squash them locally instead.
- Reference the governing spec section or ADR identifier in the body when it is not obvious —
  for example `00/01-01` or §6 of the spec.

---

## Pull Request Flow

1. **Rebase and push.** Bring the branch up to date with `main`, push it, and open the pull request
   against `main`.
2. **Describe the change.** The description states what changed, why, which spec section or ADR
   justifies it, and how it was verified.
3. **Keep it focused.** Reviewers read the whole diff; a large mixed pull request is sent back.
4. **Link traceability.** Cite the tracked issue and the governing spec section or ADR by
   identifier, for example `00/01-01` or `03/03-04`.
5. **Green CI first.** The full pipeline in `05/03-03` must pass before review is requested.
6. **Address feedback with new commits.** Do not force-push over a review in progress unless asked.
7. **Merge when approved and green.** The author merges once approval is recorded and CI is green,
   then deletes the branch.
8. **Update docs in the same pull request.** A code change whose documentation lands later is
   incomplete (`00/02-01`).

Reviewers must not approve on the basis of CI alone; a green pipeline is necessary but not
sufficient (`00/02-03`).

---

## Required Approvals

Approval counts are owned by the [review policy](../../00-governance/02-policies/03-review-policy.md)
(`00/02-03`). In summary:

| Change | Approvals required |
|---|---|
| Any pull request | At least one maintainer or code owner of the touched area. |
| Alters a locked decision (stack, topology, API style, auth posture, scope exclusions) | A maintainer **and** a new or superseding ADR under `00/01-*`. |
| Touches `docs/00-governance/` | One maintainer and one documentation owner. |
| Touches authentication, authorization, or payment code | An additional security-aware maintainer. |
| No designated code owner for the area | One maintainer approval is the default. |

Additional rules:

- The author may not approve their own pull request; a maintainer who authors a change still needs an
  independent approval.
- An approval is invalidated by any push that changes behaviour; the reviewer must be re-requested.
- Evidence in the description must match the final commit; the review policy lists the evidence a
  pull request must carry (`00/02-03`).

---

## Definition of Done

A change is complete only when every item in the contributing guide's Definition of Done is true
(`00/02-01`):

- [ ] Acceptance criteria are met and demonstrable.
- [ ] Tests are added or updated at the right layer (`06/01-01`).
- [ ] The solution builds and the full test suite passes locally.
- [ ] Formatting, linting, and type checking pass for the touched projects.
- [ ] Documentation is updated and follows the documentation rules.
- [ ] The placeholder scan and link check used by CI (`05/03-03`) are clean.
- [ ] No secrets, credentials, or personal data are committed or logged.
- [ ] An ADR was added or superseded if the change alters a locked decision.
- [ ] The approvals required by `00/02-03` are recorded.

If an item cannot be met, say so explicitly in the pull request and record the gap under an open
question rather than leaving it implied.

---

## Merge Strategy and History

- The branch is rebased on `main` before merge, so the change set applies cleanly.
- The author merges once approved and green; the approver does not merge on the author's behalf.
- Delete the branch after merge.

## Open Questions

1. Whether `main` keeps a linear history through squash merges or records a merge commit per pull
   request is a repository setting finalized with the CI scaffold (`05/03-03`); the rebase-first and
   one-concern rules are fixed here.
2. The exact issue tracker and its identifier format are not fixed by this file; the pull request
   description links the tracked issue once the tracker is chosen.

## Links

- [Contributing](../../00-governance/02-policies/01-contributing.md) — setup, commit, and
  documentation rules plus the Definition of Done.
- [Review policy](../../00-governance/02-policies/03-review-policy.md) — approvals and the reviewer
  checklist.
- [Code of conduct](../../00-governance/02-policies/02-code-of-conduct.md) — expected behaviour in
  project spaces.
- [Build and run](./01-build-run.md) · [CI](./03-ci.md)
- [Naming](../02-conventions/04-naming.md) — names across code and docs, including citations.
- [Governance index](../../00-governance/README.md) · [ADR 0001](../../00-governance/01-adr/01-stack.md)
- [Design spec](../../11-spec/README.md) — §11 (documentation map),
  §12 (verification).
