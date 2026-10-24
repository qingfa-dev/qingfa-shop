---
name: code-comment-labels
description: Write greppable intent comments. Use when adding/reviewing comments, or when code has non-obvious rules, assumptions, constraints, failure behavior, or consequences.
---

# Code Comment Labels — Optimized Skill Guide

**Use when:** writing or reviewing comments.  
**Goal:** label intent, not mechanics.  
**Form:** `Label: Descriptor`  
**Rule:** if code explains *what*, comment only the non-obvious *why*.

---

## 1. Comment Decision Rule

Comment only if all are true:

1. Code cannot explain itself.
2. There is a non-obvious reason, rule, assumption, edge case, or external constraint.
3. A maintainer would reasonably ask: “Why is this necessary?”

Prefer:

```csharp
// Enforce: Orders below the configured minimum cannot enter checkout.
```

Not:

```csharp
// Bad: Check if order total is less than minimum.
```

> Comment the reason, constraint, assumption, or consequence — not the mechanics.

---

## 2. Label Syntax

```text
Label: Explanation
```

Rules:

- Label is **PascalCase**.
- One `:` after label.
- Explanation starts with a capital letter.
- Keep concise; use project prose.
- Match surrounding comment syntax and indentation.
- One primary intent per comment.
- Do not add a label merely because a comment exists.
- Language-agnostic: `//`, `#`, `<!-- -->`, etc.

Examples:

```csharp
// Validate: Email must belong to an approved domain.
// Retry: Provider failures are transient and the request is idempotent.
// Fallback: Cached data is acceptable when the recommendation service is unavailable.
```

---

## 3. Core Vocabulary

| Category | Approved labels |
|---|---|
| Validation | `Validate:`, `Check:`, `Guard:`, `Verify:`, `Assert:` |
| Object state | `Create:`, `Assign:`, `Update:`, `Initialize:`, `Merge:`, `Clone:` |
| Processing | `Compute:`, `Transform:`, `Filter:`, `Parse:`, `Normalize:`, `Generate:` |
| Flow | `Await:`, `Retry:`, `Skip:`, `Fallback:`, `Batch:`, `Throttle:` |
| Resources | `Acquire:`, `Release:`, `Lock:`, `Cache:`, `Pool:`, `Dispose:` |
| Domain / Events | `Raise:`, `Trigger:`, `Notify:`, `Enforce:`, `Handle:` |
| Integration | `Call:`, `Send:`, `Receive:`, `Map:`, `Publish:`, `Webhook:` |
| Errors | `Catch:`, `Recover:`, `Compensate:`, `Escalate:`, `Degrade:` |
| Observability | `Log:`, `Trace:`, `Monitor:`, `Audit:`, `Profile:` |

Do not invent one-off labels. Extend vocabulary at project level only.

---

## 4. Context Descriptor Principle

Descriptor after `:` is context-dependent. Capture the useful missing information:

- rule, reason, assumption, constraint
- source, consequence, trade-off, boundary
- recovery, lifecycle, compatibility, temporal condition

Do not force a fixed sentence template.

Example:

```csharp
// Retry: Provider timeout is transient and the request is idempotent.
```

`Retry:` = intent category.  
Descriptor = why retry is safe/useful.

---

## 5. Validation & Checks

| Label | Use for | Example |
|---|---|---|
| `Validate:` | Input/domain rule must be satisfied | `// Validate: Order must contain at least one purchasable item.` |
| `Check:` | Inspect condition, state, capability, permission | `// Check: User has permission to modify the organization.` |
| `Guard:` | Defensive boundary against invalid/unsafe execution | `// Guard: Ignore duplicate events received during provider retries.` |
| `Verify:` | Independently confirm signature, identity, claim, checksum | `// Verify: Webhook signature matches the provider secret.` |
| `Assert:` | Expected invariant should already hold | `// Assert: Transaction must be active before publishing.` |

`Validate:` asks “does it satisfy a rule?”  
`Check:` asks “is this condition/state/capability present?”

---

## 6. Object State

| Label | Use for | Example |
|---|---|---|
| `Create:` | New object/entity with important initial semantics | `// Create: Order starts in Draft until payment is confirmed.` |
| `Assign:` | Value comes from meaningful authority/source | `// Assign: Gateway response is authoritative for the external transaction ID.` |
| `Update:` | Existing state changes by business/lifecycle rule | `// Update: Inventory reflects the quantity confirmed by shipment processing.` |
| `Initialize:` | Establish initial usable state/configuration | `// Initialize: Worker starts from the last committed checkpoint.` |
| `Merge:` | Combine sources with precedence/conflict rules | `// Merge: Account data takes precedence over CRM values.` |
| `Clone:` | Copy has snapshot/isolation/mutability semantics | `// Clone: Preserve original aggregate state before migration.` |

