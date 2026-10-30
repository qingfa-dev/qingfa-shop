# API Result Contract Release

## What changed

- Every `/api/*` endpoint now returns the existing monad directly: `Result<T>` on success and the
  non-generic `Result` on failure. There is no `ApiEnvelope`, `ApiError`, or other response DTO.
- The HTTP status comes from `Result.StatusCode`. A `201 Created` carries the value and a `Location`
  header, `202 Accepted` carries the value, and `204 No Content` has no body. Failures preserve their
  status; `0` falls back to `400` on failure and `200` on success.
- Failure bodies are converted with `Result<T>.ToResult()` before writing, so a failing `Value` is
  never evaluated. `isFailure` and `hasValue` are `[JsonIgnore]`; enums are camelCase strings.
- `ApiJson.Options` is the only serializer. It lives in
  `src/Infrastructure/Integrations/Serialization` next to `ApiResult`, `ApiResult<T>`, and
  `ToApiResult`. `AddApiJsonOptions()` mirrors every setting into the host's
  `ConfigureHttpJsonOptions` because `JsonSerializerOptions` is read-only after first use; the result
  writer serializes with `ApiJson.Options` itself, so a host override cannot change the contract.
- `app/Web.Api/Program.cs` is now a thin composition root. `AddApi()` / `UseApi()` in
  `app/Web.Api/Composition/Api.Composition.cs` register the JSON contract, the Carter modules, and the
  `/time` probe.
- Endpoint modules only bind, dispatch, and project with `ToApiResult`. Command handlers own statuses:
  creates return `Result<T>.Created`, deletes return `Result.NoContent`, normal success returns
  `Result<T>.Ok` / `Result.Ok`.

## Identity surface

- Identity is five MediatR slices under `Application/Features/Identity/{Auth,Users,Roles,Permissions,External}`.
  Carter modules bind requests and dispatch through `IMediator`; they no longer own identity logic.
- Shared authentication stays at `/api/auth/*`. Admin management is permission-gated at
  `/api/identity/users`, `/api/identity/roles`, and `/api/identity/permissions`.
- External sign-in (providers, challenge, callback, exchange) is a command/query slice. The callback
  stores the token response in Redis behind a random one-time code, redirects with only
  `?code=<code>`, and the client exchanges it at `POST /api/auth/external/exchange`. Only a SHA-256
  hash of the code is stored; the entry is consumed atomically, expires after
  `ExternalAuth:CodeLifetimeSeconds` (60 by default), and is never logged.

### Breaking route change

`/api/authorization/*` is removed. Unknown paths under it now return `404`, not a redirect.

| Old route | New route |
|---|---|
| `GET /api/authorization/permissions` | `GET /api/identity/permissions` |
| `GET /api/authorization/permissions/{identifier}` | `GET /api/identity/permissions/{identifier}` |
| `GET /api/authorization/permissions/domains` | `GET /api/identity/permissions/domains` |
| `GET /api/authorization/permissions/categories` | `GET /api/identity/permissions/categories` |
| `POST /api/authorization/permissions/refresh` | `POST /api/identity/permissions/refresh` |

The shared `/api/auth/*` routes and payloads are unchanged. Migration requires updating any client
that still calls the removed authorization routes.

## Backing services

PostgreSQL, Redis, and RabbitMQ now have the same local shape:

- AppHost declares `rabbitmq` with a `rabbitmq-data` volume and waits for it, matching the Postgres
  and Redis resources. The Api receives `Messaging__Transport=RabbitMq`.
- `compose.yaml` keeps one RabbitMQ service with the data volume, health check, management UI on
  `15672`, and the transport environment on the Api.
- `infra.backing.up`, `infra.backing.down`, and `infra.backing.health` start, stop, and probe all
  three services. `infra.up/down/ps/logs/smoke` still cover the whole stack.

There is deliberately no `compose.rabbitmq.yaml` and no `infra.rabbit.*` target. A broker-only path
would duplicate the lifecycle that the other backing services already share.

## Test prerequisites

- The Api integration suites boot the real `Program.cs` pipeline over a `postgres:17-alpine`
  Testcontainer, apply migrations, seed the identity stores, and Respawn across `public` and
  `messaging`. A container engine (Docker or Podman) is required; without one those tests report
  `Assert.Skip`, so a green run with skips does not mean the identity or persistence stack was
  exercised.
- Messaging is covered by the Infrastructure suite and the Compose smoke, not by a broker-specific
  Api suite. `make infra.test` and `make ci.full` need the full Compose stack (PostgreSQL, Redis,
  RabbitMQ, Api, Gateway, both SPAs), including a running RabbitMQ container.
- If RabbitMQ loses the container start race, rerun the gate once before treating it as a failure.

## Compatibility

The response body is a public contract break: every `/api/*` consumer must read `Result<T>` /
`Result` instead of the previous raw models and `ProblemDetails` errors. There are no frontend API
consumers in this repository yet, so no compatibility shim or versioned route was added. OpenAPI and
the integration suites move with the contract.

## Verification

`make lint`, `make build`, `make test`, `make coverage.unit`, `make api.test`, `make coverage.api`,
`make infra.test`, `make structure.check`, `make lib.format.check`, and `make ci.full` pass. The
`ci.full` run includes the Compose smoke (`/alive`, `/api/time`, `/`, and `/admin/`) and the backing
service health checks. See `docs/specs/2026-10-30-api-result-envelope-design.md` for the contract and
implementation note.
