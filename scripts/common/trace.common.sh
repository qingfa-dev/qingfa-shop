#!/usr/bin/env bash
# Sourced by the other scripts: echo a command line, then run it.
#
# Keeping the echo and the argv in one place means the printed command can never
# drift from the command that actually executes. Trace lines go to stderr so a
# caller can still capture the command's stdout (for example `config.infra.sh`
# silences `docker compose config`).

_TRACE_START_DIR="$PWD"

_trace_print() {
  if [ "$PWD" != "$_TRACE_START_DIR" ]; then
    printf '  (in %s)\n' "${PWD#"$_TRACE_START_DIR"/}" >&2
  fi
  printf '$' >&2
  printf ' %q' "$@" >&2
  printf '\n' >&2
}

# Run a command after echoing it.
trace() {
  _trace_print "$@"
  "$@"
}

# Replace this shell with the command after echoing it.
trace_exec() {
  _trace_print "$@"
  exec "$@"
}
