# Review Policy

Defines who reviews a change, what they check, and the evidence a pull request must carry.

**Links:** [Contributing](./01-contributing.md) · [Code of conduct](./02-code-of-conduct.md) · [Governance index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Review is the project's main quality gate. It verifies that a change is correct,
traceable to a requirement or decision, documented, and proven — not merely that it
compiles. This policy applies to every pull request into `main`; the workflow that
surrounds it is described in `05/03-02` and the automated checks in `05/03-03`.

## Reviewer Responsibilities

A reviewer is accountable for the change they approve, not only for the lines they read.
Specifically, a reviewer must:

- Read the whole diff, including tests and documentation, before approving.
- Understand the intent from the pull request description and the linked requirement; if
  the intent is unclear, ask before reviewing the implementation.
- Check that the change belongs in the layer and module where it was made, per the module
  conventions in `05/02-02`.
- Verify that tests actually exercise the new behaviour and would fail without the change.
- Confirm the documentation rules in [Contributing](./01-contributing.md) were followed.
- Call out security-relevant changes explicitly: authentication, authorization, store
  scoping, input validation, secrets, and logging of personal data.
- Distinguish blocking problems from optional suggestions, and say which is which.
- Re-review after the author addresses feedback; an approval does not carry over to new
  commits that change behaviour.

Reviewers must not approve on the basis of CI alone. A green pipeline is necessary but
not sufficient.

## Required Approvals

- Every pull request requires **at least one approval** from a maintainer or code owner
  of the touched area before merge.
- Changes that alter a **locked decision** (stack, topology, API style, auth posture,
  scope exclusions) require approval from a maintainer **and** a new or superseding ADR
  under `../01-adr/`.
- Changes to the **governance area** (`docs/00-governance/`) require two approvals: one
  from a maintainer and one from a documentation owner.
- Changes that touch **authentication, authorization, or payment code** require an
  additional approval from a security-aware maintainer.
- The author may not approve their own pull request. A maintainer who authors a change
  still needs an independent approval.
- An approval is invalidated by any push that changes behaviour; the reviewer must be
  re-requested.

Where an area has no designated code owner, the default is one maintainer approval.

## What Reviewers Check

### Spec Traceability

- The change states the requirement, spec section, or ADR that motivates it.
- The behaviour matches that source; where the implementation departs from it, the
  departure is called out and either the source is corrected or an ADR is added.
- Locked decisions are not silently re-opened. REST stays REST, the topology stays a
  modular monolith, and the exclusions in [ADR 0003](../01-adr/03-no-cms-no-gql.md) stay
  excluded.
- Cross-area citations use the `AA/BB-CC` form and point at the right file.

### Link Integrity

- Every relative markdown link added or changed resolves to a real file.
- New documentation is reachable from its area `README.md` index, and the index entry is
  a link once the file exists.
- Plain-text `AA/BB-CC` citations are used for files that do not exist yet; markdown
  links are used only for files that do.
- The reviewer runs the link check from §12 of the
  [design spec](../../11-spec/README.md) and expects no
  broken-link output.

### No Placeholders

- The change contains no unfinished markers — no to-be-determined notes, action-item
  comments, fix-me notes, triple-X markers, or upper-case placeholder words — and no
  promise to finish the work later.
- Genuinely undecided items appear under an explicit `## Open Questions` section with
  enough context to resolve them later.
- The reviewer runs the placeholder scan from §12 of the
  [design spec](../../11-spec/README.md) and expects a
  clean result.

### Correctness, Security, and Tests

- The logic matches the requirement, including edge cases and failure paths.
- Authorization is applied and store scope is enforced on every admin query and mutation,
  per [ADR 0004](../01-adr/04-headless-jwt.md).
- Errors are handled and surfaced consistently; nothing sensitive is logged.
- Tests are at the correct layer, as mapped in `06/01-01`, and are deterministic.
- Migrations, if any, are reversible and reviewed by someone who understands the data
  model.

## Verification Evidence Required

A pull request is not ready for approval until it shows evidence that the change works.
Evidence belongs in the pull request description and must be reproducible.

- **Build and tests** — the exact command run and its result, for example
  `dotnet test` or the relevant per-layer command from `06/01-01`.
- **Front-end checks** — lint and type-check output for any touched Nuxt app.
- **Automated gates** — confirmation that the CI pipeline in `05/03-03` is green, with a
  link to the run.
- **Docs gates** — for documentation changes, the placeholder scan and link check output.
- **Manual evidence** — for behaviour a test does not cover, a short description of the
  manual check, the steps taken, and the observed result.
- **Traceability** — the spec section or ADR identifier the change implements.

Evidence must match the final commit. If the author pushes after providing evidence, the
evidence is stale and must be refreshed before approval.

## Open Questions

None. Approval counts, the reviewer checklist, and the evidence requirements are fixed by
this policy. Any future change to them is itself a governance change and follows the
two-approval rule above.

## Related Policies

- [Contributing](./01-contributing.md) — setup, branching, commits, pull requests, and
  documentation rules.
- [Code of conduct](./02-code-of-conduct.md) — expected behaviour during review.
- [Governance index](../README.md) — ADRs and the area index.
