# Software Quality Audit — Pro Edition

**Version:** 1.0
**Audit date:** 2026-10-31
**Repository:** `QingFa.Shop`
**Scope:** whole software system — architecture, correctness, security, reliability, performance, operations, frontend, testing, maintainability. This is **not** limited to the learning roadmap.
**Mode:** five independent clean-context subagent audits + parent verification of every Critical/High claim.
**Relationship to prior work:** complements `roadmap-implementation-audit.pro.md`. That document asks *"is the roadmap concept implemented?"*; this one asks *"would this software survive production?"* They reach different conclusions because they measure different things.

---

## 0. Model selection, cost, and effort provenance

### 0.1 Selection policy

The requirement was: **most-frontier model, high effort, cheapest among that tier.**

Definitions used, so the choice is reproducible:

1. **Frontier** = current-generation flagship `*Pro` / Opus / Gemini Pro families. Excluded: `nano`, `mini`, dated snapshots (e.g. `0813`), efficiency-tier aliases (`Luna`), and non-flagship vendors' mid-tier `Pro` names.
2. **High effort** = the model must expose a `high` reasoning variant.
3. **Cheapest** = lowest blended cost at a 3:1 input:output ratio, with cache-read price as tiebreaker.

### 0.2 Candidates considered (USD per 1M tokens)

| Model | Generation | Effort variants | Input | Output | Cache read | Blended 3:1 | Decision |
|---|---|---|---|---|---|---|---|
| **GPT-6.1 Terra Pro** | 6.1 (current) | low, medium, **high**, xhigh, max | **1.00** | **5.00** | **0.05** | **2.00** | **Selected** |
| DeepSeek V4 Pro | V4 (current) | none, **high**, xhigh | 1.60 | 3.20 | 0.135 | 2.00 | Runner-up; higher cache cost, older generation |
| DeepSeek V4 Pro 0813 | V4 snapshot | low, **high**, max | 0.66 | 1.98 | 0.022 | 0.99 | Excluded: dated snapshot, not current frontier |
| GPT-6.1 Sol Pro | 6.1 | low, medium, **high**, xhigh, max | 2.00 | 10.00 | 0.10 | 4.00 | More expensive sibling |
| Gemini 3.1 Pro Preview | 3.1 | low, medium, **high** | 2.00 | 12.00 | 0.20 | 4.50 | More expensive |
| Kimi K3.1 Pro | 3.1 | none, low, **high**, max | 2.00 | 10.00 | 0.20 | 4.00 | More expensive |
| Claude Opus 5.5 | 5.5 | low, medium, **high**, xhigh, max | 4.00 | 20.00 | 0.20 | 8.00 | More expensive |
| GPT-6 Astra Pro | 6 | low, medium, **high**, xhigh, max | 10.00 | 50.00 | 1.00 | 20.00 | Most expensive |
| GPT-6 Luna Pro | 6 | low, medium, **high**, xhigh, max | 0.10 | 0.50 | 0.01 | 0.20 | Excluded: efficiency tier, not flagship |
| Nex-N2.5-Pro | 2.5 | medium, **high** | 0.075 | 0.25 | 0.015 | 0.12 | Excluded: not frontier tier |

**Selected model:** `openrouter/openai/gpt-6.1-terra-pro#high`
**Effort:** `high` (explicit variant; the model also supports xhigh/max, deliberately not used because the requirement was high effort at lowest cost).
**Why:** it is the newest-generation flagship Pro model with a `high` variant, and it is the cheapest in that set on blended cost, winning the tie with DeepSeek V4 Pro on cache-read price and generation.

**Transparency caveat:** if "frontier" is read to include efficiency-tier `Luna Pro` or the dated `0813` snapshot, those are cheaper. They were excluded by the policy above, not overlooked.

### 0.3 Subagent roster and estimated spend

