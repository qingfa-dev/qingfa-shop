# Production Topology

How resys-eshop runs in production: the edge load balancer, the API and Nuxt SSR workloads, PostgreSQL primary and replica, object storage, email transport, secrets management, and health endpoints.

**Links:** [Environments](./02-environments.md) · [Capacity](../04-scaling/01-capacity.md) · [App topology](../../03-architecture/01-overview/02-app-topology.md) · [Deployment](../../07-release/03-deployment.md) · [Auth](../../03-architecture/04-integration/03-auth.md) · [Email & files](../../03-architecture/04-integration/06-email-files.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the production infrastructure contract for resys-eshop. It names the running
workloads, how traffic reaches them, and how they reach stateful dependencies. It is the
authoritative owner of the production registry, cluster, ingress, load balancer, secret-manager
mechanism, and managed services that `07/03` deploys into and that `03/01-02` explicitly leaves to
`08/01-01`. The concrete secret inventory and per-environment values are owned by `08/01-02` and
`05/01-02`.

The topology has exactly **three application workloads** — `resys-api`, `resys-storefront`, and
`resys-admin` — plus managed dependencies: PostgreSQL 18, an S3-compatible object store, and an
email transport. `web/shared` and the `Resys.*` libraries are compile-time inputs and are never
deployed on their own. Only `Resys.Api` holds database, storage, and gateway credentials;
neither Nuxt runtime does (`03/01-02`).

Sizing for this topology, the replica floors, and the scaling triggers are owned by `08/04-01`.

---

## Reference Topology

```text
                          Internet (browsers, payment SDKs)
                                     │ HTTPS :443
                       ┌─────────────▼─────────────┐
                       │   Edge L7 load balancer    │  TLS termination, WAF, rate cap
                       └──┬───────────┬─────────┬───┘
                          │           │         │
             shop.example │  admin.   │  api.   │
                          ▼           ▼         ▼
                   ┌────────────┐ ┌────────────┐ ┌────────────┐
                   │ storefront │ │   admin    │ │  Resys.Api │
                   │ SSR (Nitro)│ │ SSR (Nitro)│ │  (Kestrel) │
                   │  N replicas│ │  N replicas│ │  N replicas│
                   └─────┬──────┘ └─────┬──────┘ └──┬───┬───┬─┘
                         │  server-side API calls   │   │   │
                         └──────────────┬───────────┘   │   │
                                        │ /api/v1 · /api/admin
                                        ▼               │   │
                       ┌────────────────────────┐       │   │
                       │  PgBouncer (txn pool)  │◄──────┘   │
                       └───────┬────────┬───────┘           │
                               ▼        ▼                   │
                       ┌───────────┐ ┌───────────┐           │
                       │ Postgres  │ │ Postgres  │  outbound │
                       │  primary  │─│  replica  │  HTTPS    │
                       └─────┬─────┘ └───────────┘           │
                             │ WAL archive                   │
                             ▼                               ▼
                       Object storage (backups)   ┌────────────────────────┐
                                                  │ S3 object storage+CDN  │
                                                  │ SES/SMTP email         │
                                                  │ Stripe · PayPal        │
                                                  └────────────────────────┘
```

The edge load balancer and the cluster ingress are the only publicly reachable path. Database,
object storage, and email are private or outbound; payment SDKs are called directly from the
browser at checkout and never proxied through the platform (`03/01-02`).

---

## Edge and Load Balancing

- **DNS and hostnames.** `shop.<domain>` resolves to the storefront, `admin.<domain>` to the
  admin, and `api.<domain>` to the API. Each host is a distinct route at the edge; the storefront
  resolves its `Store` from the request host as described in `03/01-02`.
- **TLS.** HTTPS is required on every non-development environment and is terminated at the edge
  (NFR-SEC-010); HSTS is enabled. Certificates are managed and renewed automatically.
- **L7 routing.** The load balancer forwards to the cluster ingress, which routes by host and
  path to the matching Kubernetes `Service`. Long-lived connections and streaming responses are
  supported; health checks remove unhealthy targets before they receive traffic.
- **Edge protections.** A managed WAF and volumetric protection sit in front of the ingress. They
  complement — never replace — the in-process rate limiter and CORS allow-list (`03/04-03`,
  NFR-SEC-006). Exact WAF policy is a deployment value; the application-level limits are fixed.
- **No wildcard origins.** Browser origins remain exactly the two configured front-end origins;
  the edge does not add CORS headers of its own (`03/01-02`).

---

## API Instances (`Resys.Api`)

- **Packaging.** One immutable image per release, published in Release from a tagged commit and
  promoted by digest (`07/03`). The image is non-root, listens on `8080`, and contains no SDK or
  source.
- **Statelessness.** Every instance is interchangeable: all state is in PostgreSQL, object
  storage, or the outbox. Any replica may serve any request, and there are no sticky sessions.
- **Replica floor.** Production runs **at least two** API replicas across failure domains so a
  node or zone loss does not drop the API. Replica count above the floor is governed by
  `08/04-01`.
- **Migrations.** Schema change is never applied on pod start. The per-module idempotent migration
  bundle runs once as a release `Job` before the rollout (`07/03`, `03/03-04`).
- **Outbox relay.** The relay runs inside the API host but is concurrency-safe across replicas:
  only one instance leases a given outbox row, so horizontal scale never double-publishes a
  domain event (NFR-AVAIL-007; relay design owned by `03/02-03`).
- **Outbound calls.** Payment gateways, storage, and email are reached over outbound HTTPS with
  bounded timeouts, retries with backoff, and circuit breaking so a slow dependency cannot
  exhaust request threads (NFR-AVAIL-008).

---

## Nuxt SSR Runtime

- **Two independent deployments.** `resys-storefront` runs Nuxt 4 SSR (Nitro Node server) for SEO
  and first paint; `resys-admin` runs the authenticated back-office shell with the PrimeVue UI.
  Neither calls the other; both call the API (`03/01-02`).
- **Runtime image.** Each app ships its Nitro `.output` on a non-root `node:24-slim` image and
  receives its API base URL and public runtime config from environment variables at start, so one
  image serves staging and production (`07/03`, `05/01-02`).
- **Scaling.** Storefront replicas scale on request rate and Node event-loop lag; admin replicas
  scale on operator traffic. Both are stateless at the HTTP layer; per-store branding and store
  resolution are recomputed per request from the API.
- **Health.** Each Nitro server exposes a lightweight HTTP probe on its server port that the
  ingress and orchestrator use for readiness; the API's richer dependency probes are below.
- **Caching.** Public storefront routes use Nuxt route rules / SWR with a per-store cache key and
  never shared-cache authenticated responses; the CDN and cache policy is `08/04-01`.

---

## PostgreSQL — Primary and Replica

- **Version and role.** Managed PostgreSQL 18 is the only shared stateful service. It holds all
  commerce state, the ASP.NET Identity tables, and the outbox (`03/01-02`).
- **Primary/replica.** Production runs **one primary** plus **at least one streaming read
  replica** in a synchronous-or-near-synchronous configuration with automatic failover. The
  replica exists for read offload and for failover, not as an independent writer.
- **Writes and reads.** Orders, checkout, inventory, payments, outbox writes, and any
  read-after-write flow go to the primary. Read-only catalog, listing, and search queries may be
  routed to a replica where a bounded replication lag is acceptable; store-scoped reads never
  serve stale cross-store data. Replica routing rules and lag limits are in `08/04-01`.
- **Pooling.** Application connections reach PostgreSQL through PgBouncer in **transaction**
  pooling mode. The pooler caps total backend connections below the server's `max_connections`,
  decoupling application replica count from database connection count. Migration jobs connect
  outside the transaction pool.
- **Backups and recovery.** Continuous WAL archiving to object storage gives point-in-time
  recovery with RPO ≤ 15 min and RTO ≤ 1 h (NFR-AVAIL-004). Restores are rehearsed on a defined
  cadence from the runbook in `08/03-02`.
- **Guardrails.** A 5 s request-path statement timeout, indexed foreign keys and high-selectivity
  filters, and no long transaction held across an external call (NFR-PERF-009). Slow queries are
  surfaced by `pg_stat_statements` and the slow-query log (`08/02-01`).

---

## Object Storage and CDN

- **Backend.** Production uses an S3-compatible bucket (`Storage:Provider=s3`), with credentials
  from the secret store. The database stores object keys, never absolute URLs (`03/04-06`).
- **Public derivatives.** Product-image derivatives (`thumb`, `card`, `full`) are world-readable,
  immutable per key, and served through a CDN with a long TTL. A public base URL points at the
  CDN or bucket host.
- **Private assets.** Originals and any future invoices/exports are served only through
  short-lived signed URLs generated per request; a private key never appears in a public page.
- **Durability.** The bucket's own durability and versioning protect uploaded assets. Local-disk
  storage is rejected outside `Development`/`Test`, so no production pod serves files from its
  filesystem (`05/01-02`).
- **Backups.** The same object store holds database WAL archives and backup snapshots in a
  separate bucket or prefix with restricted access.

---

## Email / SMTP

- **Production transport.** Amazon SES is the default production sender (`Email:Provider=ses`);
  an SMTP relay via MailKit is the supported self-hosted alternative. Both sit behind
  `IEmailSender` (`03/04-06`).
- **Delivery model.** Email is triggered by domain events through the outbox and sent by a
  consumer, never inline on a request thread; a failed send is retried with backoff and never
  rolls back the originating state change (NFR-AVAIL-007).
- **Sending identity.** The from address, reply-to, branding, and locale come from the resolved
  store; a payment event for one store never sends through another store's identity.
- **Deliverability.** SPF, DKIM, and DMARC are configured for the sending domain at the edge; SES
  bounce and complaint notifications feed the dashboards in `08/02-02`.
- **No sensitive data.** Messages never include PAN, CVV, tokens, or gateway payloads, and logs
  redact recipients and payment references (`03/04-06`, NFR-SEC-007).

---

## Secrets Management

- **External store.** All secrets live in a managed secret manager backed by a KMS, not in the
  image, the manifest, or source. This includes `ConnectionStrings__ResysDb`, `Jwt__SigningKey`,
  Stripe/PayPal secrets, SES/SMTP credentials, and S3 keys (`07/03`, `05/01-02`).
- **Injection.** The secret manager syncs values into namespaced Kubernetes `Secret` objects;
  pods consume them as environment variables. Non-secret settings come from a `ConfigMap`.
- **Least privilege.** Each workload gets only its own secrets; the Nuxt runtimes receive no
  database or provider credentials. The API is the only workload with stateful-dependency access.
- **Rotation.** Database passwords, JWT signing keys, gateway keys, and storage keys are rotated
  on a schedule and immediately on suspected compromise. JWT rotation is coordinated with the
  refresh-token design in `03/04-03`; the schedule itself is recorded with `08/01-02`.
- **Audit.** Secret access is logged by the secret manager; no secret value is ever logged by the
  application (NFR-SEC-010).

---

## Health Endpoints

| Endpoint | Owner | Semantics | Consumer |
|---|---|---|---|
| `/health/live` | `Resys.Api` | Process liveness; **no** dependency checks, never blocks on the database. | `livenessProbe`; a failure restarts the pod. |
| `/health/ready` | `Resys.Api` | Readiness; verifies PostgreSQL reachability and completed wiring. | `readinessProbe`; gates traffic and is failing during start, migration, or disconnect. |
| HTTP `/healthz` on Nitro port | storefront / admin | Nitro server responsiveness. | `readinessProbe` on the Node pods. |
| Edge target health | load balancer / ingress | TCP/HTTP reachability of each `Service`. | Removes unhealthy targets before traffic. |

Rules:

- Readiness flips to failing **before** an instance stops accepting traffic during a rolling
  deploy, and in-flight requests finish within the termination grace period (NFR-AVAIL-006).
- Liveness must not check the database, so a slow dependency cannot trigger a restart loop
  (NFR-AVAIL-001).
- Probe intervals, timeouts, and thresholds follow the deployment manifests; their sizing is part
  of the reference topology in `08/04-01`.
- Health state, request success, and dependency error rates feed the telemetry in `08/02-01` and
  the dashboards in `08/02-02`; alerts and error-budget burn are `08/02-03`.

---

## Open Questions

1. The exact cloud provider, Kubernetes distribution, ingress controller, and secret manager
   product are deployment choices; this file fixes their roles, not their brands. `08/01-02`
   records the concrete secret inventory.
2. Whether catalog reads are routed to the replica per endpoint or per query is finalized with
   the read-routing rules and lag limit in `08/04-01`; the primary-writes rule is fixed here.
3. Whether the public asset base is fronted by a CDN in front of the bucket or by a caching proxy
   is a deployment value owned here; the key/URL split in `03/04-06` is unaffected.
4. The backup retention window and the restore-rehearsal cadence are finalized with the database
   runbook in `08/03-02`; the RPO/RTO targets are fixed by `01/03-03`.
