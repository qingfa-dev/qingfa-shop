# Documentation Map

The deliverable structure and the conventions for every doc file.

**Links:** [Spec index](../README.md) · [← Prev](10-cross-cutting.md) · [Next →](12-verification.md) · [Root README](../../../README.md)

---

Per-concern **subfolders** + **numbered files** (~100–300 lines each). Each area has a
`README.md` index. This "distributes the context": a reader (or agent) loads one small file
at a time. The set is deliberately granular.

```
docs/
├─ 00-governance/
│  ├─ README.md
│  ├─ 01-adr/            # 01-stack.md, 02-modular-monolith.md,
│  │                     #   03-no-cms-no-gql.md, 04-headless-jwt.md
│  └─ 02-policies/       # 01-contributing.md, 02-code-of-conduct.md, 03-review-policy.md
│
├─ 01-requirements/
│  ├─ README.md
│  ├─ 01-business/       # 01-goals.md, 02-actors.md, 03-constraints.md
│  ├─ 02-functional/     # 01-catalog.md … 12-stores-i18n.md (one file per module)
│  ├─ 03-nonfunctional/  # 01-performance.md, 02-security.md, 03-availability.md, 04-i18n.md
│  └─ 04-user-stories/   # 01-browse-buy.md, 02-account.md, 03-admin.md
│
├─ 02-planning/
│  ├─ README.md
│  └─ (01-roadmap.md, 02-backlog.md, 03-risks.md)
│
├─ 03-architecture/
│  ├─ README.md
│  ├─ 01-overview/       # 01-c4.md, 02-app-topology.md, 03-data-flow.md
│  ├─ 02-components/     # 01-solution-layout.md, 02-module-contracts.md,
│  │                     #   03-order-lifecycle.md, 04-promotion-engine.md,
│  │                     #   05-tax-engine.md, 06-inventory-model.md,
│  │                     #   07-extension-architecture.md
│  ├─ 03-data/           # 01-entities.md, 02-multi-store.md, 03-i18n-money.md,
│  │                     #   04-migrations.md
│  └─ 04-integration/    # 01-rest-public.md, 02-rest-admin.md, 03-auth.md,
│                        #   04-openapi.md, 05-payment-gateways.md, 06-email-files.md
│
├─ 04-design/
│  ├─ README.md
│  ├─ 01-storefront/     # 01-flows.md, 02-wireframes-pages.md, 03-components.md, 04-tokens.md
│  └─ 02-admin/          # 01-flows.md, 02-screens.md, 03-components.md
│
├─ 05-development/
│  ├─ README.md
│  ├─ 01-setup/          # 01-prerequisites.md, 02-environment.md,
│  │                     #   03-docker-compose.md, 04-seed-data.md
│  ├─ 02-conventions/    # 01-dotnet-style.md, 02-module-conventions.md,
│  │                     #   03-vue-nuxt-style.md, 04-naming.md
│  └─ 03-workflow/       # 01-build-run.md, 02-branching.md, 03-ci.md
│
├─ 06-verification/
│  ├─ README.md
│  ├─ 01-test-strategy/  # 01-matrix.md, 02-unit.md, 03-integration.md, 04-e2e.md, 05-coverage.md
│  ├─ 02-test-plans/     # 01-checkout.md, 02-promotions.md, 03-tax.md,
│  │                     #   04-inventory.md, 05-returns.md
│  └─ 03-quality/        # 01-metrics.md, 02-performance-budgets.md
│
├─ 07-release/
│  ├─ README.md
│  └─ (01-versioning.md, 02-release-notes.md, 03-deployment.md, 04-rollback.md)
│
├─ 08-operations/
│  ├─ README.md
│  ├─ 01-infrastructure/ # 01-topology.md, 02-environments.md
│  ├─ 02-monitoring/     # 01-telemetry.md, 02-dashboards.md, 03-alerts.md
│  ├─ 03-incidents/      # 01-oncall.md, 02-runbook-db.md, 03-runbook-gateway.md,
│  │                     #   04-runbook-double-charge.md
│  └─ 04-scaling/        # 01-capacity.md
│
├─ 09-user/
│  ├─ README.md
│  ├─ 01-admin-guide/    # 01-onboarding.md, 02-catalog.md, 03-orders.md,
│  │                     #   04-fulfillment-returns.md, 05-settings.md
│  ├─ 02-customer/       # 01-guide.md
│  ├─ 03-api/            # 01-auth.md, 02-catalog.md, 03-cart-checkout.md,
│  │                     #   04-account.md, 05-admin-api.md, 06-errors.md
│  └─ 04-support/        # 01-faq.md, 02-troubleshooting.md
│
├─ 10-maintenance/
│  ├─ README.md
│  └─ (01-technical-debt.md, 02-dependency-policy.md, 03-performance-tuning.md)
│
└─ 11-spec/              # 01-design/ (this spec), 02-plan/ (implementation plan)
```

**Conventions for every doc file**

- One concern per file, ~100–300 lines.
- Numbered for stable cross-reference (`03-01-order-lifecycle.md` → cite as "03/01-03").
- Start with a one-line purpose + a "Links" block to related files.
- No placeholders / TBD; if a detail is unknown, state it as an explicit open question.
