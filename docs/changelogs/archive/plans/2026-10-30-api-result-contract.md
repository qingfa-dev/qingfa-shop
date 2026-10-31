# API Result Contract — Remaining Work

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox syntax.

**Goal:** Finish the API Result contract slice: RabbitMQ as a first-class stack resource, full container-backed API proof, release documentation, and final gates.

**Status:** complete — Task 1 in `9927361`, docs in `d79a7e9`; all listed gates pass.

**Baseline already shipped:**
- `ApiResult` / `ApiResult<T>` write `Result<T>` / `Result` directly with status from `Result.StatusCode`, 201 Location, 204 no body.
- `ApiJson.Options` and `AddApiJsonOptions()` live in `src/Infrastructure/Integrations/Serialization`.
- Every `/api/*` module uses `ToApiResult`; `Program.cs` is a thin composition root.
- Identity is fully MediatR-based (Auth/Users/Roles/Permissions/External) and tested.
- Result contract, Todo, Auth, Users, Roles, Permissions, and External API suites pass (161 tests).
- `make ci.full` passes; Compose smoke passes.
- Remaining: RabbitMQ AppHost/Compose/make ergonomics, one full broker-backed API proof, release note, spec status.

**Spec:** `docs/specs/2026-10-30-api-result-envelope-design.md`

## Global Constraints

- No envelope model; success body is `Result<T>`, failure body is non-generic `Result`.
- Status from `Result.StatusCode`; `201` Location; `204` no body.
- `ApiJson.Options` remains the only serializer.
- RabbitMQ targets are additive; `infra.up` still starts the whole stack.
- Docker-unavailable suites skip cleanly.
- `make ci` green after each task; `make ci.full` at the end.

---

### Task 1: Backing-Service Symmetry (PostgreSQL, Redis, RabbitMQ)

**Files:**
- Modify: `app/AppHost/AppHost.cs`
- Modify: `compose.yaml`
- Modify: `make/infra.mk`
- Create: `scripts/infra/backing-up.infra.sh`
- Create: `scripts/infra/backing-down.infra.sh`
- Create: `scripts/infra/backing-health.infra.sh`
- Modify: `scripts/infra/smoke.infra.sh`
- Modify: `make/README.md`, `scripts/README.md`

**Interfaces:**
- Produces: one symmetric lifecycle for all backing services: `infra.backing.up`, `infra.backing.down`, `infra.backing.health`; `infra.up/down/ps/logs` continue to cover the whole stack.
- No `compose.rabbitmq.yaml`, no `infra.rabbit.*`, no RabbitMQ-only path.

- [x] **Step 1: AppHost**

RabbitMQ follows the same shape as Postgres and Redis:

```csharp
var rabbitmq = builder
    .AddRabbitMQ("rabbitmq")
    .WithDataVolume("rabbitmq-data");

api.WithEnvironment("Messaging__Transport", "RabbitMq");
```

Do not add a management-plugin call unless the Postgres/Redis resources also need provider-specific extras; keep the resource declaration minimal.

- [x] **Step 2: Compose**

Keep one `compose.yaml`. Add only the RabbitMQ data volume and the messaging transport env:

```yaml
services:
  api:
    environment:
      Messaging__Transport: RabbitMq
  rabbitmq:
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq

volumes:
  rabbitmq-data:
```

Postgres and Redis keep their existing service definitions. No broker-only compose file.

- [x] **Step 3: Symmetric backing-service scripts**

`scripts/infra/backing-up.infra.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"
trace_exec docker compose up -d postgres redis rabbitmq
```

`scripts/infra/backing-down.infra.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"
trace_exec docker compose stop postgres redis rabbitmq
```

`scripts/infra/backing-health.infra.sh` checks all three the same way Docker health checks do:

```bash
#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
cd "$ROOT"
trace_exec docker compose exec -T postgres pg_isready -U qingfa -d db
trace_exec docker compose exec -T redis redis-cli ping
trace_exec docker compose exec -T rabbitmq rabbitmq-diagnostics -q ping
```

- [x] **Step 4: Make targets**

```make
infra.backing.up: ## Start postgres, redis, and rabbitmq only
	$(call run,$@,$(ROOT)/scripts/infra/backing-up.infra.sh)

infra.backing.down: ## Stop postgres, redis, and rabbitmq (keeps volumes)
	$(call run,$@,$(ROOT)/scripts/infra/backing-down.infra.sh)

infra.backing.health: ## Check postgres, redis, and rabbitmq health
	$(call run,$@,$(ROOT)/scripts/infra/backing-health.infra.sh)
```

Add the `.PHONY` entries. `infra.up/down/ps/logs` stay generic and cover the same three services plus the app images.

- [x] **Step 5: Smoke**

Extend `scripts/infra/smoke.infra.sh` with a backing-service block using the same commands:

```bash
(cd "$ROOT" && docker compose exec -T postgres pg_isready -U qingfa -d db)
(cd "$ROOT" && docker compose exec -T redis redis-cli ping)
(cd "$ROOT" && docker compose exec -T rabbitmq rabbitmq-diagnostics -q ping)
```

- [x] **Step 6: Run and commit**

```bash
make infra.config
make infra.backing.up
make infra.backing.health
make infra.smoke
make infra.backing.down
git add app/AppHost compose.yaml make scripts
git commit -m "feat(infra): treat rabbitmq like the other backing services"
```

---

### Task 2: Docs, Spec Status, and Final Gates

**Files:**
- Modify: `docs/specs/2026-10-30-api-result-envelope-design.md` (Status → Implemented)
- Create: `docs/releases/2026-10-30-api-result-contract.md`
- Modify: `README.md`, `make/README.md`, `scripts/README.md`
- Force-add the release note if `[Rr]eleases/` is git-ignored.

- [x] **Step 1: Release note**

Cover:

- direct `Result<T>` / `Result` body and status mapping
- `ApiJson.Options` serializer injection
- identity routes moved to MediatR feature slices
- `/api/authorization/*` removed; `/api/identity/permissions/*` replacement
- external one-time-code flow
- symmetric `infra.backing.*` lifecycle for PostgreSQL, Redis, and RabbitMQ
- container prerequisites for API tests

- [x] **Step 2: Gates**

```bash
make lint
make build
make test
make coverage.unit
make api.test
make coverage.api
make infra.test
make structure.check
make lib.format.check
make ci.full
```

- [x] **Step 3: Commit**

```bash
git add README.md make/README.md scripts/README.md docs
git commit -m "docs: record the api result contract release"
```

---

## Self-Review

| Spec section | Task |
|---|---|
| Direct Result body + status | Baseline |
| Shared serializer | Baseline |
| Identity surface | Baseline (identity slice) |
| Full API test host | Baseline (PostgreSQL + signed admin token); no broker-specific API suite |
| RabbitMQ as a backing service | Task 1 |
| Symmetric infra lifecycle | Task 1 |
| Docs/release/gates | Task 2 |

Known deviations to record in the release note:

1. RabbitMQ is a backing service like PostgreSQL and Redis; there is no `compose.rabbitmq.yaml` or `infra.rabbit.*` target.
2. API tests use a PostgreSQL-backed fixture; messaging is covered by the Infrastructure suite and Compose smoke, not a broker-specific API suite.
3. `ApiResult` and `ApiJson` live in `Infrastructure/Integrations/Serialization`, not `Web.Api`.
4. `Program.cs` delegates to `AddApi`/`UseApi` composition.
5. Identity endpoints are MediatR slices; see `docs/plans/2026-10-30-identity-feature-slices.md`.