| Agent | Dimension | Model | Effort | Session |
|---|---|---|---|---|
| A | Architecture and code quality | GPT-6.1 Terra Pro | high | `ses_e6ee76bcdffez26tQYyh0uoCtP` |
| B | Correctness and data integrity | GPT-6.1 Terra Pro | high | `ses_e6ee76bcaffeP64md1XHqdN51Y` |
| C | Security and privacy | GPT-6.1 Terra Pro | high | `ses_e6ee76bc7ffervkY5oCiIqGJb0` |
| D | Reliability, performance, operations | GPT-6.1 Terra Pro | high | `ses_e6ee76bc4ffe2g3racV7GYFoFW` |
| E | Frontend, testing, maintainability | GPT-6.1 Terra Pro | high | `ses_e6ee76bc1ffeBL0DPMd4eUiX13` |

Estimated usage: ~150k–200k input tokens, ~20k–25k output, ~20k cache reads. Estimated cost at the selected rates: **≈ US$0.28–0.40 total** (0.2 × $1.00 + 0.025 × $5.00 + 0.02 × $0.05). Exact billing is provider-side; token counts were not exposed to this session.

### 0.4 Method and limits

- Read-only static review. No containers, databases, brokers, browsers, or load tests were run.
- Subagents received the repository path and their dimension charter only. They were explicitly told to ignore previous audit documents.
- Parent verification re-read the decisive files for every Critical and High finding; the **Verification** column records the result.
- "Conditional" means the defect depends on configuration or an integration not present in the repo (e.g. enabling an external provider, adding order fulfillment).

---

## 1. Verdict

### 1.1 Overall: **D — not production-ready**

The platform is thoughtfully structured and contains genuinely strong reliability primitives. It is nevertheless **not safe to operate as-is**, because the browser-facing edge is misrouted, payment retries can duplicate money movement, projections can silently lose events, and the deployed Compose stack is a development configuration with split signing keys.

### 1.2 Scorecard

| Dimension | Score | One-line reason |
|---|---|---|
| Architecture and code quality | **C−** | Good layering and DI; edge/API contract drift and external I/O in transactions. |
| Correctness and data integrity | **D** | Payment retry/idempotency and projection replay defects can corrupt financial state. |
| Security and privacy | **F** | Committed signing key; revocation not enforced; reset links logged; privilege escalation path. |
| Reliability, performance, operations | **D** | Silent terminal failures, unsafe deployment defaults, no backup/restore. |
| Frontend, testing, maintainability | **D** | Shell UI; gateway and browser paths untested; vacuous architecture tests; docs drift. |
| **Overall** | **D** | Strong primitives, unsafe integration and operational defaults. |

### 1.3 The five root causes

1. **Edge/API contract drift.** The gateway rewrites `/api/{remainder}` to `/{remainder}`, but the API maps `/api/...` for every route except the specially listed ones. Authentication is also challenged at the edge before reaching anonymous endpoints. Direct API tests cannot see this.
2. **Remote side effects inside database transactions.** Payment provider calls happen inside the command transaction; idempotency reservations are rolled back with it; provider keys are not stable per operation.
3. **The projection pipeline has no commit-safe cursor, no atomic checkpoint, and no replay fence.** Events can be skipped permanently, applied twice, or double-counted.
4. **Silent failure paths.** Webhook dedupe before settlement, notifications on unconsumed queues, outbox dead letters counted as healthy, identity emails logged instead of sent.
5. **Unsafe configuration defaults.** Compose runs `Development` with fake payments, split JWT keys, seeded credentials, and no backups; base config contains a signing key.

---

## 2. Consolidated findings register

Severity: **Critical** = data loss, money loss, or full compromise; **High** = feature outage or realistic corruption; **Medium** = conditional or degradation; **Low** = hygiene.
Verification: **V** = parent re-read the decisive code; **C** = corroborated by two agents and paths exist; **Cond** = conditional on config/integration.

### 2.1 Critical

