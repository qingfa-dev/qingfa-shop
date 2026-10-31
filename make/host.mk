##@ Host

.PHONY: host.install host.format host.format.check host.lint host.build host.test

host.install: _dotnet.restore ## Restore NuGet packages

host.format: _dotnet.restore ## Format the AppHost and orchestration tests
	$(call run,$@,$(ROOT)/scripts/backend/format.backend.sh app/AppHost tests/Integration/AppHost.Integration.Tests)

host.format.check: _dotnet.restore ## Verify AppHost formatting without writing
	$(call run,$@,$(ROOT)/scripts/backend/format-check.backend.sh app/AppHost tests/Integration/AppHost.Integration.Tests)

host.lint: host.format.check structure.check ## Formatting and structure checks

host.build: _dotnet.build ## Build the .NET solution

host.test: host.build _web.install ## Full Aspire orchestration suite
	$(call run,$@,$(ROOT)/scripts/backend/test-apphost.backend.sh)
