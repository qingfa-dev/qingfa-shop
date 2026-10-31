---
name: convention-gate
description: Keep domain values in constants, errors in outcomes, and service contracts on monadic Result. Use when adding literals, error factories, or public members to services, interfaces, or abstract classes, or when `make convention.check` fails.
---

# Convention Gate — Optimized Skill Guide

**Use when:** writing or reviewing production C#, or fixing `make convention.check`.  
**Goal:** one place for every domain value and every error; every fallible contract says `Result`.  
**Rule:** a literal with meaning gets a name; an error gets an outcome; a fallible public member returns `Result`.

---

## 1. Why the Gate Exists

Three rules are cheaper to follow than to review:

1. **No inline magic values** — the same `30` or `todo.completed` must not appear in five files.
2. **No inline outcomes** — callers consume stable codes, not locally invented strings.
3. **Monadic results** — callers handle failure without catching exceptions or guessing sentinels.

`make convention.check` verifies all three without a build. The baseline records
legacy exceptions; it never accepts new ones.

---

## 2. Rule Summary

| Rule | Fails when | Fix |
|---|---|---|
| `magic-number` | a literal with abs ≥ 10 sits outside a constant file | declare it in the owning `*Constant.cs` |
| `magic-string` | a wire-shaped literal (dotted, hyphenated, colon key) sits outside a constant file | declare it in the owning `*Constant.cs` |
| `outcome-shape` | `Error.*(` appears outside `*Outcome.cs`, or an outcome lacks `Codes`/grouping | move the error into the owning outcome |
| `monadic-result` | a public/protected `Service`/`Interface`/`Abstract` member returns `void`, `Task`, `ValueTask`, or `Task<T>` that is not `Result` | return `Result` / `Result<T>` |

---

## 3. Magic Numbers

Flagged: integer or decimal literals whose absolute integer part is **10 or more**.

```csharp
// Bad: 30 is a domain value with no name.
command.CommandTimeout = 30;

// Good: the value lives with its siblings.
command.CommandTimeout = PersistenceConstant.Commands.TimeoutSeconds;
```

Not flagged:

- `0`–`9`, negative small values, hex (`0x…`), binary (`0b…`).
- literals inside comments, XML docs, and string contents.
- files whose name contains `Constant`, or ends with `Outcome.cs`, `Codes.cs`,
  `Enum.cs`, `Option(s).cs`, `Setting(s).cs`, `Config.cs`, `Model.cs`,
  `Logger.cs`, or generated suffixes.
- `**/Migrations/**`, `**/obj/**`, `**/bin/**`, and tests.

Where the constant belongs:

| Value kind | Owning file |
|---|---|
| Command timeout, batch size, retry, page size | the role's `*Constant.cs` next to the member |
| Column length, precision, schema limit | the entity/configuration's constants |
| Event ID | the `*Logger.cs` that declares the `LoggerMessage` |
| Provider or integration name | the adapter's `*Constant.cs` |

---

## 4. Magic Strings

Only **wire-shaped** literals are flagged, so prose stays legal.

| Shape | Example | Regex |
|---|---|---|
| dotted identifier | `todo.completed` | `^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9_]+)+$` |
| hyphenated identifier | `X-Correlation-Id` | `^[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)+$` |
| colon key | `seeder:todos`, `todo:{id}` | `^[a-z][a-z0-9]*(?::[A-Za-z0-9_*{}.-]+)+$` |

```csharp
// Bad: the routing key is a contract shared with the consumer.
options.PublishMessage<TodoCompletedIntegration>()
    .ToRabbitRoutingKey(exchange, "todo.completed", ...);

// Good: the contract has one owner.
options.PublishMessage<TodoCompletedIntegration>()
    .ToRabbitRoutingKey(
        exchange,
        MessagingConstant.Events.TodoCompleted,
        ...);
```

Never flagged: URIs (`http://`, `amqp://`, `redis://`, `unix://`), connection
strings (`Host=`, `Server=`, `Data Source=`, `Endpoint=`), MIME types
(`application/…`), anything containing whitespace, `nameof(...)`, and
interpolation holes.

---

## 5. Outcome Shape

Every `*Outcome.cs` is the single owner of one capability's errors.

```csharp
public static class TodoOutcome
{
    public static class Failure
    {
        public static Error NotFound => Error.NotFound(
            code: Codes.NotFound,
            message: "Todo was not found.");
    }

    // Codes: this class owns dynamic builders too.
    public static class Codes
    {
        public const string NotFound = "Todo.NotFound";

        public static string For(string operation, string reason) =>
            $"Todo.{operation}.{reason}";
    }
}
```

Required shape:

1. root `static class` ending in `Outcome`;
2. nested `Codes`;
3. nested `Failure` and/or `Success`;
4. every code-taking `Error.*(...)` call passes `Codes.*` or a builder declared
   in `Codes`.