| ID | Finding | Evidence | Verification | Impact | Fix |
|---|---|---|---|---|---|
| Q1 | **Gateway breaks every non-payment API route.** `/api/{**remainder}` strips `/api`, but API routes are mapped at `/api/todos`, `/api/todo-categories`, `/api/auth`, `/api/identity/*`. Anonymous auth routes are also forced through `ProtectedApiPolicy`, so login/register get 401 at the edge. | `app/Gateway/Gateway.Router.cs:68-74`; `app/Web.Api/Endpoints/Todos/Todo.Endpoint.cs:21`; `Identity.Endpoint.cs:32-40` | **V** | The product does not work through its own gateway. Only `/api/time`, payments, and the webhook have correct routes. | Add explicit anonymous auth routes and prefix-preserving routes for all API groups; add edge regression tests for login + authorized todo + denial. |
| Q2 | **Committed JWT signing key, and gateway/API defaults differ.** Base configs contain a known HMAC key; Compose gives the gateway `${JWT_KEY:-qingfa-compose-only...}` while the API uses `appsettings.json`'s `qingfa-scaffold-only...`. | `app/Web.Api/appsettings.json:9-14`; `app/Gateway/appsettings.json:9`; `compose.yaml:12,49-53` | **V** | Forged Admin tokens if deployed with defaults; API-issued tokens fail edge validation in Compose. | Remove keys from base config; fail startup outside Development; inject one high-entropy key into both services; rotate. |
| Q3 | **Refund idempotency is ineffective.** Each attempt generates `refundId = Guid.NewGuid()` and derives the provider key from it, ignoring the caller's idempotency key. | `src/Application/Modules/Payments/Refunds/Commands/RefundPayment.cs:63,129-135` | **V** | Provider success + local commit failure + retry can refund twice. | Persist a stable operation ID before I/O; derive provider key from caller key + payment; reconcile unknown outcomes. |
| Q4 | **Projection cursor is not commit-ordered.** Position is a PostgreSQL identity allocated at insert; a later transaction can commit first, the checkpoint advances, and an earlier commit becomes invisible. | `EventStoreEvent.Configuration.cs:17-19`; `PostgresEventStore.cs:79,137`; `Projection.Processor.cs:24-53` | **V** (structurally) | Silent, permanent loss of read-model updates. | Use a commit-safe log cursor (gap detection/serialized publication) and add a delayed-commit test. |
| Q5 | **Projection apply and checkpoint are not atomic or replay-safe.** `PaymentProjection.ApplyAsync` saves per event; the checkpoint is written afterwards in a separate commit. `PaymentInitiated` inserts unconditionally and refund totals increment. | `PaymentProjection.cs:30-40,163,191`; `Projection.Processor.cs:35-53` | **V** | Crash/retry duplicates keys or double-counts refunds; multiple replicas race. | Apply + checkpoint in one transaction, fence workers, dedupe by stream/version, rebuild into a shadow table. |
| Q6 | **Webhook dedupe precedes settlement and suppresses retries.** `TryRecordAsync` runs first; any later settlement failure is marked processed; redelivery is then a duplicate. Any `DbUpdateException` is also treated as duplicate. | `ProcessPaymentWebhook.cs:85-127`; `WebhookEventStore.cs:21-48` | **V** | A transient failure permanently discards a valid payment settlement. | Durable inbox with processing/lease state; only unique violations are duplicates; retry incomplete deliveries. |

### 2.2 High

