# Foundation & Building-Block Implementation Guide

**Version:** 1.0
**Date:** 2026-10-31
**Author model:** DeepSeek V4 Pro (high effort)
**Scope:** platform/foundation only (F1–F11), in dependency order. No new commerce modules.
**Ground truth:** `docs/architecture/software-quality-audit.pro.md` (Q1–Q32), `docs/architecture/roadmap-implementation-audit.pro.md`, `AGENTS.md`, `guide/*.md`.
**Prerequisite for:** `docs/architecture/guides/modules-implementation.md` and `docs/architecture/guides/experience-edge-ops.md`.

---

## 0. Rules that apply to every item

1. **No second reliability mechanism.** Every change rides the existing event-store/outbox/inbox substrate. No new broker, no CDC, no competing dispatcher, no Wolverine transactional outbox.
2. **Modular monolith.** No new deployable except the optional `app/Web.Worker`; do not extract it until measurement justifies it.
3. **Additive, reversible.** Every schema change is a forward migration with a documented rollback. Every behavior change is feature-flagged.
4. **Conventions.** `Error.*` factories live in the owning `*Outcome.cs`; magic numbers ≥ 10 and wire-shaped strings live in `*Constant.cs`; public members of `*.Service.cs`/`*.Interface.cs`/`*.Abstract.cs` return `Result`/`Result<T>`; comments use the labeled vocabulary; dotted role suffixes are mandatory.
5. **Evidence.** Each item ends with a binary acceptance check that runs in `make test` or `make test.all`.

### Dependency graph

```text
F1 → F2 → (F3, F4, F5) → F6 → F7 → F8 → F9 → F10 → F11
```

F3–F5 are independent of each other and share the event-store/outbox substrate. F9 consumes meters added in F3/F6/F7. F10 gates everything. F11 can run beside F9–F10 once F3/F4 land.

---

## F1 — Edge/API contract repair (Q1)

**Invariant:** for every API route, the path at the gateway equals the path the API maps; an endpoint marked `.AllowAnonymous()` at the API is reachable without a token at the edge.

**Decision:** preserve `/api` at the gateway and add one explicit anonymous auth route. Rejected: remapping the API (touches every endpoint, breaks OpenAPI), enumerating every auth sub-path (duplicates API policy), header sniffing (fragile).

**Files**

| Action | Path |
|---|---|
| Modify | `app/Gateway/Gateway.Router.cs` |
| Modify | `tests/Integration/AppHost.Integration.Tests/GatewayRouting.Spec.cs` |

**Steps**

1. Insert before the payments routes:
   ```csharp
   new RouteConfig
   {
       RouteId = "api-auth",
       ClusterId = ApiCluster,
       Match = new RouteMatch { Path = "/api/auth/{**remainder}" },
       AuthorizationPolicy = AnonymousPolicy,
   },
   ```
2. Remove `Transforms = StripPrefix("/{remainder}")` from the `api` catch-all; keep `ProtectedApiPolicy`.
3. Keep order: `api-time` → `api-auth` → webhook/admin/payments → `api` → `admin` → `shopfront`.
4. Update the `<remarks>` so the prefix-preservation rule is explicit.

**Tests** (`GatewayRouting.Spec.cs`)

- `ApiAuthLogin_WithoutToken_ReachesApi` — assert the API envelope, not an edge 401.
- `ApiTodo_WithoutToken_Returns401` — edge challenge.
- `ApiTodo_WithToken_Returns200` — mint through the API, call through the gateway.
- `ApiTime_StillReachesApi` — regression.
- `ApiIdentityUsers_WithToken_Returns200` — proves `/api/identity/*` is not stripped.

**Acceptance:** four new specs plus existing ones green under `[Collection(AppHostCollection.Name)]`.

**Rollback:** revert the route table; no data.

---

## F2 — Configuration, secrets, and startup guards (Q2, Q27, Q46)

**Invariant:** outside `Development`, startup fails unless a high-entropy key is injected and shared by API and gateway; production-shaped Compose never enables fake payments or `Development`.

**Decision:** remove `Jwt:Key` from both base configs, bind `Jwt__Key` from the environment, and add a validation guard. Rejected: encrypted config (KMS dependency too early), per-service derived keys (breaks cross-service validation).

**Files**

