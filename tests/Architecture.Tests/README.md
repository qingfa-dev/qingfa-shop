# Architecture.Tests

Architectural fitness functions — NetArchTest rules for layers, slices, naming, comments. _test · architecture_

## Summary

Architecture.Tests encodes **architectural constraints as executable tests** using NetArchTest. These run on every `dotnet build` and fail CI if violated. They replace manual code reviews for structural rules: layer dependencies, vertical slice isolation, naming conventions, and comment label vocabulary.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `LayerArchitectureTests.cs` | spec | Layer dependency rules (Core → Contract → App → Infra) |
| `VerticalSliceArchitectureTests.cs` | spec | Feature isolation, handler/validator co-location |
| `NamingConventionTests.cs` | spec | Dotted file naming (PascalCase, .Interface, .Base, .Validator, etc.) |
| `CodeCommentLabelTests.cs` | spec | Documents approved comment label vocabulary |

## Boundaries

In scope:
- NetArchTest rules for all production assemblies.
- Layer dependency graph validation.
- Vertical slice structure enforcement.
- Naming convention compliance (C# PascalCase, suffixes).
- Comment label vocabulary documentation.

Out of scope:
- Runtime behavior tests → [`../Integration/`](../Integration/), [`../Unit/`](../Unit/).
- Contract tests → [`../Contract/`](../Contract/).
- Code formatting → `.editorconfig`, `dotnet format`.
- Business logic correctness → unit/integration tests.

## Assumptions

- All production assemblies have `AssemblyMarker` type for reliable scanning.
- NetArchTest runs at build time via package reference (no separate tool install).
- Rules are **strict** — violations fail the build.
- New rules added here must pass on current codebase before merging.

## Invariants

- **Core** has zero project references.
- **Contract** depends only on Core.
- **Application** depends on Core + Contract; NOT Infrastructure.
- **Infrastructure** depends on Core + Contract + Application + ServiceDefaults.
- **ServiceDefaults** has zero project references.
- **Web.Api** depends on all layers (composition root).
- No circular dependencies between any layers.
- Production code never references test assemblies.
- All interfaces end with `.Interface`; abstract classes with `.Base`.
- All handlers/validators/mappers/policies/factories/builders use standard suffixes.

## Contracts

- `LayerArchitectureTests` — layer dependency matrix.
- `VerticalSliceArchitectureTests` — feature folder structure.
- `NamingConventionTests` — C# naming rules from dotted naming guide.
- `CodeCommentLabelTests` — approved label vocabulary reference.

## Conventions

- One test class per architectural concern.
- Test names: `Should<Rule>`, `ShouldNot<Rule>`.
- Use `AwesomeAssertions` (`result.IsSuccessful.Should().BeTrue()`).
- Include `because` clause explaining the architectural reason.
- Document anti-patterns in test comments.

## Reading Order

1. `LayerArchitectureTests.cs` — core layer rules.
2. `VerticalSliceArchitectureTests.cs` — feature isolation.
3. `NamingConventionTests.cs` — naming conventions.
4. `CodeCommentLabelTests.cs` — comment labels.

## Related

- Parent: [`../README.md`](../README.md)
- Siblings: [`../Unit/`](../Unit/), [`../Integration/`](../Integration/), [`../Contract/`](../Contract/), [`../Testing/`](../Testing/)
- Guides: [`../../guide/dotted-file-naming-guide.md`](../../guide/dotted-file-naming-guide.md), [`../../guide/code-comment-style-guide.md`](../../guide/code-comment-style-guide.md)
- ADR: [`../../docs/adr/0008-architecture-tests.md`](../../docs/adr/0008-architecture-tests.md)

## Ownership

- Owning team: **Platform**.
- Rule changes require Platform + Architecture review.
- New rules must be discussed in ADR before implementation.

## Notes

- `TODO-2026Q3:` Add rule: "All public APIs have XML docs" (CS1591).
- `TEMP:` `VerticalSliceArchitectureTests` feature namespace check is soft; tighten when features exist.
- `DEADLINE-2026-12-01:` Implement Roslyn analyzer for comment labels (replace documentation-only test).