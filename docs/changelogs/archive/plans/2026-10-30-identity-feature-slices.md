# Identity Feature Slices Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Convert every Identity operation into a MediatR command/query, add admin user/role/permission slices, and move external sign-in into MediatR while keeping Carter as a thin HTTP adapter.

**Architecture:** `Application/Features/Identity` owns Auth, Users, Roles, Permissions, and External slices. New Application ports isolate ASP.NET Identity. Carter modules only bind, dispatch, and write `Result<T>` / `Result` via `ToApiResult`.

**Tech Stack:** .NET 10, MediatR, FluentValidation, Carter, ASP.NET Identity, Redis, xUnit v3.

**Status:** Implemented (all tasks complete; gates green)

**Spec:** `docs/specs/2026-10-30-identity-feature-slices-design.md`

## Global Constraints

- Shared `/api/auth/*`; admin-gated `/api/identity/users|roles|permissions`.
- All Identity use cases are MediatR commands/queries.
- No endpoint contains identity business logic.
- Permissions feature lives under `Features/Identity/Permissions`; shared authorization primitives stay in `Common/Security/Authorization`.
- External challenge/callback/exchange are commands; Carter applies redirects/challenges.
- Result contract unchanged: `Result<T>` success, `Result` failure, status from `Result.StatusCode`.
- Dotted-role filenames, labeled comments, structure/format checks, coverage floors.

---

### Task 1: Ports, Models, and Specs

**Files:**
- Create: `src/Application/Features/Identity/Abstractions/IUserAccountService.cs`
- Create: `src/Application/Features/Identity/Abstractions/IRoleManagementService.cs`
- Create: `src/Application/Features/Identity/Abstractions/IPermissionCatalogService.cs`
- Create: `src/Application/Features/Identity/External/Abstractions/IExternalCodeStore.cs`
- Create: `src/Application/Features/Identity/Users/Models/User.Model.cs`
- Create: `src/Application/Features/Identity/Roles/Models/Role.Model.cs`
- Create: `src/Application/Features/Identity/Permissions/Models/Permission.Model.cs`
- Create: `src/Application/Features/Identity/External/Models/External.Model.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Identity/Ports/IdentityPorts.Test.cs`

**Interfaces:**
- Produces: the three admin ports, `IExternalCodeStore`, and the shared application models.
- Consumes: `QueryParameters`, `QueryPage<T>`, `PermissionMatchMode`.

- [x] **Step 1: Add the port contracts**

Use the signatures in spec §6. Add XML docs and `Result` returns.

- [x] **Step 2: Add models**

```csharp
public sealed record UserModel(
    Guid Id,
    string UserName,
    string Email,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    IReadOnlyList<string> Roles);

public sealed record UserSummaryModel(
    Guid Id,
    string UserName,
    string Email,
    bool IsActive,
    bool IsLockedOut,
    IReadOnlyList<string> Roles);

public sealed record RoleModel(
    Guid Id,
    string Name,
    IReadOnlyList<string> Permissions);

public sealed record PermissionItemModel(
    string Identifier,
    string Domain,
    string Category,
    string Resource,
    string Action,
    string Description);

public sealed record PermissionSnapshotModel(
    IReadOnlyList<PermissionItemModel> Permissions,
    DateTimeOffset RefreshedOnUtc);

public sealed record ExternalChallengeModel(string Provider, string RedirectUri);

public sealed record ExternalCallbackModel(string RedirectUrl);

public sealed record CreateUserSpec(
    string UserName,
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    IReadOnlyList<Guid> RoleIds);

public sealed record UpdateUserSpec(
    string? Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<Guid>? RoleIds);

public sealed record CreateRoleSpec(string Name, IReadOnlyList<string> Permissions);

public sealed record UpdateRoleSpec(string Name);
```

- [x] **Step 3: Add port tests**

Assert the specs are immutable, models expose the expected fields, and `IExternalCodeStore` has store/consume only.

- [x] **Step 4: Commit**

```bash
git add src/Application/Features/Identity tests/Unit/Application.Unit.Tests/Features/Identity
git commit -m "feat(app): add identity feature ports and models"
```

---

