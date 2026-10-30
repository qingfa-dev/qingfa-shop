# API Result Contract, Identity Surface, and Full API Test Host

- **Status:** Proposed
- **Date:** 2026-10-30
- **Branch:** `feature/service-foundation`
- **Depends on:** event-sourcing slice ④ (Todo migration)

## 1. Context

The API currently returns raw response models on success and `ProblemDetails` on failure. Each endpoint decides its own success shape (`Results.Ok`, `Results.Created`, `Results.NoContent`) and its own failure shape. There is no shared response serializer, no authenticated HTTP test suite, and no identity endpoints even though `IIdentityService` and the external authentication schemes are already composed.

The Event Sourcing slice delivered a monadic `Result`/`Error` core. The next slice exposes that contract consistently over HTTP **without adding a new response model**.

## 2. Goals

1. Return the existing `Result<T>` / `Result` model directly for every `/api/*` response.
2. Let `Result.StatusCode` drive the HTTP status.
3. Inject one serializer configuration shared by the API and the API tests.
4. Expose the full local identity surface and the configured external OAuth/OIDC providers.
5. Test the real API pipeline through `WebApplicationFactory` with PostgreSQL, Redis, and RabbitMQ containers.
6. Make RabbitMQ a first-class local/deploy/AppHost resource with explicit make targets.

## 3. Non-goals

- No `ApiEnvelope`, `ApiError`, or other new response DTO.
- No changes to `/time`, health, OpenAPI, or static file responses.
- No refresh-token or access-token redesign; this slice only exposes the existing services.
- No frontend consumer changes.
- No distributed saga/process manager.

## 4. Decisions

| Decision | Choice |
|---|---|
| HTTP status | Derived from `Result.StatusCode` |
| Success body | `Result<T>` (contains `value`) |
| Failure body | non-generic `Result` (no `value`) |
| Error body | existing `Error` model |
| Serializer | shared `ApiJson.Options` injected into the host and tests |
| Enums | camelCase JSON, enums as strings |
| Value-less success | Honors `Result.StatusCode`; `204` has no body |
| `201 Created` | Endpoint supplies the `Location` |
| Identity surface | Full local surface + provider discovery + external challenge/callback |
| External callback | One-time code exchanged for tokens, never tokens in URLs |
| Test auth | Header-based test scheme for CRUD suites + one real login test |
| API test databases | One PostgreSQL container per API collection, Respawn between classes `public` + `messaging` |
| API test broker | Real RabbitMQ Testcontainer |
| RabbitMQ infra | Integrated stack + broker-only `compose.rabbitmq.yaml` + make targets |

## 5. Response contract

### Success

