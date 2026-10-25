# syntax=docker/dockerfile:1
FROM redis:7-alpine

# Initialize: cache-only defaults. Redis is not a system of record here, so RDB and
# AOF stay off and the 128 MB ceiling matches the 192 MB container limit.
# Fallback: override the command with `--appendonly yes` if Redis ever needs to
# survive a restart.
CMD ["redis-server", \
     "--save", "", \
     "--appendonly", "no", \
     "--maxmemory", "128mb", \
     "--maxmemory-policy", "allkeys-lru"]
