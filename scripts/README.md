# scripts

The shell implementations behind every Make target. _util · shared_

## Summary

Each script performs exactly one action for one layer and is called by a Make
target, never by a human or by CI directly. Grouping by layer keeps the
dependency story obvious: `common` has no project knowledge, `backend` and
`web` know their toolchains, `infra` knows the images, and nothing reaches
upward.

## Contains

| Path | Role | Notes |
|---|---|---|
| `common/` | util | `doctor`, `clean`, `structure.check`, `convention-check.*` (magic values, outcomes, monadic `Result`, baseline ratchet), `restore-tools`, `trace.common.sh` (shared command echo), `coverage.common.sh` + `coverage-check.*` (report paths, merge, thresholds) |
| `backend/` | util | solution restore/build, scoped `dotnet format`, Api/Gateway/AppHost/lib test runners, the RabbitMQ broker runner, coverage collectors, Api/Gateway publish |
| `web/` | util | workspace install, per-app Prettier, `vue-tsc`, Vitest unit/integration, optional Playwright e2e, coverage, build, publish |
| `infra/` | util | `config`, `build` (seven images), `size`, `test`, `up`, `down`, `logs`, `ps`, `smoke`, plus `backing-up/down/health` for PostgreSQL, Redis, and RabbitMQ |

## Boundaries

In scope:
- Resolving the repository root and invoking one external tool.
- Failing fast with the tool's own message when a command fails.

Out of scope:
- Deciding *when* something runs → [`../make/`](../make/README.md).
- CI job structure → [`../.github/workflows/`](../.github/workflows/ci.yml).
- Application logic → [`../src/`](../src/README.md) and [`../app/`](../app/).

## Assumptions

- `bash` 4+ is available; every file starts with `set -euo pipefail`.
- `ROOT` is derived from `BASH_SOURCE`, so scripts work from any directory.
- Every script is executable (`chmod +x`); git records the mode.
- `CONFIGURATION` defaults to `Release`; `Makefile` exports it.
- `python3` is available for the coverage threshold checker (standard library
  only; no third-party packages).
- The two SPAs are members of the `app/` pnpm workspace and share one lock file.

## Invariants

- No script calls another script; composition lives in Make. The exception is
  the coverage collectors, which deliberately wrap the matching test runner so
  the suite definition exists once. Shared code is limited to
  `common/trace.common.sh` and `common/coverage.common.sh`, which are sourced.
- Every script that runs a tool prints the exact argv via `trace` or
  `trace_exec` first. Use `trace` when the script must run more than one
  command; `trace_exec` replaces the shell. Trace lines go to stderr so stdout
  stays capturable.
- No script runs a git command.
- A script prints nothing on success unless it has something useful to say.
- A suite with no test sources is skipped explicitly, never by ignoring an exit
  code that could also hide a real failure.
- Web tests run Vitest once (`vitest run`), never in watch mode.

## Contracts

Environment variables read by scripts:

| Variable | Used by | Meaning |
|---|---|---|
| `CONFIGURATION` | `backend/*`, `infra/test` | Build configuration, default `Release` |
| `CI` | `backend/restore` | Enables locked restore through MSBuild |
| `ALL` | `clean` | `1` also removes the workspace `node_modules` |
| `REGISTRY`, `TAG` | `infra/build`, `infra/size` | Image coordinates, default `qingfa-shop`, `local` |
| `SERVICES` | `infra/build` | Optional space-separated subset of `gateway api shopfront admin postgres redis rabbitmq` |
| `SERVICE` | `infra/logs` | Optional Compose service filter |
| `GATEWAY_URL` | `infra/smoke` | Default `http://localhost:5050` |
| `COVERAGE_LINES`, `COVERAGE_BRANCHES` | `common/coverage-check`, `web/coverage` | Per-assembly/per-app floors, default `90` / `80` |
| `COVERAGE_ROOT` | `common/coverage.common.sh` | Report directory, default `artifacts/coverage` |
| `COVERAGE_GATED` | `common/coverage-check` | Assemblies that must meet the floor |
| `CONVENTION_BASELINE` | `common/convention-check` | Baseline path, default `scripts/common/convention-baseline.tsv` |

## Conventions

- Filename: `<action>.<layer>.sh` — the layer repeats so the file stands alone
  when quoted in a Makefile or a CI log.
- One action per file. Shared helpers are limited to `common/trace.common.sh`
  and `common/coverage.common.sh`; coverage collectors wrap the matching test
  runner on purpose so the suite list has one owner.
- Scripts that operate on one app take the workspace directory or package name
  as their first argument. `test.web.sh` takes an optional second argument:
  `unit`, `integration`, or `all` (default).
- Call `trace` for a normal command and `trace_exec` for the final command that
  replaces the shell; both print `$ <argv>` before executing.
- Diagnostic reporters (`doctor`, `structure.check`, `smoke`, `size`) print
  their own progress instead of individual probe commands.

## Related

- Parent: [`../README.md`](../README.md)
- Interface: [`../make/`](../make/README.md)
- Docker definitions: [`../compose.yaml`](../compose.yaml)
- CI: [`../.github/workflows/`](../.github/workflows/ci.yml)

## Notes

- `infra/smoke` waits up to three minutes for the gateway, then asserts the edge
  paths, both SPA titles, and backend listener health. It prints the gateway log
  tail when readiness fails.
- `infra/backing-up` uses `docker compose up --wait`, so callers never race a
  broker that is still booting. `backing-health` and `smoke` probe RabbitMQ with
  `check_port_connectivity`, which covers the AMQP listener rather than only the
  Erlang process.
- `backend/test-gateway` filters the shared AppHost suite to
  `AppHost.Integration.Tests.GatewayRoutingSpec`.
- `backend/test-broker` filters the Infrastructure suite to `*RabbitMq*` and
  skips when the broker specs are absent; the specs themselves skip when no
  container engine is available.
- `common/convention-check` scans `src/` and `app/`, masks comments and string
  contents, and enforces four rules: `magic-number` (abs ≥ 10), `magic-string`
  (dotted/hyphenated/colon keys), `outcome-shape` (static `*Outcome` with
  `Codes` and `Failure`/`Success`, no inline `Error.*`), and `monadic-result`
  (`Result` returns on the service/interface/abstract surface). The TSV
  baseline records `rule ⇥ path ⇥ key ⇥ count ⇥ reason`; excess occurrences,
  stale rows, and duplicate rows fail, so the list only shrinks.
  `--write-baseline` regenerates it; `--self-test` runs the parsing fixtures.
- `backend/coverage-*` write raw Cobertura under
  `artifacts/coverage/dotnet/raw/`; `coverage.report` merges every raw file and
  enforces the gated assemblies. `common/coverage-check.common.py` deliberately
  uses only the Python standard library so it runs on a bare CI runner.- `trace.common.sh` prints `(in <dir>)` when the command runs from a different
  directory than the caller, which matters for the `app/` pnpm commands.
- `web/e2e.web.sh` is opt-in: it skips unless both a Playwright config and at
  least one spec exist under `app/<app>/e2e`, so `make test.all` stays green
  before a browser suite is added.
