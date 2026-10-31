# Unit Tests

The unit scope per module, the test project layout, naming and arrangement conventions, and the boundary between what may be doubled and what must be exercised for real.

**Links:** [Test matrix](./01-matrix.md) · [Integration tests](./03-integration.md) · [End-to-end tests](./04-e2e.md) · [Coverage](./05-coverage.md) · [Solution layout](../../03-architecture/02-components/01-solution-layout.md) · [Module contracts](../../03-architecture/02-components/02-module-contracts.md) · [.NET style](../../05-development/02-conventions/01-dotnet-style.md) · [Verification index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Unit tests are the fast, deterministic layer that proves one behavior of one type. The back-end half
runs in `tests/Resys.UnitTests` on xUnit and requires no Docker and no network; the front-end half
runs in `web/storefront`, `web/admin`, and `web/shared` on Vitest with Nuxt test utils (see
"Front-End Unit Layer" below). Both are the first gate in the CI pipeline (`05/03-03`). This file
fixes what belongs in a unit test per module, how a test is named and arranged, and which
collaborators may be replaced. Persistence and HTTP are proven in `06/01-03`; the language-level
test style floor is `05/02-01`.

The guiding rule: **a unit test replaces every collaborator that crosses a process or a database
boundary and exercises the unit under test for real.** A test that opens a connection is not a unit
test.

---

## Unit Scope per Module

The table names the behavior each module's unit tests must cover. Engine algorithms (promotions, tax,
shipping, order state machine) are the highest-value units because their arithmetic is the most
expensive to get wrong and the cheapest to prove in isolation.

| Module | Unit scope |
|---|---|
| Catalog | Option-type/value resolution to exactly one variant; slug and SKU uniqueness; translation fallback to the store default locale; price-set invariants; taxonomy tree rules. |
| Orders | Every legal state transition and every rejected one; line-item merge on repeated add; address snapshot semantics; the totals formula `item total + adjustments + tax`; idempotency-key replay returns the original order. |
| Inventory | `on_hand`/`reserved`/`available` arithmetic; backorder policy; `StockMovement` deltas sum to `on_hand`; reserve, release, and commit keyed by order. |
| Shipping | Zone match specificity (city > region > country); calculator key resolution; free-shipping threshold and fixed-rate math. |
| Tax | Jurisdiction walk and fallback; per-line half-up rounding to minor units; inclusive vs exclusive extraction; tax base is the post-promotion amount. |
| Promotions | Rule AND semantics; the stable application-order tuple; the per-line clamping algorithm and its worked example; coupon validation reasons; usage-limit gating. |
| Payments | Payment-attempt state; `IPaymentGatewayRegistry` last-registered-wins; refund ceiling (captured minus prior refunds); no PAN or CVV reaches a domain type. |
| Returns | Eligibility window; request → approve → receive transitions; restock idempotency per accepted item; refund vs store-credit settlement. |
| Wishlists | Get-or-create per (customer, store, name); idempotent item add; ownership enforcement. |
| Reviews | One active review per product per store; moderation transitions (`pending` → `published`/`rejected`); verified-purchase computed from order history. |
| Accounts | Address book default handling; email uniqueness; `IAdminAccessPolicy.CanManageStoreAsync` for assigned, unassigned, and platform admins. |
| Stores | Store resolution by id, code, and domain; supported-locale and currency validation; store settings never rewrite a placed order. |
| Cross-cutting | Dependency direction (`03/02-01`); module entry-point surface; `KeyedRegistry` last-registered-wins; `Money` currency mismatch rejection; domain events carry `StoreId` and `OccurredAtUtc`. |

Architecture-boundary assertions live here because they need no infrastructure: a test reflects over
the solution's project references and fails when a module references another module, when `Resys.Core`
references an outward project, or when a module exposes a public type other than `<Name>Module`
(`03/02-02`).

---

## Project Layout

```
tests/Resys.UnitTests/
├─ Resys.UnitTests.csproj
├─ Architecture/            # dependency-direction and contract-surface tests
├─ Catalog/                 # one folder per module
├─ Orders/
├─ Inventory/
├─ Shipping/
├─ Tax/
├─ Promotions/
├─ Payments/
├─ Returns/
├─ Wishlists/
├─ Reviews/
├─ Accounts/
├─ Stores/
└─ Fakes/                   # shared hand-written test doubles and builders
```

- **Folder per module**; a test for a module type lives in the module's folder. The namespace mirrors
  the folder (`Resys.UnitTests.Orders`), matching the project convention in `05/02-04`.
- **One test class per unit under test**, named `<TypeUnderTest>Tests` (`OrderStateMachineTests`,
  `PromotionClampingTests`). A test class with a second subject is split.
- **Shared doubles and builders** live in `Fakes/`; a double used by one module stays in that module's
  folder.

---

## Naming and Arrangement

- **xUnit `[Fact]` and `[Theory]`.** Use `[Theory]` with `[InlineData]` for a table of equivalent
  cases (rounding inputs, jurisdiction levels) and `[Fact]` for a single behavior.