| Action | Path |
|---|---|
| Modify | `app/Web.Api/appsettings.json`, `app/Gateway/appsettings.json` |
| Modify | `src/Infrastructure/BuildingBlocks/Adapters/Security/Authentication/Options/Token.Option.cs` (or validator) |
| Modify | `src/Infrastructure/Modules/Payments/PaymentsOptionsValidator.cs` |
| Add | `compose.prod.yaml` |
| Modify | `compose.yaml` |

**Implementation**

- `Jwt:Key` has **no default** outside Development. Guard: reject the known scaffold literals and keys < 32 bytes when `Environment != Development`.
- Compose dev: `JWT_KEY` injected into both `api` and `gateway`; `Persistence__AutoMigrate: "true"`; probes use `/health/live`.
- `compose.prod.yaml`: `ASPNETCORE_ENVIRONMENT=Production`, `JWT_KEY=${JWT_KEY:?JWT_KEY required}`, real providers, `Persistence__AutoMigrate=false`.
- `PaymentsOptionsValidator`: reject `Fake` for `Production` **and** `Staging` (case-insensitive).

**Tests**

- `Configuration.Startup.Test.cs`: `ScaffoldKey_OutsideDevelopment_Fails`, `ShortKey_OutsideDevelopment_Fails`, `MissingKey_OutsideDevelopment_Fails`.
- `PaymentsOptionsValidator.Test.cs`: `FakeProvider_InStaging_Fails`.

**Acceptance:** `docker compose -f compose.prod.yaml config` fails without `JWT_KEY`; with one key, an API-issued token validates at the gateway.

**Rollback:** environment-only; re-add the key is a one-line revert.

---

## F3 — Payment operation durability (Q3, Q7–Q14)

**Invariant:** one caller idempotency key maps to exactly one persisted operation ID and one provider idempotency key, stable across retries; every provider call is preceded by a durable reservation and followed by a reconcile-able local decision.

**Decision:** add a `PaymentOperation` table written **before** any provider I/O. Derive the provider key deterministically from `(customerId, callerKey, operationKind)`. Rejected: keeping reservations in the command transaction (rollback loses them), using the caller key verbatim at the provider (cross-customer collision/secret leak), synchronous reconciliation on every `Unknown` (latency), full saga (overkill).

**Files**

| Action | Path |
|---|---|
| Add | `src/Application/Modules/Payments/Persistence/IPaymentOperationStore.Interface.cs` |
| Add | `src/Infrastructure/Modules/Payments/Persistence/PaymentOperation.cs` |
| Add | `src/Infrastructure/Modules/Payments/Persistence/PaymentOperation.Configuration.cs` |
| Add | `src/Infrastructure/Modules/Payments/Persistence/PaymentOperation.Store.cs` |
| Add | `src/Application/Modules/Payments/Ports/PaymentProviderKey.Deriver.cs` |
| Modify | `RefundPayment.cs`, `CreatePayment.cs`, `CapturePayment.cs`, `CancelPayment.cs`, `ConfirmPayment.cs` |
| Modify | `Core/Modules/Payments/Intents/Payment.cs`, `Payment.Validator.cs` |
| Modify | `Infrastructure/Modules/Payments/Adapters/{PayPal,Shared}/**` |
| Modify | `Infrastructure/Modules/Payments/Reconciliation/**`, `Persistence/PaymentReadStore.cs` |

**Schema**

```sql
CREATE TABLE payment.payment_operations (
    id UUID PRIMARY KEY,
    customer_id UUID NOT NULL,
    caller_key_hash TEXT NOT NULL,
    kind TEXT NOT NULL,                 -- Create | Capture | Refund | Cancel | Confirm
    fingerprint TEXT NOT NULL,
    status TEXT NOT NULL,               -- Reserved | ProviderCalled | Succeeded | Failed | Unknown
    provider_key TEXT NOT NULL,
    provider_reference TEXT,
    attempts INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ix_payment_operations_key
    ON payment.payment_operations (customer_id, kind, caller_key_hash);
```

**Key derivation**

```csharp
public static string Derive(Guid customerId, string callerKey, string kind)
    => Convert.ToHexStringLower(
        HMACSHA256.HashData(
            Encoding.UTF8.GetBytes($"{customerId:N}:{kind}"),
            Encoding.UTF8.GetBytes(callerKey)));
```

**State machine**