| ID | Finding | Evidence | Verification | Impact |
|---|---|---|---|---|
| Q7 | Payment creation reserves idempotency and calls the provider inside the command transaction; rollback loses the reservation and a retry uses a new payment ID with the same provider key. | `CreatePayment.cs:113-152`; `PaymentIdempotencyStore.cs:17-37`; `Transaction.Behavior.cs:35-55`; `StripePaymentGateway.cs:63-79` | **V** | Orphaned provider intent; retry may conflict on changed parameters. |
| Q8 | Provider idempotency keys are not customer-scoped; local uniqueness is `(CustomerId, Key)`. | `PaymentIdempotencyKey.Configuration.cs:22-29`; `CreatePayment.cs:141-152` | **V** | Two customers reusing a key collide at the provider. |
| Q9 | Capture validates after provider I/O. | `CapturePayment.cs:80-106`; `Payment.cs:202-221` | **C** | Provider captures funds the domain later rejects. |
| Q10 | Cancel ignores `PaymentCancelResult.Succeeded`, treating any transport success as cancellation. | `CancelPayment.cs:73-82` | **V** | Local cancelled, provider still payable. |
| Q11 | Dispute/refund webhook fields are dropped; settlement ignores refund observations; Stripe switch has no dispute events. | `ProcessPaymentWebhook.cs:119-127`; `SettlePayment.cs:90-95,127-140`; `StripePaymentGateway.cs:184-197` | **C** | Disputes and externally initiated refunds are not recorded. |
| Q12 | PayPal rounds amounts for the wire while the aggregate keeps the original precision. | `MoneyConverter.cs:55-61`; `PayPalPaymentGateway.cs:51-55,181-187` | **C** | Provider/local amounts diverge; over-precision accepted locally. |
| Q13 | Partial capture: aggregate stays `Authorized`, projection says `Captured`; validator compares against requested, not authorized, remainder. | `Payment.cs:331-340`; `PaymentProjection.cs:61-71`; `Payment.Validator.cs:46-63` | **C** | Wrong status and potential over-capture. |
| Q14 | Confirm maps `RequiresAction`/`Processing` to `Failed`. | `ConfirmPayment.cs:95-101` | **C** | A live intent is marked failed and leaves reconciliation. |
| Q15 | Reconciliation is disabled by default and starves: it always selects the oldest open rows; unchanged rows stay at the front; failed sweeps are discarded. | `appsettings.json:138`; `PaymentReadStore.cs:126-144`; `PaymentReconciliation.Worker.cs:65-70,98-103,143-149` | **C** | Missed webhooks remain unresolved; newer payments never checked. |
| Q16 | Outbox dead-letter is terminal, and health ignores dead letters (counts pending only). | `Outbox.Dispatcher.cs:145-151`; `Outbox.Health.Monitor.cs:41-56` | **V** | Broker outage > ~20 min loses events while health stays green. |
| Q17 | Outbox leases 60 s for up to 100 serially published rows; later rows can publish after lease expiry and fail to mark. | `Outbox.Dispatcher.cs:51-84`; `Outbox.Store.cs:103`; `appsettings.json:82-86` | **C** | Duplicate publishes and retry churn. |
| Q18 | Notification runtime is broken: only an SMS logging provider is registered; providers use `TryAddScoped`; templates enqueue to `critical`/`high` but Hangfire consumes only `default`; `RetryCount` is never read. | `Notification.Extension.cs:50-53`; provider `*.Extension.cs:10-13`; `Background.Extension.cs:19,88`; `Notification.Service.Mapper.cs:78-85` | **V** | Email never sends; most queued notifications never run; no retry. |
| Q19 | Identity email is unconditionally a logging stub that writes reset/confirmation links and addresses at Information level. | `Authentication.Composition.cs:136-140`; `IdentityEmail.Service.cs:12-28` | **V** | Account takeover via log readers; no delivery. |
| Q20 | Logout blacklist is not consulted during JWT validation; refresh does not recheck active/locked; deactivation does not revoke sessions. | `Authentication.Composition.cs:148-166` (no `OnTokenValidated`); `IdentityService.cs:196,243`; `UserAccountService.cs:221` | **V** | Stolen access/refresh tokens survive logout and deactivation. |
| Q21 | Refresh rotation is not concurrency-safe: read-then-write without conditional update or row lock. | `TokenRotation.Service.cs:30-49`; `EfCoreRefreshToken.Store.cs:48` | **C** | Two concurrent redemptions both succeed, defeating reuse detection. |
| Q22 | External identities are linked by email without verified-email evidence or local confirmation. | `ExternalPrincipal.Mapper.cs:24`; `ExternalAuthentication.Service.cs:60` | **C** (providers disabled by default) | Account takeover if a provider asserts an unverified address. |
| Q23 | A user manager can assign the Admin role (no grant ceiling, no self-escalation check); Admin bypasses permission checks. | `Identity.User.Endpoint.cs:92`; `UserAccountService.cs:245`; `Permission.Requirement.Handler.cs:55` | **V** | Privilege escalation to full access. |
| Q24 | Payment amount and external order reference are caller-supplied; no authoritative order binding. | `PaymentEndpointModel.cs:6`; `CreatePayment.cs:83-100` | **C** (integration-dependent) | Underpayment against a later order fulfillment flow. |
| Q25 | Todo list/category references are not validated for existence or tenant; cleanup relies on possibly stale projections. | `CreateTodo.cs:56-68`; `AssignTodoToList.cs:59-65`; `TodoReferenceCleaner.cs:38-84` | **C** | Dangling and cross-tenant references. |
| Q26 | Issued JWTs carry no tenant claim (`TenantId: null`); `TenantContext` falls back to a role claim then empty; Todo handlers reject empty tenants. | `IdentityTokenIssuer.cs:28`; `TenantContext.Context.cs:18`; `TenantContext.Extension.cs:11` | **V** | Todo API is unusable with normally issued tokens (independent of Q1). |
| Q27 | Compose runs `ASPNETCORE_ENVIRONMENT=Development` with fake payments; the Fake validator only rejects in the exact `Production` name; development EF sensitive logging is active. | `compose.yaml:11,49`; `PaymentsOptionsValidator.cs:23`; `appsettings.Development.json:12` | **V** | Unsafe deployment if used beyond local development. |
| Q28 | Health/readiness and migration semantics are inconsistent: Compose probes `/alive` (Development-only), migrations are off by default in Compose but on in AppHost, and outbox degradation maps to HTTP 200. | `compose.yaml:42,57`; `Infrastructure.Composition.cs:43`; `AppHost.cs:27`; `ServiceDefaults.Extension.cs:112-131`; `HealthChecks.Extension.cs:42-55` | **C** | Unmigrated instances receive traffic; production probes fail. |
| Q29 | No off-host backup/restore; single local PostgreSQL volume; no RPO/RTO. | `compose.yaml:160-213` | **C** | Host/volume loss loses the event store. |
| Q30 | Payment metric meter is never subscribed by OpenTelemetry; export is conditional on an endpoint not configured in Compose. | `PaymentMetrics.cs:12,20`; `ServiceDefaults.Extension.cs:58-90` | **V** | Payment failures/latency invisible. |
| Q31 | Architecture tests are partly vacuous; the real boundary scan checks signatures but not bodies/generic arguments. | `LayerArchitecture.Tests.cs:57-67`; `VerticalSliceArchitecture.Tests.cs:19-28`; `BuildingBlockBoundary.Tests.cs:80-110` | **V** | False assurance; cross-module drift enters green. |
| Q32 | Broker CI gate is fail-open: it skips with exit 0 if test class names change, and container tests skip without a runtime. | `test-broker.backend.sh:9-19`; `RabbitMq.Fixture.cs:65-74` | **V** | PR can be green without exercising RabbitMQ. |

