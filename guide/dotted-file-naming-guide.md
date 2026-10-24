---
name: dotted-file-naming
description: File-centric naming. Use when creating, renaming, or reviewing files. The filename alone must communicate subject and architectural role.
---

# Dotted File Naming — Optimized Skill Guide

**Use when:** creating, renaming, or reviewing files.  
**Goal:** filename alone tells subject + architectural role.  
**Rule:** read filename first. Folders organize; they do not carry required meaning.  
**Order:** subject → role → sub-role → casing → extension → no free-text suffixes.

---

## 1. Core Pattern

Universal:

```text
SUBJECT + RECOGNIZED ROLE + OPTIONAL SUB-ROLE
```

Every dotted segment must have a defined purpose.  
The vocabulary is **language-agnostic**; casing and delimiters are **tech-conventional**.

---

## 2. Core Vocabulary (Agnostic)

The semantic vocabulary below is the same across ecosystems.  
Each role describes an **architectural responsibility**, not a file's contents.

### 2.1 API / Transport

| Role | Responsibility |
|---|---|
| `controller` | HTTP/transport entrypoint that delegates to services |
| `router` | Route table / endpoint registration |
| `resolver` | GraphQL or similar field/query resolver |
| `middleware` | Request/response pipeline hook |
| `interceptor` | Cross-cutting call boundary (auth, logging, retries) |
| `guard` | Access/authorization gate before handling |
| `handler` | Handles a specific command/event/message |
| `endpoint` | Single-purpose API surface |
| `webhook` | External callback receiver |

### 2.2 Application / Logic

| Role | Responsibility |
|---|---|
| `service` | Application/domain use-case orchestration |
| `use-case` | Single application action |
| `command` | Intent to change state |
| `query` | Intent to read state |
| `workflow` | Multi-step orchestrated process |
| `orchestrator` | Coordinates multiple services/steps |
| `policy` | Decision/rule strategy |
| `rule` | Single business rule |
| `specification` | Composable predicate/rule object |
| `validator` | Input/state validation |
| `mapper` | Model ↔ model conversion |
| `transformer` | Representation change |
| `factory` | Object/aggregate construction |
| `builder` | Stepwise construction |
| `strategy` | Interchangeable algorithm |
| `provider` | Supplies a capability/value |

### 2.3 Data / Persistence

| Role | Responsibility |
|---|---|
| `model` | Domain/application model |
| `entity` | Persisted identity-bearing object |
| `aggregate` | Consistency boundary |
| `value-object` | Immutable value type |
| `dto` | Data transfer object across boundaries |
| `schema` | Shape/validation definition |
| `migration` | Schema/data migration |
| `seed` | Initial/reference data |
| `repository` | Persistence access for an aggregate |
| `store` | Client/state/read-model store |
| `projection` | Read-model builder |
| `snapshot` | Point-in-time state capture |

### 2.4 Contracts / Types

| Role | Responsibility |
|---|---|
| `interface` | Contract/protocol |
| `type` | Type alias/definition |
| `enum` | Enumerated values |
| `constant` | Shared immutable values |
| `config` | Configuration surface |
| `options` | Configurable behavior input |
| `event` | Domain/integration event |
| `message` | Transport message |
| `result` | Outcome/return shape |
| `error` | Error type or code container |

### 2.5 UI / Presentation

| Role | Responsibility |
|---|---|
| `component` | Reusable UI unit |
| `page` | Route-level view |
| `layout` | Structural wrapper |
| `view` | Rendered representation |
| `directive` | Declarative UI behavior |
| `pipe` | Value transformation in templates |
| `hook` | Composable stateful logic |
| `store` | UI state container |
| `slice` | State reducer segment |
| `selector` | Derived state accessor |
| `style` | Styling unit |
| `theme` | Design tokens |

### 2.6 Cross-Cutting

| Role | Responsibility |
|---|---|
| `util` | Pure, stateless helper |
| `helper` | Narrow utility (prefer `util`) |
| `extension` | Language-level extension |
| `decorator` | Wraps/annotates behavior |
| `adapter` | Boundary translation |
| `client` | External system client |
| `gateway` | Boundary façade |
| `proxy` | Delegating indirection |
| `listener` | Subscribes to events/messages |
| `scheduler` | Time/period-driven trigger |
| `worker` | Background processor |
| `job` | Discrete unit of background work |

### 2.7 Testing

