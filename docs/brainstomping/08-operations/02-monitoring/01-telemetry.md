# Telemetry

How resys-eshop instruments the API and both Nuxt front-ends with OpenTelemetry traces and metrics, Serilog structured logs, health probes, request-id propagation, and outbox/relay metrics.

**Links:** [Dashboards](./02-dashboards.md) · [Alerts](./03-alerts.md) · [Infrastructure topology](../01-infrastructure/01-topology.md) · [Capacity planning](../04-scaling/01-capacity.md) · [App topology](../../03-architecture/01-overview/02-app-topology.md) · [Data flow](../../03-architecture/01-overview/03-data-flow.md) · [Order lifecycle](../../03-architecture/02-components/03-order-lifecycle.md) · [Payment gateways](../../03-architecture/04-integration/05-payment-gateways.md) · [Performance NFRs](../../01-requirements/03-nonfunctional/01-performance.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Security NFRs](../../01-requirements/03-nonfunctional/02-security.md) · [Environment configuration](../../05-development/01-setup/02-environment.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the observability contract for resys-eshop. It defines how the API, both Nuxt
front-ends, PostgreSQL, and the outbox are instrumented: the OpenTelemetry traces and metrics that
are emitted, the Serilog structured logs that are written, the health endpoints an orchestrator
probes, the request-id that joins one request's signals, and the outbox/relay metrics that prove
durable event delivery. It is the source of the signals the dashboards in `08/02-02` render and the
alerts in `08/02-03` evaluate.

The stack is fixed by spec §10 — OpenTelemetry (traces, metrics, structured logs via Serilog),
health checks `/health/live` and `/health/ready`, and request-id propagation — and is constrained
by the performance NFRs (`01/03-01`), the availability NFRs (`01/03-03`), and the security NFRs
(`01/03-02`; no PAN or PII in logs). The runtime topology and the collector that receives the data
are owned by `08/01-01`; the environment split is `08/01-02`; the exact setting names are
`05/01-02`. Incident procedures that act on these signals live in `08/03-01` … `08/03-04`.

The one rule that holds the stack together: **every signal carries the same `trace_id`, the same
`service.name`, and the resolved `store` key, so any signal can be joined to the request that
produced it without a correlation database.**

---

## Export Pipeline

- **API (`Resys.Api`).** ASP.NET Core with the OpenTelemetry .NET SDK exports traces, metrics, and
  logs over **OTLP/gRPC** to the collector at `Observability:OtlpEndpoint`
  (`Observability__OtlpEndpoint`; default `http://localhost:4317`, optional in development). The API
  is the only workload with a collector endpoint in production and the only one that exports the
  business and outbox metrics.
- **Nuxt runtimes.** `web/storefront` and `web/admin` use the OpenTelemetry JS SDK for Nitro
  server-render spans, `fetch` client spans, and `web-vitals` field metrics, exported over OTLP to
  the same collector. They export no database or payment credentials and no server-side commerce
  state.
- **Collector.** The collector receives OTLP, applies tail sampling for traces, enriches resource
  attributes (`deployment.environment`, `service.version`), batches, and fans out to the telemetry
  backend. Its placement and scaling are `08/01-01`.
- **Environments.** Every signal is tagged with `deployment.environment` from
  `ASPNETCORE_ENVIRONMENT` (or the Nuxt runtime equivalent), so staging signals never mix with
  production pages (`08/01-02`). Development may run without a collector; telemetry is then
  console-only and never leaves the workstation.

**Resource attributes (every signal):**

| Attribute | Source | Notes |
|---|---|---|
| `service.name` | image / Nuxt app | `resys-api`, `resys-storefront`, `resys-admin`. |
| `service.version` | release tag | The promoted image digest's version (`07/03`). |
| `deployment.environment` | `ASPNETCORE_ENVIRONMENT` | `Development`, `Test`, `Staging`, `Production`. |
| `store` | resolved store | The store key from store resolution (`03/01-02`); absent only on pre-resolution spans. |

---

## Traces

- **Instrumentation.** ASP.NET Core server spans, `HttpClient` client spans, EF Core database
  spans, and Npgsql spans are automatic. Module services add a span around the unit of work (for
  example `orders.confirm`) so a trace names the business operation, not just the HTTP route.
- **HTTP spans.** Named by route template and method (`GET /api/v1/products/{id}`), never by the
  raw path, so a span name does not explode in cardinality. The resolved `store` is a span
  attribute. Status is derived from the response code; a `5xx` marks the span as error.
- **Database spans.** Carry the operation name and table/collection, the store scope, and the
  duration. A statement that reaches the **5 s** request-path timeout (NFR-PERF-009) is marked
  error and joined to the slow-query log.
- **Payment spans.** The gateway round-trip is a distinct client span under the checkout step, with
  a `gateway` attribute (`stripe`, `paypal`, …). Platform work and gateway time are separated so
  the NFR-PERF-003 budget excludes provider latency (`03/04-05`).
- **Front-end spans.** Nuxt SSR emits a render span per route with at most one batched API fetch
  (NFR-PERF-006); the browser emits `fetch` spans and a navigation span. Client spans link to the
  API trace through the propagated context so one purchase is one trace across both tiers.
- **Outbox propagation.** The relay starts no new root trace: it continues the trace of the
  transition that wrote the row (or records the originating `trace_id` on the outbox row), so a
  consumer's work is traceable back to the checkout that caused it.
- **Sampling.** Head-based parent-based sampling is the default. Error spans, checkout/payment
  spans, webhook spans, and outbox relay spans are always kept; catalog read spans are sampled at
  the collector's configured ratio. Tail sampling at the collector keeps all traces containing an
  error or a payment operation. The baseline ratio is a deployment value owned by `08/01-01`.

---

## Request-Id Propagation

Request-id propagation is required by spec §10 and makes a support report actionable: the customer
quotes one id and every log, span, and metric for that request can be found.

1. **Inbound.** The API reads the W3C `traceparent`/`tracestate` headers and continues that
   context. If `traceparent` is absent, the server starts a new trace.
2. **`X-Request-Id`.** A client may also send `X-Request-Id`. When present it is recorded on the
   root span and is the id used in the response header; when absent, the 32-hex `trace_id` is used.
3. **Response echo.** The API returns `X-Request-Id` on every response so a browser, the admin
   shell, or the storefront can log it and show it to support.
4. **Logs.** A Serilog enricher writes `TraceId`, `SpanId`, and `RequestId` onto every event, so a
   single id search finds every log line for the request.
5. **Outbound.** `HttpClient` automatically propagates `traceparent` on outbound calls. Storefront
   and admin server-side calls forward the originating `X-Request-Id`; browser `fetch` calls
   propagate the OTel context for the API trace.
6. **Across the outbox.** The relay and its consumers carry the originating request's id on the
   event, so an email or analytics side effect can be traced to the request that triggered it.
   `Idempotency-Key` is a separate business key and is never used as the request-id.

---

## Metrics

All metrics use the OpenTelemetry naming convention and carry `service.name`,
`deployment.environment`, and — wherever the operation is store-scoped — `store`.

| Metric | Type | Unit | Source | Labels |
|---|---|---|---|---|
| `http.server.request.duration` | Histogram | s | ASP.NET Core | `http.request.method`, `http.route`, `http.response.status_code`, `store` |
| `http.server.active_requests` | UpDownCounter | {request} | API middleware | `http.request.method`, `http.route` |
| `resys.http.requests.total` | Counter | {request} | API middleware | `endpoint_group` (`public_read`, `search`, `checkout`, `admin`), `status_class` |
| `db.client.operation.duration` | Histogram | s | EF Core / Npgsql | `db.operation.name`, `db.collection.name`, `store` |
| `db.client.connection.count` | UpDownCounter | {connection} | Npgsql pool | `db.client.connection.state` (`idle`, `used`) |
| `db.client.connection.pending_requests` | UpDownCounter | {request} | Npgsql pool | — |
| `db.client.connection.timeouts` | Counter | {timeout} | Npgsql pool | — |
| `resys.checkout.step.duration` | Histogram | s | Orders module | `step` (`delivery`, `payment`, `confirm`), `store` |
| `resys.checkout.completed` | Counter | {order} | Orders module | `store` |
| `resys.payment.attempts` | Counter | {attempt} | Payments module | `gateway`, `outcome` (`authorized`, `captured`, `declined`, `failed`) |
| `resys.payment.gateway.duration` | Histogram | s | Payments module | `gateway`, `operation` |
| `resys.payment.gateway.errors` | Counter | {error} | Payments module | `gateway`, `retryable` |
| `resys.payment.circuit.open` | UpDownCounter | {circuit} | Payments module | `gateway` |
| `resys.payment.duplicate_suspected` | Counter | {order} | reconciliation job | `store` |
| `resys.outbox.published` | Counter | {event} | outbox relay | `event_type`, `store` |
| `resys.outbox.failed` | Counter | {event} | outbox relay | `event_type`, `store` |
| `resys.outbox.pending` | UpDownCounter | {event} | outbox relay | `store` |
| `resys.outbox.oldest_age_seconds` | Gauge | s | outbox relay | `store` |
| `resys.outbox.relay_lag_seconds` | Histogram | s | outbox relay | `store` |
| `resys.outbox.dead_lettered` | Counter | {event} | outbox relay | `event_type`, `store` |
| `resys.ssr.render.duration` | Histogram | s | Nuxt/Nitro | `route`, `store` |
| `resys.webvitals.lcp` / `.inp` / `.cls` / `.ttfb` | Histogram | ms / ms / 1 / ms | `web-vitals` | `route`, `store` |
| `process.runtime.dotnet.*` | mixed | — | .NET runtime | runtime-supplied |
| `process.cpu.utilization`, `process.memory.usage` | Histogram / Gauge | 1 / By | .NET runtime | — |

**Rules**

- Histograms are the unit of latency; no metric averages a percentile across routes or stores.
- Every counter that can double-count under outbox redelivery is keyed so a duplicate is
  observable, never silently merged (NFR-AVAIL-007).
- The `endpoint_group` label matches the four groups in `06/03-02`, so a production panel and a k6
  threshold speak the same vocabulary.
- Money is never a metric label and never a raw decimal; amount-like series, where they exist, carry
  `store` and `currency` and are derived sums only.

---

## Structured Logs (Serilog)

- **Format.** Serilog writes **JSON** in `Staging`/`Production` and human-readable console in
  `Development`/`Test`. The same event reaches the collector over OTLP and the log backend, so a
  dashboard and a trace point at one record.
- **Enrichers.** `FromLogContext`, machine/pod name, `deployment.environment`, `service.version`,
  `TraceId`/`SpanId`/`RequestId`, and the resolved `store`.
- **Request logging.** One summary event per request: method, route template, status, duration, and
  request id. Request and response **bodies are not logged by default**; failure diagnostics log
  typed error data, not payloads.
- **Redaction (NFR-SEC-007).** Destructuring policies mask emails, names, addresses, tokens,
  `Authorization`/cookie headers, passwords, refresh tokens, and provider payloads. PAN, CVV, and
  gateway secrets are never present in the process and therefore can never be logged. A redaction
  test suite feeds representative sensitive payloads through the pipeline and asserts they never
  reach the sink (`01/03-02`).
- **Levels.** `Information` for normal transitions and request summaries; `Warning` for a retryable
  dependency failure or a suppressed/duplicate event; `Error` for an unhandled exception or a 5xx;
  `Fatal` for a startup failure that aborts the host. A log level change is a configuration change,
  not a code change.
- **Health probes.** Probe calls are logged at `Debug` (or excluded) so liveness/readiness polling
  does not drown the request log.

---

## Health Checks

Health is fixed by spec §10; semantics and consumers are `08/01-01` and `01/03-03`.

| Endpoint | Owner | Checks | Tags | Consumer |
|---|---|---|---|---|
| `/health/live` | `Resys.Api` | Process liveness only; **no** dependency check, never blocks on PostgreSQL. | `live` | Kubernetes `livenessProbe`; failure restarts the pod. |
| `/health/ready` | `Resys.Api` | PostgreSQL reachability and completed startup/migration wiring. | `ready` | Kubernetes `readinessProbe`; gates traffic and fails during start, migration, or disconnect. |
| HTTP `/healthz` on the Nitro port | storefront / admin | Nitro server responsiveness. | `ready` | Probe on the Node pods. |

**Rules**

- The API maps health checks by tag: `live` for liveness, `ready` for readiness. The readiness
  check includes the Npgsql and startup-wiring checks; it is written to complete within the probe
  timeout even when a dependency is slow (NFR-AVAIL-002).
- Liveness never checks a dependency, so a slow database cannot cause a restart loop
  (NFR-AVAIL-001).
- The health response is JSON with the overall status and per-check duration; check descriptions
  and exception text are filtered so no connection string, host, or secret is exposed.
- Probe intervals, timeouts, and thresholds are deployment manifests (`08/01-01`); the endpoints
  and their semantics are fixed here.
- A failing probe is not itself an alert rule; the alerts in `08/02-03` key off request success,
  error-budget burn, and dependency metrics so a single pod restarting is not a page.

---

## Outbox and Relay Telemetry

Durable event delivery is NFR-AVAIL-007 and the relay design is `03/02-03`. Telemetry makes the
guarantee observable:

- **Backlog size and age.** `resys.outbox.pending` and `resys.outbox.oldest_age_seconds` per store
  show whether events are draining. Age is the primary health signal: a growing oldest-age means
  consumers or the relay are stuck even if the API looks healthy.
- **Relay throughput and lag.** `resys.outbox.published` per `event_type` and
  `resys.outbox.relay_lag_seconds` (commit time to publish time) show the relay keeping up. The
  relay is concurrency-safe across API replicas; adding replicas does not multiply publish count
  because only one leases a row (`08/04-01`).
- **Failures.** `resys.outbox.failed` counts retried publishes; `resys.outbox.dead_lettered` counts
  events exhausted past the retry budget and parked for operator action. A non-zero dead-letter
  increment is an alert (`08/02-03`) and is investigated through the runbooks in `08/03-01`.
- **Consumers.** Email and analytics consumers emit their own delivery counters keyed by
  `event_type`, so a relay that publishes but a consumer that cannot send is distinguishable from a
  relay that is stuck (`03/04-06`).
- **Traceability.** Each outbox row records the originating request id and `trace_id`; the relay and
  consumers attach them to their spans and logs so an event's full path is one trace.

---

## Database Telemetry

- **Spans and durations.** EF Core/Npgsql emits `db.client.operation.duration`; the slow-query
  signal is `pg_stat_statements` plus the PostgreSQL slow-query log (`08/01-01`). A request-path
  statement over the **5 s** timeout is both a span error and a slow-query entry (NFR-PERF-009).
- **Pool health.** `db.client.connection.count` (idle vs used),
  `db.client.connection.pending_requests`, and `db.client.connection.timeouts` expose pool
  saturation; the pool caps and PgBouncer budget are `08/04-01`.
- **Replication.** Replica replication lag is read from the managed database's metrics and carried
  into the same backend (`08/01-01`); it gates replica read eligibility (`08/04-01`).
- **Backups.** WAL-archive lag and last-successful-backup time are exported by the managed service
  and feed the RPO/RTO panels in `08/02-02` (NFR-AVAIL-004).

---

## Configuration

Names and defaults are owned by `05/01-02`; only the observability-relevant entries are listed here.

| Setting | Environment variable | Purpose |
|---|---|---|
| `Observability:OtlpEndpoint` | `Observability__OtlpEndpoint` | OTLP/gRPC collector endpoint for traces, metrics, and logs. Optional locally; required in staging/production. |

`service.name` and `deployment.environment` come from the image and the runtime environment;
`deployment.environment` is derived from `ASPNETCORE_ENVIRONMENT`. Sampling ratios, collector
batching, and log retention are deployment values owned by `08/01-01`.

## Open Questions

1. The exact baseline trace-sampling ratio, the tail-sampling policy at the collector, and log
   retention are deployment values finalized with the collector in `08/01-01`; the always-keep
   rules for error/payment/outbox traces are fixed here.
2. Whether the `.NET` runtime emits the full OTel runtime metric set or a curated subset is a
   cardinality decision owned by `08/01-01`; the HTTP, database, checkout, payment, and outbox
   series above are required.
3. The concrete log-backend and metric-backend products behind the collector are `08/01-01`
   choices; this file fixes the schema, names, and attributes they must retain.
4. Whether the Nuxt front-ends export OTLP directly or through a server-side proxy on the Nitro
   process is an implementation detail; either path preserves the propagation and resource
   attributes fixed above.