### 2.3 Medium

| ID | Finding | Evidence | Verification |
|---|---|---|---|
| Q33 | Permission cache stores partial failures as empty permission sets, denying users and losing invalidation tags. | `Permission.Service.cs:47-59,80-108` | C |
| Q34 | Payment reference store accepts an existing mapping without verifying ownership and swallows all `DbUpdateException`s. | `PaymentReferenceStore.cs:21-45` | C |
| Q35 | Cancellation is inconsistently converted to failure (repositories, SMTP) vs rethrown (transaction behavior). | `EfCoreRepository.Read.cs:20-30`; `Smtp.Provider.cs:104-122` | C |
| Q36 | PayPal singleton resolves a typed `HttpClient` once and mutates token state without synchronization. | `Modular.Infra.Composition.cs:102-117`; `PayPalTokenProvider.cs:17-49` | C |
| Q37 | Validation behavior runs all validators concurrently against one `ValidationContext`. | `Validation.Behavior.cs:34-37` | C |
| Q38 | Public mutable collections in static constants/notification templates. | `Result.Constant.cs:96-99`; `Result.cs:40-45`; `NotificationStore.Template.Enum.cs:5-9` | C |
| Q39 | Test factories mutate process-wide environment variables without restoring them. | `ShopApi.Factory.cs:37-47`; `ResultContractStack.Fixture.cs:68-77` | C |
| Q40 | Sandbox test env vars do not match provider configuration keys; either provider satisfies both guards. | `test-payments-sandbox.backend.sh:8-12`; `Payments.Sandbox.Test.cs:24-45,77-87` | C |
| Q41 | Webhook body is buffered before the size limit is enforced. | `PaymentWebhookEndpoint.cs:56` | C |
| Q42 | External exchange codes are read and removed in two steps (not atomic), and the cache holds token pairs. | `ExternalCode.Store.cs:37,60` | C |
| Q43 | Retention gaps: dead letters and inbox markers are never pruned; event store grows append-only; no growth alert. | `Outbox.Store.cs:234`; `Inbox.Store.cs:82`; `PostgresEventStore.cs:58` | C |
| Q44 | Deep offset pagination and missing composite index for customer payment listing; `Payment` is not snapshotable, so loads are O(events). | `PaymentReadStore.cs:70,113`; `PaymentReadModel.Configuration.cs:43`; `EventSourcedRepository.cs:38,125` | C |
| Q45 | Health chip calls the gateway's own `/health`, not the API; in non-Development it is unmapped and the SPA catch-all can return HTML 200. Health is fetched twice with no timeout/cancellation. | `health.ts:3-5`; `App.vue:4-7`; `HomeView.vue:3-6`; `useApiHealth.ts:10-20` | C |
| Q46 | Sandbox/deployment docs use env names that do not match bound configuration (`JWT_SECRET` vs `Jwt:Key`). | `README.md:271-280`; `appsettings.json:9-14` | C |
| Q47 | Mutable GitHub Action tags and base-image tags in the publishing workflow. | `.github/workflows/cd.yml:92-111`; Dockerfiles | C |
| Q48 | Dynamic health state has no live-region announcement; placeholder nav is presented as navigation. | `Web.Admin/src/App.vue:14-26`; `HomeView.vue:10-12` | C |