`Create:` = producing a new object.  
`Initialize:` = establishing its initial usable state.

---

## 7. Processing

| Label | Use for | Example |
|---|---|---|
| `Compute:` | Non-obvious calculation/derived value | `// Compute: Tax is applied after item-level discounts.` |
| `Transform:` | Representation/model change | `// Transform: Provider status codes map to internal payment states.` |
| `Filter:` | Intentional inclusion/exclusion rule | `// Filter: Archived products are excluded from customer-facing search.` |
| `Parse:` | Extract/interpret structured external data | `// Parse: Provider timestamps are interpreted as UTC.` |
| `Normalize:` | Convert to canonical representation | `// Normalize: Phone numbers are stored in E.164 format.` |
| `Generate:` | Deliberately derive value/artifact/identifier | `// Generate: Correlation ID when the client did not provide one.` |

---

## 8. Flow & Coordination

| Label | Use for | Example |
|---|---|---|
| `Await:` | Meaningful waiting/order semantics | `// Await: Persist the event before acknowledging the message.` |
| `Retry:` | Intentionally repeat operation | `// Retry: Connection failures can occur during database failover.` |
| `Skip:` | Intentionally omit operation | `// Skip: Reprocessing is unnecessary for an already-confirmed payment.` |
| `Fallback:` | Primary path fails; alternative result is acceptable | `// Fallback: Local configuration is used when remote configuration cannot be loaded.` |
| `Batch:` | Deliberately group operations | `// Batch: Provider accepts at most 100 items per request.` |
| `Throttle:` | Deliberately rate-limit execution | `// Throttle: Stay below the provider's request-per-second limit.` |

Do not use `Await:` just because `await` exists.

---

## 9. Resources

| Label | Use for | Example |
|---|---|---|
| `Acquire:` | Obtain ownership/access | `// Acquire: Distributed lock prevents concurrent inventory updates.` |
| `Release:` | Intentionally relinquish resource | `// Release: Reservation must be returned when payment fails.` |
| `Lock:` | Mutual exclusion/synchronization invariant | `// Lock: Only one worker may update a SKU at a time.` |
| `Cache:` | Meaningful freshness/consistency/performance trade-off | `// Cache: Product metadata may remain stale for five minutes.` |
| `Pool:` | Reuse/capacity control | `// Pool: Connections are reused to avoid repeated TLS setup.` |
| `Dispose:` | Meaningful cleanup/lifetime requirement | `// Dispose: Native handle must be released before the worker exits.` |

`Acquire:` = obtaining resource.  
`Lock:` = synchronization invariant.

Do not add `Dispose:` to every `using` statement.

---

## 10. Domain & Events

| Label | Use for | Example |
|---|---|---|
| `Raise:` | Emit domain/application event | `// Raise: Notify subscribers after the order becomes payable.` |
| `Trigger:` | Condition initiates another operation/workflow | `// Trigger: Reconciliation starts when settlement differs from the ledger.` |
| `Notify:` | Inform another component/system/user | `// Notify: Members receive confirmation after the draw is finalized.` |
| `Enforce:` | Business rule actively prevents invalid action/state | `// Enforce: Refund cannot exceed the captured amount.` |
| `Handle:` | Specific event/command/message handling semantics | `// Handle: Duplicate webhook deliveries must remain idempotent.` |

`Raise:` = emitting event.  
`Trigger:` = initiating behavior because of a condition.  
`Notify:` = informing another party.

---

## 11. Integration

| Label | Use for | Example |
|---|---|---|
| `Call:` | Invoke external/internal service with contract/assumption | `// Call: Provider requires the original correlation ID.` |
| `Send:` | Deliberately transmit data/message/request | `// Send: Publish finalized order only after transaction commits.` |
| `Receive:` | Incoming data has trust/interpretation/ordering semantics | `// Receive: Provider retries may deliver the same event multiple times.` |
| `Map:` | Convert between models/contracts | `// Map: Provider status codes to internal payment states.` |
| `Publish:` | Make event/message available with timing/guarantee | `// Publish: Emit only after the order transaction commits.` |
| `Webhook:` | Webhook delivery/verification/idempotency/callback behavior | `// Webhook: Signature verification prevents forged payment notifications.` |

