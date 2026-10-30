##@ Infra

.PHONY: infra.config infra.build infra.size infra.test infra.up infra.down infra.ps infra.logs infra.smoke infra.backing.up infra.backing.down infra.backing.health

infra.config: ## Validate the Compose files
	$(call run,$@,$(ROOT)/scripts/infra/config.infra.sh)

infra.build: ## Build all seven images (REGISTRY, TAG, SERVICES)
	$(call run,$@,$(ROOT)/scripts/infra/build.infra.sh)

infra.size: ## Print local image sizes
	$(call run,$@,$(ROOT)/scripts/infra/size.infra.sh)

infra.test: _dotnet.build ## Postgres, Redis, and RabbitMQ integration suite
	$(call run,$@,$(ROOT)/scripts/infra/test.infra.sh)

infra.up: ## Start the Compose stack (builds images)
	$(call run,$@,$(ROOT)/scripts/infra/up.infra.sh)

infra.down: ## Stop the Compose stack (keeps volumes)
	$(call run,$@,$(ROOT)/scripts/infra/down.infra.sh)

infra.ps: ## Show Compose service status
	$(call run,$@,$(ROOT)/scripts/infra/ps.infra.sh)

infra.logs: ## Follow Compose logs (SERVICE=name to filter)
	$(call run,$@,$(ROOT)/scripts/infra/logs.infra.sh $(SERVICE))

infra.smoke: ## Assert the stack answers through the gateway and backing services are healthy
	$(call run,$@,$(ROOT)/scripts/infra/smoke.infra.sh)

infra.backing.up: ## Start and wait for postgres, redis, and rabbitmq
	$(call run,$@,$(ROOT)/scripts/infra/backing-up.infra.sh)

infra.backing.down: ## Stop postgres, redis, and rabbitmq (keeps volumes)
	$(call run,$@,$(ROOT)/scripts/infra/backing-down.infra.sh)

infra.backing.health: ## Check postgres, redis, and rabbitmq listeners
	$(call run,$@,$(ROOT)/scripts/infra/backing-health.infra.sh)
