# Local Docker Compose

The complete Compose file for the local PostgreSQL 18 database plus optional tooling containers, and the commands that start them.

**Links:** [Prerequisites](./01-prerequisites.md) · [Environment](./02-environment.md) · [Seed data](./04-seed-data.md) · [App topology](../../03-architecture/01-overview/02-app-topology.md) · [Migrations](../../03-architecture/03-data/04-migrations.md) · [Development index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Local development runs **PostgreSQL 18 in a container** rather than on the host, so every
contributor uses the same database major version as CI and the Testcontainers integration tests
(`03/03-04`). This file provides the complete `docker-compose.yml`, the ports it publishes, the
volumes that persist data, a healthcheck that gates dependent tooling, and the exact `docker
compose up` commands. The connection string it produces is the default in
`ConnectionStrings:ResysDb` (`05/01-02`).

The default command starts **only PostgreSQL**. pgAdmin and a local SMTP catcher are optional and
live behind a Compose profile, so a normal start stays fast and tool-free.

---

## Services

| Service | Image | Required | Purpose |
|---|---|---|---|
| `postgres` | `postgres:18` | yes | All commerce state, Identity tables, and the outbox. |
| `pgadmin` | `dpage/pgadmin4:latest` | no (`tools` profile) | Browser UI for inspecting the local database. |
| `mailpit` | `axllent/mailpit:latest` | no (`tools` profile) | Captures transactional email on port `1025`. |

Ports are the documented development defaults from `03/01-02`: PostgreSQL `5432`, SMTP `1025`,
API `8080`/`8443`, storefront `3000`, admin `3001`, and the optional pgAdmin UI on `5050`.

---

## `docker-compose.yml`

```yaml
name: resys-eshop

services:
  postgres:
    image: postgres:18
    container_name: resys-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: resys
      POSTGRES_PASSWORD: resys
      POSTGRES_DB: resys_eshop
      # Deterministic collation so local and CI sort identically.
      POSTGRES_INITDB_ARGS: "--locale=C --encoding=UTF8"
    ports:
      - "5432:5432"
    volumes:
      # postgres:18 keeps PGDATA under /var/lib/postgresql/18/docker and declares
      # VOLUME /var/lib/postgresql, so the named volume must mount at the parent.
      - resys-pgdata:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U resys -d resys_eshop -h 127.0.0.1"]
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 15s

  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: resys-pgadmin
    profiles: ["tools"]
    restart: unless-stopped
    environment:
      PGADMIN_DEFAULT_EMAIL: dev@resys.test
      PGADMIN_DEFAULT_PASSWORD: resys
      PGADMIN_CONFIG_SERVER_MODE: "False"
    ports:
      - "5050:80"
    volumes:
      - resys-pgadmin:/var/lib/pgadmin
    depends_on:
      postgres:
        condition: service_healthy

  mailpit:
    image: axllent/mailpit:latest
    container_name: resys-mailpit
    profiles: ["tools"]
    restart: unless-stopped
    environment:
      MP_SMTP_AUTH_ACCEPT_ANY: 1
      MP_SMTP_AUTH_ALLOW_INSECURE: 1
    ports:
      - "1025:1025"   # SMTP transport used by Email:Provider=smtp
      - "8025:8025"   # Web mailbox
    volumes:
      - resys-mailpit:/data

volumes:
  resys-pgdata:
  resys-pgadmin:
  resys-mailpit:
```

Notes on the file:

- **`name: resys-eshop`** fixes the Compose project name so volume names and `docker compose down`
  target a predictable project regardless of the directory name.
- **`postgres:18` is exact**; do not float to `latest`, which would cross a major version and
  invalidate the data volume.
- **The data volume mounts at `/var/lib/postgresql`.** In the `postgres:18` image `PGDATA` moved
  to `/var/lib/postgresql/18/docker` and the image declares `VOLUME /var/lib/postgresql`, so the
  named volume must mount at that parent; mounting the old `/var/lib/postgresql/data` path would
  leave `resys-pgdata` empty and persist data in an anonymous volume.
- **The tool images use `:latest` deliberately.** `pgadmin` and `mailpit` hold no stateful schema
  and are opt-in behind the `tools` profile, so floating them is intentional; only the database
  image is pinned.
- **`POSTGRES_INITDB_ARGS`** sets the C locale so local text ordering matches CI and the
  integration tests.
- **The healthcheck** uses `pg_isready` against the application database; `pgadmin` waits for it
  with `condition: service_healthy`, so the UI never starts against a database that is still
  initializing.
- **Named volumes** persist data across `down` and container recreation. Only `down -v` deletes it.
- **The `tools` profile** keeps the optional containers out of the default start. They are pulled
  and started only when the profile is requested.

---

## Commands

Run these from the repository root, where `docker-compose.yml` lives.

Start the required database (the normal first step):

```bash
docker compose up -d
```

Start the database and the optional tools together:

```bash
docker compose --profile tools up -d
```

Common operations:

```bash
docker compose ps                          # service status and health
docker compose logs -f postgres            # follow database logs
docker compose exec postgres psql -U resys -d resys_eshop   # open a psql shell
docker compose stop                        # stop containers, keep volumes
docker compose down                        # remove containers, keep volumes
docker compose down -v                     # remove containers AND data (destructive)
```

`docker compose up -d` is idempotent: re-running it recreates only containers whose definition or
image changed and leaves a healthy database running.

---

## Connecting

The Compose defaults map to the connection string in `05/01-02`:

| Parameter | Value |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | `resys_eshop` |
| User | `resys` |
| Password | `resys` (local only) |

For the API, this is `ConnectionStrings__ResysDb`; for `dotnet ef database update` and the seed
command it is the same default in `appsettings.Development.json`. The credentials are deliberately
low-friction and local-only; production credentials come from the deployment secret store
(`08/01-02`), never from this file.

When the `tools` profile is running:

- **pgAdmin** — `http://localhost:5050` (email `dev@resys.test`, password `resys`). Add a server
  with host `postgres` (the service name, not `localhost`) and the credentials above.
- **Mailpit** — `http://localhost:8025` shows captured email; SMTP listens on `localhost:1025`.

---

## Reset and Troubleshooting

- **Reset the database completely.** `docker compose down -v` then `docker compose up -d`; the
  next run must re-apply migrations and seed data (`05/01-04`).
- **Port 5432 already in use.** Another PostgreSQL (host install or another container) owns the
  port. Stop it, or change the published port and update the connection string; do not silently
  run two databases.
- **Container unhealthy.** `docker compose logs postgres` shows the cause; a common one is a
  half-written data volume from an interrupted first start. `down -v` and restart is the fix.
- **`pgadmin` cannot reach the database.** Use host `postgres`, not `localhost`, because the
  connection is made container-to-container inside the Compose network.
- **Email never arrives.** With `Email:Provider=smtp`, messages are captured by Mailpit, not
  delivered (`05/01-02`); read them at `http://localhost:8025`.

---

## What Is Not in This File

- **No application containers.** The API and the two Nuxt apps run on the host with `dotnet` and
  `pnpm` (`05/03-01`); only dependencies are containerized locally.
- **No production orchestration.** Staging and production topology is owned by `08/01-01`.
- **No object storage service.** Development uses the `local` disk backend (`05/01-02`), so MinIO
  is not required locally; production uses an S3-compatible bucket.
- **No migration bootstrap.** The database starts empty; schema and seed come from the commands in
  `03/03-04` and `05/01-04`.

## Open Questions

1. Whether the optional tools are pulled by a Makefile/script target or invoked directly is owned
   by `05/03-01`; the service definitions and commands here are the source of truth.
2. The production PostgreSQL hosting and backup configuration are owned by `08/01-01`; this file is
   the local development contract only.
