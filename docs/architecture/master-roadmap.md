# Master Roadmap — Production-Grade QingFa.Shop

**Version:** 1.0
**Date:** 2026-10-31
**Planner model:** GPT-6.1 Terra Pro (high effort)
**Note on model:** GPT-6 Astra Pro was attempted first and rejected by the provider: *"requested up to 128000 tokens, can only afford 40926."* The roadmap was produced with the next-cheapest current-generation frontier model. See `README.md` for the full model/cost table.
**Ground truth:** `docs/architecture/software-quality-audit.pro.md` (Q1–Q32), `docs/architecture/roadmap-implementation-audit.pro.md`, and the repository itself. Findings are static-review results and must be reproduced during remediation.

**Priority:** **Must** blocks the phase or release; **Should** is expected unless an owner accepts the risk; **Could** is optional and trigger-driven.
**Product assumption:** one merchant, one configured market/currency, authenticated checkout, physical inventory, one real payment provider. Tax, shipping, and legal requirements for that market must be approved before release.

---

## 1. Current state

`src/*/Modules` contains two business modules: **Todos** and **Payments**. Identity is a BuildingBlocks capability. Payments has Fake, Stripe, and PayPal adapters; event-sourced writes stage domain events in the outbox, but Payments publishes **no cross-module integration contract**.

`app/Web.Api`, `app/Gateway`, `app/Web.Storefront`, and `app/Web.Admin` have Dockerfiles. `app/AppHost` orchestrates development dependencies. All workers run inside the API; there is no worker deployable.

The foundation is substantial but unfinished: EF persistence, event streams, snapshots, projections, outbox/inbox, OpenAPI, JWT authorization, health checks, and tests exist. Projection correctness, notification delivery, gateway routing, payment recovery, security, and operations are **not production-safe**. Both Vue apps are health-check shells. Catalog, cart, inventory, orders, checkout, real search, and ML are absent.

Five root causes block production:

1. **Edge/API contract drift** — `app/Gateway/Gateway.Router.cs` strips `/api` and challenges anonymous auth routes.
2. **Remote effects inside database transactions** — `src/Application/Modules/Payments` cannot atomically commit provider money movement with local state.
3. **Unsafe projections** — the event cursor is not commit-ordered; apply/checkpoint and replay lack atomicity and fencing.
4. **Silent failures** — webhook dedupe, notification queues, outbox dead letters, and identity-email logging hide loss.
5. **Unsafe defaults** — `compose.yaml` runs Development with fake payments, split JWT keys, and no backup plan.

---

## 2. Target architecture and ownership

**Must:** retain the modular monolith and inward dependencies from `AGENTS.md`. Add business modules under `src/{Core,Application,Infrastructure}/Modules/`. BuildingBlocks must not reference modules.

```text
Browser ─TLS─> app/Gateway ─> app/Web.Storefront (/)
                           ├─> app/Web.Admin (/admin)
                           └─> app/Web.Api (/api, prefix preserved)
                                  ├─ Identity / Catalog / Cart / Inventory
                                  ├─ Orders / Payments / Notifications
                                  ├─ in-process workers; optional app/Worker later
                                  ├─ Postgres: owned schemas, events, outbox/inbox
                                  ├─ Redis: cache/session support, not financial truth
                                  ├─ RabbitMQ: at-least-once integration delivery
                                  └─ provider ports ─TLS─> payment / email / optional ML
```

`app/Gateway` is the public edge; `app/Web.Api` independently enforces authorization and resource ownership. Provider webhooks are anonymous only at HTTP authentication: `app/Web.Api/Endpoints/Payments` must verify signatures, enforce body limits, and record durably. Postgres, Redis, RabbitMQ, telemetry, and administration endpoints remain private. Use least-privilege identities, encrypted connections, and secrets from the deployment secret store — never SPA bundles or base configuration.

**Data authority:**

| Owner | Authoritative data |
|---|---|
| Catalog | product publication, prices, media, categories |
| Inventory | stock levels and reservations |
| Cart | mutable customer selections and price snapshots |
| Orders | immutable purchased lines, totals, addresses, lifecycle |
| Payments | provider operations, captures, refunds, disputes |
| Identity | accounts, roles, sessions |

