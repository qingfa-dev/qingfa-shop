# QingFa.Shop

A modern e-commerce platform built with .NET Aspire, Vue 3 + TypeScript, PostgreSQL, Redis, and RabbitMQ.

## Overview

QingFa.Shop is a cloud-native e-commerce solution leveraging .NET Aspire for service orchestration, providing observability, resilience, and local development parity with production.

## Tech Stack

- **Frontend**: Vue 3 + TypeScript + Vite + StyleX + Vue Router
- **Backend**: .NET 10 + ASP.NET Core + Entity Framework Core
- **Orchestration**: .NET Aspire (Service Discovery, Telemetry, Health Checks)
- **Database**: PostgreSQL 17 + EF Core Migrations
- **Cache/Session**: Redis 7 (StackExchange.Redis)
- **Messaging**: Wolverine + RabbitMQ (Aspire resource and Compose service)
- **Auth**: JWT + Refresh Tokens (ASP.NET Core Identity)
- **Payments**: Stripe .NET SDK
- **Observability**: OpenTelemetry + Prometheus + Grafana (via Aspire Dashboard)
- **Containerization**: Docker + Docker Compose

## Project Structure

```
QingFa.Shop/
├── app/
│   ├── package.json           # pnpm workspace root (shared Prettier)
│   ├── pnpm-workspace.yaml    # Web.Storefront + Web.Admin members
│   ├── AppHost/               # Aspire orchestration (api, gateway, Vite apps)
│   ├── Gateway/               # YARP edge: routing, JWT validation, rate limiting
│   ├── Infrastructure/        # Postgres, Redis, and RabbitMQ image definitions
│   ├── Web.Api/               # Backend API (ASP.NET Core)
│   ├── Web.Admin/             # Admin SPA (Vite, served under /admin/)
│   └── Web.Storefront/        # Storefront SPA (Vite)
├── src/
│   ├── Core/                  # Domain models, interfaces
│   ├── Application/           # Use cases and handlers
│   ├── Infrastructure/        # EF Core, Redis, external services
│   └── ServiceDefaults/       # Shared Aspire service config
├── tests/                     # Unit, integration, contract, architecture suites
├── make/                      # Make fragments (public command surface)
├── scripts/                   # Shell implementations behind every target
├── compose.yaml               # Local Compose stack
├── compose.deploy.yaml        # Pull-only deployment overlay
└── Makefile                   # Facade: `make help`
```

## Prerequisites

- **.NET SDK 10.0.401** (`global.json` pins the exact version)
- **Node.js 24+** and **pnpm 12.8.1** (frontend package managers)
- **Docker** with Compose v2 (backing services and container smoke tests)

## Getting Started

### 1. Clone

```bash
git clone <repo-url>
cd QingFa.Shop
make doctor          # confirm dotnet, node, pnpm, and Docker versions
```

### 2. Start the full stack (Aspire)

```bash
dotnet run --project app/AppHost
```

This starts:
- **Aspire Dashboard** → the URL printed in the console (observability, service graph, logs)
- **PostgreSQL**, **Redis**, and **RabbitMQ** containers
- **API** → `api` resource
- **Gateway** → `gateway` resource (the browser entry point)
- **Storefront** and **Admin** → Vite dev servers behind the gateway

### 3. Database Migrations

```bash
dotnet ef database update --project src/Infrastructure --startup-project app/Web.Api
```

### 4. Frontend only

```bash
make store.install                       # installs the app/ pnpm workspace once
cd app
pnpm --filter @qingfa/storefront dev     # or @qingfa/admin
```

## Available Scripts

The Makefile is the command surface; `make help` prints every target. Each run
prints `==> <target>` and then the exact CLI it executes (`$ dotnet ...`,
`$ pnpm ...`) before the command starts.