| Role | Responsibility |
|---|---|
| `spec` | Behavioral specification |
| `test` | Unit/integration test |
| `mock` | Test double with expectations |
| `stub` | Test double with canned responses |
| `fake` | Lightweight in-memory implementation |
| `fixture` | Reusable test data |
| `factory` | Test data builder |
| `e2e-spec` | End-to-end specification |

### 2.8 Vocabulary Rules (Agnostic)

- Roles are **closed**; add deliberately at project level, not per file.
- Roles describe **responsibility**, not contents.
- Prefer the closest existing role over inventing a new one.
- Do not encode behavior, timing, or implementation detail in the role.
- Every dotted segment must map to a role or a recognized sub-role.
- Tech-specific suffixes **derive from this pool** — do not maintain parallel lists.

Bad:

```text
user.does-auth-stuff
order.special-handler
payment.business-logic
Order.HandlesStuff
```

Good:

```text
user.guard
order.service
payment.validator
Order.Factory
```

### 2.9 Project-Level Roles (QingFa.eShop)

These roles extend §2 for this repository. They are listed here because the
code adopts them deliberately and consistently; §9.6 says a new role is added
by extending §2, not by maintaining a parallel list.

| Role | Responsibility | Where |
|---|---|---|
| `outcome` | Failure-code container for a concept | `Todo.Outcome.cs` |
| `marker` | Type that anchors an assembly for scanning or tests | `Assembly.Marker.cs` |
| `composition` | Composition entry point for a layer or feature | `Application.Composition.cs` |
| `collection` | xUnit collection definition | `AppHost.Collection.cs` |
| `guard` | Argument and state guard | `Error.Guard.cs` |
| `behavior` | MediatR pipeline behavior | `Exception.Behavior.cs` |
| `logger` | Source-generated logging collaborator | `Logging.Behavior.Logger.cs` |
| `operator` | Operator over a type, e.g. implicit conversions | `Result.Generic.Operator.cs` |
| `severity` | Enumeration of a value's severity | `Error.Severity.cs` |
| `default` | Default implementation of a concept | `Metadata.Default.cs` |
| `exception` | Exception type or its mapping | `Result.Exception.cs` |
| `generic` | Generic arity variant of a concept | `Result.Generic.cs` |
| `design` | Design-time-only factory | `InfrastructureDbContextFactory.Design.cs` |
| `tabular` | Tabular shape variant of a concept | `EfQuery.Tabular.cs` |
| `request` | Request message shape | `Test.Request.cs` |
| `querymap` | The query field allowlist for a concept | `Todo.QueryMap.cs` |
| `queryconstant` | The field names a query surface addresses | `Todo.QueryConstant.cs` |
| `configuration` | The long form of `config`, used for EF entity configurations | `Todo.Configuration.cs` |
| `flow` | The control-flow shape of a result | `Result.Extension.Flow.Async.cs` |
| `double` | Test double standing in for a production implementation | `SqliteErrorClassifier.Double.cs` |

`enum` and `schema` are already in §2.4 and §2.3; this repository uses them
for an enumerated kind and for a database model respectively.

---

## 3. Convention by Tech

Same vocabulary (§2); different casing, delimiter, and file layout.

### 3.1 TypeScript / JavaScript

```text
<subject>.<role>[.<sub-role>].<ext>
```

- Subject: lowercase kebab-case.
- Role / sub-role: lowercase kebab-case.
- Acronyms: lowercase (`api`, `jwt`, `http`).
- Broad role first, narrow sub-role second.
- Role names come directly from §2.

Examples:

```text
user.service.ts
user.service.spec.ts
order.controller.ts
auth.guard.mock.ts
product-search.repository.ts
payment-method.schema.ts
```

Avoid:

```text
User.service.ts
user_service.ts
productSearch.service.ts
user.spec.service.ts
auth.mock.guard.ts
```

### 3.2 C# / .NET

```text
<Concept>[.<Responsibility>][.<SubResponsibility>].cs
```

- Concept: PascalCase.
- Responsibility / sub-responsibility: **the same role from §2, PascalCased**.
- Broad role first, narrow sub-role second.
- Do not maintain a parallel C# role list; **render §2 in PascalCase**.

#### 3.2.1 Role mapping

