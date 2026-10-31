# On-Call and Incident Response

How resys-eshop declares, runs, and closes an incident: severity levels, the escalation ladder, the communication cadence, and the blameless postmortem template.

**Links:** [Database runbook](./02-runbook-db.md) · [Gateway runbook](./03-runbook-gateway.md) · [Double-charge runbook](./04-runbook-double-charge.md) · [Alerts](../02-monitoring/03-alerts.md) · [Dashboards](../02-monitoring/02-dashboards.md) · [Telemetry](../02-monitoring/01-telemetry.md) · [Topology](../01-infrastructure/01-topology.md) · [Environments](../01-infrastructure/02-environments.md) · [Capacity planning](../04-scaling/01-capacity.md) · [Availability NFRs](../../01-requirements/03-nonfunctional/03-availability.md) · [Operations index](../README.md) · [Spec](../../11-spec/README.md)

---

## Purpose

This file is the incident-response contract for resys-eshop. It defines the severity levels that
the alert rules in `08/02-03` assign, the roles and escalation path an incident follows, how often
the team and customers are updated, and the postmortem every Sev1 and Sev2 produces. It is the
entry point that `08/02-03` names as runbook `08/03-01`, and it indexes the three procedure
runbooks: `08/03-02` (database), `08/03-03` (payment gateway), and `08/03-04` (double charge).

The guiding rule: **an alert names the impact and a runbook; the on-call responder confirms impact,
declares an incident when it is real, and runs the procedure rather than improvising under load.**

---

## Roles

| Role | Responsibility | Notes |
|---|---|---|
| **Incident Commander (IC)** | Owns the incident end to end: declares it, assigns roles, decides mitigation, declares resolution. | Always explicitly assigned; never left implicit. |
| **Operations Lead** | Executes the runbook and the technical mitigation; owns the fix. | May be the same person as the IC on a small rotation, but the IC decision is stated aloud. |
| **Communications Lead** | Owns the status page, customer and stakeholder updates, and the single message. | Filled for every Sev1 and for any customer-visible Sev2. |
| **Scribe** | Keeps the timestamped timeline and the decision log used by the postmortem. | Filled for every Sev1; optional for Sev2. |

On a one-person rotation the same engineer wears several hats, but the IC role and the timeline
still exist: the responder states "I am IC", opens a timeline, and hands off explicitly if the
incident outlasts the shift. No mitigation proceeds on an incident that has no owner.

---

## Severity Levels

Severities match the definitions owned by `08/02-03`; this table adds the response target and the
examples that make the choice unambiguous.

| Severity | Meaning | Response target | Examples (alert names in `08/02-03`) |
|---|---|---|---|
| **Sev1 — Page** | Customer-facing loss or severe impairment of a revenue path; data-integrity risk. | Acknowledge within 5 min, 24/7; IC assigned within 10 min. | `PublicAPIErrorBudgetFastBurn`, `CheckoutConfirmFailureRate`, `DatabaseReadinessFailing`, `GatewayCircuitOpen`, `DuplicateChargeSuspected`, `RpoBreach`. |
| **Sev2 — Urgent** | Significant degradation with partial impact or a workaround; a dependency close to failure; slow budget burn. | Acknowledge within 30 min in hours; out-of-hours only for revenue-path rules. | `ConnectionPoolSaturation`, `ReplicationLagHigh`, `PaymentSuccessRateDegraded`, `OutboxBacklogWarning`, `BackupFailed`. |
| **Sev3 — Ticket** | Warning or trend, not customer-visible; needs attention but no page. | Ticket to the owning team; address in the next working cycle. | `ApiSaturation`, `CacheHitRatioDrop`, `EdgeCertificateExpiry`. |

A Sev3 that worsens becomes Sev2; a Sev2 that reaches the public path or threatens data integrity
becomes Sev1. Severity is reassessed at every status update, and the highest current severity sets
the routing and the communication cadence.

---

## Declaring an Incident

1. **Acknowledge.** The paged responder acknowledges within the response target and opens the
   runbook id carried in the notification body (`08/02-03`).
2. **Confirm impact.** Check the owning dashboard in `08/02-02` and, where useful, the public
   path directly. An alert on a non-production signal never pages production (`08/01-02`).
3. **Declare if real.** If customer impact, data-integrity risk, or a breached error budget is
   confirmed, declare an incident: name it (`INC-YYYYMMDD-NN`), open the incident channel, assign
   the IC, and start the timeline.
4. **Select the runbook.** Use the runbook id from the alert row: `08/03-02` for database and
   order-state writes, `08/03-03` for payment gateways, `08/03-04` for a suspected duplicate
   charge, and this file for availability, latency, outbox, and capacity events.
5. **Mitigate first, diagnose second.** Restore the customer path before the root-cause write-up;
   the postmortem captures the cause after the incident is stable.
6. **Do not wait for certainty.** Declaring early and downgrading later is preferred to a late
   declaration; a false alarm is closed as a non-incident with a note, never as an unlogged page.

---

## Escalation

Escalation is time-boxed: the responder escalates when the target passes, not only when the
runbook is exhausted. Every escalation keeps the IC role filled.

