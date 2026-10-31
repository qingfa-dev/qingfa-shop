# Identity Feature Slices with MediatR

- **Status:** Implemented
- **Date:** 2026-10-30
- **Branch:** `feature/service-foundation`
- **Depends on:** API Result contract slice

## 1. Context

Identity currently lives in three places:

- `Application/Features/Identity/IdentityFeature.Service.cs` orchestrates local identity directly.
- `Application/Common/Security/Authorization` owns permissions and authorization primitives.
- `Web.Api/Endpoints/Identity` owns routing, external OAuth, and the Redis one-time-code store.

That works, but it does not follow the Todos feature pattern and does not expose user/role administration. Identity is shared by Storefront and Admin, so the shared authentication surface and the admin-gated management surface must be separated.

## 2. Goals

1. Organize every Identity use case as a MediatR command or query under `Application/Features/Identity`.
2. Keep `/api/auth/*` shared by Storefront and Admin.
3. Add admin-gated `/api/identity/users`, `/api/identity/roles`, and `/api/identity/permissions`.
4. Move the permission feature slice under `Application/Features/Identity/Permissions`; shared authorization primitives stay in `Common/Security/Authorization`.
5. Move external challenge/callback/exchange into MediatR while keeping framework redirects in Carter.
6. Keep the existing `Result<T>` / `Result` HTTP contract.

## 3. Non-goals

- No new authentication provider implementation; the configured Google/Microsoft/OIDC schemes stay.
- No move of `HasPermission`, `PermissionContext`, policies, requirements, caches or stores out of `Common/Security/Authorization`.
- No frontend changes.
- No new database schema for users/roles; ASP.NET Identity tables stay.

## 4. Decisions

| Decision | Choice |
|---|---|
| Shared auth routes | `/api/auth/*` |
| Admin management routes | `/api/identity/users`, `/roles`, `/permissions` |
| MediatR | Every Identity use case |
| Admin ports | `IUserAccountService`, `IRoleManagementService`, `IPermissionCatalogService` |
| Auth port | Existing `IIdentityService` |
| External | Commands/queries for providers, challenge, callback, exchange |
| External code storage | `IExternalCodeStore` Application port; Redis implementation in Infrastructure |
| Permission slice | `Features/Identity/Permissions`; primitives stay in `Common/Security/Authorization` |
| Existing `/api/authorization/*` | Removed; replaced by `/api/identity/permissions/*` |

## 5. Folder layout

```text
src/Application/Features/Identity/
  Abstractions/
    IUserAccountService.cs
    IRoleManagementService.cs
    IPermissionCatalogService.cs

  Auth/
    Commands/
      RegisterUser.cs
      Login.cs
      RefreshToken.cs
      Logout.cs
      ConfirmEmail.cs
      ResendConfirmation.cs
      ForgotPassword.cs
      ResetPassword.cs
      UpdateCurrentUser.cs
      UpdateTwoFactor.cs
    Queries/
      GetCurrentUser.cs
      GetTwoFactor.cs
    Models/
      Auth.Model.cs
    Mappings/
      Auth.Mapper.cs
    Validators/
      Auth.Validation.cs

  Users/
    Commands/
      CreateUser.cs
      UpdateUser.cs
      DeleteUser.cs
      ActivateUser.cs
      DeactivateUser.cs
      AssignRoleToUser.cs
      RemoveRoleFromUser.cs
      ResetUserPassword.cs
      ConfirmUserEmail.cs
      LockUser.cs
      UnlockUser.cs
      RevokeUserSessions.cs
    Queries/
      SearchUsers.cs
      GetUser.cs
      GetUserRoles.cs
    Models/
      User.Model.cs
    Mappings/
      User.Mapper.cs
    Validators/
      User.Validation.cs

  Roles/
    Commands/
      CreateRole.cs
      UpdateRole.cs
      DeleteRole.cs
      AssignPermissionToRole.cs
      RemovePermissionFromRole.cs
    Queries/
      SearchRoles.cs
      GetRole.cs
      GetRolePermissions.cs
    Models/
      Role.Model.cs
    Mappings/
      Role.Mapper.cs
    Validators/
      Role.Validation.cs

  Permissions/
    Commands/
      RefreshPermissionCatalog.cs
    Queries/
      SearchPermissions.cs
      GetPermission.cs
      ListPermissionDomains.cs
      ListPermissionCategories.cs
      GetUserPermissions.cs
      CheckUserPermissions.cs
    Models/
      Permission.Model.cs
    Mappings/
      Permission.Mapper.cs

  External/
    Abstractions/
      IExternalCodeStore.cs
    Commands/
      ExternalChallenge.cs
      ExternalCallback.cs
      ExchangeExternalCode.cs
    Queries/
      GetExternalProviders.cs
    Models/
      External.Model.cs
    Mappings/
      External.Mapper.cs
    Validators/
      External.Validation.cs
```

