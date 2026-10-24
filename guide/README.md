# guide

Skill-style style guides for writing code comments and folder documentation. _docs · meta_

## Summary

`guide/` holds the repository's living writing conventions in guide form —
documents written for both humans and agents, with frontmatter so they can
be loaded as skills. They refine project-wide rules rather than restate
them; when a guide and the code disagree, the code wins and the guide is
updated.

## Contains

| Path | Role | Notes |
|---|---|---|
| `code-comment-style-guide.md` | docs | Labelled comments (`Validate:`, `Compute:`, `Fallback:`) instead of free prose |
| `folder-readme-guide.md` | docs | Fixed-section folder `README.md` convention (this tree) |

## Boundaries

In scope: writing and documentation conventions.

Out of scope:
- Architecture and dependency rules → [`../tests/Architecture.Tests/`](../tests/Architecture.Tests/README.md)
- Formatting of code → `.editorconfig` + `make format`
- Project onboarding → [`../README.md`](../README.md)

## Assumptions

- Guides are read before writing comments or folder READMEs; they are not
  enforced by a linter.

## Conventions

- New style rules belong here as guides, not as one-off paragraphs in a pull
  request description.
- Each guide keeps YAML frontmatter (`name`, `description`) for skill loading.

## Related

- Parent: [`../README.md`](../README.md)
- Applied across: [`../src/`](../src/README.md), [`../tests/`](../tests/README.md)