---

## 12. Error Handling & Recovery

| Label | Use for | Example |
|---|---|---|
| `Catch:` | Exception intentionally caught at boundary | `// Catch: Provider timeout is recoverable at this boundary.` |
| `Recover:` | Restore acceptable operation after failure | `// Recover: Rebuild the read model from the latest committed event.` |
| `Compensate:` | Reverse earlier successful action after later failure | `// Compensate: Release inventory reserved for the failed payment.` |
| `Escalate:` | Local handling insufficient; move failure elsewhere | `// Escalate: Unresolved settlement mismatches require manual review.` |
| `Degrade:` | Continue with reduced functionality | `// Degrade: Continue checkout without personalization.` |

`Fallback:` = secondary path provides an alternative result.  
`Degrade:` = system intentionally continues with reduced capability.

Do not use `Catch:` just because a `catch` exists.

---

## 13. Observability

| Label | Use for | Example |
|---|---|---|
| `Log:` | Logging has meaningful operational intent | `// Log: Record provider rejection details for support diagnostics.` |
| `Trace:` | Distributed/request tracing correlation purpose | `// Trace: Preserve payment correlation ID across provider calls.` |
| `Monitor:` | Metrics/monitoring observes operational condition | `// Monitor: Queue depth indicates delayed settlement processing.` |
| `Audit:` | Historical accountability/compliance traceability | `// Audit: Financial changes must remain attributable to the acting user.` |
| `Profile:` | Intentional performance measurement | `// Profile: Measure serialization cost for large catalog responses.` |

Do not comment ordinary logging calls merely because they log.

---

## 14. Choosing Between Similar Labels

| Intent | Prefer |
|---|---|
| Input must satisfy a rule | `Validate:` |
| Condition/state/capability inspected | `Check:` |
| Protect execution from invalid/unsafe path | `Guard:` |
| Authenticity/identity/claim independently confirmed | `Verify:` |
| Invariant expected to already hold | `Assert:` |
| New object/entity produced | `Create:` |
| Existing object state changed | `Update:` |
| Initial state/configuration matters | `Initialize:` |
| Value from authoritative source | `Assign:` |
| Multiple sources combined | `Merge:` |
| Calculation produces derived result | `Compute:` |
| Data changes representation | `Transform:` |
| Data converted to canonical form | `Normalize:` |
| Primary path has alternative result source | `Fallback:` |
| Functionality continues reduced | `Degrade:` |
| Failed action repeated | `Retry:` |
| Failed action undone by another action | `Compensate:` |
| Failed operation restored | `Recover:` |
| Failure needs another handling boundary | `Escalate:` |
| Business rule actively prevents action | `Enforce:` |
| Event/message emitted | `Raise:` or `Publish:` |
| Behavior begins because of condition | `Trigger:` |
| Another party informed | `Notify:` |
| External system invoked | `Call:` |
| Data converted between contracts | `Map:` |
| Resource access obtained | `Acquire:` |
| Resource synchronization enforced | `Lock:` |
| Resource intentionally relinquished | `Release:` |
| Data retained for reuse | `Cache:` |
| Historical accountability required | `Audit:` |
| Performance measurement intentional | `Profile:` |

Decision aid, not rigid classifier.

---

## 15. One Comment Should Explain One Primary Intent

One comment block = one primary reason/semantic concern.  
Not every line needs a comment.

Bad:

```csharp
// Create: New list.
// Add: Saved items.
// Add: Current item.
```

Good:

```csharp
// Create: Cart starts with saved items, then includes the current item for checkout.
```

---

## 16. Comment Scope

Attach comment to the smallest useful code region that depends on the intent.

```csharp
// Retry: Provider timeout is transient and the request is idempotent.
var response = await provider.SendAsync(request);
```

Avoid placing local explanation far from behavior.

---

## 17. Prefer Stable Intent Over Implementation Detail

Bad:

```csharp
// Call: Use ExecuteAsync overload #2.
```

Good:

```csharp
// Call: Legacy provider requires the compatibility flag until API v3.
```

Prefer comments that survive refactoring.

---

## 18. Context Types Worth Documenting

