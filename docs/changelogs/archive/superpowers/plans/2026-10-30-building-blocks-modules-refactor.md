# BuildingBlocks and Modules Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure Core, Application, and Infrastructure into `BuildingBlocks/` and `Modules/` buckets, with a ports-and-adapters taxonomy, without adding projects or changing behavior.

**Architecture:** Core BuildingBlocks hold the domain kernel, modeling primitives, and functional primitives. Application BuildingBlocks hold use-case execution, persistence/querying/messaging/event-sourcing/security/caching ports, and outbound ports. Infrastructure BuildingBlocks hold adapters. Todos is the only business module and is fully removed from BuildingBlocks. Identity is a platform BuildingBlock in Application and Infrastructure. `Infrastructure/Migrations` is the only cross-module exception.

**Tech Stack:** .NET 10, C# 14, xUnit v3 + Microsoft.Testing.Platform, FluentValidation, MediatR, EF Core/Npgsql, Wolverine/RabbitMQ, Aspire, Vue/pnpm (unchanged).

**Spec:** `docs/specs/2026-10-30-building-blocks-modules-refactor-design.md`

## Global Constraints

- **Do not commit.** The human partner explicitly requires a dirty worktree. Never run `git commit`, `git push`, `git reset --hard`, or `git clean`.
- **Do not create projects or packages.** Only folders, namespaces, and code moves.
- **Do not touch unrelated WIP.** Leave `src/Application/Features/Identity/Users/Models/User.Model.cs`, `src/Application/Features/Todos/Items/Models/Todo.Model.cs`, and unrelated docs alone unless a task explicitly moves them.
- **One assembly per layer.** No new `.csproj`; project files and lock files must not change.
- **Namespace == folder path.** `make structure.check` must pass after every task.
- **Dotted file naming.** New files must use a recognized role suffix; see `guide/dotted-file-naming-guide.md`.
- **Labeled comments.** Use `Validate:`, `Initialize:`, `Register:`, `Move:`, `Split:`, `Enforce:`, `Fallback:`; no freeform narration.
- **Dependency direction:** `Modules → BuildingBlocks`; BuildingBlocks never reference `Modules.Todos`.
- **Identity is BuildingBlocks** in Application and Infrastructure.
- **Preserve wire behavior:** exchange name, queue names, routing keys, permission identifiers, schema names, and role defaults must not change.
- **Review staging is allowed; committing is not.** Stage only the task paths with `git add -A -- <paths>` so reviewers can read `git diff --cached`. Never stage the whole repository.
- **No-commit review adaptation:** because commits are forbidden, do not use the commit-based `review-package` script. Each task writes `artifacts/refactor/task-N.diff` and `task-N-stat.txt` from `git diff --cached`. Treat those as cumulative: the reviewer focuses on the paths and requirements in the task brief; earlier reviewed changes are context, not new findings.
- **MTP test invocation:** use `dotnet test --project …`; never VSTest/coverlet. Integration tests that need Docker skip cleanly when Docker is absent; report skip counts.
- **No `git mv`.** Use `mkdir -p` + `mv` so the worktree stays unstaged until the explicit task-staging step.

## Pre-flight

- [ ] **Step 1: Save a recovery snapshot**

```bash
mkdir -p artifacts/refactor
git status --short > artifacts/refactor/baseline-status.txt
git diff > artifacts/refactor/baseline.patch
git ls-files -o --exclude-standard > artifacts/refactor/baseline-untracked.txt
```

Expected: three files exist under `artifacts/refactor/`.

- [ ] **Step 2: Record the baseline commit and test state**

```bash
git rev-parse HEAD > artifacts/refactor/baseline-head.txt
make lib.build
```

Expected: `Build succeeded` and no new build errors. If the baseline is already red, record the failure in `artifacts/refactor/baseline-build.txt` and continue; do not attribute it to the refactor.

- [ ] **Step 3: Define the mechanical helpers for every task**

```bash
rewrite_ns() {
  local old="$1" new="$2"
  grep -rlF "$old" --include='*.cs' -Z \
    --exclude-dir=bin --exclude-dir=obj --exclude-dir=node_modules \
    src tests app 2>/dev/null \
    | xargs -0 -r env OLD="$old" NEW="$new" \
        perl -pi -e 's/\Q$ENV{OLD}\E/$ENV{NEW}/g'
}

move_tree() {
  mkdir -p "$(dirname "$2")"
  if [ -e "$2" ]; then
    echo "refusing to overwrite existing target: $2" >&2
    return 1
  fi
  mv "$1" "$2"
}

assert_no_ns() {
  local old="$1"
  if grep -rnF "$old" --include='*.cs' \
    --exclude-dir=bin --exclude-dir=obj --exclude-dir=node_modules \
    src tests app 2>/dev/null; then
    echo "ERROR: stale namespace/reference: $old" >&2
    return 1
  fi
}
```

Expected: functions are available in the current shell. Re-source or redefine them in every subagent.

- [ ] **Step 4: Keep pre-flight artifacts untracked**

`artifacts/refactor/` is git-ignored. Do not stage it. The baseline files are for local recovery only; per-task source diffs are written there for review.

---

### Task 1: Core BuildingBlocks and Todos module

