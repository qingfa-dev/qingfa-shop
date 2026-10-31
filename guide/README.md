# guide

Skill-style style guides for comments, file names, folder documentation, and the convention gate. _docs · meta_

## Summary

`guide/` holds the repository's living writing conventions in guide form —
documents written for both humans and agents, with frontmatter so they can
be loaded as skills. They refine project-wide rules rather than restate
them; when a guide and the code disagree, the code wins and the guide is
updated. The convention-gate guide is the exception that proves the rule:
`make convention.check` enforces it mechanically.

## Contains

| Path | Role | Notes |
|---|---|---|
| `code-comment-style-guide.md` | docs | Labelled comments (`Validate:`, `Compute:`, `Fallback:`) instead of free prose |
| `convention-gate-guide.md` | docs | Constants, outcomes, and monadic `Result` rules enforced by `make convention.check` |
| `dotted-file-naming-guide.md` | docs | Subject + role filenames, the closed role vocabulary, and `make structure.check` |
| `folder-readme-guide.md` | docs | Fixed-section folder `README.md` convention (this tree) |

## Boundaries

In scope: writing and documentation conventions.

Out of scope:
- Architecture and dependency rules → [`../tests/Architecture.Tests/`](../tests/Architecture.Tests/README.md)
- Formatting of code → `.editorconfig` + `make format`
- Project onboarding → [`../README.md`](../README.md)

## Assumptions

- Guides are read before writing comments, file names, or folder READMEs.
- Most guides rely on review; `convention-gate-guide.md` is also enforced by
  [`../scripts/common/convention-check.common.sh`](../scripts/common/convention-check.common.sh)
  through `make convention.check`.

## Conventions

- New style rules belong here as guides, not as one-off paragraphs in a pull
  request description.
- Each guide keeps YAML frontmatter (`name`, `description`) for skill loading.

## Related

- Parent: [`../README.md`](../README.md)
- Applied across: [`../src/`](../src/README.md), [`../tests/`](../tests/README.md)
- Enforcement: [`../scripts/common/`](../scripts/common/), `make structure.check`, `make convention.check`
