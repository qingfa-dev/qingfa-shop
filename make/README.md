# make

Included fragments that define the repository's public command surface.
_config · shared_

## Summary

The root `Makefile` is a facade: it computes `ROOT`, pins `CONFIGURATION`, and
includes nine fragments. Each fragment owns one group — shared libraries, Api,
Gateway, AppHost, Storefront, Admin, Infra, Quality, or CI/CD — and every
recipe delegates to one script under [`../scripts/`](../scripts/README.md).
Nothing builds in the Makefile itself, so the same command works locally and in
CI.

## Contains

| Path | Role | Notes |
|---|---|---|
| `common.mk` | config | `run` macro plus `##@ Setup` (`help`, `doctor`) and `##@ Quality` aggregates (`install`, `format`, `lint`, `build`, `test`, `test.all`) |
| `lib.mk` | config | `##@ Libraries`: shared `src/` and `.NET` test projects |
| `api.mk` | config | `##@ Api`: install, format, lint, build, test, publish |
| `gate.mk` | config | `##@ Gateway`: install, format, lint, build, test, publish |
| `host.mk` | config | `##@ Host`: install, format, lint, build, test |
| `store.mk` | config | `##@ Store`: install, format, lint, test, build, publish |
| `admin.mk` | config | `##@ Admin`: install, format, lint, test, build, publish |
| `infra.mk` | config | `##@ Infra`: `config`, `build`, `size`, `test`, `up`, `down`, `ps`, `logs`, `smoke` |
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
  admin, infra, ci.
- `ROOT` and `CONFIGURATION` are exported before the includes are read.
- GNU Make 4+ is required; no POSIX-make portability is attempted.

## Invariants

- Every public target is `.PHONY`; there are no file targets.
- Exactly one script invocation per public recipe.
- Every public recipe dispatches through `$(call run,$@,...)`, so it prints
  `==> <target>` before the script starts. Internal prerequisites stay quiet.
- The script prints the exact tool CLI (`$ dotnet ...`, `$ pnpm ...`) to stderr
  immediately before it executes, so the last line is always reproducible.
- `_dotnet.restore`, `_dotnet.build`, and `_web.install` are internal shared
  prerequisites, so an aggregate never restores or installs the same tree twice.
- Every app group exposes the same verbs where they are meaningful: `install`,
  `format`, `format.check`, `lint`, `build`, `test`; `publish` is added for the
  Api, Gateway, Storefront, and Admin.
- `make ci` never touches Docker; `make ci.full` is the only target that does.
- `ci.full` includes the AppHost, infrastructure, and Compose suites.
- `ci.full` tears the stack down through an `EXIT` trap even when smoke fails.

## Contracts

- `CONFIGURATION ?= Release` — override with `make CONFIGURATION=Debug`.
- `REGISTRY`, `TAG` — consumed by `make infra.build` and `make cd`.
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