### 2.4 Low

- Docs claim product/checkout screens that do not exist (`app/Web.*/README.md` vs route tables).
- `docs/specs` link points at an archived path (`README.md:203-210`).
- Architecture README overstates what the tests enforce.
- Unused/duplicate health endpoints (`/health`, `/alive`, `/health/live`, `/health/ready`).
- `[ExcludeFromCodeCoverage]` on projections/rebuild.

---

## 3. Dimension deep dives

### 3.1 Architecture and code quality — C−

**Genuinely good:** inward project references; explicit composition roots; scoped workers; options validated at startup; nullable + warnings-as-errors; lease-based outbox; `Result`-based core.

**Structural problems:**

1. **The edge and the API disagree about paths and anonymity.** This is not a style issue; it is a contract defect (Q1). The route table documents ordering carefully but not prefix preservation, and the only edge tests cover `/api/time` and payments.
2. **Remote calls inside transactions.** `TransactionBehavior` wraps the whole command, including Stripe/PayPal calls (`Q7`, `Q9`, `Q12`). This couples money movement to database rollback, which cannot work.
3. **Provider identity is generated per attempt** (`Q3`, `Q7`). Idempotency exists locally but not at the provider boundary.
4. **Notification composition cannot express multiple providers.** All provider extensions use `TryAddScoped<INotificationProvider>`, so the first wins and fallback is impossible (`Q18`).
5. **Architecture enforcement is partial** (`Q31`). Only the building-block signature scan and convention tests are executable; layer and slice rules are prose.

**As the system grows:** add `Catalog`/`Orders` only after fixing Q1/Q3/Q5, or the new modules will inherit the same edge and projection defects.

### 3.2 Correctness and data integrity — D

**Genuinely good:** closed payment status enum; aggregate-level refund rules; expected-version stream appends; unique stream/version; inbox transaction; Todo projection version guard.

**Failure clusters:**

1. **Money movement is not recoverable.** Refund (Q3), create (Q7), capture (Q9), cancel (Q10), amount rounding (Q12), partial capture (Q13). The common pattern is external I/O before a durable local decision.
2. **Projection correctness** (Q4, Q5). The pipeline assumes events arrive in commit order and that applying an event is idempotent. Neither holds for PostgreSQL identity positions or unconditional inserts.
3. **Webhook durability** (Q6, Q11). Recording before processing turns transient failures into permanent acknowledgements.
4. **Domain state can be bypassed.** Some Todo entities expose public setters; snapshots restore without invariant validation; `Payment` is not snapshotable.
5. **Tests give false confidence.** Refund tests mock the repository and gateway; projection tests run SQLite once; concurrency/commit-failure paths are untested.

**The one invariant to enforce first:** *a caller's idempotency key must map to exactly one provider operation and one local outcome, independent of retries.*

### 3.3 Security and privacy — F

