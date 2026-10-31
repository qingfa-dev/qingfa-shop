# 08 — Operations

How resys-eshop runs in production: the infrastructure topology, the environment differences, and the capacity and scaling model that keep it available and within budget.

**Links:** [Infrastructure topology](./01-infrastructure/01-topology.md) · [Environments](./01-infrastructure/02-environments.md) · [Capacity planning](./04-scaling/01-capacity.md) · [Spec](../11-spec/README.md)

---

## Purpose

This area is the operations contract for resys-eshop. It describes the production infrastructure
that `07/03` deploys into, the environment separation that keeps real customer data out of lower
environments, and the capacity model that decides when each tier scales. It is the operational
counterpart to the architecture in `03` and the release discipline in `07`.

The area has **four subfolders**: infrastructure topology and environments, monitoring telemetry,
dashboards and alerts, incident runbooks, and the capacity model. The file index below lists every
document. When a statement in this area conflicts with the
[design spec](../11-spec/README.md), the spec wins and this area is
corrected.

Operations consumes the deployables, ports, CORS, and health surface from `03/01-02`, the
deployment artifacts and migration ordering from `07/03`, the configurable values from `05/01-02`,
and the performance and availability requirements from `01/03-01` and `01/03-03`.

---

## File Index

| Path | Concern | Read it when |
|---|---|---|
| [01-infrastructure/01-topology.md](./01-infrastructure/01-topology.md) | Production topology: edge load balancer, API and Nuxt SSR workloads, PostgreSQL primary/replica, object storage, email, secrets management, health endpoints. | You are planning or reviewing the production deployment. |
| [01-infrastructure/02-environments.md](./01-infrastructure/02-environments.md) | Development, test, staging, and production differences, the promotion path, and data-handling rules. | You are promoting a release or handling environment data. |
| [04-scaling/01-capacity.md](./04-scaling/01-capacity.md) | Reference sizing, request/user assumptions, API horizontal scaling, connection pooling, read replicas, and SSR cache/CDN. | You are sizing the platform or responding to a load signal. |
| [02-monitoring/01-telemetry.md](./02-monitoring/01-telemetry.md) | OpenTelemetry traces, metrics, and structured logs. | You are wiring or reading telemetry. |
| [02-monitoring/02-dashboards.md](./02-monitoring/02-dashboards.md) | Dashboards for health, latency, throughput, and pool saturation. | You are watching a release or an incident. |
| [02-monitoring/03-alerts.md](./02-monitoring/03-alerts.md) | Alert rules, thresholds, and routing. | You are tuning or receiving an alert. |
| [03-incidents/01-oncall.md](./03-incidents/01-oncall.md) | On-call rotation, severity levels, and break-glass access. | You are on call or declaring an incident. |
| [03-incidents/02-runbook-db.md](./03-incidents/02-runbook-db.md) | Database failure, failover, and restore procedures. | The database is unavailable or degraded. |
| [03-incidents/03-runbook-gateway.md](./03-incidents/03-runbook-gateway.md) | Payment-gateway outage and degradation procedures. | A gateway is failing checkout. |
| [03-incidents/04-runbook-double-charge.md](./03-incidents/04-runbook-double-charge.md) | Double-charge reconciliation and remediation. | A duplicate charge is reported. |

Every file in this area is linked from the file index above; prose elsewhere cites it by its
identifier (for example `08/02-01`, `08/03-02`) and links it when a jump is useful.

---

## How to Read This Area

1. **See the shape.** `08/01-01` shows what runs and how traffic and data reach each tier — the one
   file to read before any production change.
2. **Separate the environments.** `08/01-02` fixes what differs between development, test,
   staging, and production, and the data-handling rules that apply when promoting.
3. **Size it.** `08/04-01` gives the reference sizing, the per-instance capacity model, and the
   concrete triggers for scaling the API, the database pool, read replicas, and the CDN.
4. **Watch it.** The `02-monitoring/` documents turn the topology and capacity signals into
   telemetry, dashboards, and alerts.
5. **Recover it.** The `03-incidents/` runbooks cover on-call, the database, the payment gateway,
   and double-charge reconciliation.
6. **Cite by identifier, link what exists.** Every doc in this set exists; cite another doc as
   `AA/BB-CC` (flat areas as `AA/NN`) and link it when a jump is useful.

---

## Operations at a Glance

| Concern | Decision | Owner |
|---|---|---|
| Deployables | `Resys.Api`, `web/storefront`, `web/admin` — one immutable image each, promoted by digest. | `08/01-01`, `07/03` |
| Edge | L7 load balancer and ingress terminate TLS; WAF complements the in-process limiter. | `08/01-01` |
| API instances | Stateless, replica floor ≥ 2, outbox relay safe across replicas. | `08/01-01`, `03/02-03` |
| PostgreSQL | Managed PostgreSQL 18, primary + ≥ 1 read replica, PgBouncer transaction pooling. | `08/01-01`, `08/04-01` |
| Object storage | S3-compatible bucket, CDN for public derivatives, signed URLs for private. | `08/01-01`, `03/04-06` |
| Email | SES default, SMTP relay alternative, event-driven via the outbox. | `08/01-01`, `03/04-06` |
| Secrets | External secret manager, least privilege per workload, scheduled rotation. | `08/01-01`, `08/01-02` |
| Health | `/health/live` (no deps) and `/health/ready` (deps) gate rolling rollout. | `08/01-01` |
| Environments | Development, test, staging, production; build once, promote the same digest. | `08/01-02`, `07/03` |
| Capacity | ≥ 200 req/s per API instance, plan at 120 req/s headroom, scale on measured signals. | `08/04-01` |
| Caching | CDN + Nuxt route rules, per-store keys, 60 s catalog invalidation. | `08/04-01`, `03/01-02` |

The only stateful services are PostgreSQL and object storage; every application workload is
stateless and can be replaced or scaled without draining shared state (`03/01-02`).

---

## Related Areas

- `01` (requirements) — performance and availability NFRs that set the operational targets
  (`01/03-01`, `01/03-03`) and the security controls enforced in production (`01/03-02`).
- `03` (architecture) — the deployables, ports, health surface, CORS origins, and store
  resolution (`03/01-02`); the data-flow and outbox model (`03/01-03`); the email and file
  contracts (`03/04-06`).
- `05` (development) — the full configuration and local-stack details the environments here
  summarize (`05/01-02`, `05/01-03`).
- `06` (verification) — the performance budgets and load profiles measured on this topology
  (`06/03-02`) and the quality metrics (`06/03-01`).
- `07` (release) — the build, migrate, and promote path (`07/03`) and rollback (`07/04`) that this
  area's environments and topology support.
- `10` (maintenance) — dependency upgrades and performance tuning that follow an operational
  finding (`10/02`, `10/03`).

---

## Keeping This Area Current

- Update `08/01-01` when a workload, managed service, endpoint, or secret scope changes; keep it
  aligned with `03/01-02` and `07/03`.
- Update `08/01-02` when an environment, a configurable value, or a data-handling rule changes;
  keep it aligned with `05/01-02` and the topology in `08/01-01`.
- Update `08/04-01` when the reference sizing, replica floors, pool budget, or scaling triggers
  change, and reflect the same change into the reference conditions in `06/03-02`.
- Keep the file index above current and confirm every child file is listed.

## Open Questions

None at the area level. Unresolved details live in the owning file under its `## Open Questions`
section: the concrete provider and secret inventory in `08/01-01`/`08/01-02`, the staging
anonymization tool in `08/01-02`, and the replication-lag threshold and HPA wiring in `08/04-01`.
