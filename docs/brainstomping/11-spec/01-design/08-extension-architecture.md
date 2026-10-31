# Extension Architecture

How third parties extend modules through DI contracts.

**Links:** [Spec index](../README.md) · [← Prev](07-api-surface.md) · [Next →](09-frontends.md) · [Root README](../../../README.md)

---

Modules expose **contracts, not hooks**:

- `IShippingCalculator`, `IPaymentGateway`, `IPromotionAction`, `ITaxZoneResolver`,
  `OrderStateHooks`.
- A third party adds an extension by implementing a contract and registering it in DI —
  **no source modification** of core modules.
- Override semantics: **last-registered wins** per key (documented and tested).
- Replaces Spree's concern/monkey-patch style with testable decorators/pipelines.
- `docs/03-architecture/02-components/07-extension-architecture.md` includes a worked example
  (a new flat-rate shipping calculator + a coupon promotion type).
