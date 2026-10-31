# Extension Architecture

How third parties extend resys-eshop without modifying core modules: the five Core contracts, DI registration, last-registered-wins override semantics, and two complete worked examples.

**Links:** [Module contracts](./02-module-contracts.md) · [Solution layout](./01-solution-layout.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

Spree's `Core::Extension` let third parties reopen core classes and monkey-patch behavior: an
untestable, order-dependent model. resys-eshop replaces it with **contracts, not hooks** — a module
exposes an interface, an extension implements it, and the extension is registered in DI. No core
source is modified, and override behavior is an explicit, documented rule. This file defines the
five contract families declared in `Resys.Core`, shows how they are registered and resolved, and
walks through two full implementations. Module ownership rules are `03/02-02`; gateway adapters are
`03/04-05`.

An extension is an ordinary .NET class library referencing `Resys.Core` (and optionally
`Resys.Shared`), packaged, referenced by `Resys.Api`, and enabled via its registration method.

---

## Contract Catalog

All five contracts live in `Resys.Core`; the four keyed ones share `string Key { get; }` (the
`IHasKey` marker) so they resolve through a keyed registry.

| Contract | Namespace | Role | Override model |
|---|---|---|---|
| `IShippingCalculator` | `Resys.Core.Shipping` | Compute a shipping quote for a cart/destination. | Last-registered wins per key. |
| `IPaymentGateway` | `Resys.Core.Payments` | Authorize, capture, refund, void, query status, and handle webhooks. | Last-registered wins per key. |
| `IPromotionAction` | `Resys.Core.Promotions` | Produce adjustments for one configured promotion action. | Last-registered wins per key. |
| `ITaxZoneResolver` | `Resys.Core.Tax` | Resolve the tax jurisdiction for an address. | Last-registered wins per key. |
| `OrderStateHooks` | `Resys.Core.Orders` | Observe order state transitions and lifecycle events. | Fan-out: all run, in registration order. |

`OrderStateHooks` is intentionally the one non-keyed contract. A resolver answers "which one?" and
must have a single winner; a hook answers "tell everyone", so every registration runs. An extension
that must *replace* behavior uses a keyed contract; one that must *observe* uses a hook.

---

## The Contracts

### IShippingCalculator — `Resys.Core.Shipping`

```csharp
public interface IShippingCalculator : IHasKey
{
    Task<ShippingQuote> CalculateAsync(ShippingCalculationContext context, CancellationToken ct = default);
}
```

### IPaymentGateway — `Resys.Core.Payments`

```csharp
public interface IPaymentGateway : IHasKey
{
    Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken ct = default);
    Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken ct = default);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default);
    Task<VoidResult> VoidAsync(VoidRequest request, CancellationToken ct = default);
    Task<PaymentStatusResult> GetStatusAsync(string providerReference, CancellationToken ct = default);
    Task<WebhookOutcome> HandleWebhookAsync(PaymentWebhook webhook, CancellationToken ct = default);
}
```

### IPromotionAction — `Resys.Core.Promotions`

```csharp
public interface IPromotionAction : IHasKey
{
    Task<PromotionActionOutcome> ApplyAsync(PromotionActionContext context, CancellationToken ct = default);
}
```

`PromotionActionContext` carries the `PromotionDefinition` (which exposes `Decimal(string key)`,
`Matches(PromotionLine line)`, and `CouponCode`), the order lines, the coupon code, and the
subtotal. `PromotionActionOutcome` is either `NotApplied` or `Applied(adjustments)`, where each
`PromotionAdjustment` names a line item, an amount, a source key, and a label.

### ITaxZoneResolver — `Resys.Core.Tax`

```csharp
public interface ITaxZoneResolver : IHasKey
{
    Task<TaxZoneResolution> ResolveAsync(TaxZoneRequest request, CancellationToken ct = default);
}
```

### OrderStateHooks — `Resys.Core.Orders`

```csharp
public abstract class OrderStateHooks
{
    public virtual Task OnTransitionAsync(OrderTransitionContext context, CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task OnOrderPlacedAsync(OrderPlacedContext context, CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task OnShippedAsync(OrderShippedContext context, CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task OnCanceledAsync(OrderCanceledContext context, CancellationToken ct = default) => Task.CompletedTask;
}
```

The Orders module invokes every registered `OrderStateHooks` once per matching event, in
registration order, after the state change commits. A hook that throws is logged and does not roll
back the transition.

---

## DI Registration and Resolution

Modules register their built-ins; an extension registers its own afterward. Container order is the
override mechanism: `AddCommerceModules()` then `AddInfrastructure()` then the extension's
`Add<Extension>()` call, all on the same `IServiceCollection` in `Resys.Api/Program.cs`.

Keyed contracts resolve through an open-generic registry. Per `03/02-08` / ADR 0005 the **physical type** lives in `BuildingBlocks.Kernel` (pure toolkit); contract families stay in Core/`Resys.Core.*` namespaces until naming ADR updates them. Sample shape:

```csharp
namespace Resys.Core;

public interface IHasKey { string Key { get; } }

public interface IKeyedRegistry<T> where T : class, IHasKey { T Resolve(string key, T fallback); }

public sealed class KeyedRegistry<T> : IKeyedRegistry<T> where T : class, IHasKey
{
    private readonly Dictionary<string, T> _byKey;

    public KeyedRegistry(IEnumerable<T> registrations)
    {
        _byKey = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var registration in registrations)
        {
            _byKey[registration.Key] = registration; // last registration for a key replaces earlier ones
        }
    }

    public T Resolve(string key, T fallback) =>
        _byKey.TryGetValue(key, out var registration) ? registration : fallback;
}
```

### Last-registered wins per key

`KeyedRegistry<T>` enumerates registrations in order into a dictionary indexed by `Key`, so a later
registration with the same key overwrites the earlier one while different keys coexist:

- To replace a built-in, an extension registers an implementation with the **same key** after
  `AddCommerceModules()`; the last one wins.
- To add a capability, it registers a **new key**; nothing is overridden.
- Two extensions claiming the same key resolve deterministically to whichever registered last; this is documented and covered by a test in `tests/Resys.UnitTests`.

`OrderStateHooks` is registered as `IEnumerable<OrderStateHooks>` and invoked for every element; it
does not pass through `KeyedRegistry`.

The typed registries exposed per concern — for example `IPaymentGatewayRegistry` (`03/02-02`) — are
thin facades over this single Core `KeyedRegistry<T>`, the only home of the last-registered-wins logic.

---

## Worked Example 1 — Flat-Rate Shipping Calculator

Add a store-scoped flat rate alongside the built-in weight-based calculator. The key `flat-rate`
differs from the built-in key, so this is purely additive; implementation and registration follow.

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resys.Core;
using Resys.Core.Shipping;

namespace Acme.Resys.Extensions.Shipping;

public sealed class FlatRateShippingCalculator : IShippingCalculator
{
    public const string CalculatorKey = "flat-rate";
    private readonly FlatRateOptions _options;

    public FlatRateShippingCalculator(IOptions<FlatRateOptions> options) => _options = options.Value;
    public string Key => CalculatorKey;

    public Task<ShippingQuote> CalculateAsync(ShippingCalculationContext context, CancellationToken ct = default)
    {
        var amount = _options.RateFor(context.Store.Currency) ?? _options.DefaultRate;
        var quote = new ShippingQuote(amount, CalculatorKey, _options.DisplayName, _options.EstimatedDays);
        return Task.FromResult(quote);
    }
}

public sealed record FlatRateOptions(
    Money DefaultRate,
    string DisplayName = "Flat rate",
    int EstimatedDays = 3,
    IReadOnlyDictionary<string, Money>? PerCurrencyRates = null)
{
    public Money? RateFor(string currency) =>
        PerCurrencyRates is not null && PerCurrencyRates.TryGetValue(currency, out var rate) ? rate : null;
}

public static class FlatRateShippingRegistration
{
    public static IServiceCollection AddFlatRateShipping(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FlatRateOptions>(configuration.GetSection("Shipping:FlatRate"));
        services.AddSingleton<IShippingCalculator, FlatRateShippingCalculator>();
        return services;
    }
}
```

Call `builder.Services.AddFlatRateShipping(builder.Configuration);` after `AddCommerceModules()`.
Because `Shipping:FlatRate` is plain configuration, the rate changes per store and per currency
without code. If a store should instead *replace* the built-in weight-based calculator, the
extension registers with the built-in key and, being later, wins.

---

## Worked Example 2 — Coupon Promotion Type

Add a promotion action, `coupon-percent-off`, discounting eligible lines by a configured percentage
for a matching coupon. Promotions store an action key, so a new key adds a type with no module change.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Resys.Core;
using Resys.Core.Promotions;

namespace Acme.Resys.Extensions.Promotions;

public sealed class CouponPercentOffAction : IPromotionAction
{
    public const string ActionKey = "coupon-percent-off";
    public string Key => ActionKey;

    public Task<PromotionActionOutcome> ApplyAsync(PromotionActionContext context, CancellationToken ct = default)
    {
        if (context.CouponCode is null ||
            !string.Equals(context.CouponCode, context.Promotion.CouponCode, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(PromotionActionOutcome.NotApplied);
        }

        var percent = context.Promotion.Decimal("percent");
        var adjustments = context.Lines
            .Where(context.Promotion.Matches)
            .Select(line => new PromotionAdjustment(
                LineItemId: line.Id,
                Amount: line.Amount * (percent / 100m),
                SourceKey: ActionKey,
                Label: $"Coupon {context.CouponCode}"))
            .ToList();

        return Task.FromResult(PromotionActionOutcome.Applied(adjustments));
    }
}

public static class CouponPromotionRegistration
{
    public static IServiceCollection AddCouponPromotion(this IServiceCollection services)
    {
        services.AddSingleton<IPromotionAction, CouponPercentOffAction>();
        return services;
    }
}
```

A promotion record is then configured with `ActionKey = "coupon-percent-off"`, its `percent`
parameter, and its coupon code. The engine discovers the action through `IPromotionAction`, applies
the per-line clamping documented in `03/02-04`, and writes the returned adjustments; the extension
never sees a database session or another module's types.

---

## Testing Extensions

- **Unit** the implementation directly with recorded request objects; it has no infrastructure
  dependencies.
- **Register** it in an in-memory `ServiceCollection` and assert `KeyedRegistry<T>` resolves the
  intended winner, including the last-registered-wins case.
- **Integration** the extension through the owning module's API against a Testcontainers Postgres
  database (`06/01-03`).

## Open Questions

1. The mechanism that installs an extension into a running deployment (a plugin manifest versus a
   rebuild with the registration call) is owned by `07/03` (deployment); the contract and DI
   semantics here are unchanged either way.
2. Whether extension option sections are namespaced by extension id or by module is confirmed in
   `05/02-02` (module conventions).
3. The cancellation and retry contract for a hook calling an external provider is owned by
   `03/02-03` (order lifecycle) and `08/03-03` (gateway runbook).

## Links

- [Module contracts](./02-module-contracts.md)
- [Solution layout](./01-solution-layout.md)
- [Design spec](../../11-spec/README.md) — §8 (extension
  architecture).
- Payment gateway adapters: `03/04-05`; promotion clamping: `03/02-04`; order lifecycle:
  `03/02-03`; integration tests: `06/01-03`; module conventions: `05/02-02`.
