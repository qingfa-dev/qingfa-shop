# 04 — Migration

One-time guides for moving legacy or placeholder code into the current architecture.

**Links:** [old_references → BuildingBlocks](./01-old-references-to-building-blocks.md) · [Development index](../README.md)

---

## Purpose

Migration files are **runbooks**, not standing conventions: they exist while a port is in flight and are retired (or marked historical) when the port finishes. Standing rules live in `02-conventions` and `03-architecture`.

## Files

| File | Purpose |
|---|---|
| [01-old-references-to-building-blocks.md](./01-old-references-to-building-blocks.md) | Port `old_references/Shared.*` into the six `BuildingBlocks.*` projects; phase checklist, mapping tables, verification commands. |

## Related

- Design of the destination kit: [`03/02-08`](../../03-architecture/02-components/08-building-blocks.md)
- ADR: [`00/01-05`](../../00-governance/01-adr/05-building-blocks.md)
- Module rules while the kit is incomplete: [`05/02-02`](../02-conventions/02-module-conventions.md)
