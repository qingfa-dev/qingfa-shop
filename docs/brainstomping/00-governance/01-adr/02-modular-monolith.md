# ADR 0002: Modular Monolith

Records the locked back-end topology: one deployable .NET service split into internal
module projects with dependency-injection contracts.

**Links:** [Governance index](../README.md) · [Spec](../../11-spec/README.md)

---

## Status

Accepted — 2026-09-23. Locked by the design spec (§2 decision 3, §5, §8).

## Context

The commerce domain spans catalog, orders, inventory, shipping, tax, promotions, payments,
returns, wishlists, reviews, accounts, and stores. These concerns are strongly related:
placing an order reads catalog prices, reserves inventory, computes tax and promotions,
and captures a payment, all within a single business transaction.

Two structural options were available:

- **A single deployable service with internal module boundaries** (modular monolith).
- **Separate network services per domain** (microservices).

Forces that shaped the decision:

- **Transactional integrity.** Order placement, stock reservation, and payment capture
  must be consistent; a single process and a single database make that tractable.
- **Deployment simplicity.** The deliverable is one API host plus two static/SSR
  front-ends; fewer moving parts reduce operational load.
- **Extensibility.** Spree's `Core::Extension` analogue must let third parties add
  behaviour without editing core modules. That requires explicit contracts, not a
  distributed service mesh.
- **Team size.** A small team would pay a coordination tax for service splits that the
  current load does not require.
- **Future optionality.** Boundaries should be clean enough that a module could be
  extracted later if a genuine scaling need appears.

## Decision

Build resys-eshop as a **modular monolith**: **one deployable .NET service**
(`Resys.Api`) that references internal module projects, each owning its entities,
services, and extension points.

### Modules

Each of the following is a module project under `src/`:

`Catalog`, `Orders`, `Inventory`, `Shipping`, `Tax`, `Promotions`, `Payments`, `Returns`,
`Wishlists`, `Reviews`, `Accounts`, `Stores`.

Supporting projects:

- `Resys.Core` — shared kernel: `Money`, identities, store scoping, domain events, and
  extension contracts.
- `Resys.Infrastructure` — EF Core + PostgreSQL, ASP.NET Identity, file storage, email,
  payment-gateway adapters, and the outbox relay.
- `Resys.Api` — the Minimal API host wiring modules through DI and exposing
  `/api/v1` and `/api/admin`.
- `Resys.Shared` — DTOs and contracts shared with API clients.

### Boundaries and contracts

- Modules interact through **explicit DI contracts**, not by reaching into each other's
  entities or DbContexts. A module publishes interfaces in `Resys.Core` or its own
  contracts assembly and consumes other modules only through those interfaces.
- Third parties extend behaviour by implementing contracts and **registering them in
  DI**, with **no source modification** of core modules. Override semantics are
  **last-registered wins** per key, documented and tested.
- Cross-module side effects are expressed as **domain events** dispatched in-process
  and persisted through an **outbox table** in the same transaction, so delivery is
  reliable without a message broker.
- Each module owns its slice of the EF Core model and migrations; there is one physical
  database with logical module ownership.

### Explicitly not microservices

There is no service-per-module split, no inter-service RPC, and no distributed
transaction coordinator. The single database is intentional.

## Consequences

**Positive**

- Atomic order/inventory/payment flows in one process and one transaction.
- One build, one deploy, one set of logs and traces — simple operations.
- Clean module contracts keep the internal architecture honest and make future
  extraction possible without committing to it now.
- The extension model (DI contracts + last-registered-wins) replaces Spree's
  monkey-patching with testable, documented seams.

**Negative / costs**

- A single process means one scaling unit; a hot module cannot be scaled independently
  without a later extraction.
- Discipline is required: nothing physical prevents a module from referencing another
  module's internals. This must be enforced by convention and review (area `05`) and by
  module-boundary tests (area `06`).
- One database couples migrations; ordering and locking need the documented migration
  process in area `03` / area `10`.
- In-process events are not durable indefinitely; the outbox gives reliable at-least-once
  delivery but consumers must be idempotent.

**Neutral**

- The module list mirrors the Spree module map in the spec's Appendix A, which is the
  stated goal for domain accuracy.

## Alternatives Considered

- **Microservices per domain.** Rejected: distributed transactions across order, stock,
  payment, and tax would add significant complexity without a demonstrated scaling
  requirement; the team and traffic profile do not justify it.
- **A single project with folders instead of module projects.** Rejected: folder-only
  separation erodes because there is no compile-time boundary; module projects make the
  dependency direction explicit.
- **A shared "god" DbContext for all modules.** Rejected: it defeats module ownership
  and makes the boundary cosmetic. Modules own their model slices; contracts carry data
  across boundaries.
- **Message broker (e.g. RabbitMQ/Kafka) for events.** Rejected for now: the outbox
  table plus in-process dispatch covers current consumers (email, analytics stub) with
  less operational overhead. A broker can be introduced behind the same event
  abstraction if volume demands it.
- **Modular monolith with an API gateway in front.** Rejected: there is one API host and
  no service fan-out for a gateway to manage.

## Open Questions

None. The module list and contract style are fixed; the detailed contract catalogue
(catalogues, method signatures) is elaborated in area `03` and does not change this
decision.

## Links

- [Governance index](../README.md)
- [Design spec](../../11-spec/README.md) — §2
  (decisions), §5 (solution layout), §8 (extension architecture).
- [ADR 0001 — Application stack](./01-stack.md)
- [ADR 0004 — Headless API and JWT auth](./04-headless-jwt.md)
- Solution layout, module contracts, and extension architecture: `03/02-01`,
  `03/02-02`, `03/02-07`.
