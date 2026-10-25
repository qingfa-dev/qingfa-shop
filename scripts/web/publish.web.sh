#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
. "$ROOT/scripts/common/trace.common.sh"
app="${1:?usage: publish.web.sh <workspace-app-directory> <artifact-name>}"
artifact="${2:?usage: publish.web.sh <workspace-app-directory> <artifact-name>}"

source_dir="$ROOT/app/$app/dist"
target_dir="$ROOT/artifacts/publish/$artifact"

if [ ! -d "$source_dir" ]; then
  echo "publish: $source_dir is missing; run the app's build target first" >&2
  exit 1
fi

trace rm -rf "$target_dir"
trace mkdir -p "$target_dir"
trace cp -R "$source_dir/." "$target_dir"

echo "publish: $target_dir"