Each module writes only its own schema through application contracts. Projections and caches are disposable derivatives. Cross-module contracts are versioned integration events or BuildingBlocks contracts — never direct table writes.

Checkout creates an order from server-side prices and stock, reserves stock transactionally, then drives a durable payment operation. **A timeout means unknown, not failed:** reconcile before retrying or releasing stock. Hosted provider UI keeps card data outside this system.

---

## 3. Workstreams

| Priority / stream | Objective and deliverables | Dependencies |
|---|---|---|
| **Must — Foundation** | Repair `app/Gateway`, payment operations, projections, webhook inbox; enforce real architecture rules in `tests/Architecture.Tests`. | Phase 0 before any new module |
| **Must — Commerce** | Add Catalog, Cart, Inventory, Orders under `src/*/Modules`; enforce publication, ownership, price snapshots, stock concurrency. | Foundation gates; market rules |
| **Must — Payments/checkout** | Bind `src/Application/Modules/Payments` to authoritative orders; durable operation identity, reconciliation, refunds, provider state mapping. | Phase 0 payment repair; Orders + Inventory |
| **Must — Notifications** | Repair `src/Infrastructure/BuildingBlocks/Adapters/Notifications` and BackgroundJobs: provider registration, consumed queues, delivery records, retry, DLQ. | Durable events; safe identity email; Orders |
| **Should — Search/ML** | Catalog-owned search port + PostgreSQL FTS adapter; optional HTTP ML boundary with timeout/fallback. | Published Catalog; projection guarantees |
| **Must — Frontend** | Accessible customer flows in `app/Web.Storefront/src`, authorized operations in `app/Web.Admin/src`; typed clients, forms, guards. | Gateway/auth contracts; API slices |
| **Must — Security** | Fix JWT lifecycle, grants, tenant claims, external linking, secrets, privacy. | Shared signing config; authorization matrix |
| **Must — Reliability/SRE** | Correct readiness, dead-letter visibility, telemetry, alerts, runbooks. | Foundation fixes; real environment |
| **Must — Data** | Version schemas, protect migrations, backups/PITR, retention, projection rebuild. | DB ownership and recovery decisions |
| **Must — Testing/QA** | Real Postgres, broker, provider-contract, and browser gates in `tests/`, `app/Web.*/e2e`, `make/`, CI. | CI runners with Docker and browsers |
| **Must — Docs/governance** | ADRs, ownership, threat model, authorization matrix, runbooks; correct `README.md`. | Named owners |
| **Should — DX** | Keep `make help/doctor/install/ci` and `guide/*.md` authoritative; fix sandbox config and deterministic fixtures. | Working gates; pinned toolchains |

---

## 4. Phase gates

No phase advances on elapsed time. **Phase 0 permits remediation and tests only — no new business module.**

| Phase | Scope | Required exit evidence |
|---|---|---|
| **0 — Stabilize** | Gateway, JWT/secrets, payment idempotency, projections, webhook durability. Disable real-money/public deployment until gates pass. | Edge tests prove login, protected access, denial; API/Gateway share an injected non-default key and reject unsafe non-development config; payment crash/retry shows one provider operation per durable operation; Postgres delayed-commit, crash/replay, multi-worker projection tests pass; webhook failure/redelivery survives restart. Q1–Q8 closed. |
| **1 — Foundation gates** | Security, tenant isolation, payment semantics, outbox, reconciliation, architecture, operational baselines. | Q9–Q23, Q25–Q32 have passing tests or disabled integrations; backup restore and migration drills pass; broker and architecture CI fail closed; signed authorization matrix and threat model. No real users before email/revocation fixes. |
| **2 — Catalog + cart** | Product publication, price, stock, cart, customer/admin interfaces. | Admin publishes a product; shopper finds it and mutates an owned cart through the Gateway; concurrent stock tests prevent oversell; contract and browser tests run in CI. No checkout accepting money. |
| **3 — Orders + checkout** | Order snapshots, inventory reservation, payment binding and recovery. | Browser test creates an order, pays in sandbox, handles retry/unknown, checks ownership; duplicates cannot create duplicate provider operations; failed/expired reservations recover. |
| **4 — Notifications + search** | Durable confirmations and identity email; catalog FTS; optional ML boundary. | Duplicate notifications controlled; outage visible via retry/DLQ; priority queues drain; reset links absent from logs; search returns only published products and meets baseline; ML outage does not block browse/checkout. |
| **5 — Hardening + scale** | Security review, performance, recovery, deployment, release rehearsal. | All release gates pass with no unexplained skips; load/failure drills meet SLOs; recovery, canary, rollback rehearsals succeed; owners sign off. |

