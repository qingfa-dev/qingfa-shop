# syntax=docker/dockerfile:1
FROM rabbitmq:4.3-management-alpine

# Initialize: bake the small-host tuning once so Compose, the AppHost, and CD all
# agree on the broker's listeners and resource limits.
COPY app/Infrastructure/RabbitMQ/rabbitmq.conf /etc/rabbitmq/rabbitmq.conf

# Detail: the -management image already enables the management plugin; the
# configuration file only tunes listeners, memory, disk, and consumer timeouts.