| Level | Who | Trigger | Authority |
|---|---|---|---|
| L1 | Primary on-call | Alert pages. | Acknowledge, declare, run the named runbook. |
| L2 | Secondary / backup on-call | No acknowledgment within the target, or L1 needs a second pair of hands. | Take over the IC or the operations role. |
| L3 | Owning service engineer (database, payments, front-end, platform) | The runbook names a domain owner, or mitigation needs a code/config change. | Change configuration, disable a gateway method, approve a failover. |
| L4 | Engineering lead / incident manager | Sev1 not mitigated within 15 min, Sev2 within 30 min, or a multi-service impact. | Cross-team coordination, vendor engagement, change-freeze decision. |
| L5 | Cloud/database/payment provider support | The fault is provider-side or needs provider action (failover, quota, key rotation). | Provider-side remediation; the IC owns the comms. |

**Break-glass access.** Production database and secret access is role-controlled and audited; a
break-glass operator follows the restore and read paths in `08/03-02` and the access rules in
`08/01-02`. Break-glass use is logged and reviewed in the postmortem; it is never routine.

---

## Communication Cadence

The Communications Lead owns a single source of truth (the incident channel plus the status page)
so the team, customers, and stakeholders never receive two versions of the same incident.

| Severity | Internal update | Customer/status update | Audience |
|---|---|---|---|
| Sev1 | Every 15 min until mitigated, then hourly. | First notice within 30 min; update every 30 min until resolved. | On-call, stakeholders, affected customers. |
| Sev2 | Every 30–60 min while active. | Status page only when customer-visible; otherwise internal. | On-call, owning team. |
| Sev3 | At handover or ticket update. | None. | Owning team. |

Rules for every update:

- **State impact, not speculation.** Say what customers cannot do and what the workaround is;
  avoid unconfirmed cause claims.
- **No sensitive data.** Updates never contain PAN, CVV, tokens, full customer PII, or gateway
  payloads (`03/04-05`, NFR-SEC-007); reference payments by id or reference only.
- **One message.** The status page, the channel, and the handover note carry the same wording;
  the Scribe records what was said and when.
- **Resolution notice.** The final update states the mitigation, the residual risk, and the
  postmortem commitment.

---

## Runbook Index

| Trigger group | Runbook |
|---|---|
| Availability, latency, error budget, readiness, outbox, capacity, security spikes. | `08/03-01` (this file). |
| Database unavailable/slow/failover, order-state write errors, backup and PITR restore. | `08/03-02`. |
| Payment gateway outage/degradation, circuit open, reconciliation backlog. | `08/03-03`. |
| Suspected duplicate charge, reconciliation `duplicate_suspected`. | `08/03-04`. |

The mapping from each alert name to its runbook id is fixed in `08/02-03`; a rule whose runbook no
longer exists is a defect to fix in that file.

---

## Postmortem Template

Every Sev1 and Sev2 produces a blameless postmortem. Sev1 drafts are complete within five business
days and Sev2 within ten; action items are tracked to closure and reviewed with the on-call policy.

```text
# Postmortem — <incident title>

- Incident id / date / severity / duration / author:
- Runbook used: 08/03-0N
- Affected services, stores, and paths:

## Summary
Two or three sentences: what broke, for whom, and how it was mitigated.

## Impact
- Customer impact (browse, cart, checkout, account) and order/payment counts.
- Availability and error-budget consumption against NFR-AVAIL-003 (99.9% public / 99.5% admin).
- Data integrity (duplicate charges, lost or double-sent events) and reconciliation state.
- Financial exposure and refunds initiated.

## Detection
- Which alert fired, at what time, and how long before a human acknowledged.
- What the alert missed or over-reported; whether a dashboard panel was stale.

## Timeline (UTC)
- HH:MM — event, page, decision, mitigation, escalation.

## Root Cause and Contributing Factors
- The trigger, the latent conditions, and the guardrail that did not hold.
- Blameless: focus on systems, defaults, and signals, not individuals.

## What Went Well
- Signals, runbooks, and decisions that reduced impact.

## What Went Wrong
- Gaps in detection, procedure, tooling, or communication.

## Action Items
| Action | Type (prevent / detect / mitigate) | Owner | Due |
|---|---|---|---|
| ... | ... | ... | ... |

## Links
- Related alerts (`08/02-03`), dashboards (`08/02-02`), runbooks (`08/03-0N`), and changes.
```

A postmortem is complete only when its action items have owners and dates and the affected alerts,
dashboards, or runbooks are updated in the same review. An action item with no owner is reopened,
not closed.

---

## On-Call Rotation

- **Primary and secondary.** Every rotation names a primary and a secondary; the secondary backs
  up the primary and takes over at shift end or on escalation.
- **Handover.** A handover states open incidents, degraded dependencies, active maintenance
  windows, and any alert in a suppressed state (`08/02-03`).
- **Response targets.** Sev1 is acknowledged within 5 minutes, Sev2 within 30 minutes in hours;
  out-of-hours Sev2 paging is limited to revenue-path rules.
- **Suppression is not silence.** A maintenance window (`07/03`, `07/04`) suppresses the affected
  rules only for its duration and is closed and verified, never left on.
- **No silent acceptance.** A flapping or routinely ignored alert is tuned or deleted in
  `08/02-03`; it is not muted indefinitely.

---

## Open Questions

1. The concrete paging product, rotation tooling, and out-of-hours contact policy are deployment
   choices; this file fixes the roles, severities, targets, and cadence they must implement.
2. The exact L3/L4 owner names per service are recorded with the team roster; the escalation
   ladder and time boxes are fixed here.
3. The break-glass operator process and its audit retention are finalized with `08/01-02`; the
   audited, least-privilege rule is fixed here.
4. The postmortem action-item tracker is a tooling choice; the requirement that items have owners
   and dates and feed back into `08/02-02`/`08/02-03` is fixed here.
