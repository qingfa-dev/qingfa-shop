# Architecture.Tests

Architectural fitness functions — layer, slice, naming, and comment-label rules. _test · architecture_

## Summary

Architecture.Tests encodes **architectural constraints as executable tests** using reflection-based fitness checks. They run as part of `make lib.test` (after the unit suites) and fail CI if violated. NetArchTest rules are still being filled in; the current checks document the intended dependency graph and enforce what reflection can verify.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Assembly.Marker.cs` | marker | Anchors assembly for scanning |
| `LayerArchitecture.Tests.cs` | spec | Layer dependency rules (Core → Application → Infrastructure) |
| `VerticalSliceArchitecture.Tests.cs` | spec | Feature isolation, handler/validator co-location |
| `NamingConvention.Tests.cs` | spec | Dotted file naming (PascalCase, .Interface, .Base, .Validator, etc.) |
| `CodeCommentLabel.Tests.cs` | spec | Documents approved comment label vocabulary |

## Boundaries

In scope:
- Reflection checks for all production assemblies.
- Layer dependency graph validation.
- Vertical slice structure enforcement.
- Naming convention compliance (C# PascalCase, suffixes).
- Comment label vocabulary documentation.

Out of scope:
- Runtime behavior tests → [`../Integration/`](../Integration/), [`../Unit/`](../Unit/).
- Code formatting → `.editorconfig`, `dotnet format`.
- Business logic correctness → unit/integration tests.

## Assumptions

- All production assemblies have an `AssemblyMarker` type for reliable scanning.
- Checks run through the test runner, not during `dotnet build`.
- New rules added here must pass on the current codebase before merging.

## Invariants

- **Core** has zero project references.
- **Application** depends on Core; NOT Infrastructure.
- **Infrastructure** depends on Core + Application + ServiceDefaults.
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

1. `LayerArchitecture.Tests.cs` — core layer rules.
2. `VerticalSliceArchitecture.Tests.cs` — feature isolation.
3. `NamingConvention.Tests.cs` — naming conventions.
4. `CodeCommentLabel.Tests.cs` — comment labels.

## Related

- Parent: [`../README.md`](../README.md)
- Siblings: [`../Unit/`](../Unit/), [`../Integration/`](../Integration/), [`../Testing/`](../Testing/)
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