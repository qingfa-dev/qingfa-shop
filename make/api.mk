##@ Api

.PHONY: api.install api.format api.format.check api.lint api.build api.test api.publish

api.install: _dotnet.restore ## Restore NuGet packages

api.format: _dotnet.restore ## Format the Api and its integration tests
	$(call run,$@,$(ROOT)/scripts/backend/format.backend.sh app/Web.Api tests/Integration/Api.Tests)

api.format.check: _dotnet.restore ## Verify Api formatting without writing
	$(call run,$@,$(ROOT)/scripts/backend/format-check.backend.sh app/Web.Api tests/Integration/Api.Tests)

api.lint: api.format.check structure.check ## Formatting and structure checks

api.build: _dotnet.build ## Build the .NET solution

api.test: api.build ## Api integration suite
	$(call run,$@,$(ROOT)/scripts/backend/test-api.backend.sh)

api.publish: api.build ## Publish the Api to artifacts/publish/api
	$(call run,$@,$(ROOT)/scripts/backend/publish-api.backend.sh)
