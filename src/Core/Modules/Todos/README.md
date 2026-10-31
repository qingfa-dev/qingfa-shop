# Todos

Task tracking domain model for the Core layer. _module · kernel_

## Summary

This folder owns the todo business module: a `Todo`, the `TodoList` it can be
assigned to, the `TodoCategory` it can reference, and the `TodoReminder` that
schedules a reminder. The aggregates are independent roots that reference each
other by identifier only, so loading a todo never pulls in its list, categories,
or reminders. Every operation returns a `Result` and every state change raises a
domain event.

## Contains

| Path | Role | Notes |
|---|---|---|
| `Items/Todo.cs` | aggregate | The todo aggregate root and its factory |
| `Items/Todo.Interface.cs` | interface | `ITodo` contract |
| `Items/Todo.Constant.cs` | constant | Title and description limits, category cap, defaults |
| `Items/Todo.Outcome.cs` | outcome | Failure errors for the todo rules |
| `Items/Todo.Validator.cs` | validator | Pure invariant checks and the status transition check |
| `Items/Todo.Extension.cs` | extension | The behaviours: transitions, title, due date, references |
| `Items/Todo.Event.cs` | event | `TodoCreated`, `TodoUpdated`, `TodoStatusChanged`, and related events |
| `Items/Todo.Factory.cs`, `Items/Todo.Snapshot.cs` | event sourcing | Rehydration factory and snapshot payload |
| `Items/TodoStatus.cs`, `Items/TodoStatus.Extension.cs` | enum · extension | The lifecycle states and the transition table |
| `Items/TodoPriority.cs` | enum | The urgency levels |
| `Lists/TodoList.*` | aggregate | The list aggregate, its contract, constants, outcomes, validator, behaviours, and events |
| `Categories/TodoCategory.*` | aggregate | The category aggregate, its contract, constants, outcomes, validator, behaviours, and events |
| `Reminders/TodoReminder.*` | aggregate | The reminder aggregate, factory, snapshot, constants, outcomes, validator, and events |

## Boundaries

In scope:
- The todo, list, category, and reminder aggregates and their invariants.
- The status machine and the events the aggregates raise.

Out of scope:
- Persistence and EF Core mapping → [`../../../Infrastructure/`](../../../Infrastructure/README.md).
- Use-case orchestration and event handlers → [`../../../Application/`](../../../Application/README.md).
- Wire shapes → application handlers under [`../../../Application/`](../../../Application/README.md).
- Recurrence, subtasks, attachments, and comments.

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

1. `Todo.Create` trims the title and description, applies the tenant and audit
   values, and sets the initial status.
2. `EnsureCreatedAt` rejects a missing or default creation timestamp.
3. `TodoValidator.Validate` checks every invariant.
4. `TodoCreated` is raised through `RaiseEvent`.

### Complete a todo

1. `Complete` calls `Transition(TodoStatus.Completed, ...)`.
2. `TodoValidator.ValidateTransition` consults the transition table.
3. The status becomes `Completed` and `CompletedAtUtc` is stamped.
4. `TodoStatusChanged` is raised.

## Reading Order

1. `Items/TodoStatus.Extension.cs` — the transition table.
2. `Items/Todo.cs` and `Items/Todo.Interface.cs` — the aggregate and its contract.
3. `Items/Todo.Validator.cs` — the invariants.
4. `Items/Todo.Extension.cs` — the behaviours.
5. `Items/Todo.Event.cs` — what the aggregate announces.
6. `Lists/`, `Categories/`, `Reminders/` — the sibling aggregates.

## Related

- Parent: [`../../README.md`](../../README.md)
- Siblings: [`Categories/`](Categories/), [`Items/`](Items/), [`Lists/`](Lists/), [`Reminders/`](Reminders/)

## Notes

- `TODO-2027Q1:` Add reminder recurrence rules; the persistence mapping for the
  existing aggregates lives under
  [`Infrastructure/Modules/Todos/Persistence`](../../../Infrastructure/Modules/Todos/Persistence).
- Known gap: cross-aggregate integrity — a `ListId` may point at a list that no
  longer exists — is out of reach for this layer by design.