```text
Reserved ──provider call──> ProviderCalled ──> Succeeded
                                      ├──────> Failed        (definitive decline)
                                      └──────> Unknown       (timeout/5xx)
Unknown ──reconciliation──> Succeeded | Failed
```

**Implementation order**

1. Add the entity, configuration, store, and migration.
2. Add the key deriver + unit tests.
3. `RefundPayment`: reserve `operationId` before the provider call; derive the provider key from `(customerId, callerKey, "Refund")`, **not** `refundId`; map timeout to `Unknown`, not `Failed`.
4. `CreatePayment`: commit the reservation before the provider call; never release it on transport failure; replay returns the stored operation.
5. `CapturePayment`: validate the aggregate **before** provider I/O.
6. `CancelPayment`: inspect `PaymentCancelResult.Succeeded`; only then cancel locally.
7. `ConfirmPayment`: map `RequiresAction`/`Processing` to pending, not `Failed`.
8. Partial capture: derive status from the remaining balance; validator compares against the authorized remainder.
9. PayPal: convert to integer minor units before the wire call; reject unrepresentable precision.

**Config:** `Payments:Operations:LeaseSeconds`, `:MaxAttempts`, `:UnknownReconcileAfterMinutes:2`, `Payments:Reconciliation:Enabled=true` in production, `:FairnessWindowMinutes:60`.

**Tests**

- Unit: `PaymentProviderKey.Deriver.Test.cs` (stable across attempts, differs by customer/kind); `RefundPayment.Test.cs` (`Refund_ReplaysSucceededOperation`, `Refund_TimeoutIsUnknown`).
- Integration: `PaymentOperation.Store.Integration.Test.cs` (concurrent reserve → one winner); `PaymentOperation.Reconcile.Integration.Test.cs` (unknown resolves without a second charge).
- Failure injection: provider success + commit failure → retry issues **one** provider operation.

**Acceptance:** replaying the same key never calls the provider twice; a crashed create leaves a reconcile-able `Unknown`; no provider call occurs inside `TransactionBehavior`'s transaction.

**Rollback:** additive table; `Payments:DurableOperations` flag.

---

## F4 — Projection correctness (Q4, Q5)

**Invariant:** an event is projected exactly once in commit order; crash/retry never skips or double-applies it; only one writer advances a projection checkpoint.

**Decision:** add a transactional `CommitPosition` counter stamped in the append transaction (PostgreSQL identities are allocated before commit), and apply **one event + marker + checkpoint in one transaction**. Rejected: gap detection on identity (ambiguous with long transactions), single serialized publisher (throughput), per-event save with separate checkpoint (current bug).

**Files**

| Action | Path |
|---|---|
| Modify | `EventStoreEvent.Configuration.cs` |
| Modify | `PostgresEventStore.cs` |
| Modify | `Projection.Processor.cs` |
| Modify | `PaymentProjection.cs`, `Todo.Projection.cs`, `TodoReminder.Projection.cs` |
| Add | `Projection.AppliedMarker.cs` + configuration |
| Modify | `ProjectionCheckpoint.Store.Interface.cs` + EF store |
| Modify | `RebuildProjection.Service.cs` |

**Commit cursor**

```sql
CREATE TABLE event_store.commit_cursor (
    id INT PRIMARY KEY CHECK (id = 1),
    position BIGINT NOT NULL
);
INSERT INTO event_store.commit_cursor (id, position) VALUES (1, 0);

ALTER TABLE event_store.events ADD COLUMN commit_position BIGINT;
CREATE UNIQUE INDEX ix_events_commit_position ON event_store.events (commit_position)
    WHERE commit_position IS NOT NULL;
```

In `PostgresEventStore.AppendAsync`, inside the ambient transaction and before `SaveChanges`:

```sql
UPDATE event_store.commit_cursor
SET position = position + 1
RETURNING position;
```

