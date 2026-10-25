#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

status=0

# 1. A namespace mirrors the folder path under its project root.
#    The project root is the folder that holds the .csproj. Its expected
#    namespace comes from <RootNamespace> when the project declares one,
#    otherwise from Assembly.Marker.cs, otherwise from the folder name.
#    This keeps intentional roots such as QingFa.Shop.Architecture.Tests
#    working without forcing every project into one naming style.
while read -r file; do
  project_dir="$file"
  while [ "$project_dir" != "." ] && ! compgen -G "$project_dir/*.csproj" >/dev/null; do
    project_dir="$(dirname "$project_dir")"
  done

  compgen -G "$project_dir/*.csproj" >/dev/null || continue

  # Detail: tests/Testing is a shared kit that uses one flat namespace
  # (`Testing`) so consumers write a single using; foldering is not mirrored.
  [ "$project_dir" = "tests/Testing" ] && continue

  root_ns="$(grep -m1 -oP '<RootNamespace>\K[^<]+' "$project_dir"/*.csproj 2>/dev/null || true)"
  if [ -z "$root_ns" ] && [ -f "$project_dir/Assembly.Marker.cs" ]; then
    root_ns="$(grep -m1 -oP '^namespace \K[A-Za-z0-9_.]+' "$project_dir/Assembly.Marker.cs" || true)"
  fi
  [ -n "$root_ns" ] || root_ns="$(basename "$project_dir")"

  rel="${file#"$project_dir"/}"
  rel_dir="$(dirname "$rel")"
  [ "$rel_dir" = "." ] && rel_dir=""

  expected="$root_ns"
  [ -n "$rel_dir" ] && expected="$expected.$(echo "$rel_dir" | tr '/' '.')"

  actual="$(grep -m1 -oP '^namespace \K[A-Za-z0-9_.]+' "$file" || true)"
  [ -n "$actual" ] || continue

  if [ "$actual" != "$expected" ]; then
    echo "namespace mismatch: $file declares $actual, folder implies $expected"
    status=1
  fi
done < <(find src tests app -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -not -path '*/Migrations/*')

# 2. Every file's last dotted segment is a known role, or a known sub-role
#    whose chain already contains a known role.
ROLES="controller router resolver middleware interceptor guard handler endpoint webhook
service use-case command query workflow orchestrator policy rule specification validator
mapper transformer factory builder strategy provider model entity aggregate value-object
dto schema migration seed repository store projection snapshot interface type enum
constant config options event message result error util helper extension decorator
adapter client gateway proxy listener scheduler worker job spec test tests mock stub fake
fixture e2e-spec base
outcome marker composition collection behavior logger operator severity default exception
generic design tabular request querymap queryconstant configuration flow double registry"

SUBROLES="async sync"

is_role() {
  echo "$ROLES" | tr ' ' '\n' | grep -qx "$1"
}

is_subrole() {
  echo "$SUBROLES" | tr ' ' '\n' | grep -qx "$1"
}

while read -r file; do
  name="$(basename "$file" .cs)"
  case "$name" in
    *.*) ;;
    *) continue ;;
  esac

  last="$(echo "${name##*.}" | tr '[:upper:]' '[:lower:]')"

  if is_role "$last"; then
    continue
  fi

  # A sub-role is only accepted when the chain already names a role, so
  # `Result.Extension.Flow.Async.cs` passes but `Foo.Async.cs` does not.
  if is_subrole "$last"; then
    chain="${name%.*}"
    found=0
    while [ -n "$chain" ] && [ "$chain" != "$name" ]; do
      seg="$(echo "${chain##*.}" | tr '[:upper:]' '[:lower:]')"
      if is_role "$seg"; then found=1; break; fi
      chain="${chain%.*}"
    done
    [ "$found" -eq 1 ] && continue
  fi

  echo "unknown role '${name##*.}': $file"
  status=1
done < <(find src tests app -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -not -path '*/Migrations/*')

if [ "$status" -ne 0 ]; then
  echo "structure check failed"
  exit 1
fi

echo "structure check passed"
