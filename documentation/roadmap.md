# Strategic Roadmap

## Authority
This document is the strategic product evolution authority.

## Purpose
Define product phases and business capability progression. It does not govern implementation sequence.

## Rules
This document may define:
- Product phases.
- Business objectives.
- Phase-level acceptance.

This document must not define:
- Implementation slices.
- File changes.
- Project structure.
- Prompts.
- Technical tasks.

## Phase 1 — Core Foundation
Business objective: establish the enterprise foundation for institutional structure, key catalog, loan/return workflow, immutable auditability, UTC timestamps, migration strategy, and CI readiness.

Acceptance:
- Complete loan/return cycle.
- Full audit trail.
- Zero orphan records.
- Business rules enforced by the owning authority.

## Phase 2 — Operational Security
Business objective: add identity and RBAC foundations, authorization workflows, time restrictions, alerts, notifications, dashboards, reports, and inventory verification.

Acceptance:
- Identity and RBAC responsibilities are established.
- High-risk keys require approval.
- Overdue alerts are generated.
- Operational dashboard is available.

## Phase 3 — Custody Chain
Business objective: every possession transfer becomes a permanent event.

Acceptance:
- Full chain of custody can be reconstructed.
- Current custodian is always derivable.

## Phase 4 — State Machine
Business objective: replace ambiguous mutable status with a formal key lifecycle state machine.

Acceptance:
- Transition matrix exists.
- Forbidden transitions are impossible.

## Phase 5 — Event Sourcing Readiness
Business objective: evolve operational history toward immutable event-based authority.

Acceptance:
- Core lifecycle events are defined.
- State is derivable from valid events.

## Phase 6 — Physical Inventory
Business objective: support periodic and surprise inventory sessions.

Acceptance:
- Inventory sessions, counts, discrepancies, and investigations are supported.

## Phase 7 — Maintenance Lifecycle
Business objective: support maintenance, rekey, duplicate creation, retirement, and destruction lifecycle.

Acceptance:
- Maintenance history is complete and auditable.

## Phase 8 — Unified Parties
Business objective: replace employee-only assumptions with a unified party model.

Acceptance:
- Employees, contractors, visitors, vendors, and external companies can be modeled without redesign.

## Phase 9 — Smart Cabinet Integration
Business objective: prepare architecture for electronic cabinets and storage devices.

Acceptance:
- Cabinet, slot, locker, and RFID integration concepts are supported without changing core custody authority.

## Phase 10 — Policy Engine
Business objective: replace hardcoded authorization rules with configurable policy authority.

Acceptance:
- Policies govern authorization decisions without workflow rewrites.

## Phase 11 — Advanced Authorization
Business objective: support dual approval, N-of-M approval, emergency override, escalation, and expiration.

Acceptance:
- Advanced authorization is policy-driven.

## Phase 12 — Digital Trust
Business objective: separate integrity proof from acceptance/authentication.

Acceptance:
- Integrity and authentication are distinct authorities.

## Phase 13 — Business Intelligence
Business objective: provide operational and analytical KPIs.

Acceptance:
- Operational and analytical metrics are available from authoritative sources.

## Phase 14 — Enterprise Readiness
Business objective: complete disaster recovery, backup validation, security testing, performance testing, monitoring, runbooks, and guides.

Acceptance:
- Enterprise operation is documented and validated.

## Depends On
- product-vision.md

## Depended On By
- implementation-roadmap.md