### Task 2: Auth Slice

**Files:**
- Create: `src/Application/Features/Identity/Auth/Commands/*.cs`
- Create: `src/Application/Features/Identity/Auth/Queries/*.cs`
- Create: `src/Application/Features/Identity/Auth/Models/Auth.Model.cs`
- Create: `src/Application/Features/Identity/Auth/Mappings/Auth.Mapper.cs`
- Create: `src/Application/Features/Identity/Auth/Validators/Auth.Validation.cs`
- Delete: `src/Application/Features/Identity/IdentityFeature.Service.cs`
- Delete: `src/Application/Features/Identity/Models/Identity.Model.cs`
- Delete: `src/Application/Features/Identity/Mappings/Identity.Mapper.cs`
- Modify: `app/Web.Api/Endpoints/Identity/Identity.Endpoint.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Identity/Auth/*.Test.cs`

**Interfaces:**
- Consumes: `IIdentityService`, `IUserContext`.
- Produces: `RegisterUser`, `Login`, `RefreshToken`, `Logout`, `ConfirmEmail`, `ResendConfirmation`, `ForgotPassword`, `ResetPassword`, `UpdateCurrentUser`, `UpdateTwoFactor`, `GetCurrentUser`, `GetTwoFactor`.

- [x] **Step 1: Write Auth tests**

Cover success, validation failure, service failure, missing user id (401), and 204 mapping for void commands.

- [x] **Step 2: Implement Auth commands/queries**

Follow the Todos static-class pattern:

```csharp
public static class Login
{
    #region Request
    public sealed record Request(string Identifier, string Password, string? TwoFactorCode = null);
    #endregion

    #region Response
    public sealed record Response : AuthTokenModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<Response>;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.Identifier).NotEmpty();
            RuleFor(command => command.Request.Password).NotEmpty();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            Result<AuthResponse> result = await identity
                .LoginAsync(
                    new LoginRequest(
                        command.Request.Identifier,
                        command.Request.Password,
                        command.Request.TwoFactorCode),
                    cancellationToken)
                .ConfigureAwait(false);

            return result.Map(AuthMapper.ToResponse);
        }
    }
    #endregion
}
```

Void commands return `Result` and convert success to `Result.NoContent()`.

`GetCurrentUser`/`GetTwoFactor` use a `RequireUserId` helper:

```csharp
internal static Result<Guid> RequireUserId(IUserContext context) { ... }
```

- [x] **Step 3: Thin the Auth endpoint**

Each handler becomes:

```csharp
private static async Task<IResult> LoginAsync(
    Login.Request request,
    IMediator mediator,
    CancellationToken cancellationToken)
    => (await mediator.Send(new Login.UseCase(request), cancellationToken)).ToApiResult();
```

Delete `IdentityFeature.Service.cs` and the old Web.Api-local models/mapper.

- [x] **Step 4: Run and commit**

```bash
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --filter-class "*IdentityAuth*"
dotnet test --project tests/Integration/Api.Integration.Tests/Api.Integration.Tests.csproj -c Release
git add src/Application/Features/Identity app/Web.Api/Endpoints/Identity tests
git commit -m "refactor(app): move identity auth to mediatr slices"
```

---

### Task 3: Users Slice

**Files:**
- Create: `src/Application/Features/Identity/Users/Commands/{CreateUser,UpdateUser,DeleteUser,ActivateUser,DeactivateUser,AssignRoleToUser,RemoveRoleFromUser,ResetUserPassword,ConfirmUserEmail,LockUser,UnlockUser,RevokeUserSessions}.cs`
- Create: `src/Application/Features/Identity/Users/Queries/{SearchUsers,GetUser,GetUserRoles}.cs`
- Create: `src/Application/Features/Identity/Users/Mappings/User.Mapper.cs`
- Create: `src/Application/Features/Identity/Users/Validators/User.Validation.cs`
- Create: `app/Web.Api/Endpoints/Identity/Identity.User.Endpoint.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Identity/Users/*.Test.cs`

**Interfaces:**
- Consumes: `IUserAccountService`, `IUserContext`, `ITenantContext`.
- Produces: the `/api/identity/users` routes in spec §7.

