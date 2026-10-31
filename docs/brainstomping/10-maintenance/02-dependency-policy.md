# Dependency Policy

The cadence for reviewing, upgrading, and scanning resys-eshop dependencies, the runtime upgrade rules for .NET and Nuxt/Vue, and the security-patch SLA.

**Links:** [Technical debt](./01-technical-debt.md) · [Performance tuning](./03-performance-tuning.md) · [Versioning](../07-release/01-versioning.md) · [Risks](../02-planning/03-risks.md) · [Maintenance index](./README.md) · [Spec](../11-spec/README.md)

---

## Purpose

Every dependency in resys-eshop is a standing commitment: it must be patched, upgraded, and
eventually replaced before the vendor stops supporting it. This file is the single update contract.
It fixes **when** dependencies are reviewed, **how** a runtime upgrade is justified, and **how fast** a
security patch must land. It does not restate the version numbers themselves — those are locked in
§4 of the [spec](../11-spec/README.md) and the ADR `00/01-01` — and
it does not restate the release mechanics, which are owned by `07/01`.

The guiding rule: **pin what is supported, upgrade on evidence, and never run a runtime or package
past its support date.** Pinning without a cadence is as unsafe as tracking the latest release.

NFR-SEC-010 (`01/03-02`) requires dependencies to be pinned and scanned on a defined cadence; this
file is that definition.

---

## Scope

The policy covers every versioned artifact the platform builds or runs:

| Class | Examples | Versioned in |
|---|---|---|
| .NET runtime and SDK | .NET 10.0 LTS, ASP.NET Core, EF Core, ASP.NET Identity | `global.json`, project files |
| NuGet packages | Npgsql, Stripe.net, PayPal SDK, Scalar, Serilog | `Directory.Packages.props` |
| Node runtime and npm packages | Node.js LTS, Nuxt 4.5.x, Vue 3.5.43, PrimeVue 4, `@nuxtjs/i18n`, Tailwind v4, Pinia | `package.json`, lockfile |
| Data and messaging services | PostgreSQL 18 | deployment manifests |
| Container base images | .NET runtime image, Node image, Postgres image | image manifests |
| Build and test tools | xUnit, Testcontainers, Playwright, k6 | lockfiles |

Development-only tools follow the same scanning and patch rules even though they never ship to
production, because a compromised build tool compromises the artifact.

---

## Update Cadence

Updates are grouped into four recurring bands so a change has one place to land and one verification
gate.

| Cadence | Scope | Action | Gate |
|---|---|---|---|
| **Continuous** | Security advisories and automated dependency PRs | The dependency bot opens or updates a PR for every advisory or new version. | CI (`05/03-03`) |
| **Weekly** | Open dependency PRs | Triage new PRs: merge patch and advisory fixes, close superseded bumps. | CI green, one reviewer |
| **Monthly** | Patch and minor versions | Review and apply accumulated patches; confirm every package is within a supported line. | CI plus the affected test layer |
| **Quarterly** | Framework minors, pins, and EOL calendar | Re-verify each pinned version against its support date; plan the next framework minor. | Milestone review |
| **Triggered** | Major versions and EOL | A major upgrade or a runtime end-of-support runs as a planned release with its own window. | `07/01`, E2E, budgets `06/03-02` |

A weekly PR that only bumps a patch and passes CI does not need a design review. A change that moves
a framework major, changes a public package, or crosses an EOL boundary is a release-level decision
and follows the upgrade rules below.

---

## Backend: .NET LTS Rule

The backend targets **.NET 10.0 LTS (10.0.12)**, supported through **2028-11-14** (spec §4, `00/01-01`).
.NET 11 is RC/STS at the plan date and is not targeted.

The rules:

1. **Production runs an LTS line only.** An STS release may be evaluated in CI but is never the
   deployed runtime, because its support window is too short to cover a release cycle.
2. **A successor LTS upgrade lands with runway.** Plan the next LTS upgrade so it ships at least
   **two platform releases and not less than six months before the current line's support end date**
   (2028-11-14 for .NET 10). Cutting it closer turns a planned upgrade into an emergency.
3. **The runtime moves as one unit.** A .NET upgrade includes the SDK, ASP.NET Core, EF Core, ASP.NET
   Identity, and the Npgsql provider; a partial move that leaves one component behind is not a valid
   upgrade state.
4. **Prefer first-party packages.** Where a capability exists in the .NET runtime or first-party
   packages, prefer it over a third-party package that carries its own support clock (R-10).
5. **A downgrade is also a planned change.** Rolling a runtime back is a `MAJOR`-or-`MINOR` release
   per `07/01`, not a hotfix.

---

## Frontend: Nuxt and Vue Rule

Nuxt 3 reached **end-of-life on 2026-07-31**. That date, not a feature preference, is why the
front-ends are built on **Nuxt 4.5.x** and why no `web/*` app may depend on the Nuxt 3 line
(spec §4, `00/01-01`).