---

## 5. Dependency graph and critical path

```text
P0 edge + secrets + payments + projections + webhook inbox
  └─> P1 identity/tenant + payment semantics + CI + recovery
       └─> P2 catalog/price + inventory + cart + browser contracts
            └─> P3 order snapshots + reservation + payment binding
                 └─> P4 durable confirmations + search
                      └─> P5 release and recovery proof
```

**Critical path:** payment/projection safety → authoritative catalog and stock → order snapshot/reservation → provider-safe checkout → recovery and release proof.

Independent Phase 0 fixes may be worked in parallel, but **no new module merges before all Phase 0 exits**. Do not parallelize checkout integration against unsettled order/payment contracts, or projection rebuild against an unfenced live writer. Notification templates can be designed alongside Orders, but production confirmation depends on both the order event contract and durable delivery.

---

## 6. Sequenced epic backlog

Sizes are relative effort, not calendar commitments. All are **Must** unless marked.

| ID / phase / size / lead | Scope and location | Observable exit |
|---|---|---|
| **E01 / 0 / M / edge** | Q1: `app/Gateway/Gateway.Router.cs`; preserve `/api`, order exceptions, anonymous auth, webhook. | `tests/Integration/AppHost.Integration.Tests` proves login, authenticated Todo, 401/403, payment routes through the edge. |
| **E02 / 0 / M / security** | Q2, Q27: `app/Web.Api/appsettings*.json`, `app/Gateway`, `compose.yaml`; remove keys, rotate credentials, reject unsafe defaults. | Startup fails with missing/default key outside Development; one injected key validates API tokens at the Gateway. |
| **E03 / 0 / XL / payments** | Q3, Q7–Q8: `src/Application/Modules/Payments` + Infrastructure stores. Persist operation ID, fingerprint, customer-scoped provider key **before** I/O; no provider call inside a DB transaction. | Concurrent duplicate, changed payload, crash-after-provider-success, retry → one provider operation and recoverable local outcome. |
| **E04 / 0 / XL / data** | Q4–Q5: event store and projections under `src/Infrastructure/BuildingBlocks/Adapters/Persistence`; fence writers and rebuild. | Delayed-commit test loses no event; apply+checkpoint commit together; replay and two workers produce identical shadow-rebuild checksum. |
| **E05 / 0 / L / payments** | Q6: `ProcessPaymentWebhook.cs` + webhook store: durable received/leased/processed inbox; only unique violations are duplicates. | Persist before acknowledging; failure after intake retries after restart; duplicate settles once; DB failure is not acknowledged. |
| **E06 / 1 / L / payments** | Q9–Q15, Q24: payment commands, adapters, `PaymentProjection`, reconciliation. Validate before I/O; handle cancel result, pending states, partial capture, precision, refunds/disputes, fair sweeps. | State-table/provider-contract tests cover each transition; sweeps reach newer rows; caller amount cannot pay an order. Disable fulfillment integration until Orders exist. |
| **E07 / 1 / L / security** | Q19–Q23, Q26: Security adapters + Identity endpoints; revocation on validation, atomic refresh, grant ceilings, tenant claims, no unsafe auto-link. | Logout/deactivation and concurrent-refresh tests pass; non-admin cannot grant Admin; unverified email cannot link; cross-tenant fails; no reset links in logs. |
| **E08 / 1 / L / reliability** | Q16–Q17: outbox dispatcher/store/health; lease duration/renewal, dead-letter visibility and replay. | Broker outage beyond retry window alerts; replay recovers rows; expired lease cannot mark another worker's claim; duplicates tolerated. |
| **E09 / 1 / L / platform** | Q28–Q30: `src/ServiceDefaults`, `compose.yaml`, migrations; readiness, payment meter, migration job, backups. | Unmigrated DB is unready; alerts receive payment/outbox signals; isolated restore and PITR drill meet RPO/RTO. |
| **E10 / 1 / M / QA** | Q31–Q32: `tests/Architecture.Tests`, `scripts/backend/test-broker.backend.sh`, CI. | Deliberate forbidden dependency fails architecture test; missing broker tests or Docker fails required CI instead of skipping. |
| **E11 / 1 / M / domain** | Q25: Todo handlers/cleaner. | Missing and cross-tenant references fail; cleanup waits for a safe projection watermark. |
| **E12 / 2 / L / commerce** | Proposed Catalog modules + `app/Web.Api/Endpoints/Catalog`; price, publication, admin authorization. | Invalid price/publication rejected; only published products visible; CRUD and customer contracts pass. |
| **E13 / 2 / L / commerce** | Proposed Cart + Inventory; ownership and atomic reservation. | Concurrent last-item attempts cannot oversell; cart mutation cannot cross customer/tenant. |
| **E14 / 2 / L / frontend** | `app/Web.Storefront/src`, `app/Web.Admin/src`: catalog, cart, product admin, accessible states. | Keyboard-accessible Gateway journey and typed-client integration tests pass; no fake navigation. |
| **E15 / 3 / XL / commerce + payments** | Proposed Orders + checkout; immutable totals, inventory holds, stable payment-operation linkage. | Price tampering and stale cart fail; retry/timeout, payment failure, reservation expiry, refunds preserve stock/money invariants. |
| **E16 / 3 / L / frontend + QA** | Checkout/order history + provider sandbox/browser tests. | Required CI runs purchase, decline, retry, ownership-denial through the Gateway with nonzero executed tests. |
| **E17 / 4 / L / platform** | Q18–Q19: notifications/background jobs; real email, delivery record, dedupe, queues, retry, DLQ. | Duplicate enqueue deduped; outage retries then alerts/DLQ; critical/high queues drain; identity and order email reach a test sink. |
| **E18 / 4 / L / search** | **Should:** Catalog search port + Postgres adapter. | Published-only full-text results and relevance fixtures pass; benchmark meets latency. |
| **E19 / 4 / M / search** | **Could:** optional ML HTTP adapter; no ML in critical path. | Stub contract, timeout, privacy review, ML-down fallback pass. |
| **E20 / 5 / L / SRE + QA** | `make/`, `scripts/infra/`, CI, `docs/architecture/`: load, failure, security, release drills. | Evidence packet: load, restore, migration, canary/rollback, incident exercises; owners sign release. |

