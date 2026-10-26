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
| `common/` | util | `doctor`, `clean`, `structure.check`, `trace.common.sh` (shared command echo) |
| `backend/` | util | solution restore/build, scoped `dotnet format`, Api/Gateway/AppHost/lib test runners, Api/Gateway publish |
| `web/` | util | workspace install, per-app Prettier, `vue-tsc`, Vitest, build, publish |
| `infra/` | util | `config`, `build` (six images), `size`, `test`, `up`, `down`, `logs`, `ps`, `smoke` |

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
- The two SPAs are members of the `app/` pnpm workspace and share one lock file.

## Invariants

- No script calls another script; composition lives in Make. The only shared
  code is `common/trace.common.sh`, sourced to echo a command before it runs.
- Every script that runs a tool prints the exact argv via `trace` or
  `trace_exec` first. Trace lines go to stderr so stdout stays capturable.
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
| `SERVICES` | `infra/build` | Optional space-separated subset of `gateway api shopfront admin postgres redis` |
| `SERVICE` | `infra/logs` | Optional Compose service filter |
| `GATEWAY_URL` | `infra/smoke` | Default `http://localhost:5050` |

## Conventions

- Filename: `<action>.<layer>.sh` — the layer repeats so the file stands alone
  when quoted in a Makefile or a CI log.
- One action per file; no shared library, because the duplication is smaller than
  the indirection.
- Scripts that operate on one app take the workspace directory or package name
  as their first argument.
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
  paths and both SPA titles. It prints the gateway log tail when readiness fails.
- `backend/test-gateway` filters the shared AppHost suite to
  `AppHost.Integration.Tests.GatewayRoutingSpec`.
- `trace.common.sh` prints `(in <dir>)` when the command runs from a different
  directory than the caller, which matters for the `app/` pnpm commands.