**Genuinely good:** API validates signature/issuer/audience/lifetime; refresh tokens hashed; permission matching supports Any/All and denies on failure; customer payment reads enforce ownership; Stripe signature tolerance; webhook event unique index; central package management.

**Why F:**

1. **Forgery risk** (Q2): a known key in base config, plus split keys in Compose. If deployed unchanged, Admin tokens are forgeable; if deployed as Compose, legitimate tokens fail at the edge.
2. **Session lifecycle is incomplete** (Q20, Q21): revocation is written but never read; refresh ignores lockout; rotation races.
3. **Account takeover surfaces** (Q19, Q22): reset links logged; external link by unverified email.
4. **Privilege escalation** (Q23): delegated user admins can grant Admin.
5. **Deployment insecurity** (Q27, Q29): Development mode and fake payments in the supplied stack, no backups.

Dependency scans returned no known production advisories at review time (NuGet + pnpm).

### 3.4 Reliability, performance, operations — D

**Genuinely good:** outbox leasing and backoff; publisher confirms; inbox transaction; four workers with scope-per-tick; correlation IDs; dependency health checks.

**Operational reality:**

- **Terminal loss is normal.** Outbox dead letters after ~20 minutes of broker outage and health stops counting them (Q16); webhooks are acked before settlement (Q6); notifications strand on unconsumed queues (Q18).
- **Readiness lies.** Compose probes liveness; `/alive` exists only in Development; migrations differ between AppHost and Compose (Q28).
- **Observability is incomplete.** Payment meters are unsubscribed (Q30); no outbox lag/dead-letter, projection lag, reconciliation age, or notification metrics; no dashboards/SLOs.
- **Capacity:** 500 events × 2 s per projection, per-event saves, offset paging, O(events) Payment loads, append-only event store.
- **Recovery:** no off-host backup, no replay of dead letters, no projection repair tool beyond a rewind.

### 3.5 Frontend, testing, maintainability — D

- **Frontend is a shell.** Two views per app, one composable, one health call; no feature modules, Pinia, guards, forms, or product UI. Health is fetched twice and can report a false green (Q45).
- **Tests are real but narrow.** Unit/API/infra suites are substantive. Gaps: no required browser path, no contract tests, no property-based, no load/chaos, vacuous architecture tests, fail-open broker gate, skip-without-Docker behavior, env-mutating fixtures.
- **Docs drift:** README describes product screens that do not exist; env var names do not match configuration; architecture README overstates enforcement.

---

## 4. What is genuinely good (do not lose this)

1. **The outbox/inbox design is better than most production systems.** `FOR UPDATE SKIP LOCKED`, lease fencing, exponential backoff, dead-lettering, transactional inbox markers.
2. **Event sourcing is applied with expected-version concurrency and snapshot-aware reads.**
3. **Composition roots and DI are disciplined**; options are validated at startup; workers scope correctly.
4. **Conventions are enforced by tooling** (naming, outcome shape, monadic results, labeled comments), which makes remediation cheap and consistent.
5. **CI is thoughtfully tiered** with coverage gates and pinned toolchains.
6. **Payment provider adapters are properly isolated** behind `IPaymentGateway`; the Fake gateway supports scripted failures and webhook signatures.

---

## 5. Remediation plan

### Now (before any further feature work)

| Order | Action | Findings |
|---|---|---|
| 1 | Fix gateway routes: explicit anonymous `/api/auth/*`, preserve `/api` for all API groups, add edge tests for login, authorized todo, denial. | Q1 |
| 2 | Remove scaffold keys from base config; require one injected key for API and gateway; fail startup on defaults outside Development. | Q2, Q27 |
| 3 | Make payment operations durable: stable operation ID before I/O, provider key = f(caller key, customer, operation), reconcile unknown outcomes. | Q3, Q7, Q8 |
| 4 | Fix projection cursor, atomic apply+checkpoint, replay idempotency, single-writer fencing. | Q4, Q5 |
| 5 | Make webhook processing an inbox: record, settle, mark in a durable state machine; retry incomplete deliveries; only unique violations are duplicates. | Q6, Q11 |
| 6 | Fix cancel/capture/confirm ordering and provider-result checks. | Q9, Q10, Q14 |
| 7 | Wire notifications: register providers with `TryAddEnumerable`, consume all priority queues, implement or delete `RetryCount`, persist delivery status, add dedupe/DLQ. | Q18 |
| 8 | Replace the logging identity email in production; stop logging reset links. | Q19 |