| Agnostic role (§2) | C# suffix | Used for |
|---|---|---|
| `interface` | `.Interface` | Contract for the concept |
| `extension` | `.Extension` | Extension methods |
| `result` | `.Result` | Result/outcome type |
| `error` | `.Error` / `.Errors` | Error type or code container |
| `constant` | `.Constant` | Shared constants with architectural home |
| `options` | `.Options` | Configurable behavior input |
| `factory` | `.Factory` | Object/aggregate construction |
| `builder` | `.Builder` | Stepwise construction |
| `validator` | `.Validator` | Input/state validation |
| `specification` | `.Specification` | Composable predicate/rule object |
| `mapper` | `.Mapper` | Model ↔ model conversion |
| `policy` | `.Policy` | Decision/rule strategy |
| `rule` | `.Rule` | Single business rule |

#### 3.2.2 C#-conventional additions

These are not in the agnostic pool; they are C#-specific and closed at project level.

| Suffix | Meaning |
|---|---|
| *(none)* | Main concrete type |
| `.Base` | Abstract/base implementation |

`.Base` is a C#-idiomatic shape (abstract class naming) rather than a distinct architectural responsibility, which is why it lives here and not in §2.

#### 3.2.3 Rules

- Use the agnostic role set from §2; do not maintain a parallel C# list.
- Render roles in PascalCase only.
- Add a project-level suffix only by extending §2 deliberately, not by adding it to a C#-local table.
- One responsibility suffix; two levels only when the second level has stable, recognized meaning.

#### 3.2.4 Examples

```text
Order.cs
Order.Interface.cs
Order.Base.cs
Order.Result.cs
Order.Result.Errors.cs
Order.Extension.Async.cs
Order.Factory.cs
Order.Validator.cs
Order.Mapper.cs
```

#### 3.2.5 Avoid

```text
Order.BusinessLogic.cs      // not a role in §2
Order.Helpers.cs            // use util/helper only if adopted at project level
Order.Misc.cs
Order.Result.Errors.Validation.Required.cs   // deep chain
Order.Errors.Result.cs                       // backwards order
```

### 3.3 Python

Use `snake_case`; dots have package/import semantics.

```text
user_service.py
order_repository.py
payment_validator.py
```

Do not use:

```text
user.service.py
```

### 3.4 Java / Kotlin

Use the public class/type name; package path provides namespace.

```text
UserService.java
OrderRepository.kt
PaymentValidator.kt
```

Do not force dotted naming.

### 3.5 React Components

Follow the project's component convention.

Common:

```text
Button.tsx
UserProfile.tsx
```

Do not introduce:

```text
button.component.tsx
```

into a codebase that uses component-name filenames.

### 3.6 Configuration / Infrastructure

Use the native tool convention; it is stronger than this guide.

```text
docker-compose.override.yml
jest.config.ts
webpack.config.prod.js
```

Dotted segments here identify **scope/variant**, not architectural role.

### 3.7 Error Codes

Structured, agnostic shape:

```text
<subject>.<field>.<rule>
```

Each segment is lower snake_case; split PascalCase words at segment
boundaries (`TooLong` → `too_long`, `StoreScoped` → `store_scoped`).

Examples:

```text
order.shipping_address.required
order.total.positive
user.email.invalid
localizable.property.too_long
activatable.state.already_active
```

State predicates without a natural field use `state` as the field
segment (`activatable.state.not_active`).

Avoid:

```text
Order.ShippingAddress.Required
OrderShippingInvalid
OrderBadTotal
InvalidOrderEmail
```

### 3.8 Convention Selection Matrix

| Tech | Subject case | Role case | Role source | Example |
|---|---|---|---|---|
| TypeScript / JS | kebab-case | kebab-case | §2 | `user.service.spec.ts` |
| C# / .NET | PascalCase | PascalCase | §2 + §3.2.2 | `Order.Result.Errors.cs` |
| Python | snake_case | snake_case | §2 | `user_service.py` |
| Java / Kotlin | PascalCase | PascalCase | §2 | `UserService.java` |
| React | PascalCase | project-defined | §2 | `UserProfile.tsx` |
| Config / Infra | tool-defined | tool-defined | tool | `jest.config.ts` |

---

## 4. Subject

Subject = main domain/application/technical concept.

Use:

- domain concepts over implementation details;
- singular/plural per project domain vocabulary;
- casing per tech convention (§3).

Examples:

```text
user, order, order-item, payment-method, product-search
Order, OrderItem, ShippingAddress, PaymentMethod
```

Avoid:

```text
does-authentication
special-processing
SomeSharedThings
```

---

## 5. Qualifier Order

Broad role first; narrow sub-role second.

Good:

```text
user.service.spec.ts
auth.guard.mock.ts
order.controller.e2e-spec.ts
Order.Result.Errors.cs
Order.Extension.Async.cs
```

