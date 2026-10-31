# Cross-Cutting Concerns

Events, files, email, observability, security, and configuration.

**Links:** [Spec index](../README.md) · [← Prev](09-frontends.md) · [Next →](11-documentation-map.md) · [Root README](../../../README.md)

---

- **Events** — in-process dispatch + outbox table. Consumers: email (order confirmation,
  shipment), analytics stub. Domain event catalog in `docs/03-architecture/`.
- **Files/images** — `IFileStorage` abstraction (local disk + S3-compatible backends);
  product image upload/processing flow documented.
- **Email** — `IEmailSender` (SES/SMTP); transactional templates: order confirmation,
  payment receipt, shipment, return status.
- **Observability** — OpenTelemetry (traces, metrics, structured logs via Serilog);
  health checks `/health/live`, `/health/ready`; request-id propagation.
- **Security** — as §7, plus an **audit-log table** for admin mutations (who/what/when).
- **Configuration** — options pattern; per-environment `appsettings` + env vars;
  storefront config (API base URL, per-store branding) via Nuxt runtime config.