- [x] **Step 1: Tests first**

Cover every command/query: success, not found, validation, role assignment/removal, lock/unlock, revoke.

- [x] **Step 2: Implement commands/queries**

`CreateUser` example:

```csharp
public sealed record Request(
    string UserName,
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    IReadOnlyList<Guid> RoleIds);
```

Handler maps to `CreateUserSpec` and calls `IUserAccountService.CreateAsync`.

`SearchUsers` returns `QueryPage<Response>` and uses `QueryParameters`.

- [x] **Step 3: Add Carter module**

Route group `/api/identity/users`, `.HasPermission(PermissionContext.Identity.Users.Read/Manage)`.

- [x] **Step 4: Run and commit**

```bash
dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --filter-class "*IdentityUsers*"
git add src/Application/Features/Identity/Users app/Web.Api/Endpoints/Identity tests
git commit -m "feat(app): add the identity users slice"
```

---

### Task 4: Roles Slice

**Files:**
- Create: `src/Application/Features/Identity/Roles/Commands/{CreateRole,UpdateRole,DeleteRole,AssignPermissionToRole,RemovePermissionFromRole}.cs`
- Create: `src/Application/Features/Identity/Roles/Queries/{SearchRoles,GetRole,GetRolePermissions}.cs`
- Create: `src/Application/Features/Identity/Roles/Mappings/Role.Mapper.cs`
- Create: `src/Application/Features/Identity/Roles/Validators/Role.Validation.cs`
- Create: `app/Web.Api/Endpoints/Identity/Identity.Role.Endpoint.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Identity/Roles/*.Test.cs`

**Interfaces:**
- Consumes: `IRoleManagementService`.
- Produces: `/api/identity/roles` routes.

- [x] **Step 1: Tests first**
- [x] **Step 2: Implement commands/queries with `IRoleManagementService`.**
- [x] **Step 3: Add Carter module with `Identity.Roles.Read/Manage` permissions.**
- [x] **Step 4: Run and commit**

```bash
git commit -m "feat(app): add the identity roles slice"
```

---

### Task 5: Permissions Slice

**Files:**
- Create: `src/Application/Features/Identity/Permissions/Commands/RefreshPermissionCatalog.cs`
- Create: `src/Application/Features/Identity/Permissions/Queries/{SearchPermissions,GetPermission,ListPermissionDomains,ListPermissionCategories,GetUserPermissions,CheckUserPermissions}.cs`
- Create: `src/Application/Features/Identity/Permissions/Mappings/Permission.Mapper.cs`
- Create: `app/Web.Api/Endpoints/Identity/Identity.Permission.Endpoint.cs`
- Delete: `app/Web.Api/Endpoints/Authorization/Permission.Registry.Endpoint.cs`
- Delete/retire: `tests/Integration/Api.Integration.Tests/Authorization/Permission.Registry.Endpoint.Test.cs`
- Test: `tests/Unit/Application.Unit.Tests/Features/Identity/Permissions/*.Test.cs`

**Interfaces:**
- Consumes: `IPermissionCatalogService`, `IPermissionMetadataRegistry`, `IPermissionService`.
- Produces: `/api/identity/permissions` routes; `/api/authorization/*` removed.

- [x] **Step 1: Tests first**
- [x] **Step 2: Implement queries/command.**
- [x] **Step 3: Add Carter module with `Identity.Permissions.Read/Manage`.**
- [x] **Step 4: Remove the old authorization module and update tests.**
- [x] **Step 5: Run and commit**

```bash
git commit -m "feat(app): move permissions into the identity feature slice"
```

---

### Task 6: External Slice