The rules:

1. **Vue stays on 3.5.43 until 3.6 reaches GA.** 3.6 is RC at the plan date; the pin moves only after
   general availability plus a passing E2E run.
2. **Nuxt tracks 4.5.x.** Patch and minor updates within the 4.5 line are ordinary monthly work; a
   move to 4.6 or a new major is a triggered upgrade that must pass the storefront and admin E2E
   suites before adoption (R-05).
3. **Nuxt, `@nuxtjs/i18n`, PrimeVue 4, Tailwind v4, and Pinia move independently.** Each tracks its
   own support line; upgrading one does not imply upgrading the rest, and a coupled bump is tested as
   one change.
4. **Third-party usage stays behind `web/shared`.** Apps consume shared contracts and components
   rather than importing a library directly wherever possible, so a library swap is contained
   (`05/02-03`).
5. **Node.js follows an active LTS line** for the build and the SSR runtime; a Node end-of-life is a
   triggered upgrade under the same rules as a framework minor.

---

## Data and Image Rule

PostgreSQL 18 and the container base images follow the vendor support lifecycle. A PostgreSQL major
upgrade uses the migration and backup path in `03/03-04` and `08/03-02`, and runs as a triggered
release with a tested restore. Base images are rebuilt and re-scanned whenever the upstream image
receives a security fix, whether or not application code changed.

---

## Security-Patch SLA

A vulnerability in any in-scope dependency is triaged by severity and patched within a fixed window.
Severity uses the CVSS base score, adjusted for exploitability and exposure.

| Severity | CVSS base | Patch within | Notes |
|---|---|---|---|
| **Critical** | 9.0–10.0 | **7 calendar days**; **72 hours** if an exploit is known or the service is internet-facing | The clock starts at advisory publication or detection, whichever is later. |
| **High** | 7.0–8.9 | **14 calendar days** | If no fixed version exists, apply a mitigation until one does. |
| **Medium** | 4.0–6.9 | **30 calendar days** | May be batched into the monthly cadence if the deadline allows. |
| **Low** | 0.1–3.9 | Next scheduled patch cycle, **not more than 90 days** | Batch with other changes. |
| **No fix available** | any | Immediate mitigation, tracked here until a fix ships | Mitigation is a change, not a note. |

Rules that make the SLA real:

- **Detection.** Advisories for NuGet, npm, the .NET runtime, and the base images feed the continuous
  band above; secret-scanning in CI covers accidental credential exposure (`05/03-03`).
- **Patch is not enough.** A dependency patch follows the normal CI gates: the affected test layer
  plus, for a critical path, the performance budgets (`06/03-02`).
- **Risk acceptance is explicit and time-boxed.** If a patch cannot meet its window, the security
  owner records a written acceptance with compensating controls, an owner, and an expiry date; an
  accepted risk is reviewed at the next milestone, never left open-ended.
- **A critical advisory can pre-empt the release train.** The patch ships on the next compatible
  release, which may be an out-of-band `PATCH` release per `07/01`.

---

## Upgrade Verification

Every upgrade is verified at the layer it can break, so a bump is merged on evidence rather than on a
green compile:

1. **CI.** Build, unit, integration, and front-end lint/typecheck pass (`05/03-03`). A dependency
   bump runs the full pipeline, not a reduced one.
2. **E2E.** A front-end or contract-affecting bump passes Playwright for both apps; a Nuxt or PrimeVue
   major does not merge on unit tests alone.
3. **Performance.** A bump on a hot path (catalog, checkout, or the data provider) re-checks the
   budgets in `06/03-02`; a regression holds the bump.
4. **Release classification.** The platform version bump follows `07/01`: a patch bump is `PATCH`, a
   new supported capability is `MINOR`, and a break is `MAJOR` with a `BREAKING CHANGE:` note.

---

## Ownership

| Surface | Owner | Reviews |
|---|---|---|
| .NET runtime, ASP.NET Core, EF Core, Npgsql | Backend lead | Monthly and on a triggered LTS upgrade |
| Nuxt, Vue, PrimeVue, i18n, Tailwind | Frontend lead | Monthly and on a triggered framework upgrade |
| PostgreSQL and base images | Platform architect | Quarterly and on a vendor security release |
| Security advisories and the patch SLA | Security owner | Continuous triage, milestone report |
| The policy itself | Tech writer | Quarterly, aligned with `00/02-03` |

---

## Open Questions

1. The exact dependency bot and its grouping configuration is a CI-scaffold decision owned by
   `05/03-03`; the cadence bands and the no-unsupported-runtime rule are fixed here.
2. The concrete Node.js LTS line the front-ends pin is finalized with the toolchain in `05/01-01`;
   the "active LTS only" rule is fixed.
3. Whether an STS .NET preview is exercised in a non-blocking CI lane is a tooling decision; it never
   changes the LTS-only production rule.
