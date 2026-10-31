#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"

# Initialize: coverage depends on repository-pinned tools (dotnet-coverage), so
# the manifest is restored instead of relying on a developer's global install.
trace_exec dotnet tool restore
