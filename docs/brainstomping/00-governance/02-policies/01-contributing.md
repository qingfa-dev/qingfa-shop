# Contributing to resys-eshop

Defines how to set up, branch, commit, open a pull request, and prove that a change is done.

**Links:** [Code of conduct](./02-code-of-conduct.md) · [Review policy](./03-review-policy.md) · [Governance index](../README.md) · [Spec](../../11-spec/README.md)

---

## Getting Started

Work from a clone of the repository with the pinned toolchain installed. The exact
prerequisites — .NET SDK 10.0.x, a Nuxt 4.5-compatible Node.js LTS, the package manager,
Docker with Compose, and PostgreSQL 18 — are listed in `05/01-01`. Do not substitute
versions; the stack is locked by [ADR 0001](../01-adr/01-stack.md).

A first-time setup is:

1. Install the toolchain from `05/01-01`.
2. Configure the API and Nuxt runtime settings from `05/01-02`.
3. Start PostgreSQL 18 with the Compose file in `05/01-03`.
4. Apply migrations and load the seed dataset from `05/01-04`.
5. Build and run the API and both front-ends with the commands in `05/03-01`.

If any of those files is not yet written, follow the equivalent commands already present
in the repository root rather than inventing a new workflow.

## Branching

The branching model and naming rules are owned by `05/03-02`; this section is the summary
contributors must follow.

- Branch from an up-to-date `main`. Never commit directly to `main`.
- Use a short-lived branch named `<type>/<short-description>`, for example
  `feat/catalog-variations` or `docs/review-policy`.
- `<type>` is one of `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `perf`, `ci`.
- Keep one concern per branch. A branch that mixes a refactor with a behaviour change is
  hard to review and hard to revert.
- Rebase on `main` before opening or updating a pull request; do not merge `main` into a
  feature branch just to catch up.
- Delete the branch after it is merged.

## Commit Messages

All commits use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

- **`<type>`** — `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`,
  `ci`, or `chore`.
- **`<scope>`** — the module or area touched, for example `catalog`, `orders`, `api`,
  `admin`, or `governance`. Scope is optional for repository-wide changes.
- **`<subject>`** — imperative mood, lower case, no trailing period, at most about 72
  characters. Write "add tax rate endpoint", not "added" or "adds".
- **`<body>`** — explain what changed and why, wrapped at 72 columns. The subject says
  what; the body says why.
- **`<footer>`** — reference issues and note a breaking change with `BREAKING CHANGE:`.

Examples:

```
feat(catalog): add variant option type endpoints
fix(orders): scope stock reservation to the order store
docs(governance): add contributing, CoC, review policy
refactor(tax)!: drop the legacy jurisdiction cache

BREAKING CHANGE: TaxRate now requires an explicit jurisdiction id.
```

A commit should build and pass tests on its own. Avoid "work in progress" commits on a
branch that is under review; squash them locally instead.

## Pull Requests

- Open the pull request against `main` and fill in the description: what changed, why,
  which spec section or ADR justifies it, and how it was verified.
- Keep the change set small and focused. Reviewers are expected to read the whole diff;
  large mixed pull requests are sent back.
- Link the tracked issue and cite the governing spec section or ADR by identifier, for
  example `00/01-01` or §7 of the spec.
- Ensure the full CI pipeline in `05/03-03` is green before requesting review.
- Address review comments with new commits; do not force-push over a review in progress
  unless asked. When approved and green, the author merges.
- Update documentation in the same pull request as the change it describes. A code change
  whose docs land later is incomplete.

## Definition of Done

A change is done only when every item below is true:

- [ ] The acceptance criteria are met and demonstrable.
- [ ] Tests are added or updated at the right layer, following `06/01-01`.
- [ ] The solution builds and the full test suite passes locally.
- [ ] Formatting, linting, and type checking pass for the touched projects.
- [ ] Documentation is updated and follows the rules in the next section.
- [ ] The placeholder scan and link check used by CI in `05/03-03` are clean.
- [ ] No secrets, credentials, or personal data are committed or logged.
- [ ] An ADR was added or superseded if the change alters a locked decision.
- [ ] Review approvals required by `00/02-03` are recorded.

If an item cannot be met, say so explicitly in the pull request and record the gap as an
open question rather than leaving it implied.

## Documentation Rules

Documentation is a first-class deliverable of this project. Every file under `docs/`
follows the same rules, and a pull request that adds or edits one is reviewed against
them.

- **One concern per file.** A file explains a single subject. If it needs a second
  subject, that is a second file with its own index entry.
- **Length 100–300 lines.** Files shorter than 100 lines are usually merged with a
  sibling; files longer than 300 lines are usually split.
- **Mandatory header.** Every file starts with a level-one title, a one-line purpose, and
  a `**Links:**` block whose spec link uses the correct relative depth, for example:

  ```markdown
  # <Title>

  <One-line purpose.>

  **Links:** [Code of conduct](./02-code-of-conduct.md) · [Spec](../../11-spec/README.md)
  ```

- **No placeholders.** Never leave an unfinished marker — a to-be-determined note, an
  action-item comment, a fix-me note, a triple-X marker, or an upper-case placeholder
  word — and never defer work with a promise to finish it later. If a detail is genuinely
  undecided, put it under an explicit `## Open Questions` section.
- **Stable citations.** Reference another file by its numeric identifier, written as
  `AA/BB-CC` — for example `05/01-01` for the first setup file or `05/03-02` for the
  branching file. Cite future docs this way in plain text so the reference survives until
  the target exists; use a markdown link only when the file is present.
- **Working links.** Every relative link between doc files must resolve. Link integrity
  is checked in CI per `05/03-03`.
- **Trailing newline.** Every file ends with exactly one newline.
- **Immutable decisions.** Accepted ADRs are not edited in place; a reversal is a new ADR
  that supersedes the old one.

The documentation map and the per-area file lists live in §11 of the
[design spec](../../11-spec/README.md). The naming and
citation rules are elaborated in `05/02-04`.

## Related Policies

- [Code of conduct](./02-code-of-conduct.md) — expected behaviour in project spaces.
- [Review policy](./03-review-policy.md) — approvals and the reviewer checklist.
- [Governance index](../README.md) — ADRs and the area index.