| Context | Example |
|---|---|
| Business rules | `// Enforce: Refund cannot exceed captured payment amount.` |
| External contracts | `// Call: Provider requires original idempotency key on every retry.` |
| Data authority | `// Assign: Ledger value is authoritative over cached balance.` |
| Consistency constraints | `// Publish: Event emitted only after transaction commits.` |
| Concurrency | `// Lock: Prevent concurrent workers from allocating same inventory.` |
| Failure semantics | `// Compensate: Release reservation created before payment failed.` |
| Security boundaries | `// Verify: Signature must match provider secret before accepting callback.` |
| Performance trade-offs | `// Batch: Group writes to reduce transaction overhead.` |
| Compatibility | `// Transform: Legacy status values mapped to new payment state model.` |
| Temporal/migration context | `// TEMP: Preserve legacy identifier until all clients use new format.` |

---

## 19. Temporal Markers

Not ordinary intent labels. Use for known temporary conditions.

```text
TODO-<period>:
TEMP:
DEADLINE-<date>:
```

Examples:

```csharp
// TODO-2026Q4: Replace with new provider SDK after migration support lands.
// TEMP: Workaround for provider rate limiting.
// DEADLINE-2026-12-15: Remove legacy feature flag after full rollout.
```

Rules:

- Include issue/owner reference when supported.
- Include concrete removal condition.
- Do not use `TEMP:` for permanent behavior.
- Remove marker when condition is removed.
- Do not convert every technical debt into a comment.

---

## 20. Anti-Patterns

| Anti-pattern | Fix |
|---|---|
| Redundant narration | Comment only non-obvious reason. |
| Vague label/descriptor | Use specific intent, e.g. `Validate: Order must contain confirmed payment.` |
| Free-text labels | Use approved vocabulary. |
| Mixed formats | Standardize `Label:` PascalCase. |
| Stale comments | Update or remove; stale misleads. |
| Implementation trivia | Document stable intent, not line mechanics. |
| Commenting every line | One comment per primary intent block. |

---

## 21. Agent Procedure

1. Read code: names, types, signatures, control flow, tests, architecture.
2. Identify missing information: rules, contracts, assumptions, edge cases, compatibility, failure, performance, security, audit, temporary conditions.
3. Decide if comment is needed. Do not duplicate info already encoded clearly.
4. Identify intent category: rule, condition, boundary, proof, invariant, creation, transition, calculation, transformation, retry, fallback, degradation, compensation, recovery, call, lock, audit.
5. Select closest approved label. Do not invent one-off labels.
6. Write context descriptor: rule, reason, assumption, constraint, source, consequence, trade-off, boundary, recovery, compatibility, lifecycle.
7. Test durability: would comment remain useful after refactor?
8. Validate final comment against checklist.

---

## 22. Human Review Checklist

```text
[ ] Could code itself explain this? If yes, remove.
[ ] Does it capture non-obvious reason, constraint, or consequence?
[ ] Is label standardized, PascalCase, followed by ":"?
[ ] Is descriptor context-specific?
[ ] Does it avoid narrating obvious operations?
[ ] Does it avoid implementation trivia?
[ ] Does it explain stable intent?
[ ] Is temporal marker used when temporary?
[ ] Can it be shortened without losing useful information?
```

---

## 23. Core Vocabulary Matrix — Compact Reference

Use §3 for labels and §14 for decisions.  
For each label, descriptor should usually capture:

- `Validate:` required rule
- `Check:` condition and context
- `Guard:` unsafe condition and consequence
- `Verify:` thing verified and verification basis
- `Assert:` invariant and significance
- `Create:` initial semantics or reason
- `Assign:` source/authority
- `Update:` state change and reason
- `Initialize:` required initial condition
- `Merge:` precedence/conflict rule
- `Clone:` snapshot/isolation purpose
- `Compute:` formula/rule/significance
- `Transform:` source → target and reason
- `Filter:` selection rule
- `Parse:` format/interpretation
- `Normalize:` canonical form
- `Generate:` source/reason
- `Await:` ordering/lifecycle requirement
- `Retry:` failure and retry safety
- `Skip:` condition/reason
- `Fallback:` primary failure and alternative
- `Batch:` efficiency/API/consistency reason
- `Throttle:` capacity/rate constraint
- `Acquire:` scope/ownership
- `Release:` lifecycle/recovery
- `Lock:` protected resource/concurrency
- `Cache:` freshness/consistency trade-off
- `Pool:` capacity/reuse
- `Dispose:` resource/lifetime requirement
- `Raise:` event significance
- `Trigger:` trigger condition/action
- `Notify:` recipient/reason
- `Enforce:` business rule/consequence
- `Handle:` input and handling semantics
- `Call:` contract/assumption
- `Send:` destination/purpose/timing
- `Receive:` source/trust/contract
- `Map:` source → target mapping
- `Publish:` timing/guarantee
- `Webhook:` security/delivery/idempotency
- `Catch:` failure and handling reason
- `Recover:` failure and recovery
- `Compensate:` prior action and reversal
- `Escalate:` failure and destination
- `Degrade:` missing capability and acceptable behavior
- `Log:` event and diagnostic purpose
- `Trace:` correlation/diagnostic reason
- `Monitor:` metric/signal and concern
- `Audit:` action and traceability reason
- `Profile:` operation and performance question

