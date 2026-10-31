# make

Included fragments that define the repository's public command surface.
_config · shared_

## Summary

The root `Makefile` is a facade: it computes `ROOT`, pins `CONFIGURATION`, and
includes eleven fragments. Each fragment owns one group — shared libraries, Api,
Gateway, AppHost, Storefront, Admin, Infra, Quality, Tests, Coverage, or CI/CD —
and every recipe delegates to one script under
[`../scripts/`](../scripts/README.md). Nothing builds in the Makefile itself, so
the same command works locally and in CI.

## Contains

| Path | Role | Notes |
|---|---|---|
| `common.mk` | config | `run` macro plus `##@ Setup` (`help`, `doctor`) and `##@ Quality` (`install`, `format`, `structure.check`, `convention.check`, `lint`, `build`) |
| `lib.mk` | config | `##@ Libraries`: shared `src/` and `.NET` test projects |
| `api.mk` | config | `##@ Api`: install, format, lint, build, test, publish |
| `gate.mk` | config | `##@ Gateway`: install, format, lint, build, test, publish |
| `host.mk` | config | `##@ Host`: install, format, lint, build, test |
| `store.mk` | config | `##@ Store`: install, format, lint, `test` (`unit` -> `integration`), optional `e2e`, build, publish |
| `admin.mk` | config | `##@ Admin`: install, format, lint, `test` (`unit` -> `integration`), optional `e2e`, build, publish |
| `infra.mk` | config | `##@ Infra`: `config`, `build` (seven images), `size`, `test`, `up`, `down`, `ps`, `logs`, `smoke`, and the symmetric `backing.up/down/health` lifecycle; `backing.up` waits for every healthcheck |
| `test.mk` | config | `##@ Tests`: `test`, `test.all`, and the layered `test.dotnet.*` / `test.web.*` targets |
| `coverage.mk` | config | `##@ Coverage`: `coverage` (PR gate), per-suite collectors, `coverage.report`, `coverage.all`, `coverage.clean` |
| `ci.mk` | config | `##@ CI/CD`: `ci`, `ci.full`, `cd` |

## Boundaries

In scope:
- Target names, prerequisites, and help text.
- Composition order and cleanup traps.

Out of scope:
- Shell logic → [`../scripts/`](../scripts/README.md).
- Job definitions → [`../.github/workflows/`](../.github/workflows/ci.yml).

## Assumptions

- `SHELL := bash`; recipes rely on bash only for the `ci.full` trap.
- Fragments are included in this order: common, lib, api, gate, host, store,
  admin, infra, test, coverage, ci.
- `ROOT` and `CONFIGURATION` are exported before the includes are read.
- GNU Make 4+ is required; no POSIX-make portability is attempted.

## Invariants

- Every public target is `.PHONY`; there are no file targets.
- Exactly one script invocation per public recipe.
- Every public recipe dispatches through `$(call run,$@,...)`, so it prints
  `==> <target>` before the script starts. Internal prerequisites stay quiet.
- The script prints the exact tool CLI (`$ dotnet ...`, `$ pnpm ...`) to stderr
  immediately before it executes, so the last line is always reproducible.
- `_dotnet.restore`, `_dotnet.build`, `_dotnet.tools`, and `_web.install` are
  internal shared prerequisites, so an aggregate never restores, builds,
  installs, or restores tools for the same tree twice.
- Every app group exposes the same verbs where they are meaningful: `install`,
  `format`, `format.check`, `lint`, `build`, `test`; `publish` is added for the
  Api, Gateway, Storefront, and Admin. Web groups add `test.unit`,
  `test.integration`, and optional `e2e`.
- `make ci` never starts the Compose stack; `make ci.full` is the only target
  that does.
- `make lint` includes `convention.check`, so magic-value, outcome, and monadic
  `Result` rules fail locally before CI. `make convention.check SELF_TEST=1`
  runs the checker's own fixtures; `BASELINE=<path>` overrides the baseline.
- `make ci` runs `coverage.unit`, the Api integration suite, the RabbitMQ broker
  suite, then `coverage.web`; `make ci.full` adds the merged `coverage.report`
  gate after every integration collector.
- Test aggregates live in `test.mk` and run lowest layer first: .NET unit →
  .NET integration → web unit → web integration → optional e2e.
- `ci.full` includes the AppHost, infrastructure, and Compose suites.
- `ci.full` tears the stack down through an `EXIT` trap even when smoke fails.

## Contracts

- `CONFIGURATION ?= Release` — override with `make CONFIGURATION=Debug`.
- `COVERAGE_LINES`, `COVERAGE_BRANCHES` — per-assembly/per-app coverage floors,
  default `90` and `80`.
- `REGISTRY`, `TAG` — consumed by `make infra.build` and `make cd`.
- `SERVICES` — optional space-separated subset for `make infra.build`
  (`gateway api shopfront admin postgres redis rabbitmq`).
- `SERVICE` — forwarded by `make infra.logs`.
- `make help` is the source of truth for the target list.

## Conventions

- Prefix every target with its group and an action verb: `api.test`,
  `store.format`, `infra.up`. The verb is the last segment.
- Keep backend and frontend groups symmetric: `install`, `format`,
  `format.check`, `lint`, `build`, `test`.
- Document a target with `##` on the same line; `help` reads it with `awk`.
- Add a new group as a new `.mk` file, not as a new section in `common.mk`.
- Do not prefix a public recipe with `@`; use `$(call run,$@,...)` so the target
  name is always visible in the log.

## Related

- Parent: [`../README.md`](../README.md)
- Scripts: [`../scripts/`](../scripts/README.md)
- CI: [`../.github/workflows/`](../.github/workflows/ci.yml)

## Notes

- `make` with no target prints help grouped by `##@` section.
- `store.*` and `admin.*` map to `app/Web.Storefront` and `app/Web.Admin`.
  The Compose service and YARP cluster names remain `shopfront` and `admin`.
