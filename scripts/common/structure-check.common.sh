#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

status=0

# ---------------------------------------------------------------------------
# Role / sub-role vocabulary
# ---------------------------------------------------------------------------
read -r -d '' ROLES <<'EOF' || true
action adapter aggregate aggregate-root assertion assertions authorization behavior
binder builder callback cache cache-key client collection column command combinator
comparer comparator composition configuration constant constants constraint consumer
container context controller converter coordinator contract criteria cursor decorator
definition descriptor dispatcher dto endpoint entity entity-configuration enum error
event exception executor expression expressions extension extensions factory failure
fake field fields filter fixture formatter function gateway guard handler health-check
helper hosted-service identity include index infrastructure integration interceptor
interface invariant job listener liveness logger mapper mapping matcher materializer
maybe mediator message metadata migration middleware mock model modifier module monitor
navigation navigations normalizer notification observer operation operator option
options optional orchestrator ordering outcome page page-request page-result pagination
paginator parser pipeline policy predicate procedure processor projection provider proxy
publisher query query-builder query-constant query-descriptor query-expression query-map
query-model query-option query-parameter query-provider read-model readiness registry
repository request resolver response result retry route router rule scheduler schema
search search-query search-result seeder serializer service session settings severity
snapshot sort sort-descriptor sort-order sorter spec specification spy state status
store strategy stub subscriber success table test test-case test-data test-host
test-server tests theory transaction transformer translator transport trigger type unit
unit-of-work use-case util utilities validator validation validation-result value-object
view view-model webhook worker workflow write-model
marker design tabular querymap queryconstant readmodel flow double base default generic config
seed e2e-spec enumerate create update upsert update delete read abstract write streamable
EOF

read -r -d '' SUBROLES <<'EOF' || true
async sync efcore entity-framework postgres postgresql sql redis http https grpc json
xml yaml csv binary default generic concrete abstract base derived read write create
update delete remove find get list count exists single first last stream streaming
batch cached decorated fallback retry bulk paged cursor offset descending ascending
case-insensitive case-sensitive nullable required optional internal external public
private protected static instance immutable mutable readonly writeonly partial
generated custom standard legacy v1 v2
EOF

# Build hash sets once (bash 4+). `for r in $ROLES` splits on whitespace.
declare -A ROLE_SET=() SUBROLE_SET=()
for r in $ROLES;    do ROLE_SET[$r]=1;    done
for r in $SUBROLES; do SUBROLE_SET[$r]=1; done

is_role()    { [ -n "${1:-}" ] || return 1; [ -n "${ROLE_SET[$1]:-}" ]; }
is_subrole() { [ -n "${1:-}" ] || return 1; [ -n "${SUBROLE_SET[$1]:-}" ]; }

# ---------------------------------------------------------------------------
# Only scan directories that actually exist
# ---------------------------------------------------------------------------
scan_dirs=()
for d in src tests app; do
  [ -d "$d" ] && scan_dirs+=("$d")
done
if [ ${#scan_dirs[@]} -eq 0 ]; then
  echo "no source directories (src/tests/app) found under $ROOT"
  exit 1
fi

cs_files() {
  find "${scan_dirs[@]}" -type f -name '*.cs' \
    -not -path '*/obj/*' -not -path '*/bin/*' -not -path '*/Migrations/*' 2>/dev/null
}

# ---------------------------------------------------------------------------
# 1. Namespace mirrors folder path under the project root
# ---------------------------------------------------------------------------
while read -r file; do
  project_dir="$file"
  while [ "$project_dir" != "." ] && ! compgen -G "$project_dir/*.csproj" >/dev/null; do
    project_dir="$(dirname "$project_dir")"
  done

  compgen -G "$project_dir/*.csproj" >/dev/null || continue

  # tests/Testing is a flat shared kit — foldering is not mirrored.
  if [ "$project_dir" = "tests/Testing" ]; then
    continue
  fi

  # <RootNamespace>…</RootNamespace> (sed -E: portable, no -P)
  root_ns="$(sed -E -n 's:.*<RootNamespace>([^<]*).*:\1:p' \
              "$project_dir"/*.csproj 2>/dev/null | head -n1 || true)"

  if [ -z "$root_ns" ] && [ -f "$project_dir/Assembly.Marker.cs" ]; then
    root_ns="$(sed -E -n 's/^namespace[[:space:]]+([A-Za-z0-9_.]+).*/\1/p' \
                "$project_dir/Assembly.Marker.cs" 2>/dev/null | head -n1 || true)"
  fi
  [ -n "$root_ns" ] || root_ns="$(basename "$project_dir")"

  rel="${file#"$project_dir"/}"
  rel_dir="$(dirname "$rel")"
  if [ "$rel_dir" = "." ]; then rel_dir=""; fi

  expected="$root_ns"
  if [ -n "$rel_dir" ]; then
    expected="$expected.$(printf '%s' "$rel_dir" | tr '/' '.')"
  fi

  actual="$(sed -E -n 's/^namespace[[:space:]]+([A-Za-z0-9_.]+).*/\1/p' \
            "$file" 2>/dev/null | head -n1 || true)"
  [ -n "$actual" ] || continue

  if [ "$actual" != "$expected" ]; then
    echo "namespace mismatch: $file declares $actual, folder implies $expected"
    status=1
  fi
done < <(cs_files)

# ---------------------------------------------------------------------------
# 2. Last dotted segment is a known role, or a known sub-role whose
#    chain already contains a known role.
# ---------------------------------------------------------------------------
while read -r file; do
  name="$(basename "$file" .cs)"

  # no dot in filename → rule does not apply
  case "$name" in
    *.*) ;;
    *)   continue ;;
  esac

  last="${name##*.}"
  last="${last,,}"

  if [ -z "$last" ]; then
    echo "unknown role '': $file"
    status=1
    continue
  fi

  if is_role "$last"; then
    continue
  fi

  if is_subrole "$last"; then
    chain="${name%.*}"
    found=0
    while [ -n "$chain" ]; do
      seg="${chain##*.}"
      seg="${seg,,}"
      if is_role "$seg"; then found=1; break; fi
      case "$chain" in
        *.*) chain="${chain%.*}" ;;
        *)   break ;;
      esac
    done
    if [ "$found" -eq 1 ]; then
      continue
    fi
  fi

  echo "unknown role '${name##*.}': $file"
  status=1
done < <(cs_files)

if [ "$status" -ne 0 ]; then
  echo "structure check failed"
  exit 1
fi

echo "structure check passed"