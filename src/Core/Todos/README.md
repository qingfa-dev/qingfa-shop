# Todos

Task tracking domain model for the Core layer. _module · kernel_

## Summary

This folder owns the todo domain model: a `Todo`, the `TodoList` it can be
assigned to, and the `TodoCategory` it can reference. The three are independent
aggregate roots that reference each other by identifier only, so loading a todo
never pulls in its list or its categories. Every operation returns a `Result`
and every state change raises a domain event.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Todo.cs` | aggregate | The todo aggregate root and its `Create` factory |
| `Todo.Interface.cs` | interface | `ITodo` contract |
| `Todo.Constant.cs` | constant | Title and description limits, category cap, defaults |
| `Todo.Outcome.cs` | outcome | Failure errors for the todo rules |
| `Todo.Validator.cs` | validator | Pure invariant checks and the status transition check |
| `Todo.Extension.cs` | extension | The thirteen behaviours: transitions, title, due date, references |
| `Todo.Event.cs` | event | `TodoCreated`, `TodoStarted`, `TodoCompleted`, `TodoCancelled`, `TodoReopened` |
| `TodoList.*` | aggregate | The list aggregate, its contract, constants, outcomes, validator, behaviours, and events |
| `TodoCategory.*` | aggregate | The category aggregate, its contract, constants, outcomes, validator, behaviours, and events |
| `TodoStatus.cs` | enum | The four lifecycle states |
| `TodoStatus.Extension.cs` | extension | The transition table and the terminal-state check |
| `TodoPriority.cs` | enum | The four urgency levels |

## Boundaries

In scope:
- The todo, list, and category aggregates and their invariants.
- The status machine and the events the aggregates raise.

Out of scope:
- Persistence and EF Core mapping → [`../../Infrastructure/`](../../Infrastructure/README.md).
- Use-case orchestration and event handlers → [`../../Application/`](../../Application/README.md).
- Wire shapes → application handlers under [`../../Application/`](../../Application/README.md).
- Reminders, recurrence, subtasks, attachments, and comments.

## Assumptions

- A tenant identifier is always supplied at creation; there is no unscoped todo.
- The clock is supplied by the caller. `Create` and `Complete` take `nowUtc`
  rather than reading the system clock, so the aggregates stay deterministic.
- `ListId` and `CategoryIds` are references to aggregates this layer cannot see.
  The Core layer checks shape, never existence; the Application layer checks
  that the target exists and belongs to the same tenant.
- The caller stamps modification metadata. `IModifiable` is a separate,
  deliberate step: call `.MarkModified(nowUtc, actor)` after a batch of changes.

## Invariants

- A todo is never both `Completed` and `Cancelled`.
- `Pending → Completed` is not a legal move; a todo must be started first.
- A cancelled todo must be reopened before it can be started or completed.
- A soft-deleted aggregate rejects every mutation.
- A todo references at most 20 categories, and never the same one twice.
- A due date never precedes the creation date.

## Contracts

- `ITodo`, `ITodoList`, `ITodoCategory` — the contracts the Application layer
  programs against.
- `TodoStatusExtensions.CanTransitionTo` — the single source of truth for legal
  status moves.
- `TodoCreated`, `TodoStarted`, `TodoCompleted`, `TodoCancelled`,
  `TodoReopened`, `TodoListCreated`, `TodoListRenamed`, `TodoCategoryCreated`,
  `TodoCategoryRenamed` — Mediator notifications.

## Conventions

- Every aggregate exposes a static `Create` that returns `Result<T>`; there is
  no public constructor.
- Every mutation is an extension on `Result<T>` that validates before it
  mutates, so a rejected operation leaves the aggregate unchanged.
- Error codes are `Subject.Field.Rule`: `Todo.Title.Required`,
  `TodoList.Name.TooLong`.
- Validation failures are `400`; state conflicts are `409`; a missing entity is
  `422`.
- `RaiseEvent` is `internal`. Outer layers read `Events` but never raise a todo
  event themselves.

## Flows

### Create a todo

1. `Todo.Create` normalises the title and description.
2. `InitializeAudit` stamps `CreatedAtUtc` and `CreatedBy`.
3. `SetTenantId` stamps the tenant.
4. `TodoValidator.Validate` checks every invariant.
5. `TodoCreated` is raised.

### Complete a todo

1. `Complete` checks the todo is not deleted.
2. `TodoValidator.ValidateTransition` consults the transition table.
3. The status becomes `Completed` and `CompletedAtUtc` is stamped.
4. `TodoCompleted` is raised.

## Reading Order

1. `TodoStatus.Extension.cs` — the transition table.
2. `Todo.cs` and `Todo.Interface.cs` — the aggregate and its contract.
3. `Todo.Validator.cs` — the invariants.
4. `Todo.Extension.cs` — the behaviours.
5. `Todo.Event.cs` — what the aggregate announces.

## Related

- Parent: [`../README.md`](../README.md)
- Sibling: [`../Common/`](../Common/) _(no README)_

## Notes

- `TODO-2027Q1:` Add a persistence mapping. Each aggregate has a private
  parameterless constructor ready for a materializer; the `CategoryIds`
  collection will need a join table.
- Known gap: cross-aggregate integrity — a `ListId` may point at a list that no
  longer exists — is out of reach for this layer by design.