```bash
# Workspace
make install         # restore .NET + install the web workspace
make build           # .NET solution + both SPA bundles
make lint            # formatting, structure, and vue-tsc checks
make format          # write C# and Prettier fixes
make test            # .NET unit, Api, then web tests
make test.all        # every suite, including AppHost and infra
make test.infra.broker  # RabbitMQ composition + transport (Testcontainers)
make coverage        # .NET unit + web coverage gates (line 90 / branch 80)
make coverage.all    # every suite plus the merged .NET coverage gate

# Libraries
make lib.install
make lib.format
make lib.lint
make lib.build
make lib.test

# Api
make api.install
make api.format
make api.lint
make api.build
make api.test
make api.publish

# Gateway
make gate.install
make gate.format
make gate.lint
make gate.build
make gate.test
make gate.publish

# AppHost
make host.install
make host.format
make host.lint
make host.build
make host.test

# Storefront
make store.install
make store.format
make store.lint
make store.test           # unit then integration
make store.test.unit
make store.test.integration
make store.e2e            # optional Playwright
make store.build
make store.publish

# Admin
make admin.install
make admin.format
make admin.lint
make admin.test           # unit then integration
make admin.test.unit
make admin.test.integration
make admin.e2e            # optional Playwright
make admin.build
make admin.publish

# Infra
make infra.config
make infra.build     # gateway, api, shopfront, admin, postgres, redis, rabbitmq
make infra.test
make infra.up        # docker compose up -d --build
make infra.backing.up      # postgres, redis, and rabbitmq only
make infra.backing.health  # probe all three
make infra.backing.down    # stop them, keep volumes
make infra.smoke     # assert the gateway edge, SPA titles, and backing health
make infra.logs SERVICE=gateway
make infra.down
```

When the Compose stack is running, the RabbitMQ management UI is at
`http://localhost:15672` (`qingfa` / `qingfa` by default).

## API contract

Every `/api/*` response is the monad itself: `Result<T>` on success and the non-generic `Result`
on failure. The HTTP status comes from `Result.StatusCode`; `201` carries a `Location`, `204` has no
body. `ApiJson.Options` in `src/Infrastructure/Integrations/Serialization` is the single serializer.
Identity routes live under `/api/auth/*` and `/api/identity/*`; the old `/api/authorization/*`
surface is gone. The full contract is in
[`docs/specs/2026-10-30-api-result-envelope-design.md`](docs/specs/2026-10-30-api-result-envelope-design.md).

### .NET Commands (Direct)

```bash
# Restore & build
dotnet restore
dotnet build

# Run API standalone (without Aspire)
dotnet run --project app/Web.Api

# Run tests through the Make surface
make lib.test
make api.test
make gate.test
make host.test

# EF Core tools
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> -p src/Infrastructure -s app/Web.Api
dotnet ef database update -p src/Infrastructure -s app/Web.Api
```

### Frontend Commands

Both apps are members of the `app/` pnpm workspace.

```bash
# From the repository root
make store.install
cd app

pnpm --filter @qingfa/storefront dev
pnpm --filter @qingfa/admin dev

# Or from app/ directly
pnpm install
pnpm --filter @qingfa/storefront type-check
pnpm --filter @qingfa/storefront exec vitest run
pnpm --filter @qingfa/storefront build
pnpm format          # Prettier across both apps
pnpm format:check
```

## Environment Variables

### API (exported by the shell, AppHost, or Compose)

```env
# Database
ConnectionStrings__db=Host=localhost;Port=5432;Database=db;Username=qingfa;Password=qingfa
EfCore__ConnectionStringName=db

# Redis
ConnectionStrings__redis=localhost:6379

# RabbitMQ (leave unset for Wolverine's in-process transport)
ConnectionStrings__rabbitmq=amqp://qingfa:qingfa@localhost:5672
Messaging__Transport=RabbitMq

# JWT
JWT_SECRET=your-super-secret-key-min-32-chars
JWT_ISSUER=QingFa.Shop
JWT_AUDIENCE=QingFa.Shop.Clients
JWT_ACCESS_TOKEN_MINUTES=15
JWT_REFRESH_TOKEN_DAYS=7

# Stripe
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Aspire (auto-injected when running via AppHost)
ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:4317
```

### Compose broker overrides

