# Identity Feature Slices Release

## What changed

- Identity is now five MediatR slices under `Application/Features/Identity`: Auth, Users,
  Roles, Permissions, and External. Every use case is a `ICommand`/`IQuery` handled through
  `IMediator`; `IdentityFeature.Service.cs` is deleted.
- Carter modules only bind requests, dispatch the mediator, and project `Result<T>`/`Result`
  through `ToApiResult`. No endpoint owns identity business logic.
- New Application ports isolate ASP.NET Identity: `IUserAccountService`,
  `IRoleManagementService`, and `IPermissionCatalogService`. Their implementations stay in
  `Infrastructure/Security`; `IIdentityService` now lives under `Auth/Abstractions`.
- Shared authentication remains `/api/auth/*`. Admin management is new and permission-gated:
  `/api/identity/users`, `/api/identity/roles`, and `/api/identity/permissions`.
- External sign-in (providers, challenge, callback, exchange) is a command/query slice.
  `ExternalChallenge`/`ExternalCallback` only return the provider and redirect; Carter applies
  `Results.Challenge` and `Results.Redirect`.
- The one-time-code store moved behind `IExternalCodeStore` and into
  `Infrastructure/Integrations/Security/ExternalCode.Store.cs`. It keeps only a SHA-256 hash
  of the code in Redis, consumes the entry before deserializing, and expires at
  `ExternalAuthOption.CodeLifetimeSeconds`.

## Breaking route change

`/api/authorization/*` is removed. Its permission catalog routes are replaced by
`/api/identity/permissions/*`, which require the `Identity.Permissions.Read` or
`Identity.Permissions.Manage` permission. Unknown `/api/authorization/*` paths now return
`404`.

| Old route | New route |
|---|---|
| `GET /api/authorization/permissions` | `GET /api/identity/permissions` |
| `GET /api/authorization/permissions/{identifier}` | `GET /api/identity/permissions/{identifier}` |
| `GET /api/authorization/permissions/domains` | `GET /api/identity/permissions/domains` |
| `GET /api/authorization/permissions/categories` | `GET /api/identity/permissions/categories` |
| `POST /api/authorization/permissions/refresh` | `POST /api/identity/permissions/refresh` |

Migrate clients before deploying: any caller that still hits `/api/authorization/*` will get a
`404`, not a redirect. The shared `/api/auth/*` routes and their payloads are unchanged.

## Two-factor lockout

An invalid authenticator code or recovery code during login now calls
`AccessFailedAsync` on the Identity user. When that pushes the account past the configured
lockout threshold, login returns `401` with `Identity.User.LockedOut` instead of
`Identity.TwoFactor.Invalid`. A successful verification resets the failed-access counter.
Missing codes still return `Identity.TwoFactor.Required`.

## Test host requirements

The Api integration suites (`Auth`, `IdentityUsers`, `IdentityRoles`, `IdentityPermissions`,
`ExternalAuth`, and `IdentityStack`) need a container engine. They start a
`postgres:17-alpine` Testcontainer, migrate it, and seed the identity stores. Without Docker
or Podman the tests report `Assert.Skip`; a green run with skips does not mean the identity
stack was exercised. `make infra.test` and `make ci.full` additionally need Redis and the
Compose stack (PostgreSQL, Redis, RabbitMQ, Api, Gateway, both SPAs).

## Verification

`make lint`, `make build`, `make test`, `make coverage.unit`, `make api.test`,
`make infra.test`, `make structure.check`, `make lib.format.check`, and `make ci.full` pass.
The `ci.full` run includes the Compose smoke (`/alive`, `/api/time`, `/`, and `/admin/`); if
RabbitMQ loses the container start race, rerun the gate before treating it as a failure.
