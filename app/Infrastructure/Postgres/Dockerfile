# syntax=docker/dockerfile:1
FROM postgres:17-alpine

# Initialize: keep the upstream entrypoint (initdb, user/database creation, signal
# handling) and encode the small-host tuning once, so Compose and CD agree.
# Detail: these values pair with the 1 CPU / 768 MB limit in compose.yaml.
CMD ["postgres", \
     "-c", "shared_buffers=128MB", \
     "-c", "effective_cache_size=384MB", \
     "-c", "max_connections=50", \
     "-c", "work_mem=4MB"]
