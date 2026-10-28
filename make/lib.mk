##@ Libraries

.PHONY: lib.install lib.format lib.format.check lib.lint lib.build lib.test

# Internal: the .NET projects share one solution restore, one solution build,
# and one tool manifest restore. Every backend app target depends on these, so
# Make runs each exactly once.
_dotnet.restore:
	@$(ROOT)/scripts/backend/restore.backend.sh

_dotnet.build: _dotnet.restore
	@$(ROOT)/scripts/backend/build.backend.sh

_dotnet.tools:
	@$(ROOT)/scripts/common/restore-tools.common.sh

lib.install: _dotnet.restore ## Restore NuGet packages (locked mode under CI=true)

lib.format: _dotnet.restore ## Format shared libraries and their test projects
	$(call run,$@,$(ROOT)/scripts/backend/format.backend.sh src tests/Unit tests/Architecture.Tests tests/Contract tests/Testing)

lib.format.check: _dotnet.restore ## Verify shared-library formatting without writing
	$(call run,$@,$(ROOT)/scripts/backend/format-check.backend.sh src tests/Unit tests/Architecture.Tests tests/Contract tests/Testing)

lib.lint: lib.format.check structure.check ## Formatting and structure checks

lib.build: _dotnet.build ## Build the .NET solution

lib.test: lib.build ## Architecture and unit suites
	$(call run,$@,$(ROOT)/scripts/backend/test-lib.backend.sh)
