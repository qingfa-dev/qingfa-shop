##@ Store

.PHONY: store.install store.format store.format.check store.lint store.test store.test.unit store.test.integration store.e2e store.build store.publish

# Internal: both SPAs are workspace members, so one frozen install covers both.
_web.install:
	@$(ROOT)/scripts/web/install.web.sh

store.install: _web.install ## Install workspace dependencies

store.format: _web.install ## Format the storefront with Prettier
	$(call run,$@,$(ROOT)/scripts/web/format.web.sh Web.Storefront)

store.format.check: _web.install ## Verify storefront formatting without writing
	$(call run,$@,$(ROOT)/scripts/web/format-check.web.sh Web.Storefront)

store.lint: store.format.check ## Type-check the storefront with vue-tsc
	$(call run,$@,$(ROOT)/scripts/web/lint.web.sh @qingfa/storefront)

store.test: store.test.unit store.test.integration ## Storefront unit then integration suites

store.test.unit: _web.install ## Storefront Vitest unit suite
	$(call run,$@,$(ROOT)/scripts/web/test.web.sh @qingfa/storefront unit)

store.test.integration: _web.install ## Storefront Vitest integration suite
	$(call run,$@,$(ROOT)/scripts/web/test.web.sh @qingfa/storefront integration)

store.e2e: _web.install ## Optional storefront Playwright suite (skipped when absent)
	$(call run,$@,$(ROOT)/scripts/web/e2e.web.sh Web.Storefront @qingfa/storefront)

store.build: _web.install ## Production bundle for the storefront
	$(call run,$@,$(ROOT)/scripts/web/build.web.sh @qingfa/storefront)

store.publish: store.build ## Copy the storefront bundle to artifacts/publish/storefront
	$(call run,$@,$(ROOT)/scripts/web/publish.web.sh Web.Storefront storefront)
