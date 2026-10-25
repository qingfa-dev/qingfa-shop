##@ Gateway

.PHONY: gate.install gate.format gate.format.check gate.lint gate.build gate.test gate.publish

gate.install: _dotnet.restore ## Restore NuGet packages

gate.format: _dotnet.restore ## Format the Gateway
	$(call run,$@,$(ROOT)/scripts/backend/format.backend.sh app/Gateway)

gate.format.check: _dotnet.restore ## Verify Gateway formatting without writing
	$(call run,$@,$(ROOT)/scripts/backend/format-check.backend.sh app/Gateway)

gate.lint: gate.format.check structure.check ## Formatting and structure checks

gate.build: _dotnet.build ## Build the .NET solution

gate.test: gate.build _web.install ## Gateway routing specs through the AppHost
	$(call run,$@,$(ROOT)/scripts/backend/test-gateway.backend.sh)

gate.publish: gate.build ## Publish the Gateway to artifacts/publish/gateway
	$(call run,$@,$(ROOT)/scripts/backend/publish-gateway.backend.sh)
