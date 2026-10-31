# Architecture Documentation

This folder holds the evidence, the roadmap, and the execution guides for turning `QingFa.Shop` into a production-grade platform.

## Reading order

| # | Document | What it answers |
|---|---|---|
| 1 | [`software-quality-audit.pro.md`](software-quality-audit.pro.md) | Is the software safe to run in production? (Q1–Q32, verified defects) |
| 2 | [`roadmap-implementation-audit.pro.md`](roadmap-implementation-audit.pro.md) | Which roadmap concepts are implemented, partial, or absent? |
| 3 | [`master-roadmap.md`](master-roadmap.md) | What is the complete, gated plan across every workstream? |
| 4 | [`guides/foundation-implementation.md`](guides/foundation-implementation.md) | How to execute Phase 0/1, file by file (F1–F11) |
| 5 | [`guides/modules-implementation.md`](guides/modules-implementation.md) | How to build Catalog, Cart, Inventory, Orders, Notifications, Search (M1–M9) |
| 6 | [`guides/experience-edge-ops.md`](guides/experience-edge-ops.md) | How to build the frontend, edge contract, CI/CD, SLOs, and runbooks |
| 7 | [`roadmap-implementation-audit.md`](roadmap-implementation-audit.md) | v1.0 of the roadmap audit (superseded, kept for history) |

`master-roadmap.md` is the index of intent; the three guides are the implementation detail. When the roadmap and a guide disagree, the roadmap's phase gate wins and the guide is corrected.

## Model and cost provenance

| Deliverable | Model | Effort | Input $/M | Output $/M | Cache read $/M |
|---|---|---|---|---|---|
| Master roadmap | GPT-6.1 Terra Pro | high | 1.00 | 5.00 | 0.05 |
| Foundation guide | DeepSeek V4 Pro | high | 1.60 | 3.20 | 0.135 |
| Modules guide | DeepSeek V4 Pro | high | 1.60 | 3.20 | 0.135 |
| Experience/edge/ops guide | DeepSeek V4 Pro | high | 1.60 | 3.20 | 0.135 |

**Astra:** GPT-6 Astra Pro (`$10/$50`, high effort) was requested first for the roadmap. The provider rejected the call:

> *"This request requires more credits, or fewer max_tokens. You requested up to 128000 tokens, but can only afford 40926."*

No roadmap was produced by Astra. The roadmap was generated with GPT-6.1 Terra Pro — the next-cheapest current-generation flagship in the earlier model selection — and is labelled accordingly. If Astra budget becomes available, re-run the roadmap prompt and diff the result before changing phase gates.

## How to keep this current

- Every merged Phase item updates its acceptance checklist and the roadmap status.
- Every architectural decision gets an ADR under `docs/architecture/adr/` (proposed).
- Re-run the audits when a minor release changes a Critical/High finding; do not delete history.
- Treat `docs/architecture/*.pro.md` as snapshots, not living specifications; the guides are the living execution documents.