### Next 30 days

| Action | Findings |
|---|---|
| Enforce revocation on bearer validation; revoke on deactivation/password/role change; make refresh consumption atomic. | Q20, Q21 |
| Block privileged-role assignment by non-privileged user managers; add grant ceilings. | Q23 |
| Require verified email for external linking; disable auto-link until then. | Q22 |
| Backup/restore + WAL/PITR with a rehearsed drill; define RPO/RTO. | Q29 |
| Consolidate health endpoints; run migrations as a gated job; set readiness mappings. | Q28 |
| Add outbox/projection/reconciliation/notification metrics + alerts; subscribe the payment meter. | Q30, Q16 |
| Fix reconciliation fairness and enable it where real providers run. | Q15 |
| Make broker CI fail closed; replace vacuous architecture tests with IL-based rules. | Q31, Q32 |
| Fix PayPal money rounding and partial-capture semantics. | Q12, Q13 |

### Next 90 days

- Tenant claim issuance + cross-tenant isolation tests (Q26).
- Todo reference validation and projection-watermark cleanup (Q25).
- Retention/archival for inbox, dead letters, event store (Q43).
- Keyset pagination and indexes for payment listing (Q44).
- Required Playwright smoke through the gateway; contract tests; load baseline.
- Worker extraction only if scaling requires it.
- Replace offset-only UI health with a deliberate readiness contract (Q45).

### Do not do yet

- Saga/process manager, CDC, sharding, BFF. They add failure modes before the current ones are fixed.

---

## 6. Appendix

### 6.1 Verification commands used on contested claims

```bash
# Q1 gateway vs API paths
rg -n "BasePath|StripPrefix" app/Web.Api/Endpoints app/Gateway/Gateway.Router.cs

# Q2/Q27 config and compose
rg -n "Jwt__Key|JWT_KEY|ASPNETCORE_ENVIRONMENT|Payments" compose.yaml
sed -n '1,20p' app/Web.Api/appsettings.json

# Q3 refund identity
sed -n '55,140p' src/Application/Modules/Payments/Refunds/Commands/RefundPayment.cs

# Q4/Q5 projections
sed -n '1,80p' src/Infrastructure/BuildingBlocks/Adapters/Persistence/EntityFramework/Projections/Projection.Processor.cs
rg -n "SaveChangesAsync|Add\(|Increment" src/Infrastructure/Modules/Payments/Persistence/PaymentProjection.cs

# Q6 webhook
sed -n '70,130p' src/Application/Modules/Payments/Webhooks/Commands/ProcessPaymentWebhook.cs

# Q18 notifications
rg -n "TryAddScoped<INotificationProvider>|Queues|RetryCount" src/Infrastructure/BuildingBlocks/Adapters/Notifications src/Infrastructure/BuildingBlocks/Adapters/BackgroundJobs

# Q20 revocation
rg -n "OnTokenValidated|ITokenBlacklist" src/Infrastructure/BuildingBlocks/Adapters/Security/Authentication

# Q30 metrics
rg -n "AddMeter|PaymentMetrics" src/ServiceDefaults src/Application/Modules/Payments
```

### 6.2 Static-review limitations

No exploit was executed, no container was started, and no load was generated. Findings marked **C** or **Cond** need a runtime reproduction before external disclosure. The Critical set (Q1–Q6) was verified by directly reading the decisive code paths.

### 6.3 Related documents

| Document | Purpose |
|---|---|
| `docs/architecture/software-quality-audit.pro.md` | This document: production readiness across all dimensions |
| `docs/architecture/roadmap-implementation-audit.pro.md` | Concept coverage against the 14-week roadmap |
| `docs/architecture/roadmap-implementation-audit.md` | v1.0, superseded |

**Bottom line:** the primitives are good enough to build on; the integrations are not. Fix Q1–Q6 before adding any new business module, and Q2/Q19/Q20/Q23 before exposing the system to real users.