---

## 7. Quality gates and module definition of done

For every module under `src/*/Modules` and its `app/Web.Api/Endpoints`:

- **Must:** `make lint`, `make structure.check`, `make convention.check`, `make build`, `make test`, `make coverage`; retain 90% line / 80% branch. Follow `guide/dotted-file-naming-guide.md`, `guide/code-comment-style-guide.md`, `guide/convention-gate-guide.md`; no new baseline exemptions.
- **Must:** unit-test invariants and failures; Postgres/broker integration for transactions and races; contract tests for HTTP/events/providers; required browser journeys through the Gateway. `make test.infra.broker`/`make test.all` can skip — CI must assert required test counts and zero infrastructure skips.
- **Must:** document ownership, authorization matrix, threat review, PII retention, migrations, rollback; test old/new compatibility and restore. Metrics, traces, alerts, runbook for every async failure path.
- **Must:** prove outbox/event atomicity inside and outside command transactions; require an ambient transaction or an equivalent atomic mechanism. Treat delivery as at-least-once.
- **Should:** property-based concurrency tests and load benchmarks where money, stock, or recovery is involved. `make ci.full` is wider, not proof while optional suites can skip.

---

## 8. Operational readiness and release

**Must:** separate local, CI, staging, production configuration. Local fake providers and seeded users must never be accepted as production defaults. Use production-like staging, managed secrets, TLS, restricted networks, immutable scanned images; pin actions and dependencies.