Stamp every row in the batch with the returned value. If there is no ambient transaction, open one covering append + outbox staging (see F3's non-guarantee).

**Atomic apply**

```csharp
await using var tx = await context.Database.BeginTransactionAsync(ct);

if (!await markers.ExistsAsync(projection.Name, stored.StreamId, stored.Version, ct))
{
    var applied = await projection.ApplyAsync(stored, ct);   // must NOT call SaveChanges
    if (applied.IsFailure) { await tx.RollbackAsync(ct); return applied; }

    await markers.AddAsync(projection.Name, stored.StreamId, stored.Version, ct);
    await checkpoints.SavePositionAsync(projection.Name, stored.CommitPosition, ct);
    await context.SaveChangesAsync(ct);
}
await tx.CommitAsync(ct);
```

**Fencing:** add `ProjectionCheckpoint.Owner` + `LeaseUntilUtc`; a worker must claim the projection before processing and renew per batch. A second replica cannot advance a lease it does not own.

**Rebuild:** rebuild into a shadow schema/table, verify checksum, then swap. Never rewind a live projection in place.

**Tests**

- `Projection.CommitOrder.Integration.Test.cs`: open tx A (lower identity), commit B, project B, commit A → A is still projected (cursor blocks at the gap).
- `Projection.AtomicCheckpoint.Integration.Test.cs`: force failure after apply → rerun → no duplicate insert, no double refund increment.
- `Projection.Fencing.Integration.Test.cs`: two workers → one checkpoint owner.
- `Projection.Rebuild.Integration.Test.cs`: shadow checksum equals live.

**Acceptance:** delayed-commit test passes; refund totals increment once; two workers cannot both advance.

**Rollback:** additive column/table; `EventStore:CommitOrderedCursor` flag.

---

## F5 — Webhook inbox state machine (Q6, Q11)

**Invariant:** a delivery is recorded, claimed, processed, and marked durably; only a unique-key violation is a duplicate; incomplete deliveries retry.

**Decision:** replace record-before-settle with a leased inbox: `Received → Claimed → Processed | DeadLettered`. Rejected: settle-then-record (double settlement), a broker queue for webhooks (second reliability path).

**Files**

| Action | Path |
|---|---|
| Modify | `src/Application/Modules/Payments/Persistence/IWebhookEventStore.Interface.cs` |
| Modify | `src/Infrastructure/Modules/Payments/Persistence/PaymentWebhookEvent.cs` + `.Configuration.cs` + `WebhookEventStore.cs` |
| Modify | `src/Application/Modules/Payments/Webhooks/Commands/ProcessPaymentWebhook.cs` |
| Modify | `app/Web.Api/Endpoints/Payments/PaymentWebhookEndpoint.cs` (body limit before buffering) |
| Add | `src/Infrastructure/Modules/Payments/Webhooks/WebhookRetry.Worker.cs` |

**Schema additions to `payment.webhook_events`**

```sql
ALTER TABLE payment.webhook_events
    ADD COLUMN status TEXT NOT NULL DEFAULT 'Received',
    ADD COLUMN lease_id UUID,
    ADD COLUMN locked_until_utc TIMESTAMPTZ,
    ADD COLUMN attempts INT NOT NULL DEFAULT 0,
    ADD COLUMN last_error TEXT;
CREATE INDEX ix_webhook_events_pending
    ON payment.webhook_events (status, locked_until_utc)
    WHERE status IN ('Received', 'Claimed', 'Failed');
```

**Flow**

```csharp
// Guard: signature + size validated before any state change.
var recorded = await inbox.RecordAsync(provider, eventId, type, rawBody, ct);
if (recorded.IsDuplicate) { metrics.Duplicate.Add(1); return Result<bool>.Success(false); }

var claim = await inbox.ClaimAsync(eventId, leaseSeconds, ct);
if (claim.IsFailure) return Result<bool>.Success(false);   // another worker owns it

var settled = await SettleAsync(providerEvent, command, ct);
await inbox.MarkAsync(eventId, claim.Value.LeaseId,
    settled.IsFailure ? WebhookStatus.Failed : WebhookStatus.Processed,
    settled.IsFailure ? settled.Errors[0].Message : null, ct);
return settled.Map(_ => true);
```

Only the `(provider, provider_event_id)` unique violation maps to duplicate. Every other `DbUpdateException` propagates.

**Retry worker:** claim rows where `status IN ('Received','Failed') AND attempts < max AND (locked_until IS NULL OR locked_until < now)` with `FOR UPDATE SKIP LOCKED`; re-run settlement; dead-letter after max attempts.

**Disputes/refunds:** extend `PaymentObservation` and the Stripe adapter; forward `DisputeOpened/Resolved/Challenged` and provider-side refunds to `SettlePayment`; do not drop fields.

**Config:** `Payments:WebhookInbox:LeaseSeconds:30`, `:MaxAttempts:10`, `:RetryIntervalSeconds:10`, `:RetentionDays:30`.

**Tests**

- `TransientSettlementFailure_IsRetried` (not `Duplicate`).
- `ConcurrentClaim_SingleWinner`.
- `NonUniqueDbFailure_IsNotDuplicate`.
- `DisputeWebhook_PersistsDispute`.

**Acceptance:** crash after record → restart → settlement completes; duplicate → one settlement; unrelated DB failure → 5xx and retry.

**Rollback:** columns are additive; `Payments:WebhookInbox:Enabled` flag.

---

## F6 — Outbox hardening (Q16, Q17, Q43)

**Invariant:** no committed event is permanently lost while health reports green; a batch never outlives its lease; replay is explicit and bounded.

**Files**

| Action | Path |
|---|---|
| Modify | `Outbox.Dispatcher.cs` — renew lease per row; default batch 20, lease 30 s |
| Modify | `Outbox.Health.Monitor.cs` — count dead letters, not only pending |
| Modify | `Outbox.Store.cs` — `RequeueDeadLettersAsync`, `DeleteDeadLettersBeforeAsync` |
| Add | `src/Application/.../Messaging/Outbox/ReplayDeadLetters.cs` |

**Replay**

```csharp
public async Task<Result<int>> RequeueDeadLettersAsync(int limit, CancellationToken ct)
{
    // Enforce: replay is bounded and operator-invoked; never automatic.
    int affected = await Messages
        .Where(m => m.Status == OutboxMessageStatus.DeadLettered)
        .OrderBy(m => m.OccurredOnUtc)
        .Take(limit)
        .ExecuteUpdateAsync(s => s
            .SetProperty(m => m.Status, OutboxMessageStatus.Pending)
            .SetProperty(m => m.Attempts, 0)
            .SetProperty(m => m.LastError, (string?)null)
            .SetProperty(m => m.AvailableOnUtc, clock.GetUtcNow()), ct);
    return Result<int>.Success(affected);
}
```

**Health:** `Unhealthy` when `deadLetterCount >= threshold` or oldest pending age > max age. Expose both counts.

**Retention:** prune processed rows (existing) and dead-lettered rows older than `DeadLetterRetentionDays`; prune inbox markers older than `InboxRetentionDays`.

**Config:** `Outbox:BatchSize:20`, `:LeaseSeconds:30`, `:DeadLetterHealthThreshold:10`, `:DeadLetterRetentionDays:30`.

**Tests:** `RequeueDeadLetters_ReturnsToPending`, `DeadLetter_CountAffectsHealth`, `SlowPublish_AfterRenewal_MarksProcessed`.

**Acceptance:** health degrades on dead letters; replay restores rows without a new publish path.

**Rollback:** flag `Outbox:DeadLetterReplay:Enabled`; defaults are config-only.

---

## F7 — Notification pipeline repair (Q18)

**Invariant:** every notification is delivered at most once per dedupe key, with persisted status, real retry/backoff, and a DLQ; every configured queue is drained.

**Files**

| Action | Path |
|---|---|
| Modify | `Notification.Extension.cs` — `TryAddEnumerable`; register enabled providers |
| Modify | `Background.Extension.cs` — `Queues = NotificationSettingConstant.BackgroundJobs.Queues.All` |
| Modify | provider `*.Extension.cs` — `TryAddEnumerable` |
| Add | `NotificationDelivery.cs` + `.Configuration.cs` + `NotificationDelivery.Store.cs` |
| Add | `src/Application/BuildingBlocks/Ports/Notifications/Delivery/INotificationDeliveryStore.Interface.cs` |
| Modify | `Notification.Service.Service.cs` / `Notification.Hub.Service.cs` — record → send → mark |
| Add | `NotificationRetry.Worker.cs`, `NotificationDeadLetter.Health.cs` |

**Schema**

```sql
CREATE SCHEMA IF NOT EXISTS notification;
CREATE TABLE notification.deliveries (
    id UUID PRIMARY KEY,
    dedupe_key TEXT NOT NULL,
    use_case TEXT NOT NULL,
    recipient_id TEXT NOT NULL,
    channel TEXT NOT NULL,
    status TEXT NOT NULL,              -- Pending | Sent | Failed | DeadLettered
    attempts INT NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    sent_at TIMESTAMPTZ,
    next_attempt_at TIMESTAMPTZ
);
CREATE UNIQUE INDEX ix_deliveries_dedupe ON notification.deliveries (dedupe_key);
CREATE INDEX ix_deliveries_retry ON notification.deliveries (status, next_attempt_at)
    WHERE status IN ('Pending','Failed');
```

**Flow:** `TryRecordAsync(dedupeKey)` → if duplicate, return existing (no send). Otherwise claim, call hub, mark `Sent`; on failure schedule `next_attempt_at` with exponential backoff; after `MaxAttempts` mark `DeadLettered` and alert.

**Queue fix:** templates use `critical`/`high`; the Hangfire server must consume `Critical, High, Default, Low`. `RetryCount` becomes `MaxAttempts` or is deleted — no dead config.

**Config:** `Notification:Delivery:MaxAttempts:5`, `:BackoffSeconds:5`, `:MaxBackoffSeconds:300`, `:RetentionDays:14`; `EnableBackgroundJobs=true` in production.

**Tests:** duplicate enqueue → one provider call; transient failure → retry; permanent → dead-letter; `AllChannels_HaveRegisteredProviders`; `Hangfire_ConsumesAllQueues`.

**Acceptance:** email provider registered; queues drain; duplicate suppressed; DLQ visible.

**Rollback:** `Notification:DeliveryPersistence:Enabled`.

---

## F8 — Identity and session security (Q19–Q23, Q26)

**Invariant:** a revoked token never authorizes; refresh redemption is atomic; external linking requires a verified email; no non-privileged actor can grant a privileged role; tokens carry the tenant.

**Files**

| Action | Path |
|---|---|
| Modify | `Authentication.Composition.cs` — `JwtBearerEvents.OnTokenValidated` |
| Modify | `TokenRotation.Service.cs`, `EfCoreRefreshToken.Store.cs` — atomic consume |
| Modify | `ExternalAuthentication.Service.cs`, `ExternalPrincipal.Mapper.cs` — verified email |
| Modify | `UserAccountService.cs`, `Role.cs` — privileged-role ceiling |
| Modify | `IdentityTokenIssuer.cs` — issue tenant claim |
| Modify | `IdentityEmail.Service.cs` — stop logging links |
| Add | `SmtpIdentityEmail.Sender.cs` / SendGrid sender |

**Revocation**

```csharp
options.Events = new JwtBearerEvents
{
    OnTokenValidated = async context =>
    {
        var jti = context.Principal?.FindFirstValue("jti");
        var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklist>();
        if (jti is not null)
        {
            var revoked = await blacklist.ContainsAsync(jti, context.HttpContext.RequestAborted);
            if (revoked.IsFailure || revoked.Value)
                context.Fail("Token revoked.");
        }
    }
};
```

**Atomic refresh**

```sql
UPDATE identity.refresh_tokens
SET revoked = true, revoked_reason = 'Replaced', revoked_at = @now
WHERE token_hash = @hash AND revoked = false
RETURNING id;
```

Zero rows → already consumed → revoke the family and fail.

**Revoke on:** deactivation, password reset, privileged-role removal, explicit "revoke sessions".

**Privileged ceiling:** `Role.IsPrivileged`; `AssignRoleAsync` rejects assigning a privileged role unless the actor holds one; never allow self-escalation. Seed `Admin=true`.

**Tenant claim:** `IdentityTokenIssuer` passes the user's tenant; `TenantContext` stops falling back to a role claim.

**Email:** production uses a real sender; never log token-bearing links; Development may use the logging sender behind an explicit flag.

**Tests:** `RevokedAccessToken_AfterLogout_401`, `DeactivatedUser_Refresh_Fails`, `ConcurrentRefresh_OneSucceeds`, `UnverifiedEmail_ExternalLink_Fails`, `NonPrivilegedManager_CannotAssignAdmin`, `IssuedToken_CarriesTenantClaim`, `ResetLink_NotLogged`.

**Acceptance:** all seven pass through the real API pipeline.

**Rollback:** `Security:EnforceRevocation`, `Identity:PrivilegeCeiling`, independently flag-gated.

---

## F9 — Observability and operations (Q15, Q28–Q30, Q43)

**Invariant:** every subsystem exposes a meter; traces cross the async boundary; readiness reflects migration state; restore is rehearsed.

**Files**

| Action | Path |
|---|---|
| Modify | `ServiceDefaults.Extension.cs` — `AddMeter(PaymentMetricConstant.MeterName, ...)`; new meters |
| Add | `Observability/Metrics/{Outbox,Inbox,Notification,Reconciliation}.Meter.cs` |
| Modify | `Outbox.Dispatcher.cs`, `OutboxIntegrationEventPublisher.cs`, consumers — traceparent propagation |
| Modify | `HealthChecks.Extension.cs`, `compose.yaml` — one live/ready contract |
| Add | `scripts/ops/migrate.sh`, `scripts/ops/backup-restore.sh` |
| Add | `docs/architecture/slo.md` |

**Meters:** `outbox.dispatched`, `outbox.retried`, `outbox.deadlettered`, `outbox.age.seconds`, `inbox.duplicate`, `projection.lag.events`, `notification.sent|failed|deadlettered`, `reconciliation.age.minutes`.

**Tracing:** put `Activity.Current?.Id` into outbox metadata; on consume, start an `Activity` with that parent; assert one trace ID across write → dispatch → handler.

**Health:** `/health/live` = process; `/health/ready` = DB, Redis, RabbitMQ, outbox age, migrations applied, dead letters under threshold. Remove dev-only `/alive` from production probes.

**Backup:** `pg_dump` nightly + WAL/PITR; restore drill into an isolated database; publish RPO/RTO.

**Acceptance:** payment/outbox meters visible at the OTLP endpoint; one trace across async; `/health/ready` is 503 before migration, 200 after; restore drill restores a known row count.

**Rollback:** additive meters/scripts; `Observability:ConsolidatedHealth` flag.

---

## F10 — Enforcement and CI (Q31, Q32)

**Invariant:** an architectural violation and a silent broker skip both fail the build.

**Files**

| Action | Path |
|---|---|
| Modify | `tests/Architecture.Tests/LayerArchitecture.Tests.cs` |
| Modify | `tests/Architecture.Tests/VerticalSliceArchitecture.Tests.cs` |
| Modify | `tests/Architecture.Tests/BuildingBlockBoundary.Tests.cs` |
| Modify | `scripts/backend/test-broker.backend.sh` |
| Modify | `tests/Integration/.../RabbitMq.Fixture.cs` |
| Modify | `.github/workflows/ci.yml` |

**Rules to implement**

1. `Core` references nothing; `Application` does not reference `Infrastructure`, EF Core, ASP.NET, or `Web.Api`.
2. `BuildingBlocks` never reference `Modules`.
3. `Modules.X` never reference `Modules.Y` (IL scan, including generic arguments and method bodies).
4. A deliberate violating fixture fails the test (negative test).

**CI:** broker script exits non-zero when expected tests are absent; container fixtures throw (not skip) when `CI=1` and no runtime; assert a minimum executed-test count.

**Acceptance:** seeded violation turns `make lint` red; renaming broker tests turns CI red.

**Rollback:** test-only.

---

## F11 — Data integrity and performance

**Invariant:** listing is keyset-based with a covering index; aggregate loads are O(snapshot + tail); retention bounds growth.

**Files**

| Action | Path |
|---|---|
| Add | `Querying/Pagination/CursorPageRequest.cs`, `CursorPage.cs` |
| Modify | `PaymentReadStore.cs`, `ListMyPayments.cs` — keyset query |
| Modify | `PaymentReadModel.Configuration.cs` — `(CustomerId, CreatedAtUtc, Id)` index |
| Modify | `Payment.cs`, `EventSourcedRepository.cs` — snapshot support |
| Modify | retention workers — event-store archival, inbox/dead-letter pruning |

**Acceptance:** `EXPLAIN` uses the composite index for a deep page; a payment with 500 events loads from snapshot + ≤ 50 tail events; retention dry-run reports bounded growth.

**Rollback:** additive index/table; snapshot policy opt-in.

---

## Merge order and gate

```text
F1 → F2 → F3 → F4 → F5 → F6 → F7 → F8 → F9 → F10 → F11
```

Run before each merge:

```bash
make lint
make build
make test
make coverage
make test.infra.broker   # F1, F3, F4, F5
make test.all            # F1, F4, F5
```

**Definition of done for the foundation:** every F-item has a passing acceptance test, a rollback note, and no new convention-baseline exemptions. Phase 0/1 exit evidence in `docs/architecture/master-roadmap.md` must be satisfied before `modules-implementation.md` begins.
