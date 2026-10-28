# Echo the target name before its recipe runs. The called script then echoes the
# exact tool CLI (`$ dotnet ...`, `$ pnpm ...`) immediately before executing it,
# so a failing target can be reproduced by copying the last printed line.
define run
@printf '\n==> %s\n' '$(1)'
@$(2)
endef

##@ Setup

.PHONY: help doctor

help: ## Show this help
	@awk 'BEGIN {FS = ":.*##"} /^##@/ {printf "\n\033[1m%s\033[0m\n", substr($$0, 5)} /^[a-zA-Z0-9_.-]+:.*##/ {printf "  \033[36m%-30s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

doctor: ## Check the local toolchain
	$(call run,$@,$(ROOT)/scripts/common/doctor.common.sh)

##@ Quality

.PHONY: install format format.check structure.check lint build

install: lib.install api.install gate.install host.install store.install admin.install ## Restore .NET and web dependencies

format: lib.format api.format gate.format host.format store.format admin.format ## Format C# and both web apps

format.check: lib.format.check api.format.check gate.format.check host.format.check store.format.check admin.format.check ## Verify formatting without writing

structure.check: ## Verify namespaces match folders and file roles are known
	$(call run,$@,$(ROOT)/scripts/common/structure-check.common.sh)

lint: lib.lint api.lint gate.lint host.lint store.lint admin.lint ## Formatting, structure, and type checks

build: lib.build api.build gate.build host.build store.build admin.build ## Build the solution and both web apps