```text
app/Web.Api/Endpoints/Identity/
  Identity.Auth.Endpoint.cs
  Identity.User.Endpoint.cs
  Identity.Role.Endpoint.cs
  Identity.Permission.Endpoint.cs
  Identity.External.Endpoint.cs
  Identity.Composition.cs
  Mappings/
    ExternalPrincipal.Mapper.cs
```

`IdentityFeature.Service.cs` is deleted after its logic moves into the Auth commands/queries.

## 6. Ports

### `IUserAccountService`

```csharp
Task<Result<QueryPage<UserModel>>> SearchAsync(
    QueryParameters parameters, CancellationToken ct = default);

Task<Result<UserModel>> GetAsync(Guid id, CancellationToken ct = default);

Task<Result<UserModel>> CreateAsync(CreateUserSpec spec, CancellationToken ct = default);

Task<Result<UserModel>> UpdateAsync(Guid id, UpdateUserSpec spec, CancellationToken ct = default);

Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

Task<Result<UserModel>> SetActiveAsync(Guid id, bool active, CancellationToken ct = default);

Task<Result<UserModel>> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);

Task<Result<UserModel>> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);

Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

Task<Result> ConfirmEmailAsync(Guid userId, CancellationToken ct = default);

Task<Result<UserModel>> SetLockedAsync(Guid id, bool locked, CancellationToken ct = default);

Task<Result> RevokeSessionsAsync(Guid userId, CancellationToken ct = default);
```

### `IRoleManagementService`

```csharp
Task<Result<QueryPage<RoleModel>>> SearchAsync(
    QueryParameters parameters, CancellationToken ct = default);

Task<Result<RoleModel>> GetAsync(Guid id, CancellationToken ct = default);

Task<Result<RoleModel>> CreateAsync(CreateRoleSpec spec, CancellationToken ct = default);

Task<Result<RoleModel>> UpdateAsync(Guid id, UpdateRoleSpec spec, CancellationToken ct = default);

Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

Task<Result<RoleModel>> AssignPermissionAsync(Guid roleId, string permission, CancellationToken ct = default);

Task<Result<RoleModel>> RemovePermissionAsync(Guid roleId, string permission, CancellationToken ct = default);
```

### `IPermissionCatalogService`

```csharp
Task<Result<PermissionSnapshotModel>> SearchAsync(
    QueryParameters parameters, CancellationToken ct = default);

Task<Result<PermissionItemModel>> GetAsync(string identifier, CancellationToken ct = default);

Task<Result<IReadOnlyList<string>>> ListDomainsAsync(CancellationToken ct = default);

Task<Result<IReadOnlyList<string>>> ListCategoriesAsync(CancellationToken ct = default);

Task<Result<IReadOnlyList<string>>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);

Task<Result<IReadOnlyList<string>>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);

Task<Result<bool>> CheckUserPermissionsAsync(
    Guid userId, IReadOnlyList<string> permissions, PermissionMatchMode mode,
    CancellationToken ct = default);

Task<Result<PermissionSnapshotModel>> RefreshAsync(CancellationToken ct = default);
```

### Spec models

```csharp
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

### `IExternalCodeStore`

```csharp
Task<Result<string>> StoreAsync(AuthTokenResponse response, CancellationToken ct = default);

