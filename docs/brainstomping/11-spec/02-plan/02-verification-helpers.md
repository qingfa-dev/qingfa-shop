# Verification Helpers

The placeholder-scan and link-check commands used to verify the docs deliverable.

**Links:** [Spec index](../README.md) · [Design spec](../01-design/01-purpose-scope.md) · [← Prev](01-global-constraints.md) · [Next →](03-governance.md) · [Root README](../../../README.md)

---

Run these from the repository root. They are the "test suite" for this docs deliverable.

**Placeholder scan** (scoped to what you just wrote; expect `clean`):
```bash
grep -rnE '(TBD|TODO|FIXME|XXX|PLACEHOLDER|fill in the details|implement later)' <scope> && echo "FOUND PLACEHOLDERS" || echo "clean"
```

**Link check** (all docs; expect no `BROKEN:` lines):
```bash
find docs -name '*.md' -print0 | while IFS= read -r -d '' f; do
  grep -oE '\]\([^)]+\)' "$f" | sed -E 's/^\]\(//; s/\)$//' \
    | grep -E '\.md(#|$)' | grep -vE '^https?://' \
    | while IFS= read -r l; do
        t="$(dirname "$f")/${l%%#*}"
        [ -e "$t" ] || echo "BROKEN: $f -> $l"
      done
done
```
