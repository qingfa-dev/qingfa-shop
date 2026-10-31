# Plan — Operations

Tasks 25–27: infrastructure, monitoring, and incident response.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](10-release.md) · [Next →](12-user-docs.md) · [Root README](../../../README.md)

---

### Task 25: Operations — infrastructure and scaling


**Files:**
- Modify: `docs/08-operations/README.md`
- Create: `docs/08-operations/01-infrastructure/01-topology.md`
- Create: `docs/08-operations/01-infrastructure/02-environments.md`
- Create: `docs/08-operations/04-scaling/01-capacity.md`

**Interfaces:**
- Consumes: `03/01-02`, `07/03`; Spec §10.
- Produces: `08/01-01`, `08/01-02`, `08/04-01`, cited by `06/03-02`.

- [ ] **Step 1: Write the three files and the index**

- `01-topology.md` — Production topology: API instances, Nuxt SSR runtime, PostgreSQL (primary/replica), object storage, email/SMTP, load balancer, secrets management, health endpoints.
- `02-environments.md` — dev/staging/prod config differences, promotion path, data-handling rules.
- `04-scaling/01-capacity.md` — Capacity planning: request/user assumptions, when to scale API horizontally, DB connection pooling, read replicas, SSR cache/CDN strategy.

Area README becomes an index of the 4 subfolders.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/08-operations && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/08-operations
git commit -m "docs(operations): add infrastructure, environments, scaling"
```

### Task 26: Operations — monitoring


**Files:**
- Create: `docs/08-operations/02-monitoring/01-telemetry.md`
- Create: `docs/08-operations/02-monitoring/02-dashboards.md`
- Create: `docs/08-operations/02-monitoring/03-alerts.md`

**Interfaces:**
- Consumes: `01/03-01`, `03/01-02`; Spec §10.
- Produces: `08/02-01` … `08/02-03`, cited by incident runbooks.

- [ ] **Step 1: Write the three monitoring files**

- `01-telemetry.md` — OpenTelemetry setup (traces, metrics), Serilog structured logs, health checks `/health/live` and `/health/ready`, request-id propagation, outbox/relay metrics.
- `02-dashboards.md` — Dashboard definitions: API latency/error rate/throughput, DB health, checkout funnel, payment success rate, queue/outbox lag.
- `03-alerts.md` — Alert table: `Name`, `Signal`, `Threshold`, `Severity`, `Runbook` (links to `08/03-*`).

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/08-operations && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/08-operations
git commit -m "docs(operations): add telemetry, dashboards, alerts"
```

### Task 27: Operations — incident response and runbooks


**Files:**
- Create: `docs/08-operations/03-incidents/01-oncall.md`
- Create: `docs/08-operations/03-incidents/02-runbook-db.md`
- Create: `docs/08-operations/03-incidents/03-runbook-gateway.md`
- Create: `docs/08-operations/03-incidents/04-runbook-double-charge.md`

**Interfaces:**
- Consumes: `08/02-03`, `03/04-05`, `01/03-03`.
- Produces: `08/03-01` … `08/03-04`.

- [ ] **Step 1: Write the four incident files**

- `01-oncall.md` — Severity levels, escalation, comms cadence, postmortem template.
- `02-runbook-db.md` — Symptom → diagnosis → mitigation for DB unavailable/slow/failover; backup/PITR restore steps; RPO/RTO per `01/03-03`.
- `03-runbook-gateway.md` — Payment gateway outage/degradation: degrade to alternate method, retry policy, customer comms.
- `04-runbook-double-charge.md` — Detect duplicate charge (idempotency key lookup) → verify via gateway → refund path → ledger correction → prevent recurrence.

- [ ] **Step 2: Placeholder scan**

Run: `grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' docs/08-operations && echo "FOUND PLACEHOLDERS" || echo "clean"`
Expected: `clean`

- [ ] **Step 3: Link check**

Run the Link check; expect no `BROKEN:` lines.

- [ ] **Step 4: Commit**

```bash
git add docs/08-operations
git commit -m "docs(operations): add on-call and incident runbooks"
```