HTTP status from `Result.StatusCode`, body is the full `Result<T>`:

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "errors": [],
  "metadata": {},
  "value": { "id": "...", "title": "..." }
}
```

### Failure

HTTP status from `Result.StatusCode`, body is the non-generic `Result` so the failing `Value` is never touched:

```json
{
  "isSuccess": false,
  "statusCode": 404,
  "errors": [
    {
      "code": "Repository.Entity.NotFound",
      "message": "The 'Todo' with id '...' does not exist.",
      "type": null,
      "instance": null,
      "status": 404,
      "severity": "error",
      "metadata": {
        "Field": "id"
      }
    }
  ],
  "metadata": {}
}
```

### Rules

- `Result<T>` success is serialized as-is; `Value` is included.
- Failure is converted with `Result<T>.ToResult()` before writing, so `Value` is never evaluated.
- `errors` is ordered exactly as `Result.Errors`.
- `Error.Metadata["Field"]` remains the validation property path.
- `metadata` is the full `Result.Metadata` and `Error.Metadata`.
- `201 Created` carries the value and a `Location` header.
- `202 Accepted` carries the value and may carry a `Location` header.
- `204 No Content` has no body.
- Failures preserve `Result.StatusCode`; `0` is treated as `400`.
- Computed members (`isFailure`, `hasValue`) are `[JsonIgnore]` so the wire shape is stable.

## 6. Serialization

One shared options object, `ApiJson.Options`:

```csharp
new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = false,
}
```

Add `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`. The same options are used by:

- `builder.Services.ConfigureHttpJsonOptions(...)`
- `ApiResult` when writing the result body
- `tests/Testing` when reading `Result<T>` / `Result`

`ApiResult` writes with its own options reference so a host-level JSON override cannot silently change the result contract.

`Result.IsFailure` and `Result.HasValue` get `[JsonIgnore]`; `Result<T>.Value` is only reachable on success, and failure bodies use the non-generic `Result`.

## 7. HTTP result type

`ApiResult` is a custom typed result implementing `IResult` and `IEndpointMetadataProvider`:

- `ApiResult<T>` for typed results.
- `ApiResult` for value-less results.
- `ToApiResult<T>(...)` and `ToApiResult(...)` extension methods.

The extension supplies an optional `Func<T, string>` location selector for creates. The result writes `Result<T>` or `Result` with the encoded status. `204` writes no body. `IEndpointMetadataProvider` advertises the result content type and status codes so OpenAPI stays accurate.

All endpoint modules switch from `ToHttpResult` to `ToApiResult`. Command handlers own statuses:

- create commands: `Result<T>.Created(...)`
- delete/void commands: `Result.NoContent()`
- normal success: `Result<T>.Ok(...)` / `Result.Ok()`

## 8. Identity surface

New module `app/Web.Api/Endpoints/Identity/Identity.Endpoint.cs`.

| Method | Route | Auth | Service |
|---|---|---|---|
| POST | `/api/auth/register` | public | `IIdentityService.RegisterAsync` |
| POST | `/api/auth/login` | public | `IIdentityService.LoginAsync` |
| POST | `/api/auth/refresh` | public | `IIdentityService.RefreshAsync` |
| POST | `/api/auth/logout` | authenticated | `IIdentityService.LogoutAsync` |
| POST | `/api/auth/confirm-email` | public | `IIdentityService.ConfirmEmailAsync` |
| POST | `/api/auth/resend-confirmation` | public | `IIdentityService.ResendConfirmationEmailAsync` |
| POST | `/api/auth/forgot-password` | public | `IIdentityService.ForgotPasswordAsync` |
| POST | `/api/auth/reset-password` | public | `IIdentityService.ResetPasswordAsync` |
| GET | `/api/auth/me` | authenticated | `IIdentityService.GetInfoAsync` |
| PUT | `/api/auth/me` | authenticated | `IIdentityService.UpdateInfoAsync` |
| GET | `/api/auth/me/2fa` | authenticated | `IIdentityService.GetTwoFactorInfoAsync` |
| PUT | `/api/auth/me/2fa` | authenticated | `IIdentityService.UpdateTwoFactorAsync` |
| GET | `/api/auth/providers` | public | `IExternalAuthenticationService.GetProviders` |
| GET | `/api/auth/external/{provider}/challenge` | public | challenge scheme redirect |
| GET | `/api/auth/external/{provider}/callback` | public | callback + one-time code |
| POST | `/api/auth/external/exchange` | public | consume one-time code |

### Public DTOs

```csharp
public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    IdentityUserModel User);
```

`AuthResponse`, `RefreshTokenIssue`, and the refresh-token entity are never serialized. `AccessToken.Token` and `RefreshTokenIssue.Token` map to the public strings.

### External flow

1. Client calls `/api/auth/external/{provider}/challenge`.
2. Provider redirects to `/api/auth/external/{provider}/callback`.
3. Callback authenticates the external principal with the configured scheme, maps it to `ExternalUserInfo`, and calls `IExternalAuthenticationService.LoginAsync`. The provider schemes must set `CallbackPath` to `/api/auth/external/{provider}/callback` so the route receives the provider response; the existing OpenID Connect `CallbackPath` setting remains configurable.
4. The resulting `AuthTokenResponse` is stored in Redis for 60 seconds under a random one-time code.
5. Callback redirects to `ExternalAuth:SuccessRedirectUrl?code=<code>`.
6. Client posts the code to `/api/auth/external/exchange`; the code is read and deleted atomically, and the normal `Result<AuthTokenResponse>` is returned.

Codes are random 32-byte base64url values, stored hashed, single-use, and never logged.

## 9. API test host

### Fixtures

- `RabbitMqFixture` — Testcontainers RabbitMQ.
- `ApiStackFixture` — PostgreSQL + Redis + RabbitMQ, migrations, seeders, Respawn across `public` and `messaging`.
- `ShopApiFactory` gains:
  - `DatabaseConnectionString`
  - `RedisConnectionString`
  - `RabbitMqConnectionString`
  - `UseTestAuth`
  - `MessagingEnabled`
  - `OutboxEnabled`
- `RecordingIdentityEmailSender` — replaces `IIdentityEmailSender` in the test host and captures confirmation/reset links so email-token suites can complete the flow without a real inbox.
- `TestAuthHandler` — scheme `Test`:
  - `X-Test-User` (default fixed GUID)
  - `X-Test-Tenant` (default fixed GUID)
  - `X-Test-Role` (default `Admin`)
  - `X-Test-Permissions` (comma-separated)
  - emits `nameidentifier`, `tenant_id`, `role`, and permission claims.
- `ApiResponseExtension.ReadResultAsync<T>()` uses `ApiJson.Options`; success reads `Result<T>`, failure reads `Result`.

### Suites

- `Auth.Api.Test.cs`: real seeded login, refresh, logout, profile, 2FA, register, confirm email and forgot/reset using the recorded email links, provider discovery, external callback/exchange with a fake handler.
- `Todo.Api.Test.cs`: create → get → update → start → complete → delete; category/list operations; tenant isolation; 400/404/409/422.
- `TodoReminder.Api.Test.cs`: schedule → snooze → dismiss → list.
- `Authorization.Api.Test.cs`: admin bypass, permission denied 403, unauthenticated 401.
- `ResultContract.Api.Test.cs`: 200/201/204/400/404/409/422 bodies, serializer conventions, and `value` absence on failure.

Tests that need RabbitMQ use the real container with a unique queue prefix per run. Outbox dispatch is flushed deterministically through `IOutboxDispatcher.DispatchBatchAsync` before integration-event assertions. Suites skip cleanly when Docker is unavailable.

## 10. RabbitMQ infrastructure

### AppHost

- Keep `AddRabbitMQ("rabbitmq")`.
- Add a data volume.
- Explicit `Messaging__Transport=RabbitMq` on the API.
- Keep the API health check and RabbitMQ wait.
- Expose the management endpoint.

### Compose

- Keep `rabbitmq:4-management`.
- Add a named `rabbitmq-data` volume.
- Add explicit `Messaging__Transport=RabbitMq`.
- Keep the management UI on `15672`.
- Add `compose.rabbitmq.yaml` as a broker-only stack for focused local work.
- Include the broker health check in `infra.smoke`.

### Make targets

```text
infra.rabbit.up       start only RabbitMQ
infra.rabbit.down     stop RabbitMQ, keep the volume
infra.rabbit.logs     follow RabbitMQ logs
infra.rabbit.health   run rabbitmq-diagnostics ping and list_queues
infra.rabbit.ui       print the management URL and credentials
infra.rabbit.smoke    wait for broker, assert ping and management API
infra.rabbit.reset    delete the broker volume; requires RABBITMQ_RESET=1
```

`infra.up` remains the full stack. The new targets are additive.

## 11. Rollout

1. Finish the event-sourcing release docs/gates.
2. Implement the shared `ApiJson.Options` and `ApiResult`.
3. Switch every endpoint to `ToApiResult` and move statuses into commands.
4. Implement identity endpoints and external code exchange.
5. Add test fixtures and suites.
6. Add RabbitMQ infra and make targets.
7. Run `make ci` then `make ci.full`.

The result body is a public contract break. There are no frontend API consumers today, so no compatibility shim is added; OpenAPI and tests move with the change.

## 12. Acceptance criteria

- Every `/api/*` success returns `Result<T>` and every failure returns non-generic `Result`.
- HTTP status equals `Result.StatusCode`; `204` has no body; `201` has `Location`.
- `ApiJson.Options` is the single serializer used by API and tests; enums serialize as camelCase strings.
- `Result.IsFailure` / `Result.HasValue` do not appear on the wire.
- All identity routes in §8 are reachable and the raw domain `AuthResponse` never appears on the wire.
- External callback never puts tokens in a URL; the one-time code is single-use.
- Full API tests cover every identity route, every Todo route, every reminder route, and the result failure codes.
- Real PostgreSQL, Redis, and RabbitMQ containers back the API test host.
- `infra.rabbit.*` targets work; `make ci.full` passes.
- Docker-unavailable runs skip messaging/API container suites cleanly.

## 13. Risks

- Full identity + external OAuth is large; the external test uses a fake provider and a controllable `IExternalAuthenticationService` seam.
- Real RabbitMQ makes API tests slower and container-dependent; collection-level reuse and per-run queue prefixes limit cost.
- Disabling JSON for `IsFailure` / `HasValue` is a small core-visible change; no code reads them through serialization.