- **Test method name is `Method_State_Expected`** (`Advance_FromCartWithoutPayment_ThrowsConflict`,
  `Apply_OrderDiscountExceedsSubtotal_ClampsAtZero`), per `05/02-01`.
- **One behavior per test.** A test that asserts two unrelated behaviors is split so a failure names
  the defect.
- **Arrange–Act–Assert**, in that order, with a blank line between sections. The arrange block builds
  the input through a builder; the act block is one call; the assert block names the expected value.
- **Async tests are `async Task`**, never `async void`, and accept the framework `CancellationToken`
  when one is provided (`05/02-01`).
- **No `Thread.Sleep` and no wall clock.** Time enters through `IClock`; a test uses `TestClock` and
  moves it explicitly. This keeps window, effective-date, and expiry tests deterministic.
- **No randomness without a seed.** When an id or a value must be unique, derive it from the test
  name or an explicit counter, never from `Guid.NewGuid()` at assertion time.
- **Data builders over long constructors.** `OrderBuilder`, `PromotionBuilder`, and similar builders
  in `Fakes/` produce a valid aggregate with overridable defaults, so a test states only the fields it
  cares about and the arrange block stays readable.

---

## Mocking Boundaries

There is **no third-party mocking framework**. The module contracts and infrastructure abstractions
are narrow interfaces, so a hand-written fake is clearer, faster, and free of expression-tree
fragility. The shared doubles are:

| Double | Implements | Behavior |
|---|---|---|
| `TestClock` | `IClock` | A settable UTC instant; `Advance` moves it. |
| `FakeOutboxWriter` | `IOutboxWriter` | Captures written events in a list for assertion. |
| `RecordingEventDispatcher` | `IDomainEventDispatcher` | Records dispatched events; never touches a broker. |
| `FakeEmailSender` | `INotificationService` | Records messages instead of sending (no `IEmailSender` port exists — email is a Notifications channel). |
| `InMemoryFileStorage` | `IFileStorage` | Stores bytes in a dictionary. |
| `FakePaymentGateway` | `IPaymentGateway` | Scriptable authorize/capture/refund outcomes. |
| `FakeShippingCalculator` | `IShippingCalculator` | Returns a configured quote for a key. |
| `StubStoreContext` | `IStoreContext` | Returns a fixed resolved store. |

The boundary rules:

1. **Only `Resys.Core` contracts and `Resys.Infrastructure` abstractions may be doubled.** A module's
   entities, value objects, and services are exercised for real; replacing them would test the fake.
2. **EF Core and PostgreSQL are never faked in a unit test.** A test that needs a `DbContext` is an
   integration test (`06/01-03`). `InMemory` is not a substitute for PostgreSQL.
3. **The database is never started, and no HTTP client is created.** A unit test that constructs a
   `WebApplicationFactory` is mis-layered.
4. **Time and identity are injected, never ambient.** `DateTime.UtcNow` and `Guid.NewGuid()` do not
   appear in domain code under test; `IClock` and typed ids do.
5. **A double asserts interaction only when the interaction is the behavior.** Prefer asserting the
   resulting state or the captured event over asserting that a method was called.

---

## What Is Not a Unit Test

- EF Core mapping, indexes, and migrations — `06/01-03`.
- Transaction atomicity of checkout confirm, outbox persistence, and relay delivery — `06/01-03`.
- Endpoint routing, status codes, problem details, auth, CSRF, and store scoping — `06/01-03`.
- A user journey through a browser — `06/01-04`.
- Gateway or email provider integration — the gateway is doubled in unit and integration tests; a real
  sandbox is out of scope for the required layers (`03/04-05`).

---

## Front-End Unit Layer (Vitest)

The front-end half of the unit layer proves Pinia stores, composables, formatters, and components
with behavior in isolation, using **Nuxt test utils (Vitest)** in `web/storefront`, `web/admin`, and
`web/shared`. It is owned here (`06/01-02`) and runs with each app's `test` script in the front-end
CI stage (`05/03-03`); the layer boundaries in `06/01-01` apply to it unchanged.

- **HTTP is stubbed at the `web/shared` API client boundary.** A test never starts the API host or
  the Nuxt server; a component that needs data receives a stub client or mocked fetch.
- **One behavior per test**, mirroring the back-end rule: `useCart` add/remove semantics,
  `useMoney` formatting, the variant option resolver, checkout-step gating, and problem-detail
  mapping are proven here rather than in E2E.
- **Pure formatting is not re-tested in E2E.** The front-end coverage matrix in `06/01-01` assigns
  each surface to the component or E2E layer.
- **Tooling is Vitest via Nuxt test utils**, consistent with the front-end stack; there is no
  third-party mocking framework on either half of the unit layer.

---

## Open Questions

1. Whether the architecture tests use a hand-written reflection walk or a dedicated analyzer package
   is a scaffold decision; the assertions in `03/02-02` are the contract either way.
2. The exact shared-builder API is settled when the test project is scaffolded; the naming and
   arrangement rules above do not depend on it.
3. Whether coverage thresholds are enforced per test project or per module assembly is fixed in
   `06/01-05`.