**Files:**
- Move: `src/Core/Common/Concerns/**` → `src/Core/BuildingBlocks/Modeling/Concerns/**`
- Move: `src/Core/Common/Domain/**` → `src/Core/BuildingBlocks/Domain/**`
- Move: `src/Core/Common/Monad/Errors/**` → `src/Core/BuildingBlocks/Functional/Errors/**`
- Move: `src/Core/Common/Monad/Maybes/**` → `src/Core/BuildingBlocks/Functional/Maybes/**`
- Move: `src/Core/Common/Monad/Metadata/**` → `src/Core/BuildingBlocks/Modeling/Metadata/**`
- Move: `src/Core/Common/Monad/Results/**` → `src/Core/BuildingBlocks/Functional/Results/**`
- Move: `src/Core/Todos/**` → `src/Core/Modules/Todos/**`
- Modify: `src/Core/Using.cs`
- Move: `tests/Unit/Core.Unit.Tests/Common/{Concerns,Domain,Monad}/**` → `tests/Unit/Core.Unit.Tests/BuildingBlocks/**`
- Move: `tests/Unit/Core.Unit.Tests/Todos/**` → `tests/Unit/Core.Unit.Tests/Modules/Todos/**`
- Modify: `tests/Unit/Core.Unit.Tests/Using.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `Core.BuildingBlocks.Domain.*`, `Core.BuildingBlocks.Modeling.*`, `Core.BuildingBlocks.Functional.*`, `Core.Modules.Todos.*`.

- [ ] **Step 1: Move Core production folders**

```bash
mkdir -p src/Core/BuildingBlocks/Modeling src/Core/BuildingBlocks/Functional src/Core/Modules
move_tree src/Core/Common/Concerns src/Core/BuildingBlocks/Modeling/Concerns
move_tree src/Core/Common/Domain src/Core/BuildingBlocks/Domain
move_tree src/Core/Common/Monad/Errors src/Core/BuildingBlocks/Functional/Errors
move_tree src/Core/Common/Monad/Maybes src/Core/BuildingBlocks/Functional/Maybes
move_tree src/Core/Common/Monad/Metadata src/Core/BuildingBlocks/Modeling/Metadata
move_tree src/Core/Common/Monad/Results src/Core/BuildingBlocks/Functional/Results
move_tree src/Core/Todos src/Core/Modules/Todos
rmdir src/Core/Common/Monad src/Core/Common 2>/dev/null || true
```

Expected: `src/Core/Common` is gone; `src/Core/BuildingBlocks` and `src/Core/Modules/Todos` exist.

- [ ] **Step 2: Move Core test folders**

```bash
mkdir -p tests/Unit/Core.Unit.Tests/BuildingBlocks/Modeling tests/Unit/Core.Unit.Tests/BuildingBlocks/Functional tests/Unit/Core.Unit.Tests/Modules
move_tree tests/Unit/Core.Unit.Tests/Common/Concerns tests/Unit/Core.Unit.Tests/BuildingBlocks/Modeling/Concerns
move_tree tests/Unit/Core.Unit.Tests/Common/Domain tests/Unit/Core.Unit.Tests/BuildingBlocks/Domain
move_tree tests/Unit/Core.Unit.Tests/Common/Monad/Errors tests/Unit/Core.Unit.Tests/BuildingBlocks/Functional/Errors
move_tree tests/Unit/Core.Unit.Tests/Common/Monad/Maybes tests/Unit/Core.Unit.Tests/BuildingBlocks/Functional/Maybes
move_tree tests/Unit/Core.Unit.Tests/Common/Monad/Metadata tests/Unit/Core.Unit.Tests/BuildingBlocks/Modeling/Metadata
move_tree tests/Unit/Core.Unit.Tests/Common/Monad/Results tests/Unit/Core.Unit.Tests/BuildingBlocks/Functional/Results
move_tree tests/Unit/Core.Unit.Tests/Todos tests/Unit/Core.Unit.Tests/Modules/Todos
rmdir tests/Unit/Core.Unit.Tests/Common/Monad tests/Unit/Core.Unit.Tests/Common 2>/dev/null || true
```

Expected: no `tests/Unit/Core.Unit.Tests/Common` or `.../Todos` directory.

- [ ] **Step 3: Rewrite production namespaces**

```bash
rewrite_ns Core.Common.Monad.Errors Core.BuildingBlocks.Functional.Errors
rewrite_ns Core.Common.Monad.Maybes Core.BuildingBlocks.Functional.Maybes
rewrite_ns Core.Common.Monad.Results Core.BuildingBlocks.Functional.Results
rewrite_ns Core.Common.Monad.Metadata Core.BuildingBlocks.Modeling.Metadata
rewrite_ns Core.Common.Concerns Core.BuildingBlocks.Modeling.Concerns
rewrite_ns Core.Common.Domain Core.BuildingBlocks.Domain
rewrite_ns Core.Todos Core.Modules.Todos
rewrite_ns Core.Common Core.BuildingBlocks
```

Expected: `assert_no_ns Core.Common` and `assert_no_ns Core.Todos` pass.

- [ ] **Step 4: Rewrite Core test namespaces**

```bash
rewrite_ns Core.Unit.Tests.Common.Monad.Errors Core.Unit.Tests.BuildingBlocks.Functional.Errors
rewrite_ns Core.Unit.Tests.Common.Monad.Maybes Core.Unit.Tests.BuildingBlocks.Functional.Maybes
rewrite_ns Core.Unit.Tests.Common.Monad.Results Core.Unit.Tests.BuildingBlocks.Functional.Results
rewrite_ns Core.Unit.Tests.Common.Monad.Metadata Core.Unit.Tests.BuildingBlocks.Modeling.Metadata
rewrite_ns Core.Unit.Tests.Common.Concerns Core.Unit.Tests.BuildingBlocks.Modeling.Concerns
rewrite_ns Core.Unit.Tests.Common.Domain Core.Unit.Tests.BuildingBlocks.Domain
rewrite_ns Core.Unit.Tests.Todos Core.Unit.Tests.Modules.Todos
rewrite_ns Core.Unit.Tests.Common Core.Unit.Tests.BuildingBlocks
```

Expected: no stale `Core.Unit.Tests.Common` or `Core.Unit.Tests.Todos` references.

- [ ] **Step 5: Verify Core**

```bash
make lib.build
dotnet test --project tests/Unit/Core.Unit.Tests/Core.Unit.Tests.csproj -c Release --no-build
make structure.check
```

Expected: build succeeds, tests report `failed: 0`, structure check passes.

- [ ] **Step 6: Stage the task for review (no commit)**

```bash
git add -A -- src/Core tests/Unit/Core.Unit.Tests
git diff --cached --stat -- src/Core tests/Unit/Core.Unit.Tests > artifacts/refactor/task-1-stat.txt
git diff --cached -- src/Core tests/Unit/Core.Unit.Tests > artifacts/refactor/task-1.diff
```

Expected: only Core and Core test paths are staged. No commit.

---

### Task 2: Application BuildingBlocks and Identity

**Files:**
- Move: `src/Application/Common/Caching/**` → `src/Application/BuildingBlocks/Caching/**`
- Move: `src/Application/Common/Events/**` → `src/Application/BuildingBlocks/Messaging/Events/**`
- Move: `src/Application/Common/EventSourcing/**` → `src/Application/BuildingBlocks/EventSourcing/**`
- Move: `src/Application/Common/Integrations/Notifications/**` → `src/Application/BuildingBlocks/Ports/Notifications/**`
- Move: `src/Application/Common/Integrations/Storages/**` → `src/Application/BuildingBlocks/Ports/Storage/**`
- Move: `src/Application/Common/Mapping/**` → `src/Application/BuildingBlocks/UseCases/Mapping/**`
- Move: `src/Application/Common/Mediators/**` → `src/Application/BuildingBlocks/UseCases/Mediation/**`
- Move: `src/Application/Common/Messaging/**` → `src/Application/BuildingBlocks/Messaging/**`
- Move: `src/Application/Common/Persistence/**` → `src/Application/BuildingBlocks/Persistence/**`
- Move: `src/Application/Common/Querying/**` → `src/Application/BuildingBlocks/Querying/**`
- Move: `src/Application/Common/Security/**` → `src/Application/BuildingBlocks/Security/**`
- Move: `src/Application/Common/Validation/**` → `src/Application/BuildingBlocks/UseCases/Validation/**`
- Move: `src/Application/Features/Identity/**` → `src/Application/BuildingBlocks/Security/Identity/**`
- Modify: `src/Application/Using.cs`
- Modify: `src/Application/Application.Composition.cs`
- Move: `tests/Unit/Application.Unit.Tests/Common/{Caching,Events,EventSourcing,Integrations,Mapping,Mediators,Messaging,Persistence,Querying,Security,Validation}/**` → `tests/Unit/Application.Unit.Tests/BuildingBlocks/**`
- Move: `tests/Unit/Application.Unit.Tests/Features/Identity/**` → `tests/Unit/Application.Unit.Tests/BuildingBlocks/Security/Identity/**`
- Move: `tests/Unit/Application.Unit.Tests/Common/Composition/**` → `tests/Unit/Application.Unit.Tests/Composition/**`
- Modify: `tests/Unit/Application.Unit.Tests/Using.cs`

**Interfaces:**
- Consumes: `Core.BuildingBlocks.*` and `Core.Modules.Todos.*` from Task 1.
- Produces: `Application.BuildingBlocks.*`, `Application.BuildingBlocks.Security.Identity.*`; `Application.Features.Todos.*` remains until Task 3.

- [ ] **Step 1: Move Application production folders**

```bash
mkdir -p src/Application/BuildingBlocks/Ports src/Application/BuildingBlocks/UseCases
move_tree src/Application/Common/Caching src/Application/BuildingBlocks/Caching
move_tree src/Application/Common/EventSourcing src/Application/BuildingBlocks/EventSourcing
move_tree src/Application/Common/Integrations/Notifications src/Application/BuildingBlocks/Ports/Notifications
move_tree src/Application/Common/Integrations/Storages src/Application/BuildingBlocks/Ports/Storage
move_tree src/Application/Common/Mapping src/Application/BuildingBlocks/UseCases/Mapping
move_tree src/Application/Common/Mediators src/Application/BuildingBlocks/UseCases/Mediation
move_tree src/Application/Common/Messaging src/Application/BuildingBlocks/Messaging
move_tree src/Application/Common/Events src/Application/BuildingBlocks/Messaging/Events
move_tree src/Application/Common/Persistence src/Application/BuildingBlocks/Persistence
move_tree src/Application/Common/Querying src/Application/BuildingBlocks/Querying
move_tree src/Application/Common/Security src/Application/BuildingBlocks/Security
move_tree src/Application/Common/Validation src/Application/BuildingBlocks/UseCases/Validation
move_tree src/Application/Features/Identity src/Application/BuildingBlocks/Security/Identity
rmdir src/Application/Common/Integrations src/Application/Common src/Application/Features 2>/dev/null || true
```

Expected: `src/Application/Common` is gone; `Features/Todos` remains.

- [ ] **Step 2: Move Application BuildingBlocks tests**

```bash
mkdir -p tests/Unit/Application.Unit.Tests/BuildingBlocks/Ports tests/Unit/Application.Unit.Tests/BuildingBlocks/UseCases
move_tree tests/Unit/Application.Unit.Tests/Common/EventSourcing tests/Unit/Application.Unit.Tests/BuildingBlocks/EventSourcing
move_tree tests/Unit/Application.Unit.Tests/Common/Mapping tests/Unit/Application.Unit.Tests/BuildingBlocks/UseCases/Mapping
move_tree tests/Unit/Application.Unit.Tests/Common/Mediators tests/Unit/Application.Unit.Tests/BuildingBlocks/UseCases/Mediation
move_tree tests/Unit/Application.Unit.Tests/Common/Messaging tests/Unit/Application.Unit.Tests/BuildingBlocks/Messaging
move_tree tests/Unit/Application.Unit.Tests/Common/Persistence tests/Unit/Application.Unit.Tests/BuildingBlocks/Persistence
move_tree tests/Unit/Application.Unit.Tests/Common/Querying tests/Unit/Application.Unit.Tests/BuildingBlocks/Querying
move_tree tests/Unit/Application.Unit.Tests/Common/Security tests/Unit/Application.Unit.Tests/BuildingBlocks/Security
move_tree tests/Unit/Application.Unit.Tests/Common/Validation tests/Unit/Application.Unit.Tests/BuildingBlocks/UseCases/Validation
move_tree tests/Unit/Application.Unit.Tests/Features/Identity tests/Unit/Application.Unit.Tests/BuildingBlocks/Security/Identity
move_tree tests/Unit/Application.Unit.Tests/Common/Composition tests/Unit/Application.Unit.Tests/Composition
rmdir tests/Unit/Application.Unit.Tests/Common tests/Unit/Application.Unit.Tests/Features 2>/dev/null || true
```

Expected: `tests/Unit/Application.Unit.Tests/Common` is gone; `Features/Todos` remains.

- [ ] **Step 3: Rewrite Application production namespaces**

```bash
rewrite_ns Application.Common.Integrations.Notifications Application.BuildingBlocks.Ports.Notifications
rewrite_ns Application.Common.Integrations.Storages Application.BuildingBlocks.Ports.Storage
rewrite_ns Application.Common.Mediators Application.BuildingBlocks.UseCases.Mediation
rewrite_ns Application.Common.Mapping Application.BuildingBlocks.UseCases.Mapping
rewrite_ns Application.Common.Validation Application.BuildingBlocks.UseCases.Validation
rewrite_ns Application.Common.Events Application.BuildingBlocks.Messaging.Events
rewrite_ns Application.Features.Identity Application.BuildingBlocks.Security.Identity
rewrite_ns Application.Common.Caching Application.BuildingBlocks.Caching
rewrite_ns Application.Common.EventSourcing Application.BuildingBlocks.EventSourcing
rewrite_ns Application.Common.Messaging Application.BuildingBlocks.Messaging
rewrite_ns Application.Common.Persistence Application.BuildingBlocks.Persistence
rewrite_ns Application.Common.Querying Application.BuildingBlocks.Querying
rewrite_ns Application.Common.Security Application.BuildingBlocks.Security
rewrite_ns Application.Common Application.BuildingBlocks
```

Expected: `assert_no_ns Application.Common` and `assert_no_ns Application.Features.Identity` pass.

- [ ] **Step 4: Rewrite Application test namespaces**

```bash
rewrite_ns Application.Unit.Tests.Common.Integrations.Notifications Application.Unit.Tests.BuildingBlocks.Ports.Notifications
rewrite_ns Application.Unit.Tests.Common.Integrations.Storages Application.Unit.Tests.BuildingBlocks.Ports.Storage
rewrite_ns Application.Unit.Tests.Common.Mediators Application.Unit.Tests.BuildingBlocks.UseCases.Mediation
rewrite_ns Application.Unit.Tests.Common.Mapping Application.Unit.Tests.BuildingBlocks.UseCases.Mapping
rewrite_ns Application.Unit.Tests.Common.Validation Application.Unit.Tests.BuildingBlocks.UseCases.Validation
rewrite_ns Application.Unit.Tests.Common.Events Application.Unit.Tests.BuildingBlocks.Messaging.Events
rewrite_ns Application.Unit.Tests.Features.Identity Application.Unit.Tests.BuildingBlocks.Security.Identity
rewrite_ns Application.Unit.Tests.Common.Caching Application.Unit.Tests.BuildingBlocks.Caching
rewrite_ns Application.Unit.Tests.Common.EventSourcing Application.Unit.Tests.BuildingBlocks.EventSourcing
rewrite_ns Application.Unit.Tests.Common.Messaging Application.Unit.Tests.BuildingBlocks.Messaging
rewrite_ns Application.Unit.Tests.Common.Persistence Application.Unit.Tests.BuildingBlocks.Persistence
rewrite_ns Application.Unit.Tests.Common.Querying Application.Unit.Tests.BuildingBlocks.Querying
rewrite_ns Application.Unit.Tests.Common.Security Application.Unit.Tests.BuildingBlocks.Security
rewrite_ns Application.Unit.Tests.Common.Composition Application.Unit.Tests.Composition
rewrite_ns Application.Unit.Tests.Common Application.Unit.Tests.BuildingBlocks
```

Expected: no stale Application test namespaces.

- [ ] **Step 5: Verify Application BuildingBlocks**

```bash
make lib.build
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build
make structure.check
```

Expected: build and tests pass; structure check passes even though Todos tests still use their old `Features/Todos` namespace.

- [ ] **Step 6: Stage the task for review (no commit)**

```bash
git add -A -- src/Application tests/Unit/Application.Unit.Tests
git diff --cached --stat -- src/Application tests/Unit/Application.Unit.Tests > artifacts/refactor/task-2-stat.txt
git diff --cached -- src/Application tests/Unit/Application.Unit.Tests > artifacts/refactor/task-2.diff
```

---

### Task 3: Application Todos module and composition seam

**Files:**
- Move: `src/Application/Features/Todos/**` → `src/Application/Modules/Todos/**`
- Move: `tests/Unit/Application.Unit.Tests/Features/Todos/**` → `tests/Unit/Application.Unit.Tests/Modules/Todos/**`
- Create: `src/Application/BuildingBlocks/BuildingBlock.Application.Composition.cs`
- Create: `src/Application/Modules/Modular.Application.Composition.cs`
- Modify: `src/Application/Application.Composition.cs`

**Interfaces:**
- Consumes: `Application.BuildingBlocks.*` from Task 2.
- Produces: `Application.Modules.Todos.*`; `AddBuildingBlockApplication()` and `AddModularApplication()`.

- [ ] **Step 1: Move Todos production and test folders**

```bash
move_tree src/Application/Features/Todos src/Application/Modules/Todos
move_tree tests/Unit/Application.Unit.Tests/Features/Todos tests/Unit/Application.Unit.Tests/Modules/Todos
rmdir src/Application/Features tests/Unit/Application.Unit.Tests/Features 2>/dev/null || true
```

Expected: `src/Application/Modules/Todos` and `tests/Unit/Application.Unit.Tests/Modules/Todos` exist.

- [ ] **Step 2: Rewrite Todos namespaces**

```bash
rewrite_ns Application.Features.Todos Application.Modules.Todos
rewrite_ns Application.Unit.Tests.Features.Todos Application.Unit.Tests.Modules.Todos
```

Expected: `assert_no_ns Application.Features` and `assert_no_ns Application.Unit.Tests.Features` pass.

- [ ] **Step 3: Add the BuildingBlocks application composition**

Create `src/Application/BuildingBlocks/BuildingBlock.Application.Composition.cs`:

```csharp
using System.Reflection;

using Application.BuildingBlocks.UseCases.Mediation;

using FluentValidation;

using Microsoft.AspNetCore.Builder;

namespace Application.BuildingBlocks;

/// <summary>Composition root for the reusable Application BuildingBlocks.</summary>
public static class BuildingBlockApplicationComposition
{
    /// <summary>Registers mediation, pipeline behaviors, mapping, and validators.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan for handlers and validators.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddBuildingBlockApplication(
        this WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register: MediatR with the shared pipeline behaviors.
        builder.AddMediators(additionalAssemblies);

        // Register: validators from the host, this assembly, and any extra assembly.
        builder.Services.AddValidatorsFromAssembly(Assembly.GetEntryAssembly());
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        foreach (Assembly assembly in additionalAssemblies)
            builder.Services.AddValidatorsFromAssembly(assembly);

        return builder;
    }

    /// <summary>Warms the BuildingBlocks services at startup.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseBuildingBlockApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMediators();
        return app;
    }
}
```

- [ ] **Step 4: Add the Modules application composition**

Create `src/Application/Modules/Modular.Application.Composition.cs`:

```csharp
using Microsoft.AspNetCore.Builder;

namespace Application.Modules;

/// <summary>Composition root for application business modules.</summary>
public static class ModularApplicationComposition
{
    /// <summary>Registers the application modules.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddModularApplication(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register: Todos-specific application services are registered by its own composition
        // as modules grow; the current Todos registrations are discovered by MediatR scanning.
        return builder;
    }

    /// <summary>Runs module startup hooks.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseModularApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
```

- [ ] **Step 5: Replace `Application.Composition.cs`**

Replace the body of `src/Application/Application.Composition.cs` with:

```csharp
using System.Reflection;

using Application.BuildingBlocks;
using Application.Modules;

using Microsoft.AspNetCore.Builder;

namespace Application;

/// <summary>Composition root for the Application layer.</summary>
public static class ApplicationComposition
{
    /// <summary>Adds the reusable BuildingBlocks and the business modules.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="additionalAssemblies">Additional assemblies to scan.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddApplication(
        this WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddBuildingBlockApplication(additionalAssemblies);
        builder.AddModularApplication();
        return builder;
    }

    /// <summary>Initializes the Application layer.</summary>
    /// <param name="app">The built web application.</param>
    /// <returns>The same application for further pipeline configuration.</returns>
    public static WebApplication UseApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseBuildingBlockApplication();
        app.UseModularApplication();
        return app;
    }
}
```

- [ ] **Step 6: Verify Application**

```bash
make lib.build
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build --filter-class "Application.Unit.Tests.Composition.ApplicationCompositionTest"
make structure.check
```

Expected: all pass. The composition test must still find MediatR registered through `AddApplication`.

- [ ] **Step 7: Stage the task for review (no commit)**

```bash
git add -A -- src/Application tests/Unit/Application.Unit.Tests
git diff --cached --stat -- src/Application tests/Unit/Application.Unit.Tests > artifacts/refactor/task-3-stat.txt
git diff --cached -- src/Application tests/Unit/Application.Unit.Tests > artifacts/refactor/task-3.diff
```

---

### Task 4: Infrastructure BuildingBlocks adapters and Identity baseline

**Files:**
- Move: `src/Infrastructure/Caching/**` → `src/Infrastructure/BuildingBlocks/Adapters/Caching/**`
- Move: `src/Infrastructure/Events/**` → `src/Infrastructure/BuildingBlocks/Adapters/Events/**`
- Move: `src/Infrastructure/Integrations/Backgrounds/**` → `src/Infrastructure/BuildingBlocks/Adapters/BackgroundJobs/**`
- Move: `src/Infrastructure/Integrations/Http/**` → `src/Infrastructure/BuildingBlocks/Adapters/Http/**`
- Move: `src/Infrastructure/Integrations/Messaging/**` → `src/Infrastructure/BuildingBlocks/Adapters/Messaging/**`
- Move: `src/Infrastructure/Integrations/Notifications/**` → `src/Infrastructure/BuildingBlocks/Adapters/Notifications/**`
- Move: `src/Infrastructure/Integrations/Observability/**` → `src/Infrastructure/BuildingBlocks/Adapters/Observability/**`
- Move: `src/Infrastructure/Integrations/OpenApi/**` → `src/Infrastructure/BuildingBlocks/Adapters/OpenApi/**`
- Move: `src/Infrastructure/Integrations/Serialization/**` → `src/Infrastructure/BuildingBlocks/Adapters/Serialization/**`
- Move: `src/Infrastructure/Integrations/Storages/**` → `src/Infrastructure/BuildingBlocks/Adapters/Storage/**`
- Move: `src/Infrastructure/Integrations/Integration.Composition.cs` → `src/Infrastructure/BuildingBlocks/Adapters/Adapters.Composition.cs`
- Move: `src/Infrastructure/Persistence/EntityFramework/**` → `src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/**`
- Move: `src/Infrastructure/Persistence/Initialization/**` → `src/Infrastructure/BuildingBlocks/Adapters/Persistence/Initialization/**`
- Move: `src/Infrastructure/Persistence/Options/**` → `src/Infrastructure/BuildingBlocks/Adapters/Persistence/Options/**`
- Move: `src/Infrastructure/Persistence/Persistence.Composition.cs` → `src/Infrastructure/BuildingBlocks/Adapters/Persistence/Persistence.Composition.cs`
- Move: `src/Infrastructure/Persistence/PersistenceInitialization.Composition.cs` → `src/Infrastructure/BuildingBlocks/Adapters/Persistence/PersistenceInitialization.Composition.cs`
- Move: `src/Infrastructure/Security/Authentication/**` → `src/Infrastructure/BuildingBlocks/Adapters/Security/Authentication/**`
- Move: `src/Infrastructure/Security/Authorization/**` → `src/Infrastructure/BuildingBlocks/Adapters/Security/Authorization/**`
- Move: `src/Infrastructure/Security/Identity/**` → `src/Infrastructure/BuildingBlocks/Adapters/Security/Identity/**`
- Move: `src/Infrastructure/Security/Security.Composition.cs` → `src/Infrastructure/BuildingBlocks/Adapters/Security/Security.Composition.cs`
- Move: `tests/Unit/Infrastructure.Unit.Tests/{Caching,Events,Integrations,Persistence,Security,Common}/**` → mirrored `BuildingBlocks/Adapters/**`
- Modify: `src/Infrastructure/Using.cs`

**Interfaces:**
- Consumes: `Application.BuildingBlocks.*`, `Application.Modules.Todos.*`, `Core.*`.
- Produces: `Infrastructure.BuildingBlocks.Adapters.*`; `AddAdapters()` replaces `AddIntegration()`.

- [ ] **Step 1: Move Infrastructure adapter folders**

```bash
mkdir -p src/Infrastructure/BuildingBlocks/Adapters/Persistence src/Infrastructure/BuildingBlocks/Adapters/Security
move_tree src/Infrastructure/Caching src/Infrastructure/BuildingBlocks/Adapters/Caching
move_tree src/Infrastructure/Events src/Infrastructure/BuildingBlocks/Adapters/Events
move_tree src/Infrastructure/Integrations/Backgrounds src/Infrastructure/BuildingBlocks/Adapters/BackgroundJobs
move_tree src/Infrastructure/Integrations/Http src/Infrastructure/BuildingBlocks/Adapters/Http
move_tree src/Infrastructure/Integrations/Messaging src/Infrastructure/BuildingBlocks/Adapters/Messaging
move_tree src/Infrastructure/Integrations/Notifications src/Infrastructure/BuildingBlocks/Adapters/Notifications
move_tree src/Infrastructure/Integrations/Observability src/Infrastructure/BuildingBlocks/Adapters/Observability
move_tree src/Infrastructure/Integrations/OpenApi src/Infrastructure/BuildingBlocks/Adapters/OpenApi
move_tree src/Infrastructure/Integrations/Serialization src/Infrastructure/BuildingBlocks/Adapters/Serialization
move_tree src/Infrastructure/Integrations/Storages src/Infrastructure/BuildingBlocks/Adapters/Storage
move_tree src/Infrastructure/Integrations/Integration.Composition.cs src/Infrastructure/BuildingBlocks/Adapters/Adapters.Composition.cs
move_tree src/Infrastructure/Persistence/EntityFramework src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework
move_tree src/Infrastructure/Persistence/Initialization src/Infrastructure/BuildingBlocks/Adapters/Persistence/Initialization
move_tree src/Infrastructure/Persistence/Options src/Infrastructure/BuildingBlocks/Adapters/Persistence/Options
move_tree src/Infrastructure/Persistence/Persistence.Composition.cs src/Infrastructure/BuildingBlocks/Adapters/Persistence/Persistence.Composition.cs
move_tree src/Infrastructure/Persistence/PersistenceInitialization.Composition.cs src/Infrastructure/BuildingBlocks/Adapters/Persistence/PersistenceInitialization.Composition.cs
move_tree src/Infrastructure/Security/Authentication src/Infrastructure/BuildingBlocks/Adapters/Security/Authentication
move_tree src/Infrastructure/Security/Authorization src/Infrastructure/BuildingBlocks/Adapters/Security/Authorization
move_tree src/Infrastructure/Security/Identity src/Infrastructure/BuildingBlocks/Adapters/Security/Identity
move_tree src/Infrastructure/Security/Security.Composition.cs src/Infrastructure/BuildingBlocks/Adapters/Security/Security.Composition.cs
rmdir src/Infrastructure/Integrations src/Infrastructure/Security 2>/dev/null || true
```

Expected: `src/Infrastructure/Persistence` still contains `Schemas/Todos` and `Migrations`; `Services` still exists for Task 5.

- [ ] **Step 2: Move Infrastructure BuildingBlocks unit tests**

```bash
mkdir -p tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Persistence tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Security
move_tree tests/Unit/Infrastructure.Unit.Tests/Caching tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Caching
move_tree tests/Unit/Infrastructure.Unit.Tests/Events tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Events
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Backgrounds tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/BackgroundJobs
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Http tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Http
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Messaging
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Notifications tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Notifications
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Observability tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Observability
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/OpenApi tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/OpenApi
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Storages tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Storage
move_tree tests/Unit/Infrastructure.Unit.Tests/Persistence/EntityFramework tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Persistence/EntityFramework
move_tree tests/Unit/Infrastructure.Unit.Tests/Persistence/Initialization tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Persistence/Initialization
move_tree tests/Unit/Infrastructure.Unit.Tests/Security/Authentication tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Security/Authentication
move_tree tests/Unit/Infrastructure.Unit.Tests/Security/Authorization tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Security/Authorization
move_tree tests/Unit/Infrastructure.Unit.Tests/Security/Identity tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Security/Identity
move_tree tests/Unit/Infrastructure.Unit.Tests/Security/ExternalCodeStore.Test.cs tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Security/Identity/ExternalCodeStore.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/Common/Composition tests/Unit/Infrastructure.Unit.Tests/Composition
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Feature.Composition.Test.cs tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Composition/Feature.Composition.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Integration.Composition.Test.cs tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Composition/Adapters.Composition.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/OpenApi.Composition.Test.cs tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/OpenApi/OpenApi.Composition.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/OpenApiOptions.Setup.Test.cs tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/OpenApi/OpenApiOptions.Setup.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/OpenApiSchemaNaming.Test.cs tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/OpenApi/OpenApiSchemaNaming.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/Integrations/Observability.Correlation.Test.cs tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Observability/Observability.Correlation.Test.cs
rmdir tests/Unit/Infrastructure.Unit.Tests/Common tests/Unit/Infrastructure.Unit.Tests/Integrations tests/Unit/Infrastructure.Unit.Tests/Persistence tests/Unit/Infrastructure.Unit.Tests/Security 2>/dev/null || true
```

Expected: only Todos-specific tests remain outside `BuildingBlocks`.

- [ ] **Step 3: Rewrite Infrastructure namespaces**

```bash
rewrite_ns Infrastructure.Integrations.Notifications Infrastructure.BuildingBlocks.Adapters.Notifications
rewrite_ns Infrastructure.Integrations.Backgrounds Infrastructure.BuildingBlocks.Adapters.BackgroundJobs
rewrite_ns Infrastructure.Integrations.Storages Infrastructure.BuildingBlocks.Adapters.Storage
rewrite_ns Infrastructure.Integrations.Observability Infrastructure.BuildingBlocks.Adapters.Observability
rewrite_ns Infrastructure.Integrations.OpenApi Infrastructure.BuildingBlocks.Adapters.OpenApi
rewrite_ns Infrastructure.Integrations.Serialization Infrastructure.BuildingBlocks.Adapters.Serialization
rewrite_ns Infrastructure.Integrations.Messaging Infrastructure.BuildingBlocks.Adapters.Messaging
rewrite_ns Infrastructure.Integrations.Http Infrastructure.BuildingBlocks.Adapters.Http
rewrite_ns Infrastructure.Integrations Infrastructure.BuildingBlocks.Adapters
rewrite_ns Infrastructure.Caching Infrastructure.BuildingBlocks.Adapters.Caching
rewrite_ns Infrastructure.Events Infrastructure.BuildingBlocks.Adapters.Events
rewrite_ns Infrastructure.Persistence.EntityFramework Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework
rewrite_ns Infrastructure.Persistence.Initialization Infrastructure.BuildingBlocks.Adapters.Persistence.Initialization
rewrite_ns Infrastructure.Persistence.Options Infrastructure.BuildingBlocks.Adapters.Persistence.Options
rewrite_ns Infrastructure.Security.Authentication Infrastructure.BuildingBlocks.Adapters.Security.Authentication
rewrite_ns Infrastructure.Security.Authorization Infrastructure.BuildingBlocks.Adapters.Security.Authorization
rewrite_ns Infrastructure.Security.Identity Infrastructure.BuildingBlocks.Adapters.Security.Identity
rewrite_ns Infrastructure.Security Infrastructure.BuildingBlocks.Adapters.Security

# Update only the root persistence composition namespaces. Do not rewrite
# Infrastructure.Persistence.Migrations or Infrastructure.Persistence.Schemas.Todos;
# those move in Task 7 and Task 5 respectively.
sed -i 's/^namespace Infrastructure\.Persistence;$/namespace Infrastructure.BuildingBlocks.Adapters.Persistence;/' \
  src/Infrastructure/BuildingBlocks/Adapters/Persistence/Persistence.Composition.cs \
  src/Infrastructure/BuildingBlocks/Adapters/Persistence/PersistenceInitialization.Composition.cs

rewrite_ns Infrastructure.Unit.Tests.Common.Composition Infrastructure.Unit.Tests.Composition

sed -i 's/^namespace Infrastructure\.Unit\.Tests\.Integrations;$/namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Composition;/' \
  tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Composition/*.cs
sed -i 's/^namespace Infrastructure\.Unit\.Tests\.Integrations;$/namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.OpenApi;/' \
  tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/OpenApi/*.cs
sed -i 's/^namespace Infrastructure\.Unit\.Tests\.Integrations;$/namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Observability;/' \
  tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Observability/Observability.Correlation.Test.cs
sed -i 's/^namespace Infrastructure\.Unit\.Tests\.Security;$/namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity;/' \
  tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Security/Identity/ExternalCodeStore.Test.cs

rewrite_ns Infrastructure.Unit.Tests.Integrations Infrastructure.Unit.Tests.BuildingBlocks.Adapters
rewrite_ns Infrastructure.Unit.Tests.Persistence.EntityFramework Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework
rewrite_ns Infrastructure.Unit.Tests.Persistence.Initialization Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.Initialization
rewrite_ns Infrastructure.Unit.Tests.Persistence Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence
rewrite_ns Infrastructure.Unit.Tests.Security.Authentication Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authentication
rewrite_ns Infrastructure.Unit.Tests.Security.Authorization Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authorization
rewrite_ns Infrastructure.Unit.Tests.Security.Identity Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity
rewrite_ns Infrastructure.Unit.Tests.Security Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security
rewrite_ns Infrastructure.Unit.Tests.Caching Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Caching
rewrite_ns Infrastructure.Unit.Tests.Events Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Events
```

Expected: `assert_no_ns Infrastructure.Integrations` and `assert_no_ns Infrastructure.Security` pass. `Infrastructure.Persistence.Schemas.Todos` and `Infrastructure.Persistence.Migrations` may remain intentionally for Task 5/Task 7.

- [ ] **Step 4: Rename the adapter composition type**

Edit `src/Infrastructure/BuildingBlocks/Adapters/Adapters.Composition.cs`:

```csharp
namespace Infrastructure.BuildingBlocks.Adapters;

/// <summary>Composition root for outbound adapters and cross-cutting host features.</summary>
public static class AdaptersComposition
{
    public static WebApplicationBuilder AddAdapters(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        IEnumerable<IMessagingModule> messagingModules)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(messagingModules);

        builder.AddHttpClients();
        builder.AddBackgroundJobs();
        builder.AddMessaging(configuration, messagingModules);
        builder.AddNotifications();
        builder.AddStorage();
        builder.AddObservability();
        builder.AddOpenApiDocumentation();
        return builder;
    }

    public static WebApplication UseAdapters(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseObservability();
        app.UseStorage();
        app.UseBackgroundJobs();
        app.UseMessaging();
        app.UseOpenApiDocumentation();
        return app;
    }
}
```

Add the missing usings:

```csharp
using Infrastructure.BuildingBlocks.Adapters.BackgroundJobs;
using Infrastructure.BuildingBlocks.Adapters.Http;
using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Notifications;
using Infrastructure.BuildingBlocks.Adapters.Observability;
using Infrastructure.BuildingBlocks.Adapters.OpenApi;
using Infrastructure.BuildingBlocks.Adapters.Storage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
```

Update any tests that call `AddIntegration`, `UseIntegration`, or `IntegrationComposition` to call `AddAdapters`, `UseAdapters`, or `AdaptersComposition`.

- [ ] **Step 5: Add the messaging module contract**

Create `src/Infrastructure/BuildingBlocks/Adapters/Messaging/IMessagingModule.Interface.cs`:

```csharp
using Infrastructure.BuildingBlocks.Adapters.Messaging.Options;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;

using Wolverine;
using Wolverine.RabbitMQ;

namespace Infrastructure.BuildingBlocks.Adapters.Messaging;

/// <summary>Contributes module-specific exchange, queue, and handler wiring to Wolverine.</summary>
public interface IMessagingModule
{
    /// <summary>Configures the module's Wolverine topology.</summary>
    /// <param name="options">The Wolverine options.</param>
    /// <param name="setting">The bound messaging settings.</param>
    /// <param name="eventTypes">The shared event-type registry.</param>
    /// <param name="transport">The RabbitMQ transport, or <c>null</c> for the in-memory transport.</param>
    void Configure(
        WolverineOptions options,
        MessagingSetting setting,
        EventTypeRegistry eventTypes,
        RabbitMqTransportExpression? transport);
}
```

- [ ] **Step 6: Change `AddMessaging` to accept modules**

In `src/Infrastructure/BuildingBlocks/Adapters/Messaging/Messaging.Composition.cs`, replace `RegisterEventTypes` with a single registry instance and pass it to the modules:

```csharp
public static WebApplicationBuilder AddMessaging(
    this WebApplicationBuilder builder,
    IConfiguration configuration,
    IEnumerable<IMessagingModule> modules)
{
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(modules);

    builder.Services.AddSingleton<IValidator<MessagingSetting>, MessagingSettingValidator>();
    builder.Services
        .AddOptions<MessagingSetting>()
        .Bind(configuration.GetSection(MessagingSetting.SectionName))
        .ValidateFluentValidation()
        .ValidateOnStart();

    EventTypeRegistry eventTypes = CreateEventTypeRegistry();
    builder.Services.AddSingleton(eventTypes);
    builder.Services.AddSingleton<IEventTypeRegistry>(eventTypes);

    MessagingSetting setting = configuration
        .GetSection(MessagingSetting.SectionName)
        .Get<MessagingSetting>() ?? new MessagingSetting();

    if (!setting.Enabled)
        return builder;

    string? connectionString = ResolveConnectionString(configuration);
    if (setting.Transport == MessagingTransport.RabbitMq &&
        string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Messaging:Transport is RabbitMq but no connection string was found in " +
            "'ConnectionStrings:messaging', 'ConnectionStrings:rabbitmq', or 'Messaging:ConnectionString'.");
    }

    builder.UseWolverine(options =>
        ConfigureWolverine(
            options,
            setting,
            connectionString,
            modules,
            eventTypes));

    builder.Services.AddScoped<IEventBus, WolverineEventBus>();

    if (setting.Transport == MessagingTransport.RabbitMq && connectionString is not null)
    {
        builder.Services.TryAddSingleton<IConnection>(_ =>
            new ConnectionFactory
            {
                Uri = new Uri(connectionString),
            }.CreateConnectionAsync().GetAwaiter().GetResult());

        builder.Services.AddHealthChecks()
            .AddRabbitMQ(
                provider => Task.FromResult(provider.GetRequiredService<IConnection>()),
                tags: ["ready", "messaging"]);
    }

    return builder;
}

private static EventTypeRegistry CreateEventTypeRegistry() =>
    new(
        typeof(IDomainEvent).Assembly,
        typeof(MessagingComposition).Assembly);
```

Remove the old `RegisterEventTypes` method. Replace `ConfigureWolverine` with:

```csharp
private static void ConfigureWolverine(
    WolverineOptions options,
    MessagingSetting setting,
    string? connectionString,
    IEnumerable<IMessagingModule> modules,
    EventTypeRegistry eventTypes)
{
    options.Discovery.IncludeAssembly(typeof(IntegrationEvent).Assembly);
    options.Discovery.IncludeAssembly(typeof(MessagingComposition).Assembly);
    options.OnException<Exception>().RetryWithCooldown(RetryDelays(setting));

    if (setting.Transport != MessagingTransport.RabbitMq ||
        string.IsNullOrWhiteSpace(connectionString))
    {
        // Register: event identities still need to exist for the in-memory transport.
        foreach (IMessagingModule module in modules)
            module.Configure(options, setting, eventTypes, transport: null);

        return;
    }

    RabbitMqTransportExpression transport = options
        .UseRabbitMq(new Uri(connectionString))
        .AutoProvision()
        .ConfigureChannelCreation(channel =>
        {
            channel.PublisherConfirmationsEnabled = true;
            channel.PublisherConfirmationTrackingEnabled = true;
            channel.ConsumerDispatchConcurrency = (ushort)Math.Clamp(
                setting.MaxConcurrency,
                1,
                ushort.MaxValue);
        });

    foreach (IMessagingModule module in modules)
        module.Configure(options, setting, eventTypes, transport);
}
```

- [ ] **Step 7: Change `AddEventStore` to accept modules**

Create `src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/EventSourcing/IEventStoreModule.Interface.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Contributes module aggregates and projections to the event store.</summary>
public interface IEventStoreModule
{
    /// <summary>Registers the module's factories and projections.</summary>
    /// <param name="services">The host service collection.</param>
    void Configure(IServiceCollection services);
}
```

In `EventStore.Composition.cs`, change the signature:

```csharp
public static WebApplicationBuilder AddEventStore(
    this WebApplicationBuilder builder,
    IConfiguration configuration,
    IEnumerable<IEventStoreModule> modules)
{
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(modules);

    // Keep the existing options, clock, snapshot policy, and generic store registrations.
    // Remove the Todo/Reminder factory and projection registrations.
    foreach (IEventStoreModule module in modules)
        module.Configure(builder.Services);

    return builder;
}
```

- [ ] **Step 8: Verify the adapter move**

```bash
make lib.build
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build
make structure.check
```

Expected: the suite may fail only where Task 5 has not yet moved Todo messaging/event-store composition. If the only failures are unresolved Todo registrations, record them and continue to Task 5. Do not fix them in Task 4.

**Paired review unit:** Task 4 and Task 5 are tightly coupled. Do not mark Task 4 complete or dispatch its task review independently; execute both and review the combined diff. This is the one intentional exception to per-task independence because the composition signatures and Todo module changes land together.

- [ ] **Step 9: Stage the task for review (no commit)**

```bash
git add -A -- src/Infrastructure tests/Unit/Infrastructure.Unit.Tests
git diff --cached --stat -- src/Infrastructure tests/Unit/Infrastructure.Unit.Tests > artifacts/refactor/task-4-stat.txt
git diff --cached -- src/Infrastructure tests/Unit/Infrastructure.Unit.Tests > artifacts/refactor/task-4.diff
```

---

### Task 5: Infrastructure Todos module and Infrastructure composition seams

**Paired review unit:** execute this task together with Task 4; review the combined diff. Task 4 intentionally leaves the composition signatures incomplete until this task lands.

**Files:**
- Move: `src/Infrastructure/Persistence/Schemas/Todos/**` → `src/Infrastructure/Modules/Todos/Persistence/Schemas/**`
- Move: `src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Projections/Todo*.cs` → `src/Infrastructure/Modules/Todos/Persistence/Projections/**`
- Move: `src/Infrastructure/BuildingBlocks/Adapters/Messaging/Handlers/Todo*.cs` → `src/Infrastructure/Modules/Todos/Integrations/Messaging/Handlers/**`
- Move: `src/Infrastructure/Services/Todos/TodoReferenceCleaner.cs` → `src/Infrastructure/Modules/Todos/Services/TodoReferenceCleaner.cs`
- Move: `src/Infrastructure/Services/Service.Composition.cs` → `src/Infrastructure/Modules/Todos/Services/TodoService.Composition.cs`
- Create: `src/Infrastructure/BuildingBlocks/InfrastructureModuleRegistrations.cs`
- Create: `src/Infrastructure/BuildingBlocks/BuildingBlock.Infra.Composition.cs`
- Create: `src/Infrastructure/Modules/Modular.Infra.Composition.cs`
- Create: `src/Infrastructure/Modules/Todos/Integrations/Messaging/TodoMessaging.Composition.cs`
- Create: `src/Infrastructure/Modules/Todos/Persistence/EventSourcing/TodoEventStore.Composition.cs`
- Modify: `src/Infrastructure/Infrastructure.Composition.cs`
- Move: `tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos/**` → `tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Persistence/**`
- Move: `tests/Unit/Infrastructure.Unit.Tests/Integrations/Messaging/Handlers/Todo*.cs` → `tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Integrations/Messaging/Handlers/**`
- Move: `tests/Integration/Infrastructure.Integration.Tests/{EventSourcing,Messaging,Persistence/Todos}/**` → mirrored `Modules/Todos/**`

**Interfaces:**
- Consumes: `IMessagingModule`, `IEventStoreModule`, `AddMessaging(..., modules)`, `AddEventStore(..., modules)` from Task 4.
- Produces: `Infrastructure.Modules.Todos.*`; `CreateRegistrations()`, `AddModularInfrastructure()`, `UseModularInfrastructure()`.

- [ ] **Step 1: Move Todos-specific infrastructure files**

```bash
mkdir -p src/Infrastructure/Modules/Todos/Persistence src/Infrastructure/Modules/Todos/Integrations/Messaging/Handlers src/Infrastructure/Modules/Todos/Services
move_tree src/Infrastructure/Persistence/Schemas/Todos src/Infrastructure/Modules/Todos/Persistence/Schemas
move_tree src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Projections/Todo.Projection.cs src/Infrastructure/Modules/Todos/Persistence/Projections/Todo.Projection.cs
move_tree src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Projections/TodoReminder.Projection.cs src/Infrastructure/Modules/Todos/Persistence/Projections/TodoReminder.Projection.cs
move_tree src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Projections/TodoReminderReadModel.Configuration.cs src/Infrastructure/Modules/Todos/Persistence/Projections/TodoReminderReadModel.Configuration.cs
move_tree src/Infrastructure/BuildingBlocks/Adapters/Messaging/Handlers/TodoCompletedIntegration.Handler.cs src/Infrastructure/Modules/Todos/Integrations/Messaging/Handlers/TodoCompletedIntegration.Handler.cs
move_tree src/Infrastructure/BuildingBlocks/Adapters/Messaging/Handlers/TodoReminderChangedIntegration.Handler.cs src/Infrastructure/Modules/Todos/Integrations/Messaging/Handlers/TodoReminderChangedIntegration.Handler.cs
move_tree src/Infrastructure/Services/Todos/TodoReferenceCleaner.cs src/Infrastructure/Modules/Todos/Services/TodoReferenceCleaner.cs
move_tree src/Infrastructure/Services/Service.Composition.cs src/Infrastructure/Modules/Todos/Services/TodoService.Composition.cs
rmdir src/Infrastructure/Services/Todos src/Infrastructure/Services src/Infrastructure/Persistence/Schemas src/Infrastructure/Persistence 2>/dev/null || true
```

Expected: `src/Infrastructure/Persistence` contains only `Migrations` (Task 7 moves it); `Services` is gone.

- [ ] **Step 2: Move Todos unit and integration tests**

```bash
mkdir -p tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Persistence tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Integrations/Messaging/Handlers
move_tree tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos/Seeders tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Persistence/Seeders
move_tree tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos/TodoReferenceCleaner.Test.cs tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Services/TodoReferenceCleaner.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Messaging/Handlers/TodoCompletedIntegration.Handler.Test.cs tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Integrations/Messaging/Handlers/TodoCompletedIntegration.Handler.Test.cs
move_tree tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Messaging/Handlers/TodoReminderChangedIntegration.Handler.Test.cs tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Integrations/Messaging/Handlers/TodoReminderChangedIntegration.Handler.Test.cs
rmdir tests/Unit/Infrastructure.Unit.Tests/Persistence/Todos tests/Unit/Infrastructure.Unit.Tests/BuildingBlocks/Adapters/Messaging/Handlers 2>/dev/null || true
```

Integration test folders:

```bash
mkdir -p tests/Integration/Infrastructure.Integration.Tests/Modules/Todos/EventSourcing
move_tree tests/Integration/Infrastructure.Integration.Tests/EventSourcing/TodoBackfill.Integration.Test.cs tests/Integration/Infrastructure.Integration.Tests/Modules/Todos/EventSourcing/TodoBackfill.Integration.Test.cs
move_tree tests/Integration/Infrastructure.Integration.Tests/EventSourcing/TodoReminder.Flow.Integration.Test.cs tests/Integration/Infrastructure.Integration.Tests/Modules/Todos/EventSourcing/TodoReminder.Flow.Integration.Test.cs
move_tree tests/Integration/Infrastructure.Integration.Tests/Persistence/Todos tests/Integration/Infrastructure.Integration.Tests/Modules/Todos/Persistence
```

Expected: only generic event sourcing/messaging/persistence tests remain in their BuildingBlocks folders.

- [ ] **Step 3: Rewrite Todos-specific namespaces**

```bash
rewrite_ns Infrastructure.Persistence.Schemas.Todos Infrastructure.Modules.Todos.Persistence.Schemas
sed -i 's/^namespace Infrastructure\.BuildingBlocks\.Adapters\.Persistence\.EntityFramework\.Projections;$/namespace Infrastructure.Modules.Todos.Persistence.Projections;/' \
  src/Infrastructure/Modules/Todos/Persistence/Projections/*.cs
rewrite_ns Infrastructure.BuildingBlocks.Adapters.Messaging.Handlers Infrastructure.Modules.Todos.Integrations.Messaging.Handlers
rewrite_ns Infrastructure.Services.Todos Infrastructure.Modules.Todos.Services

rewrite_ns Infrastructure.Unit.Tests.Persistence.Todos Infrastructure.Unit.Tests.Modules.Todos.Persistence
rewrite_ns Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Messaging.Handlers Infrastructure.Unit.Tests.Modules.Todos.Integrations.Messaging.Handlers
sed -i 's/^namespace Infrastructure\.Unit\.Tests\.Modules\.Todos\.Persistence;$/namespace Infrastructure.Unit.Tests.Modules.Todos.Services;/' \
  tests/Unit/Infrastructure.Unit.Tests/Modules/Todos/Services/TodoReferenceCleaner.Test.cs

sed -i 's/^namespace Infrastructure\.Integration\.Tests\.EventSourcing;$/namespace Infrastructure.Integration.Tests.Modules.Todos.EventSourcing;/' \
  tests/Integration/Infrastructure.Integration.Tests/Modules/Todos/EventSourcing/*.cs
rewrite_ns Infrastructure.Integration.Tests.Persistence.Todos Infrastructure.Integration.Tests.Modules.Todos.Persistence
```

Expected: `assert_no_ns Infrastructure.Persistence.Schemas.Todos` passes.

- [ ] **Step 4: Add the module registration record**

Create `src/Infrastructure/BuildingBlocks/InfrastructureModuleRegistrations.cs`:

```csharp
using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

namespace Infrastructure.BuildingBlocks;

/// <summary>Module contributions consumed by the BuildingBlocks composition.</summary>
/// <param name="Messaging">Messaging topology contributors.</param>
/// <param name="EventStores">Event-store aggregate/projection contributors.</param>
public sealed record InfrastructureModuleRegistrations(
    IReadOnlyList<IMessagingModule> Messaging,
    IReadOnlyList<IEventStoreModule> EventStores)
{
    /// <summary>An empty contribution set.</summary>
    public static InfrastructureModuleRegistrations Empty { get; } = new([], []);
}
```

- [ ] **Step 5: Add the BuildingBlocks infrastructure composition**

Create `src/Infrastructure/BuildingBlocks/BuildingBlock.Infra.Composition.cs`:

```csharp
using Infrastructure.BuildingBlocks.Adapters;
using Infrastructure.BuildingBlocks.Adapters.Caching;
using Infrastructure.BuildingBlocks.Adapters.Persistence;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;
using Infrastructure.BuildingBlocks.Adapters.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.BuildingBlocks;

/// <summary>Composition root for reusable Infrastructure BuildingBlocks.</summary>
public static class BuildingBlockInfraComposition
{
    /// <summary>Registers persistence, caching, security, and the adapter pipeline.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="modules">Module contributions.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddBuildingBlockInfrastructure(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        InfrastructureModuleRegistrations modules)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(modules);

        builder.AddPersistence<ApplicationDbContext>(configuration);
        builder.AddCaching(configuration);
        builder.AddSecurity(configuration);
        builder.AddOutbox(configuration);
        builder.AddEventStore(configuration, modules.EventStores);
        builder.AddAdapters(configuration, modules.Messaging);
        return builder;
    }

    /// <summary>Adds the BuildingBlocks pipeline hooks.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseBuildingBlockInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseCaching();
        app.UsePersistence();
        app.UsePersistenceInitialization();
        app.UseAdapters();
        return app;
    }
}
```

- [ ] **Step 6: Add the Todos messaging composition**

Create `src/Infrastructure/Modules/Todos/Integrations/Messaging/TodoMessaging.Composition.cs`:

```csharp
using Application.Modules.Todos.Events;

using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Options;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;

using Wolverine;
using Wolverine.RabbitMQ;

namespace Infrastructure.Modules.Todos.Integrations.Messaging;

/// <summary>Contributes the Todo exchange, queues, handlers, and event types.</summary>
public sealed class TodoMessagingModule : IMessagingModule
{
    private const string TodoCompletedEventName = "todo.completed";
    private const string TodoReminderChangedEventName = "todo.reminder.changed";
    private const int EventVersion = 1;

    /// <inheritdoc />
    public void Configure(
        WolverineOptions options,
        MessagingSetting setting,
        EventTypeRegistry eventTypes,
        RabbitMqTransportExpression? transport)
    {
        eventTypes.Register<TodoCompletedIntegration>(TodoCompletedEventName, EventVersion);
        eventTypes.Register<TodoReminderChangedIntegration>(TodoReminderChangedEventName, EventVersion);

        // Skip: the in-memory transport has no exchange or queue topology to bind.
        if (transport is null)
            return;

        string todoQueueName =
            $"{setting.QueuePrefix}.{TodoCompletedEventName}.{nameof(Handlers.TodoCompletedIntegrationHandler)}";
        string reminderQueueName =
            $"{setting.QueuePrefix}.{TodoReminderChangedEventName}.{nameof(Handlers.TodoReminderChangedIntegrationHandler)}";

        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(todoQueueName, TodoCompletedEventName);
        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(reminderQueueName, TodoReminderChangedEventName);

        options
            .PublishMessage<TodoCompletedIntegration>()
            .ToRabbitRoutingKey(
                setting.ExchangeName,
                TodoCompletedEventName,
                exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);
        options
            .PublishMessage<TodoReminderChangedIntegration>()
            .ToRabbitRoutingKey(
                setting.ExchangeName,
                TodoReminderChangedEventName,
                exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);

        options
            .ListenToRabbitQueue(todoQueueName)
            .PreFetchCount((ushort)setting.PrefetchCount);
        options
            .ListenToRabbitQueue(reminderQueueName)
            .PreFetchCount((ushort)setting.PrefetchCount);
    }
}
```

`AddMessaging` creates the registry once, registers it as a singleton, and passes it to each module. Do not call `BuildServiceProvider` inside composition.

- [ ] **Step 7: Add the Todos event-store composition**

Create `src/Infrastructure/Modules/Todos/Persistence/EventSourcing/TodoEventStore.Composition.cs`:

```csharp
using Application.BuildingBlocks.EventSourcing;

using Core.BuildingBlocks.Domain.EventSourcing;
using Core.Modules.Todos.Items;
using Core.Modules.Todos.Reminders;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Modules.Todos.Persistence.EventSourcing.Options;
using Infrastructure.Modules.Todos.Persistence.Projections;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Modules.Todos.Persistence.EventSourcing;

/// <summary>Registers the Todo aggregates, factories, and projections.</summary>
public sealed class TodoEventStoreModule : IEventStoreModule
{
    /// <inheritdoc />
    public void Configure(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TodoEventStoreSetting>()
            .BindConfiguration(TodoEventStoreSetting.SectionName);

        services.TryAddScoped<IAggregateFactory<TodoReminder>, TodoReminderFactory>();
        services.TryAddScoped<IAggregateFactory<Todo>, TodoFactory>();
        services.TryAddScoped<IProjection, TodoReminderProjection>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IProjection, TodoProjection>());
    }
}
```

Create `src/Infrastructure/Modules/Todos/Persistence/EventSourcing/Options/TodoEventStoreSetting.cs`:

```csharp
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

namespace Infrastructure.Modules.Todos.Persistence.EventSourcing.Options;

/// <summary>Todo-specific event-store switches that share the generic EventStore section.</summary>
public sealed class TodoEventStoreSetting
{
    /// <summary>The shared EventStore configuration section.</summary>
    public const string SectionName = EventStoreSetting.SectionName;

    /// <summary>Gets or sets whether the Todo backfill seeder runs.</summary>
    public bool BackfillTodos { get; set; }
}
```

Remove `BackfillTodos` from `EventStoreSetting` and `EventStoreSettingConstant`. Update `TodoBackfill.Seeder` to inject `IOptions<TodoEventStoreSetting>` for `BackfillTodos` and `IOptions<EventStoreSetting>` for `BatchSize`. Update its tests to pass both option objects.

- [ ] **Step 8: Add the Todos service composition**

Replace the moved `TodoService.Composition.cs` with:

```csharp
using Application.Modules.Todos.Abstractions;

using Infrastructure.Modules.Todos.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Modules.Todos.Services;

/// <summary>Registers cross-aggregate Todo services.</summary>
public static class TodoServiceComposition
{
    /// <summary>Adds the Todo reference cleaner.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddTodoServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddScoped<ITodoReferenceCleaner, TodoReferenceCleaner>();
        return builder;
    }
}
```

- [ ] **Step 9: Add the Modules infrastructure composition**

Create `src/Infrastructure/Modules/Modular.Infra.Composition.cs`:

```csharp
using Infrastructure.BuildingBlocks;
using Infrastructure.Modules.Todos.Integrations.Messaging;
using Infrastructure.Modules.Todos.Persistence.EventSourcing;
using Infrastructure.Modules.Todos.Services;

using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Modules;

/// <summary>Composition root for Infrastructure business modules.</summary>
public static class ModularInfraComposition
{
    /// <summary>Creates the module contributions consumed by BuildingBlocks registration.</summary>
    /// <returns>The module registration set.</returns>
    public static InfrastructureModuleRegistrations CreateRegistrations() =>
        new(
            Messaging: [new TodoMessagingModule()],
            EventStores: [new TodoEventStoreModule()]);

    /// <summary>Registers module services.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddModularInfrastructure(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddTodoServices();
        return builder;
    }

    /// <summary>Adds module pipeline hooks.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseModularInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
```

- [ ] **Step 10: Replace `Infrastructure.Composition.cs`**

```csharp
using Application;
using Application.BuildingBlocks.Persistence.Initialization;

using Infrastructure.BuildingBlocks;
using Infrastructure.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>Composition root for the Infrastructure layer.</summary>
public static class InfrastructureComposition
{
    /// <summary>Registers BuildingBlocks and the business modules.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(builder.Configuration);

        InfrastructureModuleRegistrations modules =
            ModularInfraComposition.CreateRegistrations();

        builder.AddBuildingBlockInfrastructure(builder.Configuration, modules);
        builder.AddModularInfrastructure();
        return builder;
    }

    /// <summary>Initializes the Infrastructure pipeline.</summary>
    /// <param name="app">The built web application.</param>
    /// <returns>The same application for further pipeline configuration.</returns>
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseApplication();
        app.UseBuildingBlockInfrastructure();
        app.UseModularInfrastructure();

        bool autoMigrate = app.Configuration.GetValue("Persistence:AutoMigrate", false);
        bool seed = app.Configuration.GetValue("Persistence:Seed", false);

        if (!autoMigrate && !seed)
            return app;

        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IPersistenceInitializer>();
        var result = initializer
            .InitializeAsync(runMigrations: autoMigrate, runSeeders: seed)
            .GetAwaiter()
            .GetResult();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database initialization failed: {string.Join("; ", result.Errors.Select(error => error.Message))}");
        }

        return app;
    }
}
```

- [ ] **Step 11: Verify Infrastructure**

```bash
make lib.build
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build
make test.infra.broker
make structure.check
```

Expected: all pass. The broker suite must still report 3 passed and unchanged queue names.

- [ ] **Step 12: Stage the task for review (no commit)**

```bash
git add -A -- src/Infrastructure tests/Unit/Infrastructure.Unit.Tests tests/Integration/Infrastructure.Integration.Tests
git diff --cached --stat -- src/Infrastructure tests/Unit/Infrastructure.Unit.Tests tests/Integration/Infrastructure.Integration.Tests > artifacts/refactor/task-5-stat.txt
git diff --cached -- src/Infrastructure tests/Unit/Infrastructure.Unit.Tests tests/Integration/Infrastructure.Integration.Tests > artifacts/refactor/task-5.diff
```

---

### Task 6: Todos permission and persistence-schema decoupling

**Files:**
- Create: `src/Application/BuildingBlocks/Security/Authorization/Registry/IPermissionCatalog.Interface.cs`
- Create: `src/Application/BuildingBlocks/Security/Authorization/Registry/IPermissionCatalogContributor.Interface.cs`
- Create: `src/Application/BuildingBlocks/Security/Authorization/Registry/PlatformPermissionCatalog.cs`
- Modify: `src/Application/BuildingBlocks/Security/Authorization/Registry/PermissionContext.cs`
- Create: `src/Application/Modules/PermissionCatalog/ApplicationPermissionCatalog.cs`
- Create: `src/Application/Modules/Todos/Permissions/TodoPermissionCatalog.cs`
- Create: `src/Application/BuildingBlocks/Persistence/IPersistenceSchemaCatalog.Interface.cs`
- Create: `src/Application/BuildingBlocks/Persistence/PlatformPersistenceSchemaCatalog.cs`
- Create: `src/Application/Modules/Persistence/ApplicationPersistenceSchemaCatalog.cs`
- Create: `src/Application/Modules/Todos/Persistence/TodoPersistenceSchemaCatalog.cs`
- Modify: `src/Application/BuildingBlocks/BuildingBlock.Application.Composition.cs`
- Modify: `src/Application/Modules/Modular.Application.Composition.cs`
- Modify: `src/Infrastructure/BuildingBlocks/Adapters/Security/Authorization/Permissions/Registry/Permission.Metadata.Registry.cs`
- Modify: `src/Infrastructure/BuildingBlocks/Adapters/Security/Identity/Seeders/Role.Seeder.cs`
- Modify: `src/Infrastructure/BuildingBlocks/Adapters/Security/Identity/Domain/Roles/Role.Constant.cs`
- Modify: `src/Infrastructure/BuildingBlocks/Adapters/Persistence/Initialization/EfCoreResetter.cs`
- Tests: update permission, schema, seeder, registry, and resetter tests.

**Interfaces:**
- Consumes: final folder layout from Tasks 1–5.
- Produces: `IPermissionCatalog`, `IPermissionCatalogContributor`, `IPersistenceSchemaCatalog`; BuildingBlocks no longer names `Todo` permissions or schema.

- [ ] **Step 1: Add permission catalog contracts**

Create `IPermissionCatalog.Interface.cs`:

```csharp
using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>Complete permission catalog and default role assignments.</summary>
public interface IPermissionCatalog
{
    /// <summary>Gets every known permission.</summary>
    IReadOnlyList<PermissionMetadata> All { get; }

    /// <summary>Gets the administrator defaults.</summary>
    IReadOnlyList<PermissionMetadata> Admin { get; }

    /// <summary>Gets the manager defaults.</summary>
    IReadOnlyList<PermissionMetadata> Manager { get; }

    /// <summary>Gets the standard user defaults.</summary>
    IReadOnlyList<PermissionMetadata> User { get; }
}
```

Create `IPermissionCatalogContributor.Interface.cs`:

```csharp
using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>A module's contribution to the permission catalog.</summary>
public interface IPermissionCatalogContributor
{
    /// <summary>Gets the module permissions.</summary>
    IReadOnlyList<PermissionMetadata> Permissions { get; }

    /// <summary>Gets the module's administrator defaults.</summary>
    IReadOnlyList<PermissionMetadata> Admin { get; }

    /// <summary>Gets the module's manager defaults.</summary>
    IReadOnlyList<PermissionMetadata> Manager { get; }

    /// <summary>Gets the module's standard user defaults.</summary>
    IReadOnlyList<PermissionMetadata> User { get; }
}
```

- [ ] **Step 2: Add the platform permission catalog**

Create `PlatformPermissionCatalog.cs`:

```csharp
using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>Platform permission catalog backed by the Identity baseline.</summary>
public sealed class PlatformPermissionCatalog : IPermissionCatalog, IPermissionCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> All => PermissionContext.All;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Permissions => PermissionContext.All;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Admin => PermissionContext.Admin;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Manager => PermissionContext.Manager;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> User => PermissionContext.User;
}
```

- [ ] **Step 3: Remove Todo permissions from `PermissionContext`**

Delete the nested `Todo` class and every `Todo.*` entry from `Admin`, `Manager`, and `User`. Keep the Identity permissions and the lookup helpers. After this step, `PermissionContext` contains only the Identity baseline.

- [ ] **Step 4: Add the Todos permission contributor**

Create `src/Application/Modules/Todos/Permissions/TodoPermissionCatalog.cs`:

```csharp
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;

namespace Application.Modules.Todos.Permissions;

/// <summary>Todos permission definitions and default role assignments.</summary>
public sealed class TodoPermissionCatalog : IPermissionCatalogContributor
{
    /// <summary>Todo item permissions.</summary>
    public static class Items
    {
        public static readonly PermissionMetadata Manage =
            PermissionMetadata.Create("todo", "todos", "item", "manage");
        public static readonly PermissionMetadata Read =
            PermissionMetadata.Create("todo", "todos", "item", "read");
        public static readonly PermissionMetadata Create =
            PermissionMetadata.Create("todo", "todos", "item", "create");
        public static readonly PermissionMetadata Update =
            PermissionMetadata.Create("todo", "todos", "item", "update");
        public static readonly PermissionMetadata Delete =
            PermissionMetadata.Create("todo", "todos", "item", "delete");
    }

    /// <summary>Todo list permissions.</summary>
    public static class Lists
    {
        public static readonly PermissionMetadata Manage =
            PermissionMetadata.Create("todo", "lists", "list", "manage");
        public static readonly PermissionMetadata Read =
            PermissionMetadata.Create("todo", "lists", "list", "read");
        public static readonly PermissionMetadata Create =
            PermissionMetadata.Create("todo", "lists", "list", "create");
        public static readonly PermissionMetadata Update =
            PermissionMetadata.Create("todo", "lists", "list", "update");
        public static readonly PermissionMetadata Delete =
            PermissionMetadata.Create("todo", "lists", "list", "delete");
    }

    /// <summary>Todo category permissions.</summary>
    public static class Categories
    {
        public static readonly PermissionMetadata Manage =
            PermissionMetadata.Create("todo", "categories", "category", "manage");
        public static readonly PermissionMetadata Read =
            PermissionMetadata.Create("todo", "categories", "category", "read");
        public static readonly PermissionMetadata Create =
            PermissionMetadata.Create("todo", "categories", "category", "create");
        public static readonly PermissionMetadata Update =
            PermissionMetadata.Create("todo", "categories", "category", "update");
        public static readonly PermissionMetadata Delete =
            PermissionMetadata.Create("todo", "categories", "category", "delete");
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Permissions { get; } =
    [
        Items.Manage, Items.Read, Items.Create, Items.Update, Items.Delete,
        Lists.Manage, Lists.Read, Lists.Create, Lists.Update, Lists.Delete,
        Categories.Manage, Categories.Read, Categories.Create,
        Categories.Update, Categories.Delete
    ];

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Admin => Permissions;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Manager => Permissions;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> User => Permissions;
}
```

- [ ] **Step 5: Add the composite permission catalog**

Create `src/Application/Modules/PermissionCatalog/ApplicationPermissionCatalog.cs`:

```csharp
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;

namespace Application.Modules.PermissionCatalog;

/// <summary>Merges platform and module permission contributions.</summary>
public sealed class ApplicationPermissionCatalog(
    IEnumerable<IPermissionCatalogContributor> contributors) : IPermissionCatalog
{
    private readonly IReadOnlyList<IPermissionCatalogContributor> _contributors =
        [.. contributors];

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> All { get; } = Merge(
        contributor => contributor.Permissions);

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Admin { get; } = Merge(
        contributor => contributor.Admin);

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Manager { get; } = Merge(
        contributor => contributor.Manager);

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> User { get; } = Merge(
        contributor => contributor.User);

    private IReadOnlyList<PermissionMetadata> Merge(
        Func<IPermissionCatalogContributor, IReadOnlyList<PermissionMetadata>> selector) =>
        [.. _contributors
            .SelectMany(selector)
            .DistinctBy(permission => permission.Identifier, StringComparer.OrdinalIgnoreCase)];
}
```

- [ ] **Step 6: Register permission catalogs**

In `BuildingBlock.Application.Composition.cs`, add:

```csharp
builder.Services.AddSingleton<PlatformPermissionCatalog>();
builder.Services.AddSingleton<IPermissionCatalogContributor>(
    provider => provider.GetRequiredService<PlatformPermissionCatalog>());
builder.Services.AddSingleton<IPermissionCatalog>(
    provider => provider.GetRequiredService<PlatformPermissionCatalog>());
```

Add the usings for `Application.BuildingBlocks.Security.Authorization.Registry` and `Microsoft.Extensions.DependencyInjection`.

In `Modular.Application.Composition.cs`, add:

```csharp
builder.Services.AddSingleton<TodoPermissionCatalog>();
builder.Services.AddSingleton<IPermissionCatalogContributor>(
    provider => provider.GetRequiredService<TodoPermissionCatalog>());
builder.Services.AddSingleton<IPermissionCatalog, ApplicationPermissionCatalog>();
```

Add the usings for `Application.Modules.PermissionCatalog`, `Application.Modules.Todos.Permissions`, `Application.BuildingBlocks.Security.Authorization.Registry`, and `Microsoft.Extensions.DependencyInjection`.

- [ ] **Step 7: Consume the composite catalog in Infrastructure**

Change `PermissionMetadataRegistry` to inject `IPermissionCatalog catalog` and replace every `PermissionContext.All` with `catalog.All`.

Change `RoleSeeder` to inject `IPermissionCatalog catalog` and use `catalog.Admin`, `catalog.Manager`, and `catalog.User` in `s_defaults`.

Change `RoleConstant.RolePermissions` to either remove the static aliases or keep them platform-only. If any production code still uses `RoleConstant.RolePermissions`, replace it with `catalog` access. Do not leave a Todo-aware static alias in BuildingBlocks.

- [ ] **Step 8: Add persistence-schema contracts**

Create `src/Application/BuildingBlocks/Persistence/IPersistenceSchemaCatalog.Interface.cs`:

```csharp
namespace Application.BuildingBlocks.Persistence;

/// <summary>Schemas and tables owned by the platform and its modules.</summary>
public interface IPersistenceSchemaCatalog
{
    /// <summary>Gets every managed schema, in reset order.</summary>
    IReadOnlyList<string> Schemas { get; }

    /// <summary>Gets every managed table name used by data-only resets.</summary>
    IReadOnlyList<string> Tables { get; }
}
```

Create `PlatformPersistenceSchemaCatalog.cs` with the platform schemas and tables currently in `PersistenceConstant.Schema.Names` minus `todo`, and the platform tables from `EfCoreResetter.DefaultTables` minus Todo tables:

```csharp
namespace Application.BuildingBlocks.Persistence;

/// <summary>Platform-owned schemas and tables.</summary>
public sealed class PlatformPersistenceSchemaCatalog : IPersistenceSchemaCatalog
{
    /// <inheritdoc />
    public IReadOnlyList<string> Schemas { get; } =
    [
        PersistenceConstant.Schema.Identity,
        PersistenceConstant.Schema.Messaging
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> Tables { get; } =
    [
        "refresh_tokens",
        "passkey_data",
        "passkeys",
        "user_tokens",
        "user_logins",
        "user_roles",
        "user_claims",
        "role_claims",
        "roles",
        "users"
    ];
}
```

Create `src/Application/Modules/Todos/Persistence/TodoPersistenceSchemaCatalog.cs`:

```csharp
using Application.BuildingBlocks.Persistence;

namespace Application.Modules.Todos.Persistence;

/// <summary>Todos schema and tables.</summary>
public sealed class TodoPersistenceSchemaCatalog : IPersistenceSchemaCatalog
{
    /// <summary>The Todos schema name.</summary>
    public const string SchemaName = "todo";

    /// <inheritdoc />
    public IReadOnlyList<string> Schemas { get; } = [SchemaName];

    /// <inheritdoc />
    public IReadOnlyList<string> Tables { get; } =
    [
        "todos",
        "todo_lists",
        "todo_categories"
    ];
}
```

Create `src/Application/Modules/Persistence/ApplicationPersistenceSchemaCatalog.cs`:

```csharp
using Application.BuildingBlocks.Persistence;

namespace Application.Modules.Persistence;

/// <summary>Merges platform and module persistence schemas and tables.</summary>
public sealed class ApplicationPersistenceSchemaCatalog(
    IEnumerable<IPersistenceSchemaCatalog> catalogs) : IPersistenceSchemaCatalog
{
    /// <inheritdoc />
    public IReadOnlyList<string> Schemas { get; } =
        [.. catalogs.SelectMany(catalog => catalog.Schemas).Distinct(StringComparer.Ordinal)];

    /// <inheritdoc />
    public IReadOnlyList<string> Tables { get; } =
        [.. catalogs.SelectMany(catalog => catalog.Tables).Distinct(StringComparer.Ordinal)];
}
```

- [ ] **Step 9: Remove Todo from platform persistence constants**

In `PersistenceConstant.Schema`:

- Delete `Todo` and every `Todo` reference.
- Keep `Identity` and `Messaging`.
- Change `Default` to `"public"`.
- Change `Names` to `[Identity, Messaging]`.

**Compatibility gate:** `NpgsqlOptionExtensions.UsePersistenceMigrationHistory()` currently puts `__EFMigrationsHistory` in `PersistenceConstant.Schema.Default` (`todo`). Before changing the default, inspect the local database:

```bash
docker compose exec -T postgres psql -U qingfa -d db -c '\dt todo.__EFMigrationsHistory'
```

If the table exists, do **not** silently move history to `public`. Choose one and record it:

- keep `Default = "todo"` as a temporary legacy compatibility constant, or
- add a dedicated migration/script that moves `__EFMigrationsHistory` to the new default schema before the refactor lands.

The preferred target is `public`, but preserving migration history wins over a cosmetic decoupling. Record the choice in the task report.

Update `Infrastructure/Modules/Todos/Persistence/Schemas/Todos/TodoTable.cs` and `Infrastructure/Modules/Todos/Persistence/Projections/TodoReminderReadModel.Configuration.cs` to use `TodoPersistenceSchemaCatalog.SchemaName` and add the `Application.Modules.Todos.Persistence` using. No Todo schema string may remain in BuildingBlocks.

- [ ] **Step 10: Register schema catalogs and consume them**

In `BuildingBlock.Application.Composition.cs`, add:

```csharp
builder.Services.AddSingleton<PlatformPersistenceSchemaCatalog>();
builder.Services.AddSingleton<IPersistenceSchemaCatalog>(
    provider => provider.GetRequiredService<PlatformPersistenceSchemaCatalog>());
```

In `Modular.Application.Composition.cs`, add:

```csharp
builder.Services.AddSingleton<TodoPersistenceSchemaCatalog>();
builder.Services.AddSingleton<IPersistenceSchemaCatalog>(
    provider => provider.GetRequiredService<TodoPersistenceSchemaCatalog>());
builder.Services.AddSingleton<IPersistenceSchemaCatalog, ApplicationPersistenceSchemaCatalog>();
```

Add the required usings.

Change `EfCoreResetter<TContext>` to accept `IPersistenceSchemaCatalog schemaCatalog` and replace the static `Schemas`/`DefaultTables` with `schemaCatalog.Schemas` and `schemaCatalog.Tables`.

- [ ] **Step 11: Update tests**

- Replace `PermissionContext.Todo.*` references with `TodoPermissionCatalog.*`.
- Replace tests that assert the full permission count with `ApplicationPermissionCatalog` built from `PlatformPermissionCatalog` + `TodoPermissionCatalog`.
- Update `Permission.Metadata.Registry.Tests` to pass a catalog.
- Update `RoleSeeder` tests to pass a catalog.
- Update `EfCoreResetter` tests to pass a schema catalog.
- Add `ApplicationPermissionCatalog.Test.cs` and `ApplicationPersistenceSchemaCatalog.Test.cs` verifying merging, de-duplication, and ordering.
- Add `TodoPersistenceSchemaCatalog.Test.cs` verifying the schema is `todo` and the tables are exactly the three Todo tables.

- [ ] **Step 12: Verify the decoupling**

```bash
grep -rnE '\bTodo\b|\btodo\b' --include='*.cs' src/Application/BuildingBlocks src/Infrastructure/BuildingBlocks
make lib.build
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build
dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build
make test.infra.broker
make structure.check
```

Expected: the grep returns no Todo references in BuildingBlocks. Tests pass. If `PersistenceConstant.Schema.Default = "public"` changes the EF model, stop and record the pending-model diff for Task 7.

- [ ] **Step 13: Stage the task for review (no commit)**

```bash
git add -A -- src/Application src/Infrastructure tests/Unit
git diff --cached --stat -- src/Application src/Infrastructure tests/Unit > artifacts/refactor/task-6-stat.txt
git diff --cached -- src/Application src/Infrastructure tests/Unit > artifacts/refactor/task-6.diff
```

---

### Task 7: Migrations third folder and EF model update

**Files:**
- Move: `src/Infrastructure/Persistence/Migrations/**` → `src/Infrastructure/Migrations/**`
- Modify: `src/Infrastructure/Migrations/20261030042655_InitialCreate.cs`
- Modify: `src/Infrastructure/Migrations/20261030042655_InitialCreate.Designer.cs`
- Modify: `src/Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`
- Modify: `src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Contexts/ApplicationDbContext.DesignTime.Factory.cs`
- Modify: any migration test paths.

**Interfaces:**
- Consumes: final type namespaces from Tasks 1–6.
- Produces: `Infrastructure.Migrations.*`; snapshot strings match current CLR type names.

- [ ] **Step 1: Move the migrations folder**

```bash
move_tree src/Infrastructure/Persistence/Migrations src/Infrastructure/Migrations
rmdir src/Infrastructure/Persistence 2>/dev/null || true
```

Expected: `src/Infrastructure/Migrations` is the only persistence-history folder.

- [ ] **Step 2: Rewrite migration namespace and type strings**

```bash
rewrite_ns Infrastructure.Persistence.Migrations Infrastructure.Migrations
rewrite_ns Infrastructure.Persistence.EntityFramework.Contexts.ApplicationDbContext Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts.ApplicationDbContext
rewrite_ns Infrastructure.Security.Identity Infrastructure.BuildingBlocks.Adapters.Security.Identity
rewrite_ns Application.Features.Todos Application.Modules.Todos
rewrite_ns Application.Features.Identity Application.BuildingBlocks.Security.Identity
rewrite_ns Core.Todos Core.Modules.Todos

sed -i 's/HasDefaultSchema("todo")/HasDefaultSchema("public")/g' \
  src/Infrastructure/Migrations/*.cs
```

Also update any `modelBuilder.Entity("...")` strings that still use an old namespace. Run:

```bash
grep -rnE 'Application\.Features|Core\.Todos|Infrastructure\.Security\.Identity|Infrastructure\.Persistence\.EntityFramework' src/Infrastructure/Migrations
```

Expected: no output.

- [ ] **Step 2b: Add the generic legacy migration-history guard**

Add a private guard to `EfCoreMigrator<TContext>` that runs before `GetPendingMigrationsAsync`:

```csharp
private async Task MoveLegacyMigrationHistoryAsync(CancellationToken cancellationToken)
{
    var connection = _context.Database.GetDbConnection();
    bool wasClosed = connection.State != ConnectionState.Open;
    if (wasClosed)
        await connection.OpenAsync(cancellationToken);

    try
    {
        const string historyTable = "__EFMigrationsHistory";
        string target = PersistenceConstant.Schema.Default;

        await using var discover = connection.CreateCommand();
        discover.CommandText = """
            SELECT table_schema
            FROM information_schema.tables
            WHERE table_name = @table
              AND table_schema <> @target
            """;
        discover.Parameters.Add(new NpgsqlParameter("table", historyTable));
        discover.Parameters.Add(new NpgsqlParameter("target", target));

        var legacySchemas = new List<string>();
        await using (var reader = await discover.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                legacySchemas.Add(reader.GetString(0));
        }

        foreach (string schema in legacySchemas)
        {
            await using var alter = connection.CreateCommand();
            alter.CommandText =
                $"ALTER TABLE {Quote(schema)}.{Quote(historyTable)} SET SCHEMA {Quote(target)}";
            await alter.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation(
                "Moved legacy migration history from schema '{Schema}' to '{Target}'.",
                schema,
                target);
        }
    }
    finally
    {
        if (wasClosed)
            await connection.CloseAsync();
    }
}

private static string Quote(string identifier) =>
    $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
```

Call it at the start of `ExecuteAsync`, inside the existing try/catch that returns `MigrationFailed`:

```csharp
await MoveLegacyMigrationHistoryAsync(cancellationToken);
pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
```

Add a PostgreSQL integration test that creates a `legacy_history` schema with an `__EFMigrationsHistory` table, runs the migrator, and asserts the table moved to `public`. If the target table already exists, log and skip the legacy move rather than throwing.

- [ ] **Step 3: Verify the EF model**

```bash
make lib.build
dotnet ef migrations has-pending-model-changes \
  --project src/Infrastructure \
  --startup-project app/Web.Api \
  --no-build
```

Expected: no pending model changes. If the command reports changes, inspect the generated diff and reconcile it against the namespace moves and the `PersistenceConstant.Schema.Default` change. Do not create a new migration.

If `dotnet ef` is not installed in this environment, record that in the task report and fall back to `make lib.build` plus the migrator integration tests. Do not add a tool manifest or package for this refactor.

- [ ] **Step 4: Run persistence tests**

```bash
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "Infrastructure.Integration.Tests.Persistence.Initialization.EfCoreMigratorIntegrationTest"
dotnet test --project tests/Integration/Infrastructure.Integration.Tests/Infrastructure.Integration.Tests.csproj -c Release --no-build --filter-class "Infrastructure.Integration.Tests.Persistence.Initialization.EfCorePersistenceInitializerIntegrationTest"
```

Expected: both pass. If Docker is unavailable, record the skip counts.

- [ ] **Step 5: Stage the task for review (no commit)**

```bash
git add -A -- src/Infrastructure/Migrations src/Infrastructure/BuildingBlocks tests/Integration/Infrastructure.Integration.Tests
git diff --cached --stat -- src/Infrastructure/Migrations src/Infrastructure/BuildingBlocks tests/Integration/Infrastructure.Integration.Tests > artifacts/refactor/task-7-stat.txt
git diff --cached -- src/Infrastructure/Migrations src/Infrastructure/BuildingBlocks tests/Integration/Infrastructure.Integration.Tests > artifacts/refactor/task-7.diff
```

---

### Task 8: Api integration test taxonomy

**Files:**
- Move: `tests/Integration/Api.Integration.Tests/ApiJson.Host.Test.cs` → `tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ApiJson.Host.Test.cs`
- Move: `ApiResult.Test.cs`, `ApiSmoke.Test.cs`, `ResultContract.Api.Test.cs`, `ResultContractFlow.Api.Test.cs`, `ResultContractStack.*`, `ResultSerialization.Test.cs` → `BuildingBlocks/Api/`
- Move: `Auth.Api.Test.cs`, `AuthTokenResponse.Test.cs`, `ExternalAuth.Api.Test.cs`, `Identity*` → `BuildingBlocks/Security/Identity/`
- Move: `TodoRoutes.Api.Test.cs` → `Modules/Todos/`
- Keep: `ApiHost.Collection.cs`, `Postgres.Collection.cs`, `Assembly.Marker.cs`, `AssemblyMarker.Tests.cs`, `FakeClock.Test.cs` at the project root.
- Modify: namespaces in every moved file.

**Interfaces:**
- Consumes: final production namespaces from Tasks 1–7.
- Produces: Api tests mirror the BuildingBlocks/Modules taxonomy.

- [ ] **Step 1: Create the target folders**

```bash
mkdir -p tests/Integration/Api.Integration.Tests/BuildingBlocks/Api
mkdir -p tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity
mkdir -p tests/Integration/Api.Integration.Tests/Modules/Todos
```

- [ ] **Step 2: Move the test files**

```bash
move_tree tests/Integration/Api.Integration.Tests/ApiJson.Host.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ApiJson.Host.Test.cs
move_tree tests/Integration/Api.Integration.Tests/ApiResult.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ApiResult.Test.cs
move_tree tests/Integration/Api.Integration.Tests/ApiSmoke.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ApiSmoke.Test.cs
move_tree tests/Integration/Api.Integration.Tests/ResultContract.Api.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ResultContract.Api.Test.cs
move_tree tests/Integration/Api.Integration.Tests/ResultContractFlow.Api.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ResultContractFlow.Api.Test.cs
move_tree tests/Integration/Api.Integration.Tests/ResultContractStack.Collection.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ResultContractStack.Collection.cs
move_tree tests/Integration/Api.Integration.Tests/ResultContractStack.Fixture.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ResultContractStack.Fixture.cs
move_tree tests/Integration/Api.Integration.Tests/ResultSerialization.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/ResultSerialization.Test.cs
move_tree tests/Integration/Api.Integration.Tests/Auth.Api.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/Auth.Api.Test.cs
move_tree tests/Integration/Api.Integration.Tests/AuthTokenResponse.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/AuthTokenResponse.Test.cs
move_tree tests/Integration/Api.Integration.Tests/ExternalAuth.Api.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/ExternalAuth.Api.Test.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityApi.Collection.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityApi.Collection.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityApi.Fixture.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityApi.Fixture.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityApi.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityApi.Test.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityEndpoint.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityEndpoint.Test.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityPermissions.Api.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityPermissions.Api.Test.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityRoles.Api.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityRoles.Api.Test.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityStack.Fixture.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityStack.Fixture.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityStack.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityStack.Test.cs
move_tree tests/Integration/Api.Integration.Tests/IdentityUsers.Api.Test.cs tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/IdentityUsers.Api.Test.cs
move_tree tests/Integration/Api.Integration.Tests/TodoRoutes.Api.Test.cs tests/Integration/Api.Integration.Tests/Modules/Todos/TodoRoutes.Api.Test.cs
```

- [ ] **Step 3: Rewrite Api test namespaces**

```bash
rewrite_ns Api.Integration.Tests.BuildingBlocks.Api Api.Integration.Tests.BuildingBlocks.Api
```

The existing files use `namespace Api.Integration.Tests;`. Replace it per folder:

```bash
sed -i 's/^namespace Api\.Integration\.Tests;$/namespace Api.Integration.Tests.BuildingBlocks.Api;/' \
  tests/Integration/Api.Integration.Tests/BuildingBlocks/Api/*.cs
sed -i 's/^namespace Api\.Integration\.Tests;$/namespace Api.Integration.Tests.BuildingBlocks.Security.Identity;/' \
  tests/Integration/Api.Integration.Tests/BuildingBlocks/Security/Identity/*.cs
sed -i 's/^namespace Api\.Integration\.Tests;$/namespace Api.Integration.Tests.Modules.Todos;/' \
  tests/Integration/Api.Integration.Tests/Modules/Todos/*.cs
```

Then rewrite all production namespace references in the moved files:

```bash
rewrite_ns Application.Features.Todos Application.Modules.Todos
rewrite_ns Application.Features.Identity Application.BuildingBlocks.Security.Identity
rewrite_ns Application.Common Application.BuildingBlocks
rewrite_ns Core.Todos Core.Modules.Todos
rewrite_ns Infrastructure.Persistence Infrastructure.BuildingBlocks.Adapters.Persistence
rewrite_ns Infrastructure.Security Infrastructure.BuildingBlocks.Adapters.Security
```

- [ ] **Step 4: Update collection/fixture references**

If a collection class moved, update `[Collection("...")]` strings only when the collection name itself changed. Keep the existing collection names stable.

Moved files now live in child namespaces, so add `using Api.Integration.Tests;` wherever they still reference root-level types such as `ApiHostCollection`, `PostgresCollection`, `FakeClock`, or `AssemblyMarker`. Do not duplicate fixtures or change collection names.

- [ ] **Step 5: Verify Api tests**

```bash
make lib.build
dotnet test --project tests/Integration/Api.Integration.Tests/Api.Integration.Tests.csproj -c Release --no-build
make structure.check
```

Expected: all Api tests pass. If Docker is unavailable, record skip counts.

- [ ] **Step 6: Stage the task for review (no commit)**

```bash
git add -A -- tests/Integration/Api.Integration.Tests
git diff --cached --stat -- tests/Integration/Api.Integration.Tests > artifacts/refactor/task-8-stat.txt
git diff --cached -- tests/Integration/Api.Integration.Tests > artifacts/refactor/task-8.diff
```

---

### Task 9: Architecture guardrails and documentation

**Files:**
- Create: `tests/Architecture.Tests/BuildingBlockBoundary.Tests.cs`
- Modify: `tests/Architecture.Tests/CommandMarker.Tests.cs`
- Modify: `tests/Architecture.Tests/VerticalSliceArchitecture.Tests.cs`
- Modify: `README.md`
- Modify: `AGENTS.md`
- Modify: `src/README.md`
- Modify: `src/Core/README.md`
- Modify: `src/Core/Modules/Todos/README.md`
- Modify: `src/Application/README.md`
- Modify: `src/Infrastructure/README.md`
- Modify: `tests/README.md`
- Modify: `tests/Architecture.Tests/README.md`

**Interfaces:**
- Consumes: final namespaces from Tasks 1–8.
- Produces: executable boundary rules and updated docs.

- [ ] **Step 1: Add the boundary test**

Create `tests/Architecture.Tests/BuildingBlockBoundary.Tests.cs`:

```csharp
using System.Reflection;

namespace QingFa.Shop.Architecture.Tests;

/// <summary>Enforces the BuildingBlocks/Modules dependency direction.</summary>
public sealed class BuildingBlockBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(Core.AssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.AssemblyMarker).Assembly;

    [Fact(DisplayName = "Core BuildingBlocks should not reference Core modules")]
    public void CoreBuildingBlocks_ShouldNotReferenceCoreModules()
    {
        AssertNoReferences(
            CoreAssembly,
            "Core.BuildingBlocks.",
            "Core.Modules.");
    }

    [Fact(DisplayName = "Application BuildingBlocks should not reference application modules")]
    public void ApplicationBuildingBlocks_ShouldNotReferenceApplicationModules()
    {
        AssertNoReferences(
            ApplicationAssembly,
            "Application.BuildingBlocks.",
            "Application.Modules.");
    }

    [Fact(DisplayName = "Infrastructure BuildingBlocks should not reference any module")]
    public void InfrastructureBuildingBlocks_ShouldNotReferenceModules()
    {
        AssertNoReferences(
            InfrastructureAssembly,
            "Infrastructure.BuildingBlocks.",
            "Infrastructure.Modules.");

        AssertNoReferences(
            InfrastructureAssembly,
            "Infrastructure.BuildingBlocks.",
            "Application.Modules.");
    }

    private static void AssertNoReferences(
        Assembly assembly,
        string sourcePrefix,
        string forbiddenPrefix)
    {
        var offenders = assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(sourcePrefix, StringComparison.Ordinal) == true)
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => referenced.Namespace?.StartsWith(
                    forbiddenPrefix,
                    StringComparison.Ordinal) == true)
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            because: $"{sourcePrefix} must not depend on {forbiddenPrefix}");
    }

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        if (type.BaseType is not null)
            yield return type.BaseType;

        foreach (Type contract in type.GetInterfaces())
            yield return contract;

        foreach (FieldInfo field in type.GetFields(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return field.FieldType;
        }

        foreach (PropertyInfo property in type.GetProperties(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return property.PropertyType;
        }

        foreach (MethodInfo method in type.GetMethods(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;
            foreach (ParameterInfo parameter in method.GetParameters())
                yield return parameter.ParameterType;
        }
    }
}
```

- [ ] **Step 2: Update the command-marker namespaces**

In `tests/Architecture.Tests/CommandMarker.Tests.cs`, change:

```csharp
private static readonly string[] SelfManagedNamespaces =
[
    "Application.BuildingBlocks.Security.Identity.Auth",
    "Application.BuildingBlocks.Security.Identity.External",
];
```

- [ ] **Step 3: Update the vertical-slice documentation**

In `tests/Architecture.Tests/VerticalSliceArchitecture.Tests.cs`, replace comments that mention `Application.Features` with `Application.Modules` and `Application.BuildingBlocks`.

- [ ] **Step 4: Update the root and layer READMEs**

Use these exact taxonomy statements:

```text
Core:
  BuildingBlocks/Domain, BuildingBlocks/Modeling, BuildingBlocks/Functional
  Modules/Todos

Application:
  BuildingBlocks/{Caching,EventSourcing,Messaging,Persistence,Ports,Querying,Security,UseCases}
  Modules/Todos

Infrastructure:
  BuildingBlocks/Adapters/{BackgroundJobs,Caching,Events,Http,Messaging,Notifications,Observability,OpenApi,Persistence,Security,Serialization,Storage}
  Modules/Todos
  Migrations
```

Update every `Common/`, `Features/`, `Integrations/`, `Mediators/`, and `Storages/` reference in those READMEs. Keep the existing README section structure.

- [ ] **Step 5: Verify the guardrails**

```bash
make lib.build
dotnet test --project tests/Architecture.Tests/Architecture.Tests.csproj -c Release --no-build
make structure.check
```

Expected: all pass; the new boundary tests fail only if a BuildingBlocks type still references a module.

- [ ] **Step 6: Stage the task for review (no commit)**

```bash
git add -A -- tests/Architecture.Tests README.md AGENTS.md src/README.md src/Core/README.md src/Core/Modules/Todos/README.md src/Application/README.md src/Infrastructure/README.md tests/README.md
git diff --cached --stat -- tests/Architecture.Tests README.md AGENTS.md src/README.md src/Core/README.md src/Core/Modules/Todos/README.md src/Application/README.md src/Infrastructure/README.md tests/README.md > artifacts/refactor/task-9-stat.txt
git diff --cached -- tests/Architecture.Tests README.md AGENTS.md src/README.md src/Core/README.md src/Core/Modules/Todos/README.md src/Application/README.md src/Infrastructure/README.md tests/README.md > artifacts/refactor/task-9.diff
```

---

### Task 10: Full verification and handoff

**Files:**
- No production edits. Verification only.
- Create: `artifacts/refactor/final-verification.txt`

**Interfaces:**
- Consumes: all previous tasks.
- Produces: a verified dirty worktree, ready for the human partner's review.

- [ ] **Step 1: Verify no stale namespaces**

```bash
{
  grep -rnE '\bCore\.Common\b|\bCore\.Todos\b' --include='*.cs' src tests app || true
  grep -rnE '\bApplication\.Common\b|\bApplication\.Features\b' --include='*.cs' src tests app || true
  grep -rnE '\bInfrastructure\.Integrations\b|\bInfrastructure\.Services\b' --include='*.cs' src tests app || true
  grep -rnE '\bInfrastructure\.Persistence\b' --include='*.cs' src tests app || true
} > artifacts/refactor/final-stale-namespaces.txt
```

Expected: the file contains no matches. `Infrastructure.Migrations` is allowed.

- [ ] **Step 2: Verify the folder taxonomy**

```bash
find src/Core -maxdepth 2 -type d | sort
find src/Application -maxdepth 2 -type d | sort
find src/Infrastructure -maxdepth 3 -type d | sort
```

Expected:

```text
src/Core/BuildingBlocks
src/Core/Modules
src/Application/BuildingBlocks
src/Application/Modules
src/Infrastructure/BuildingBlocks
src/Infrastructure/Modules
src/Infrastructure/Migrations
```

No `Common`, `Features`, `Integrations`, `Mediators`, `Storages`, `Services/Todos`, or `Persistence/Schemas/Todos` folders remain.

- [ ] **Step 3: Run the fast gate**

```bash
make lint
make build
make test
```

Expected: all pass. `make test` runs .NET unit, Api, web unit, and web integration.

- [ ] **Step 4: Run the coverage gate**

```bash
make coverage
```

Expected: per-assembly floors pass: 90% line / 80% branch for `Core`, `Application`, `Infrastructure`, `Web.Api`; both web apps.

- [ ] **Step 5: Run the broker and infrastructure suites**

```bash
make test.infra.broker
make coverage.infra
```

Expected: broker suite reports 3 passed; Infrastructure integration suite reports `failed: 0` and unchanged broker queue names. If Docker is unavailable, record the skip counts.

- [ ] **Step 6: Run the full CI gate**

```bash
make ci
```

Expected: `ci: ok`.

- [ ] **Step 7: Confirm no commits**

```bash
git rev-parse HEAD
cat artifacts/refactor/baseline-head.txt
git status --short
```

Expected: `git rev-parse HEAD` matches `baseline-head.txt`; the worktree is dirty with the refactor.

- [ ] **Step 8: Write the final verification report**

```bash
{
  date -u +'%Y-%m-%dT%H:%M:%SZ'
  git status --short
  git diff --cached --stat
  echo '--- lint/build/test ---'
  echo 'see terminal output above'
} > artifacts/refactor/final-verification.txt
```

Expected: the report exists and lists every changed path.

- [ ] **Step 9: Handoff**

Report:

- tasks completed,
- exact commands run,
- pass/fail/skip counts,
- any parked findings,
- confirmation that no commit was created,
- the fact that `artifacts/refactor/` contains the baseline and per-task diffs for review.

Do not commit, merge, push, or open a PR.