---

## 24. Context Descriptor Matrix

Recommended starting points, not mandatory templates.

| Context | Useful question | Suggested shape | Example |
|---|---|---|---|
| Business rule | What must be true? | `<Subject> must/must not <rule>` | `// Enforce: Refunds cannot exceed captured amount.` |
| Validation | What makes input acceptable? | `<Input> must satisfy <rule>` | `// Validate: Email must belong to an approved domain.` |
| Permission | What capability is required? | `<Actor> must have <capability>` | `// Check: User has permission to modify organization.` |
| Security | What must be trusted/verified? | `<Artifact> must match <trust basis>` | `// Verify: Signature must match provider secret.` |
| Invariant | What must already be true? | `<State> must hold before <operation>` | `// Assert: Transaction must be active before publishing.` |
| Authority | Which source wins? | `<Value> comes from <authority>` | `// Assign: Ledger value is authoritative over cached balance.` |
| Precedence | Which source wins conflicts? | `<Source A/B> + precedence rule` | `// Merge: Account data takes precedence over CRM values.` |
| Calculation | How is value derived? | `<Result> + calculation rule` | `// Compute: Tax is applied after item-level discounts.` |
| Transformation | Why change representation? | `<Source> → <target> + reason` | `// Transform: Historical rates preserve reporting reproducibility.` |
| Filtering | Why exclude/include data? | `<Data> + selection rule` | `// Filter: Archived products excluded from customer search.` |
| Normalization | What canonical form? | `<Input> → <canonical form>` | `// Normalize: Phone numbers stored in E.164.` |
| Ordering | What must happen before what? | `<A> before <B> + reason` | `// Await: Persist event before acknowledging message.` |
| Retry | Why is retry safe/useful? | `<Failure> + retry rationale` | `// Retry: Timeout is transient and request is idempotent.` |
| Fallback | What if primary path fails? | `<Primary failure> → <alternative>` | `// Fallback: Use cached recommendations when ML unavailable.` |
| Degradation | What capability is lost? | `<Unavailable capability> → <reduced behavior>` | `// Degrade: Continue checkout without personalization.` |
| Compensation | What must be undone? | `<Prior action> → <compensating action>` | `// Compensate: Release inventory reserved before payment failure.` |
| Recovery | How is service restored? | `<Failure> → <recovery strategy>` | `// Recover: Rebuild read model from committed events.` |
| Escalation | Who handles unresolved failure? | `<Failure> → <destination>` | `// Escalate: Settlement mismatches require manual review.` |
| Concurrency | What race must be prevented? | `<Resource> + synchronization reason` | `// Lock: Prevent concurrent allocation of same SKU.` |
| Caching | What consistency trade-off? | `<Data> + freshness/performance constraint` | `// Cache: Product metadata may remain stale for five minutes.` |
| External API | What dependency requires? | `<System> + contract requirement` | `// Call: Provider requires original correlation ID.` |
| Incoming data | What assumption applies? | `<Source> + interpretation/behavior` | `// Receive: Provider may redeliver same event.` |
| Event | Why emit this event? | `<Event> + business significance` | `// Raise: Notify subscribers after order becomes payable.` |
| Publication | When/under what guarantee? | `<Message> + timing/guarantee` | `// Publish: Emit only after transaction commits.` |
| Audit | Why preserve history? | `<Action> + accountability requirement` | `// Audit: Financial changes must remain attributable.` |
| Performance | What trade-off intentional? | `<Operation> + performance rationale` | `// Batch: Group writes to reduce transaction overhead.` |
| Compatibility | Why unusual behavior? | `<Legacy/version constraint> + reason` | `// Call: Legacy provider requires compatibility flag.` |
| Temporary | Why temporary? | `<Temporary condition> + removal condition` | `// TEMP: Workaround for provider rate limiting.` |

---

## 25. Descriptor Quality Test

Descriptor should normally answer at least one:

- Why is this necessary?
- What rule does it enforce?
- What assumption does it rely on?
- What external constraint explains it?
- What consequence does it prevent?
- What source is authoritative?
- What trade-off was chosen?
- What failure behavior is intentional?
- What lifecycle or ordering rule matters?

It does not need to answer all.

---

## 26. When No Label Fits

Do not force an unsuitable label. Ask:

1. Is the comment necessary?
2. Can better code naming express it?
3. Does an existing label describe intent accurately?
4. Is it really a temporal marker (`TODO`, `TEMP`, `DEADLINE`)?
5. Does the project have an established domain-specific label?

If none applies, leave unlabeled rather than invent arbitrary labels.

Introduce new label only when:

- intent occurs repeatedly;
- existing vocabulary cannot express it accurately;
- distinction is useful to humans or agents;
- label can be defined consistently;
- vocabulary change is adopted at project level.

---

## 27. Agent-Friendly Selection Algorithm

```text
1. Is behavior obvious?
   └─ Yes → no comment.

2. If not obvious, what information is missing?
   ├─ Rule/precondition        → Validate / Enforce
   ├─ Condition/state           → Check
   ├─ Defensive boundary        → Guard
   ├─ Independent confirmation  → Verify
   ├─ Invariant                 → Assert
   ├─ Object lifecycle          → Create / Initialize / Update
   ├─ Data derivation           → Compute / Generate
   ├─ Representation change     → Transform / Normalize / Parse / Map
   ├─ Flow strategy             → Await / Retry / Skip / Fallback / Batch / Throttle
   ├─ Resource semantics        → Acquire / Release / Lock / Cache / Pool / Dispose
   ├─ Domain behavior           → Raise / Trigger / Notify / Enforce / Handle
   ├─ Integration               → Call / Send / Receive / Publish / Webhook
   ├─ Failure behavior          → Catch / Recover / Compensate / Escalate / Degrade
   └─ Operational intent        → Log / Trace / Monitor / Audit / Profile

3. What context matters?
   ├─ rule, reason, assumption, constraint, source, consequence,
   ├─ trade-off, boundary, recovery, compatibility, lifecycle

4. Write shortest descriptor that preserves that context.

5. Verify comment remains useful after refactoring.
```

---

## 28. Compact Reference

Core form:

```text
Label: Why / rule / assumption / consequence
```

Examples:

```csharp
// Validate: Email must belong to an approved domain.
// Check: User has permission to modify the organization.
// Guard: Ignore duplicate events during provider retries.
// Verify: Signature matches the provider secret.
// Assert: Transaction must be active before publishing.
// Create: Order starts in Draft until payment is confirmed.
// Assign: Gateway response is authoritative for the external ID.
// Update: Inventory reflects the confirmed shipment quantity.
// Merge: Account data takes precedence over CRM values.
// Compute: Tax is applied after item-level discounts.
// Transform: Provider states map to internal payment states.
// Normalize: Phone numbers are stored in E.164 format.
// Retry: Timeout is transient and the request is idempotent.
// Fallback: Cached recommendations remain usable during ML downtime.
// Degrade: Continue checkout without personalization.
// Lock: Only one worker may update a SKU at a time.
// Enforce: Refund cannot exceed the captured amount.
// Call: Provider requires the original correlation ID.
// Publish: Emit only after the transaction commits.
// Compensate: Release inventory reserved for the failed payment.
// Audit: Financial changes must remain attributable to the acting user.
```

Temporal markers:

```csharp
// TODO-2026Q4: Replace with the new provider SDK.
// TEMP: Workaround for provider rate limiting.
// DEADLINE-2026-12-15: Remove the legacy feature flag.
```

---

## 29. Universal Rule

> If code already tells the reader what happens, do not comment it. When code cannot tell the reader why it must happen, document that intent with one consistent, greppable label and a context-specific descriptor.

Label standardized. Descriptor specific to intent and context.  
Do not turn descriptor suggestions into rigid templates.

---

## 30. Why This Convention Works

- **Scannable** — labels are semantic headlines.
- **Greppable** — `// Validate:` or `// Degrade:` finds intent classes.
- **Durable** — intent survives refactoring better than mechanical narration.
- **Controlled** — small vocabulary prevents comment-format drift.
- **Context-aware** — descriptors adapt to business, technical, integration, security, operational context.
- **Agent-friendly** — decision process is explicit and repeatable.
- **Human-friendly** — reviewer understands non-obvious decisions quickly.
- **Language-agnostic** — works across C#, TypeScript, Python, Java, similar ecosystems.