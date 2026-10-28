##@ CI/CD

.PHONY: ci ci.full cd

ci: lint build coverage.unit test.dotnet.integration.fast coverage.web ## Pull-request gate
	$(call run,$@,echo "ci: ok")

ci.full: lint build coverage.all test.web.e2e ## Main-branch gate, including containers
	@printf '\n==> %s\n' '$@'
	@trap '$(MAKE) --no-print-directory infra.down' EXIT; \
		$(MAKE) --no-print-directory infra.up && \
		$(MAKE) --no-print-directory infra.smoke

cd: infra.build ## Build tagged images for a registry
	$(call run,$@,echo "cd: images built for $${REGISTRY:-qingfa-shop}:$${TAG:-local}")