**Files:**
- Create: `src/Application/Features/Identity/External/Commands/{ExternalChallenge,ExternalCallback,ExchangeExternalCode}.cs`
- Create: `src/Application/Features/Identity/External/Queries/GetExternalProviders.cs`
- Create: `src/Application/Features/Identity/External/Mappings/External.Mapper.cs`
- Create: `src/Application/Features/Identity/External/Validators/External.Validation.cs`
- Modify: `app/Web.Api/Endpoints/Identity/Identity.Endpoint.cs` (external handlers)
- Create: `app/Web.Api/Endpoints/Identity/Mappings/ExternalPrincipal.Mapper.cs`
- Move: `ExternalCode.Store.cs` → `src/Infrastructure/Integrations/Security/ExternalCode.Store.cs` implementing `IExternalCodeStore`
- Modify: `Identity.Composition.cs` (register the port)
- Test: `tests/Unit/Application.Unit.Tests/Features/Identity/External/*.Test.cs`
- Test: `tests/Unit/Infrastructure.Unit.Tests/Security/ExternalCodeStore.Test.cs`

**Interfaces:**
- Consumes: `IExternalAuthenticationService`, `IExternalCodeStore`, `IAuthenticationService` (endpoint only).
- Produces: provider/query, challenge/callback/exchange commands.

- [x] **Step 1: Tests first**
- [x] **Step 2: Implement queries/commands**

`ExternalChallengeCommand(provider)` returns `ExternalChallengeModel(Provider, RedirectUri)`.
`ExternalCallbackCommand(provider, providerKey, displayName, email)` calls external login, stores code, returns `ExternalCallbackModel(RedirectUrl)`.
`ExchangeExternalCodeCommand(code)` consumes the code.

- [x] **Step 3: Move the store behind the port**

The Infrastructure implementation uses `IDistributedCache` and `ApiJson.Options`. Register `IExternalCodeStore` scoped.

- [x] **Step 4: Thin the external Carter handlers**

Endpoint only authenticates the external scheme, maps claims via `ExternalPrincipalMapper`, dispatches commands, and applies `Results.Challenge`/`Results.Redirect`.

- [x] **Step 5: Run and commit**

```bash
git commit -m "feat(app): move external sign-in to mediatr"
```

---

### Task 7: API and Application Tests

**Files:**
- Modify: `tests/Integration/Api.Integration.Tests/Auth.Api.Test.cs`
- Create: `tests/Integration/Api.Integration.Tests/IdentityUsers.Api.Test.cs`
- Create: `tests/Integration/Api.Integration.Tests/IdentityRoles.Api.Test.cs`
- Create: `tests/Integration/Api.Integration.Tests/IdentityPermissions.Api.Test.cs`
- Modify: `tests/Integration/Api.Integration.Tests/ExternalAuth.Api.Test.cs`

- [x] **Step 1: Auth regression** — existing flows still pass through MediatR.
- [x] **Step 2: Users** — full admin CRUD, role assignment, lock/unlock, reset/confirm/revoke, 403 without permission.
- [x] **Step 3: Roles** — CRUD + permission assignment, 403 without permission.
- [x] **Step 4: Permissions** — search/get/domains/categories/checks/refresh, old `/api/authorization` returns 404.
- [x] **Step 5: External** — providers, challenge redirect, callback code, single-use exchange.
- [x] **Step 6: Run**

```bash
make test
make api.test
make infra.test
make coverage.unit
git commit -m "test(api): cover the identity feature slices"
```

---

### Task 8: Gates and Docs

- [x] Run `make lint`, `make build`, `make test`, `make coverage.unit`, `make api.test`, `make infra.test`, `make structure.check`, `make lib.format.check`, `make ci.full`.
- [x] Update `docs/specs/2026-10-30-identity-feature-slices-design.md` to Implemented.
- [x] Add `docs/releases/2026-10-30-identity-feature-slices.md`.
- [x] Update route docs: `/api/authorization/*` removed.
- [x] Commit `docs: record the identity feature slices`.

## Self-Review

| Spec section | Task |
|---|---|
| Ports/models | Task 1 |
| Auth MediatR | Task 2 |
| Users slice | Task 3 |
| Roles slice | Task 4 |
| Permissions slice | Task 5 |
| External slice | Task 6 |
| Tests | Task 7 |
| Gates/docs | Task 8 |

No placeholders; each task names every file, the port it consumes, and the routes it produces.

## Execution Handoff

**1. Subagent-Driven (recommended)** — one fresh implementer per task, task review after each, final whole-branch review.

**2. Inline Execution** — batch tasks in this session with checkpoints.

Which approach?
