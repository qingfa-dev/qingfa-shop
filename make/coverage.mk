##@ Coverage

.PHONY: coverage coverage.unit coverage.store coverage.admin coverage.web coverage.api coverage.apphost coverage.infra coverage.integration coverage.report coverage.all coverage.clean

# Floors are exported so the backend checker and the Vitest configs agree.
COVERAGE_LINES ?= 90
COVERAGE_BRANCHES ?= 80
export COVERAGE_LINES
export COVERAGE_BRANCHES

coverage: coverage.unit coverage.web ## Fast coverage gate: .NET unit and both web apps
	$(call run,$@,echo "coverage: ok")

coverage.unit: _dotnet.build _dotnet.tools ## .NET unit coverage with per-assembly floors
	$(call run,$@,$(ROOT)/scripts/backend/coverage-lib.backend.sh)

coverage.web: coverage.store coverage.admin ## Storefront and admin coverage with per-app floors

coverage.store: _web.install ## Storefront coverage with per-app floors
	$(call run,$@,$(ROOT)/scripts/web/coverage.web.sh @qingfa/storefront)

coverage.admin: _web.install ## Admin coverage with per-app floors
	$(call run,$@,$(ROOT)/scripts/web/coverage.web.sh @qingfa/admin)

coverage.api: _dotnet.build _dotnet.tools ## Api integration coverage (Web.Api floor)
	$(call run,$@,$(ROOT)/scripts/backend/coverage-api.backend.sh)

coverage.apphost: _dotnet.build _web.install _dotnet.tools ## Gateway routing coverage (report only)
	$(call run,$@,$(ROOT)/scripts/backend/coverage-apphost.backend.sh)

coverage.infra: _dotnet.build _dotnet.tools ## Infrastructure integration coverage (report only)
	$(call run,$@,$(ROOT)/scripts/backend/coverage-infra.backend.sh)

coverage.integration: coverage.api coverage.apphost coverage.infra ## Integration suites; raw reports feed coverage.report

coverage.report: _dotnet.tools ## Merge raw reports and enforce every gated assembly
	$(call run,$@,$(ROOT)/scripts/backend/coverage-report.backend.sh)

coverage.all: coverage.unit coverage.integration coverage.web coverage.report ## Every suite plus the merged gate, in layer order

coverage.clean: ## Remove coverage reports and raw files
	$(call run,$@,rm -rf "$(ROOT)/artifacts/coverage")
