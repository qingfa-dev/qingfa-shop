# QingFa.Shop

A modern e-commerce platform built with .NET Aspire, Vue 3 + TypeScript, PostgreSQL, and Redis.

## Overview

QingFa.Shop is a cloud-native e-commerce solution leveraging .NET Aspire for service orchestration, providing observability, resilience, and local development parity with production.

## Tech Stack

- **Frontend**: Vue 3 + TypeScript + Vite + Pinia + Vue Router
- **Backend**: .NET 9 + ASP.NET Core + Entity Framework Core
- **Orchestration**: .NET Aspire (Service Discovery, Telemetry, Health Checks)
- **Database**: PostgreSQL 16 + EF Core Migrations
- **Cache/Session**: Redis 7 (StackExchange.Redis)
- **Messaging**: MassTransit + RabbitMQ (Aspire component)
- **Auth**: JWT + Refresh Tokens (ASP.NET Core Identity)
- **Payments**: Stripe .NET SDK
- **Observability**: OpenTelemetry + Prometheus + Grafana (via Aspire Dashboard)
- **Containerization**: Docker + Docker Compose

## Project Structure

```
QingFa.Shop/
├── src/
│   ├── QingFa.Shop.AppHost/           # Aspire AppHost (orchestration)
│   ├── QingFa.Shop.ServiceDefaults/   # Shared Aspire service config
│   ├── QingFa.Shop.Api/               # Backend API (ASP.NET Core)
│   ├── QingFa.Shop.Web/               # Frontend (Vue 3 + TS + Vite)
│   ├── QingFa.Shop.Core/              # Domain models, interfaces
│   ├── QingFa.Shop.Infrastructure/    # EF Core, Redis, external services
│   └── QingFa.Shop.Tests/             # Unit & integration tests
├── docker-compose.yml                 # Production compose
├── docker-compose.override.yml        # Local dev override
├── Directory.Build.props              # Shared MSBuild props
├── Directory.Packages.props           # Central package management
├── global.json                        # .NET SDK version
├── nuget.config                       # NuGet sources
├── .editorconfig                      # Code style
└── README.md
```

## Prerequisites

- **.NET SDK 9.0+** (`global.json` pins version)
- **Node.js 20+** (for Vue frontend)
- **Docker Desktop** (for PostgreSQL, Redis, RabbitMQ, Aspire Dashboard)
- **pnpm 9+** (frontend package manager)

## Getting Started

### 1. Clone & Configure

```bash
git clone <repo-url>
cd QingFa.Shop

# Copy environment template
cp src/QingFa.Shop.Api/.env.example src/QingFa.Shop.Api/.env
cp src/QingFa.Shop.Web/.env.example src/QingFa.Shop.Web/.env
```

### 2. Start Infrastructure (Aspire)

```bash
# From repository root
dotnet run --project src/QingFa.Shop.AppHost
```

This starts:
- **Aspire Dashboard** → `http://localhost:15888` (observability, service graph, logs)
- **PostgreSQL** → `localhost:5432`
- **Redis** → `localhost:6379`
- **RabbitMQ** → `localhost:5672` (management UI: `localhost:15672`)
- **API** → `https://localhost:7000` / `http://localhost:5000`
- **Web** → `http://localhost:5173` (Vite dev server)

### 3. Database Migrations

```bash
# Apply EF Core migrations
dotnet ef database update --project src/QingFa.Shop.Infrastructure --startup-project src/QingFa.Shop.Api
```

### 4. Frontend Development (Alternative)

```bash
cd src/QingFa.Shop.Web
pnpm install
pnpm dev
```

## Available Scripts

### Root Level (Makefile / `just` / `Taskfile.yml`)

```bash
# Start full Aspire stack (recommended)
task up              # dotnet run --project src/QingFa.Shop.AppHost

# Stop all
task down

# Build all projects
task build

# Run all tests
task test

# Lint & format
task lint            # dotnet format + eslint + stylelint
task format          # dotnet format --fix + eslint --fix

# Database
task db:migrate      # EF Core migrate
task db:reset        # Drop + migrate + seed
task db:studio       # Not available for PG; use pgAdmin / DBeaver

# Frontend only
task web:install     # pnpm install
task web:dev         # Vite dev server
task web:build       # Production build
task web:preview     # Preview production build

# Docker
task docker:build    # Build all images
task docker:up       # Production compose up
task docker:down     # Production compose down
```

### .NET Commands (Direct)

```bash
# Restore & build
dotnet restore
dotnet build

# Run API standalone (without Aspire)
dotnet run --project src/QingFa.Shop.Api

# Run tests
dotnet test --filter "FullyQualifiedName~Integration"   # Integration only
dotnet test --filter "FullyQualifiedName~Unit"          # Unit only

# EF Core tools
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> -p src/QingFa.Shop.Infrastructure -s src/QingFa.Shop.Api
dotnet ef database update -p src/QingFa.Shop.Infrastructure -s src/QingFa.Shop.Api
```

### Frontend Commands

```bash
cd src/QingFa.Shop.Web

pnpm install          # Install deps
pnpm dev              # Dev server (HMR)
pnpm build            # Type-check + production build
pnpm preview          # Preview build
pnpm lint             # ESLint + Stylelint
pnpm format           # Prettier + ESLint --fix
pnpm test:unit        # Vitest
pnpm test:e2e         # Playwright
```

## Environment Variables

### API (`src/QingFa.Shop.Api/.env`)

```env
# Database
CONNECTION_STRING=Host=localhost;Database=qingfa_shop;Username=postgres;Password=postgres

# Redis
REDIS_CONNECTION_STRING=localhost:6379

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

### Web (`src/QingFa.Shop.Web/.env`)

```env
VITE_API_BASE_URL=https://localhost:7000
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_...
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
# Build images
docker compose -f docker-compose.yml build

# Start stack
docker compose -f docker-compose.yml up -d

# View logs
docker compose logs -f api
```

### Kubernetes (Aspire Manifest)

```bash
# Generate K8s manifests from AppHost
dotnet run --project src/QingFa.Shop.AppHost -- --publisher manifest

# Output: ./manifests/ — apply with kubectl
kubectl apply -f ./manifests/
```

## Code Quality

| Tool | Purpose |
|------|---------|
| `dotnet format` | C# formatting (EditorConfig) |
| `ESLint` + `Prettier` | TS/Vue linting & formatting |
| `Stylelint` | CSS/SCSS linting |
| `SonarCloud` | Static analysis (CI) |
| `Husky` + `lint-staged` | Pre-commit hooks |

## Testing Strategy

| Layer | Framework | Location |
|-------|-----------|----------|
| Unit (C#) | xUnit + Moq | `src/QingFa.Shop.Tests/Unit` |
| Integration (C#) | xUnit + Testcontainers | `src/QingFa.Shop.Tests/Integration` |
| Unit (Vue) | Vitest + Vue Test Utils | `src/QingFa.Shop.Web/tests/unit` |
| E2E | Playwright | `src/QingFa.Shop.Web/tests/e2e` |

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feat/your-feature`
3. Run `task format` and `task lint` before committing
4. Ensure `task test` passes
5. Open a Pull Request with description and screenshots (if UI)

## License

MIT License — see [LICENSE](LICENSE) for details.

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Vue 3 Documentation](https://vuejs.org/)
- [EF Core Documentation](https://learn.microsoft.com/ef/core)
- [Aspire Dashboard Guide](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard)