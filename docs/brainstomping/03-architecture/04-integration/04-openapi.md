# OpenAPI & Scalar

How the REST contract is generated with the built-in `Microsoft.AspNetCore.OpenApi`, browsed in Scalar, published at build time, and kept in exact parity with the API reference in `09-user/03-api/`.

**Links:** [REST public API](./01-rest-public.md) · [REST admin API](./02-rest-admin.md) · [Authentication](./03-auth.md) · [Module contracts](../02-components/02-module-contracts.md) · [Solution layout](../02-components/01-solution-layout.md) · [ADR 0003 — No CMS, no GraphQL](../../00-governance/01-adr/03-no-cms-no-gql.md) · [Architecture index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file specifies the API description pipeline. resys-eshop generates its OpenAPI documents with
the built-in `Microsoft.AspNetCore.OpenApi` package — no third-party document generator — from the
Minimal API endpoint definitions and the `Resys.Shared` DTO records. The documents are browsable in
Scalar during development, generated to files at build time for clients and review, and are the
source of truth that the published reference in `09-user/03-api/` must mirror exactly. The public
contract is `03/04-01`, the admin contract is `03/04-02`, and auth is `03/04-03`.

The single rule: **the generated contract is the API; a hand-written reference may never diverge
from it, and a CI parity gate enforces that.**

---

## Documents

Two named documents are produced, one per surface, so a client can be generated from exactly the
surface it consumes and the public surface never advertises admin routes.

| Document | Title | Routes covered | URL |
|---|---|---|---|
| `v1` | resys-eshop Public API | `/api/v1/*` | `/openapi/v1.json` |
| `admin` | resys-eshop Admin API | `/api/admin/*` | `/openapi/admin.json` |

Each document carries the API version (`1.0` for `v1`), the server list per environment, and the
`bearerAuth` security scheme. Because GraphQL is explicitly out of scope (ADR 0003), OpenAPI is the
only machine-readable contract.

---

## Runtime Setup

`Resys.Api` registers one document per surface and maps the document endpoints:

```csharp
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<ResysInfoTransformer>();
    options.AddOperationTransformer<SurfaceTagTransformer>();
    options.AddSchemaTransformer<MoneySchemaTransformer>();
});

builder.Services.AddOpenApi("admin", options =>
{
    options.AddDocumentTransformer<ResysInfoTransformer>();
    options.AddOperationTransformer<SurfaceTagTransformer>();
    options.AddSchemaTransformer<MoneySchemaTransformer>();
});

var app = builder.Build();
app.MapOpenApi(); // exposes /openapi/v1.json and /openapi/admin.json
```

Document transformers set the title, version, server, and the `bearerAuth` scheme. The operation
transformer tags operations by module (catalog, cart, checkout, account, reviews; catalog, orders,
inventory, configuration, stores, users, reports) and records the required authorization so the
document states which operations need `admin`. The schema transformer renders `Money` as
`{ "amount": number, "currency": string }`, matching `03/03-03`; no raw decimal appears in a schema.

Endpoints are declared as Minimal API route groups and typed with `Results<T1, T2>` so the generated
status codes are truthful. Request/response types are the `Resys.Shared` records, so the schema
names are the DTO names used throughout `03/04-01` and `03/04-02` (for example `CartDto`,
`FulfillOrderRequest`).

---

## Scalar UI

Scalar is the interactive reference used in development. `Resys.Api` maps it against both documents:

```csharp
using Scalar.AspNetCore;

app.MapScalarApiReference(options =>
{
    options.WithTitle("resys-eshop API")
           .WithOpenApiRoutePattern("/openapi/{documentName}.json")
           .AddDocument("v1", "Public API")
           .AddDocument("admin", "Admin API")
           .AddPreferredSecuritySchemes("bearerAuth");
});
```

- The UI is served at `/scalar` in non-production environments and is disabled in production.
- The `bearerAuth` scheme accepts a pasted access token so a developer can exercise both surfaces.
- The admin document is usable through the same bearer scheme; the httpOnly-cookie flow is browser
  behavior, so the admin UI documents `POST /api/admin/auth/login` and the cookie exchange rather
  than setting the cookie itself.
- Scalar reads only the generated documents; it is a viewer, never a source of the contract.

---

## Published Contract

The contract is published as files at build time so clients, reviews, and the parity gate do not
depend on a running server. Build-time generation uses the built-in document generator via the
`Microsoft.Extensions.ApiDescription.Server` package and the `OpenApiDocumentsDirectory` property.

```xml
<PropertyGroup>
  <OpenApiGenerateDocumentsOnBuild>true</OpenApiGenerateDocumentsOnBuild>
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/../../artifacts/openapi</OpenApiDocumentsDirectory>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.ApiDescription.Server" Version="10.0.*" />
</ItemGroup>
```

A `dotnet build src/Resys.Api` emits `artifacts/openapi/v1.json` and
`artifacts/openapi/admin.json`. The artifacts are:

- **Attached to the build/CI run** and diffed against the committed expectation so an accidental
  contract change fails the pipeline.
- **The input to client generation.** `web/shared` derives its typed API client and TypeScript DTO
  mirrors from the generated documents, so the front-ends cannot drift from the API. The concrete
  generator and the generated-file location are a development convention owned by `05/02-03`.
- **The source for the published reference.** `docs/09-user/03-api/` is written from these documents
  (`09/03-01` … `09/03-06`).

Production hosts may also expose `/openapi/*.json` behind authentication for tooling; the files are
the canonical artifacts regardless.

---

## Parity Rule

The published API reference in `09-user/03-api/` **mirrors the generated contract exactly**. It is
hand-written prose around the same operations, not an independently designed API.

1. **Same operations.** Every endpoint token in `09/03-01` … `09/03-06` appears here as
   `METHOD /api/...` with the same method, path, and `{param}` names. The endpoint tables in
   `03/04-01` and `03/04-02` use that literal token form precisely so a parity diff can compare
   both sides verbatim.
2. **Same schemas.** DTO field names, types, and required/optional status in the reference match the
   generated schemas; `Money` is always `{ amount, currency }`.
3. **Same status codes.** The status codes documented for an operation are the ones the generated
   document reports.
4. **Same auth.** Operations marked `admin` in the generated document are described as requiring
   the `admin` role and store scope (`03/04-03`); public operations are described as public.

The parity check runs as part of verification: it extracts the endpoint tokens from the
`03/04-*` files and from the `09/03-*` reference and reports any difference. A contract change is
made once in the endpoint definitions, regenerated, and then reflected in both doc sets; the
reference is never edited to introduce an operation the API does not have.

---

## Change Workflow

1. Change the Minimal API endpoint and its `Resys.Shared` DTOs.
2. Run `dotnet build src/Resys.Api` to regenerate `v1.json` and `admin.json`.
3. Review the artifact diff; confirm no unintended operation, schema, or status change.
4. Update `03/04-01` / `03/04-02` and the matching `09/03-*` reference in the same change.
5. Run the parity check; a non-empty diff blocks the change.

## Open Questions

1. The exact client generator used by `web/shared` (`openapi-typescript` versus Kiota) and where the
   generated files are committed are owned by `05/02-03`; the contract they consume is fixed here.
2. Whether production exposes the JSON documents publicly or behind admin authentication is a
   deployment decision owned by `08/01-01`; the build-time artifacts are unaffected.
3. The precise grouping of the reference into `09/03-*` files is owned by `09/03-01` … `09/03-06`;
   the parity requirement applies across all of them.

## Links

- [REST public API](./01-rest-public.md)
- [REST admin API](./02-rest-admin.md)
- [Authentication](./03-auth.md)
- [App topology](../01-overview/02-app-topology.md)
- [Solution layout](../02-components/01-solution-layout.md)
- [Module contracts](../02-components/02-module-contracts.md)
- [i18n & money](../03-data/03-i18n-money.md)
- [ADR 0003 — No CMS, no GraphQL](../../00-governance/01-adr/03-no-cms-no-gql.md)
- [Design spec](../../11-spec/README.md) — §7 (contracts,
  OpenAPI), §12 (API parity verification).
- Public API: `03/04-01`; admin API: `03/04-02`; auth: `03/04-03`; module conventions: `05/02-03`;
  published reference: `09/03-01` … `09/03-06`.