Not allowed anywhere else in `src/` or `app/`:

```csharp
// Bad: an inline outcome in a handler.
return Result.Fail(Error.NotFound(code: "Todo.NotFound", message: "Missing."));
```

Two documented exceptions exist: `Result.Generic.cs` builds the primitive
errors, and `PermissionMetadata.cs` maps FluentValidation codes dynamically.
`Error.Unexpected(exception)` and `Error.FromException(exception)` take no code,
so the `Codes` requirement does not apply to them.

---

## 6. Monadic Results

Scope: public and protected members of `*.Service.cs`, `*.Interface.cs`, and
`*.Abstract.cs`.

| Declared return | Verdict |
|---|---|
| `Result`, `Result<T>` | allowed |
| `Task<Result>`, `Task<Result<T>>`, `ValueTask<Result>`, `ValueTask<Result<T>>` | allowed |
| `bool`, `string`, `Type`, `IReadOnlyList<T>`, `Uri`, `Expression<…>` | allowed — pure, cannot fail |
| `void` | violation |
| `Task`, `ValueTask` | violation |
| `Task<T>` / `ValueTask<T>` where `T` is not `Result` | violation |

Exempt: constructors, properties, events, operators, indexers, `Dispose`,
`DisposeAsync`, `Deconstruct`, and framework `override` members (their return
type is fixed by the base class).

```csharp
// Bad: the caller cannot tell a missing row from a transport failure.
Task<PaymentReadModel?> FindAsync(Guid id, CancellationToken ct);

// Good: absence and failure are both explicit.
Task<Result<Maybe<PaymentReadModel>>> FindAsync(Guid id, CancellationToken ct);
```

```csharp
// Bad: void hides every failure mode.
void Start();

// Good: startup can fail, and the caller decides how to react.
Task<Result> StartAsync(CancellationToken cancellationToken);
```

Composition hooks such as `void Configure(IServiceCollection services)` are
legacy baseline entries, not a pattern to copy.

Pure helpers stay simple — do not wrap `bool IsExpired(now)` in a `Result`.

---

## 7. Baseline Policy

`scripts/common/convention-baseline.tsv` records legacy exceptions:

```text
# rule            path                                     key        count  reason
magic-number      src/.../Todo.Seeder.cs                   30         3      legacy: command timeout
outcome-shape     src/.../Caching.Outcome.cs               Codes      1      legacy: dynamic builder
monadic-result    src/.../IPaymentReadStore.Interface.cs   FindAsync  7      legacy: read store
```

Rules:

- A row suppresses `count` occurrences of the same `(rule, path, key)`.
- More occurrences than `count` fail: adding one more `30` to a baselined file
  is a new violation.
- A row that matches nothing fails the run as **stale**; remove it.
- Duplicate rows fail the run.
- New code must not add rows. Fix the value, the outcome, or the return type.
- `--write-baseline` regenerates the file and must be reviewed in the diff.
  `SELF_TEST=1` only exercises the checker's parsing fixtures; it never
  rewrites the baseline.

---

## 8. Agent Procedure

```text
1. Need a literal?           -> add it to the owning *Constant.cs.
2. Need an error?            -> add a factory to the owning *Outcome.cs under Failure.
3. Need a dynamic code?      -> add the builder inside Codes.
4. Writing a service member? -> return Result / Result<T>.
5. Pure helper?              -> return the plain type; do not wrap it.
6. Gate fails?               -> fix the finding; do not baseline it.
7. Legacy finding?           -> confirm the baseline row still exists and has a reason.
```

---

## 9. Human Review Checklist

```text
[ ] No new numeric literal >= 10 outside a constant file.
[ ] No new dotted/hyphenated/colon string outside a constant file.
[ ] Every Error.* construction lives in an *Outcome.cs.
[ ] Every outcome has Codes and Failure/Success.
[ ] Fallible service/interface/abstract members return Result.
[ ] Pure helpers and framework overrides are not wrapped in Result.
[ ] No new baseline rows, and no extra occurrence of a baselined value.
[ ] make convention.check passes.
```

---

## 10. Compact Reference

```text
Value      -> *Constant.cs        (PersistenceConstant.Commands.TimeoutSeconds)
Error      -> *Outcome.cs         (TodoOutcome.Failure.NotFound)
Error code -> *Outcome.cs Codes   (TodoOutcome.Codes.NotFound)
Contract   -> Result / Result<T>  (Task<Result<Maybe<Todo>>> LoadAsync(...))
```

Commands:

```bash
make convention.check                        # the gate
make convention.check SELF_TEST=1            # checker parsing fixtures
make convention.check BASELINE=/tmp/x.tsv    # alternate baseline
make lint                                    # includes the gate
```

---

## 11. One-Sentence Rule

> **Every literal that carries meaning gets a named constant, every error gets an outcome, and every fallible service contract returns `Result` — the gate enforces it, and the baseline only ever shrinks.**