**Must:** run one locked migration job from the release artifact before API rollout, with backup/PITR verified; replicas do not race migrations. Use expand–migrate–contract changes compatible with both versions. Roll back images and flags first; do not blindly reverse destructive data migrations. Rehearse restore in isolation, then reconcile provider transactions made after the restored point before resuming payment writes.

**Must:** deploy flags off, canary a small cohort, observe checkout errors/provider unknowns/queue age/readiness, then expand or abort. Document ownership, escalation, key rotation, dead-letter replay, and restore. Require product, security, and SRE approval before real-money release.

---

## 9. Risk register and decision triggers

| Risk / priority | Mitigation and trigger |
|---|---|
| **Money duplication / Must** | Durable operation identity, provider-key retention, reconciliation, manual escalation after provider dedupe expiry. Never promise exactly-once external effects. |
| **Projection loss / Must** | Commit-safe publication + fenced atomic checkpoint. Benchmark serialized publication; prove rollback/gap behavior. Rebuild existing projections. |
| **Credential/privilege compromise / Must** | Rotate exposed keys, enforce revocation and grant ceilings, incident-test compromised sessions. |
| **Silent work loss / Must** | Alert on age, DLQ, retry exhaustion; rehearse replay with duplicate-safe consumers. |
| **Data loss / Must** | Encrypted off-host backups, WAL/PITR, restore drills, provider reconciliation after restore. |
| **Saga / Could** | Revisit when checkout spans independently committed inventory/fulfillment systems and compensation cannot be managed by durable operation state. |
| **CDC / Could** | Revisit when measured outbox polling cannot meet lag SLO or costs materially after tuning. |
| **Worker extraction / Could** | Revisit `app/Worker` only when measured contention or independent scaling persists; preserve leases and fencing. |
| **BFF / Could** | Revisit when client-specific aggregation or a second origin causes real contract/security complexity; a second origin alone needs CORS, not a BFF. |
| **Event-sourcing scope / Should** | ADR before event-sourcing another module. Prefer transactional tables for new modules unless history requirements justify streams. |

---

## 10. Success metrics

Proposed starting objectives; validate against traffic and provider contracts before enforcing. Rolling 30-day SLO windows.

- **Delivery / Should:** DORA frequency/lead time; change-failure < 15%; median restore < 1 hour. Baseline first.
- **Availability / Must:** Gateway browse and checkout ≥ 99.9%, provider failures reported separately. Alert on burn rate.
- **Latency / Should:** p95 catalog reads < 500 ms; p95 local checkout initiation < 1 s excluding provider wait.
- **Financial correctness / Must:** zero unexplained duplicate provider operations or fulfilled underpayments; 100% of unknown outcomes visible and reconciled/escalated within the operating window.
- **Async / Must:** 99% of outbox/webhook work within 5 minutes; immediate alert on dead letters and oldest pending age.
- **Recovery / Must:** Postgres RPO ≤ 15 minutes, RTO ≤ 4 hours including provider reconciliation; timed restore drill.
- **Product / Should:** catalog→cart and checkout completion, decline vs technical failure, notification delivery, search zero-result rate; set targets after baseline without unnecessary PII.

---

## 11. Explicit non-goals for the next two quarters

- No microservices rewrite, sharding, mandatory CDC, general saga framework, BFF, or worker extraction without the triggers above.
- No mandatory ML, vector database, image search, or personalization dependency for checkout.
- No multi-merchant marketplace, multi-region active-active, multi-currency conversion, or broad international tax/shipping expansion before the first market is safe.
- No event-sourcing every aggregate, no new frontend state framework by default, no replacing primitives for fashion.
- No claim of production readiness from `make ci`, coverage percentages, or a green skipped suite alone.

---

## 12. Where to go next

| Document | Purpose |
|---|---|
| `docs/architecture/guides/foundation-implementation.md` | File-level execution of Phase 0/1 (F1–F11) |
| `docs/architecture/guides/modules-implementation.md` | File-level execution of Phase 2–4 modules (M1–M9) |
| `docs/architecture/guides/experience-edge-ops.md` | Frontend, edge contract, CI/CD, SLOs, runbooks |
| `docs/architecture/software-quality-audit.pro.md` | Evidence for Q1–Q32 |
| `docs/architecture/roadmap-implementation-audit.pro.md` | Concept coverage vs the learning roadmap |