Bad:

```text
user.spec.service.ts
auth.mock.guard.ts
Order.Errors.Result.cs
```

Second qualifier is optional. Do not add pre-emptively.

---

## 6. Filename-First Rule

Filename should remain useful without folder path.

Prefer:

```text
src/orders/order-item.repository.ts
```

over:

```text
src/orders/repository.ts
```

Exception: consistency with established repository convention takes precedence.

---

## 7. Folders

Folders are organizational, not part of filename grammar.

Both may be valid depending on project:

```text
user/user.service.ts
services/user.service.ts
```

Avoid redundant names:

```text
user/user.user.service.ts
orders/order/order.order.service.ts
```

Do not introduce a filename rule that requires a particular folder hierarchy.

This repository's namespaces mirror their folder tree, so a namespace is part
of the structure a file lives in. The one exception is the shared test kit in
`tests/Testing`, which uses a single flat `Testing` namespace so a consumer
writes `using Testing;` once.

---

## 8. Anti-Patterns

| Anti-pattern | Bad | Good |
|---|---|---|
| Free-text role names | `user.does-the-auth-stuff.ts` | `user.guard.ts` |
| Implementation-description filenames | `order.business-logic.ts` | `order.service.ts` |
| Redundant qualifier chains | `order.service.business.logic.ts` | `order.service.ts` |
| Backwards qualifier order | `user.spec.service.ts` | `user.service.spec.ts` |
| Mixed casing | `userProfile.service.ts` | `user-profile.service.ts` |
| Folder-dependent meaning | `orders/repository.ts` | `orders/order.repository.ts` |
| Deep C# chains | `Order.Result.Errors.Validation.Required.cs` | `Order.Result.Errors.cs` |
| Generic C# names | `Orders/Helpers.cs` | `Order.Mapper.cs` |
| Parallel tech role lists | a C#-only role table | derive from §2 |

---

## 9. Agent Decision Procedure

1. **Identify subject** — what single concept is this file primarily about?
2. **Identify role** — what recognized responsibility from §2 does it have?
3. **Apply tech convention from §3** for casing, delimiter, extension.
4. **Add sub-role only if necessary** — e.g. `user.service.spec.ts`, `Order.Result.Errors.cs`.
5. **Validate**:
   - subject meaningful;
   - role from approved vocabulary (§2);
   - tech renders §2 rather than redefining it;
   - qualifier order broad → narrow;
   - casing matches ecosystem;
   - extension correct;
   - understandable without folder;
   - no free-text role;
   - no unnecessary qualifier;
   - no temporary implementation details.
6. **Preserve repository consistency**:
   - inspect nearby filenames;
   - check documented vocabulary;
   - reuse existing role when accurate;
   - add new role only as deliberate project-level convention (extend §2, not a tech-local list).

Existing repository convention takes precedence over this generic guide when explicitly documented.

---

## 10. Human Review Checklist

```text
[ ] Does filename identify main subject/concept?
[ ] Does it identify architectural role?
[ ] Is every dotted segment meaningful?
[ ] Are roles from the approved vocabulary (§2)?
[ ] Does the tech convention render §2 rather than redefine it?
[ ] Is qualifier order broad → narrow?
[ ] Is casing consistent with the ecosystem?
[ ] Is filename understandable without folder?
[ ] Is name free of temporary/implementation-specific wording?
[ ] Is a second qualifier genuinely necessary?
[ ] Does it match existing repository convention?
```

---

## 11. Compact Reference

Core form:

```text
SUBJECT + RECOGNIZED ROLE + OPTIONAL SUB-ROLE
```

One vocabulary (§2). Tech conventions (§3) only change casing, delimiter, extension — never the role set.

TypeScript / JavaScript:

```text
<subject>.<role>[.<sub-role>].<ext>
user.service.ts
user.service.spec.ts
auth.guard.mock.ts
product-search.repository.ts
```

C# / .NET:

```text
<Concept>[.<Responsibility>][.<SubResponsibility>].cs
Order.cs
Order.Interface.cs
Order.Base.cs
Order.Result.cs
Order.Result.Errors.cs
```

Other tech: use §3.

Rule: Filename is the primary semantic unit. Folders organize; filenames identify.

---

## 12. One-Sentence Rule

> **Name every file so a human or agent can understand its subject and architectural role from the filename itself, using one small closed vocabulary, consistent casing for the ecosystem, and no invented descriptive suffixes.**