##@ Admin

.PHONY: admin.install admin.format admin.format.check admin.lint admin.test admin.test.unit admin.test.integration admin.e2e admin.build admin.publish

admin.install: _web.install ## Install workspace dependencies

admin.format: _web.install ## Format the admin app with Prettier
	$(call run,$@,$(ROOT)/scripts/web/format.web.sh Web.Admin)

admin.format.check: _web.install ## Verify admin formatting without writing
	$(call run,$@,$(ROOT)/scripts/web/format-check.web.sh Web.Admin)

admin.lint: admin.format.check ## Type-check the admin app with vue-tsc
	$(call run,$@,$(ROOT)/scripts/web/lint.web.sh @qingfa/admin)

admin.test: admin.test.unit admin.test.integration ## Admin unit then integration suites

admin.test.unit: _web.install ## Admin Vitest unit suite
	$(call run,$@,$(ROOT)/scripts/web/test.web.sh @qingfa/admin unit)

admin.test.integration: _web.install ## Admin Vitest integration suite
	$(call run,$@,$(ROOT)/scripts/web/test.web.sh @qingfa/admin integration)

admin.e2e: _web.install ## Optional admin Playwright suite (skipped when absent)
	$(call run,$@,$(ROOT)/scripts/web/e2e.web.sh Web.Admin @qingfa/admin)

admin.build: _web.install ## Production bundle for the admin app
	$(call run,$@,$(ROOT)/scripts/web/build.web.sh @qingfa/admin)

admin.publish: admin.build ## Copy the admin bundle to artifacts/publish/admin
	$(call run,$@,$(ROOT)/scripts/web/publish.web.sh Web.Admin admin)