Task<Result<AuthTokenResponse>> ConsumeAsync(string code, CancellationToken ct = default);
```

The Redis implementation moves to `Infrastructure/Integrations/Security` and is registered there.

## 7. Routes

### Shared auth

Keep the existing routes:

| Method | Route |
|---|---|
| POST | `/api/auth/register` |
| POST | `/api/auth/login` |
| POST | `/api/auth/refresh` |
| POST | `/api/auth/logout` |
| POST | `/api/auth/confirm-email` |
| POST | `/api/auth/resend-confirmation` |
| POST | `/api/auth/forgot-password` |
| POST | `/api/auth/reset-password` |
| GET | `/api/auth/me` |
| PUT | `/api/auth/me` |
| GET | `/api/auth/me/2fa` |
| PUT | `/api/auth/me/2fa` |

### Users (admin-gated)

| Method | Route | Permission |
|---|---|---|
| GET | `/api/identity/users` | `Identity.Users.Read` |
| GET | `/api/identity/users/{id:guid}` | `Identity.Users.Read` |
| POST | `/api/identity/users` | `Identity.Users.Manage` |
| PUT | `/api/identity/users/{id:guid}` | `Identity.Users.Manage` |
| DELETE | `/api/identity/users/{id:guid}` | `Identity.Users.Manage` |
| POST | `/api/identity/users/{id:guid}/activate` | `Identity.Users.Manage` |
| POST | `/api/identity/users/{id:guid}/deactivate` | `Identity.Users.Manage` |
| POST | `/api/identity/users/{id:guid}/lock` | `Identity.Users.Manage` |
| POST | `/api/identity/users/{id:guid}/unlock` | `Identity.Users.Manage` |
| POST | `/api/identity/users/{id:guid}/reset-password` | `Identity.Users.Manage` |
| POST | `/api/identity/users/{id:guid}/confirm-email` | `Identity.Users.Manage` |
| POST | `/api/identity/users/{id:guid}/revoke-sessions` | `Identity.Users.Manage` |
| GET | `/api/identity/users/{id:guid}/roles` | `Identity.Users.Read` |
| POST | `/api/identity/users/{id:guid}/roles/{roleId:guid}` | `Identity.Users.Manage` |
| DELETE | `/api/identity/users/{id:guid}/roles/{roleId:guid}` | `Identity.Users.Manage` |

### Roles (admin-gated)

| Method | Route | Permission |
|---|---|---|
| GET | `/api/identity/roles` | `Identity.Roles.Read` |
| GET | `/api/identity/roles/{id:guid}` | `Identity.Roles.Read` |
| POST | `/api/identity/roles` | `Identity.Roles.Manage` |
| PUT | `/api/identity/roles/{id:guid}` | `Identity.Roles.Manage` |
| DELETE | `/api/identity/roles/{id:guid}` | `Identity.Roles.Manage` |
| GET | `/api/identity/roles/{id:guid}/permissions` | `Identity.Roles.Read` |
| POST | `/api/identity/roles/{id:guid}/permissions` | `Identity.Roles.Manage` |
| DELETE | `/api/identity/roles/{id:guid}/permissions/{permission}` | `Identity.Roles.Manage` |

### Permissions (admin-gated)

| Method | Route | Permission |
|---|---|---|
| GET | `/api/identity/permissions` | `Identity.Permissions.Read` |
| GET | `/api/identity/permissions/{identifier}` | `Identity.Permissions.Read` |
| GET | `/api/identity/permissions/domains` | `Identity.Permissions.Read` |
| GET | `/api/identity/permissions/categories` | `Identity.Permissions.Read` |
| GET | `/api/identity/permissions/users/{userId:guid}` | `Identity.Permissions.Read` |
| GET | `/api/identity/permissions/users/{userId:guid}/check` | `Identity.Permissions.Read` |
| GET | `/api/identity/permissions/roles/{roleId:guid}` | `Identity.Permissions.Read` |
| POST | `/api/identity/permissions/refresh` | `Identity.Permissions.Manage` |

`/api/authorization/*` is removed.

### External

| Method | Route |
|---|---|
| GET | `/api/auth/providers` |
| GET | `/api/auth/external/{provider}/challenge` |
| GET | `/api/auth/external/{provider}/callback` |
| POST | `/api/auth/external/exchange` |

## 8. MediatR shape

Follow the Todos pattern exactly:

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
    public sealed class UseCaseValidator : AbstractValidator<UseCase> { ... }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(IIdentityService identity)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(...)
            => await identity.LoginAsync(...).MapAsync(AuthMapper.ToResponse);
    }
    #endregion
}
```

Admin commands use the new ports. Queries use the same ports or `IPermissionCatalogService`.

## 9. External flow with MediatR

`ExternalChallengeCommand(provider)` returns:

```csharp
public sealed record ExternalChallengeResponse(string Provider, string RedirectUri);
```

The Carter handler applies `Results.Challenge` with the returned provider and redirect URI.

`ExternalCallbackCommand(provider, providerKey, displayName, email)` returns:

```csharp
public sealed record ExternalCallbackResponse(string RedirectUrl);
```

The Carter handler authenticates the scheme, maps the principal to `ExternalPrincipal` in `Web.Api/Endpoints/Identity/Mappings`, dispatches the command, and applies `Results.Redirect`.

`ExchangeExternalCodeCommand(code)` returns `Result<AuthTokenResponse>`.

`GetExternalProvidersQuery` returns `Result<IReadOnlyList<ExternalProviderModel>>`.

## 10. Models and mapping

- Auth models live in `Features/Identity/Auth/Models` and are the single contract
  for both MediatR use cases and `IIdentityService`. `AuthTokenResponse` is the
  public token pair.
- `AuthResponse` is an Infrastructure-internal model; `IdentityService` maps it
  to `AuthTokenResponse` before returning.
- User/Role/Permission models are new public application models.
- External models live in `Features/Identity/External/Models`; the external
  authentication port lives in `Features/Identity/External/Abstractions`.
- Options moved with their slices: `IdentityApiOption` lives in
  `Features/Identity/Auth/Options`, and `ExternalAuthOption` lives in
  `Features/Identity/External/Options`.
- Carter endpoints only bind and dispatch; they do not contain identity logic.

### Auth status contract

Auth endpoints do not add endpoint-level 422 coercion. Failures keep the
`Result` status code produced by `IdentityValidator` or `IIdentityService`:

| Case | Status | Code |
|---|---|---|
| Empty login identifier/password | 401 | `Identity.Credentials.Invalid` |
| Weak registration password | 400 | `Identity.Password.Invalid` |
| Registration username length invalid | 400 | `Identity.Operation.Failed` |
| Registration malformed email | 401 | `Identity.Credentials.Invalid` |
| Blank refresh token | 400 | `Identity.RefreshToken.Required` |
| Confirmation invalid user id or token | 400 | `Identity.Token.Invalid` |
| Resend/forgot malformed email | 401 | `Identity.Credentials.Invalid` |
| Reset malformed email | 401 | `Identity.Credentials.Invalid` |
| Reset invalid token or weak password | 400 | `Identity.Token.Invalid` / `Identity.Password.Invalid` |
| Profile update malformed email | 401 | `Identity.Credentials.Invalid` |
| Profile new password without old password | 400 | `Identity.Password.CurrentRequired` |
| Enable two-factor without a code | 401 | `Identity.TwoFactor.Required` |

Specific service outcomes (user not found, locked out, email not confirmed,
inactive, and similar `IdentityOutcome` failures) keep their own status codes.
Integration stubs must reuse `IdentityValidator` so tests cannot drift from the
real service.

## 11. Tests

### Application unit

- Every Auth command/query: success, validation failure, service failure.
- Users: create/update/delete/activate/deactivate/lock/unlock/role assignment/password reset/confirm/revoke.
- Roles: CRUD and permission assignment.
- Permissions: search/get/domains/categories/user permissions/check/refresh.
- External: providers, challenge, callback, exchange, invalid/expired code.
- Port implementations are mocked at the Application boundary.

### Infrastructure unit/integration

- Identity port implementations against the existing Identity stores.
- Redis `IExternalCodeStore` single-use and expiry.
- User/role query map behavior if the ports use the existing repository.

### API

- Existing auth flows keep working.
- Auth invalid-input assertions follow the status contract above.
- Admin user/role/permission routes enforce permissions.
- Storefront-style caller without admin permissions gets 403 on management routes.
- External callback still redirects with a one-time code and exchange is single-use.

## 12. Migration order

1. Add ports and models.
2. Move Auth orchestration into commands/queries; delete `IdentityFeature.Service.cs`.
3. Add Users commands/queries and endpoints.
4. Add Roles commands/queries and endpoints.
5. Move Permissions slice and remove `/api/authorization/*`.
6. Add External commands/queries and move the code store behind `IExternalCodeStore`.
7. Update tests and docs.

## 13. Acceptance criteria

- All Identity use cases are MediatR commands/queries.
- `/api/auth/*` remains shared; `/api/identity/users|roles|permissions` are admin-gated.
- Permissions feature lives under `Features/Identity/Permissions`; shared authorization primitives stay in `Common`.
- External challenge/callback/exchange are commands; Carter only applies redirects/challenges.
- No endpoint contains identity business logic.
- `make ci` and `make ci.full` pass.

## 14. Risks

- Moving all identity logic is a large mechanical change; the Todos pattern keeps each slice small and testable.
- Removing `/api/authorization/*` is a breaking route change; no known consumers exist.
- Full user/role admin CRUD needs careful lockout/session-revocation semantics; those operations are isolated per command.

## 15. Implementation note

The slices shipped as described. The final layout differs from §5 only where
naming was tightened during implementation:

```text
src/Application/Features/Identity/
  Abstractions/              IUserAccountService, IRoleManagementService,
                             IPermissionCatalogService
  Auth/
    Abstractions/            IIdentityService, IIdentityEmailSender
    Commands/                RegisterUser, Login, RefreshToken, Logout, ConfirmEmail,
                             ResendConfirmation, ForgotPassword, ResetPassword,
                             UpdateCurrentUser, UpdateTwoFactor
    Queries/                 GetCurrentUser, GetTwoFactor
    Models/                  Auth.Model.cs
    Validators/              Identity.Validator.cs
    Auth.Guard.cs            shared user-id guard
    Identity.Constant.cs     endpoint and claim constants
    Options/                 Identity.Option.cs
    Outcome/                 Identity.Outcome.cs
  Users/
    Commands/                CreateUser, UpdateUser, DeleteUser, ActivateUser,
                             DeactivateUser, AssignRoleToUser, RemoveRoleFromUser,
                             ResetUserPassword, ConfirmUserEmail, LockUser, UnlockUser,
                             RevokeUserSessions
    Queries/                 SearchUsers, GetUser, GetUserRoles
    Models/                  User.Model.cs, UserRole.Model.cs, UserPermission.Model.cs
    Mappings/                User.Mapper.cs
    Outcome/                 UserAccount.Outcome.cs
  Roles/
    Commands/                CreateRole, UpdateRole, DeleteRole,
                             AssignPermissionToRole, RemovePermissionFromRole
    Queries/                 SearchRoles, GetRole, GetRolePermissions
    Models/                  Role.Model.cs
  Permissions/
    Commands/                RefreshPermissionCatalog
    Queries/                 SearchPermissions, GetPermission, ListPermissionDomains,
                             ListPermissionCategories, GetUserPermissions,
                             CheckUserPermissions, GetRolePermissions
    Models/                  Permission.Model.cs
    Mappings/                Permission.Mapper.cs
    Outcome/                 PermissionCatalog.Outcome.cs
  External/
    Abstractions/            IExternalAuthenticationService, IExternalCodeStore
    Commands/                ExternalChallenge, ExternalCallback, ExchangeExternalCode
    Queries/                 GetExternalProviders
    Models/                  External.Model.cs
    Mappings/                External.Mapper.cs
    Options/                 External.Option.cs
```

`IdentityFeature.Service.cs` and the old `Models`/`Mappings` folders are gone.
Request models use the `*RequestModel` naming from §6's spec models
(`CreateUserRequestModel`, `UpdateUserRequestModel`, `CreateRoleRequestModel`,
`UpdateRoleRequestModel`, `ResetUserPasswordModel`). `IIdentityService` moved under
`Auth/Abstractions` so the port and its slice stay together; the implementation
remains `Infrastructure/Security/Identity/Services/IdentityService.cs`.

Carter is still a thin adapter. `Identity.Endpoint.cs` owns `/api/auth/*` and the
external routes; `Identity.User.Endpoint.cs`, `Identity.Role.Endpoint.cs`, and
`Identity.Permission.Endpoint.cs` own the admin groups. `/api/authorization/*`
no longer exists: its permission routes are now `/api/identity/permissions/*`
and unknown `/api/authorization/*` paths return `404`.

The one-time-code store is `Infrastructure/Integrations/Security/ExternalCode.Store.cs`,
registered as scoped `IExternalCodeStore` by `IdentityComposition`. It stores only a
SHA-256 hash of a 32-byte random code under the Redis `IDistributedCache`, consumes the
entry before deserializing, and expires at `ExternalAuthOption.CodeLifetimeSeconds`.

Two-factor failures now count toward ASP.NET Identity lockout: an invalid TOTP or
recovery code calls `AccessFailedAsync`, returns `Identity.User.LockedOut` once the
account is locked, and a successful verification resets the failed-access counter.

The Api integration suites boot a `postgres:17-alpine` Testcontainer; without Docker
or Podman they report `Assert.Skip` and do not exercise the identity stack. See
`docs/releases/2026-10-30-identity-feature-slices.md` for the release summary.