`RABBITMQ_USER`, `RABBITMQ_PASS`, `RABBITMQ_PORT`, and
`RABBITMQ_MANAGEMENT_PORT` change the Compose broker. The API's
`ConnectionStrings__rabbitmq` is built from the same user/password pair, so
override both together.

### Web

The apps read `API_BASE_URL` from the process environment. AppHost injects the
gateway endpoint; for a standalone Vite server, export it first:

```bash
cd app
API_BASE_URL=http://localhost:5050 pnpm --filter @qingfa/storefront dev
```

## Aspire Dashboard

When running via `AppHost`, the **Aspire Dashboard** is available at:

```
http://localhost:15888
```

Features:
- **Service Graph** — Visualize service dependencies
- **Structured Logs** — Filter by service, level, trace ID
- **Metrics** — Prometheus metrics per service
- **Traces** — Distributed tracing (OpenTelemetry)
- **Health Checks** — Liveness/readiness per service
- **Console** — Aggregated console output

## Deployment

### Docker (Production)

```bash
# Build images through Make
make infra.build

# Start the stack (gateway on :5050)
make infra.up

# View logs
make infra.logs SERVICE=gateway

# Stop
make infra.down
```

A pull-only host uses the deploy overlay:

```bash
DOCKER_REGISTRY=ghcr.io/<owner>/<repo> IMAGE_TAG=latest \
  docker compose -f compose.yaml -f compose.deploy.yaml up -d --no-build
```

### Kubernetes (Aspire Manifest)

```bash
# Generate K8s manifests from AppHost
dotnet run --project app/AppHost -- --publisher manifest

# Output: ./manifests/ — apply with kubectl
kubectl apply -f ./manifests/
```

## Code Quality

| Tool | Purpose |
|------|---------|
| `dotnet format` | C# formatting (EditorConfig) |
| `Prettier` | TS/Vue/CSS/Markdown formatting |
| `vue-tsc` | Vue SFC type-checking |
| `Vitest` | Web unit and integration tests |

## Testing Strategy

| Layer | Framework | Location |
|-------|-----------|----------|
| Unit (C#) | xUnit | `tests/Unit` |
| Architecture (C#) | xUnit | `tests/Architecture.Tests` |
| Integration (C#) | xUnit + Testcontainers | `tests/Integration` |
| Web | Vitest + `@vue/test-utils` | `app/Web.*/src/__tests__` (unit + `integration/` for router/API flows) |

### Test order

`make test` runs the fast gate in layer order:

1. **.NET unit** — Core → Application → Infrastructure → Architecture
2. **.NET integration** — Api
3. **Web unit** — Storefront → Admin
4. **Web integration** — Storefront → Admin

`make test.all` extends this with the full integration and optional browser layers:

1. .NET unit (same five suites)
2. .NET integration — Api → AppHost/Gateway → Infrastructure
3. Web unit → web integration
4. Web e2e — Storefront → Admin (Playwright; skipped when no suite is configured)

### Coverage

`make coverage` is the pull-request gate: .NET unit suites plus both web apps
must meet **90% line / 80% branch**, enforced per assembly and per app. The
merged .NET gate (`make coverage.report`) runs on `main` and the nightly
schedule after the Api, AppHost, and infrastructure coverage jobs upload their
raw Cobertura files.

- Gated .NET assemblies: `Core`, `Application`, `Infrastructure`, `Web.Api`.
- Report-only: `ServiceDefaults`, `Gateway`, `AppHost`, and all `*.Tests`
  assemblies.
- Reports land in `artifacts/coverage/` (git-ignored).
- Override the floors locally: `COVERAGE_LINES=95 COVERAGE_BRANCHES=85 make coverage`.

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feat/your-feature`
3. Run `make format` and `make lint` before committing
4. Ensure `make test` passes
5. Open a Pull Request with description and screenshots (if UI)

## License

MIT License — see [LICENSE](LICENSE) for details.

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Vue 3 Documentation](https://vuejs.org/)
- [EF Core Documentation](https://learn.microsoft.com/ef/core)
- [Aspire Dashboard Guide](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard)