##@ Tests

.PHONY: test test.all test.dotnet.unit test.dotnet.integration test.dotnet.integration.fast test.web.unit test.web.integration test.web.e2e

test.dotnet.unit: lib.test ## .NET unit suites: Core -> Application -> Infrastructure -> Architecture

test.dotnet.integration.fast: api.test ## Fast .NET integration: Api only

test.dotnet.integration: api.test host.test infra.test ## .NET integration: Api -> AppHost/Gateway -> Infrastructure

test.web.unit: store.test.unit admin.test.unit ## Web unit suites: Storefront -> Admin

test.web.integration: store.test.integration admin.test.integration ## Web integration suites: Storefront -> Admin

test.web.e2e: store.e2e admin.e2e ## Optional browser e2e (skipped when no Playwright suite)

test: test.dotnet.unit test.dotnet.integration.fast test.web.unit test.web.integration ## Fast gate: .NET unit -> Api -> web unit -> web integration

test.all: test.dotnet.unit test.dotnet.integration test.web.unit test.web.integration test.web.e2e ## Every suite in layer order
