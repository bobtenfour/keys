# Implementation Roadmap

## Authority
This document is the sole authority for implementation sequence.

## Purpose
Define the approved execution order without expanding into detailed slice specifications.

## Rules
- Only one slice may be In Progress.
- A slice may start only when all required previous slices are Accepted.
- Detailed scope lives in documentation/slices/*.md.
- This document does not replace slice specifications.
- Future phase slice files must be created only when the slice is approved.
- Future slices require a concrete operational need for this building and must not be justified solely by speculative scale or enterprise extensibility.
- Next Allowed Slice remains STOP until human governance selects the next concrete operational capability after the current Approved or In Progress slice completes acceptance.

## Status Values
- Planned
- Approved
- In Progress
- Implementation Complete
- Accepted
- Rework Required
- Rejected
- Closed
- Superseded

## Phase 1 — Core Foundation

| Slice | Status | Depends On | Slice Spec |
|---|---|---|---|
| SOLUTION-FOUNDATION-1 | Accepted | - | slices/SOLUTION-FOUNDATION-1.md |
| SOLUTION-FOUNDATION-2 | Accepted | SOLUTION-FOUNDATION-1 | slices/SOLUTION-FOUNDATION-2.md |
| IDENTITY-1 | Accepted | SOLUTION-FOUNDATION-2 | Created when opened |
| CATALOG-1 | Accepted | IDENTITY-1 | slices/CATALOG-1.md |
| LOAN-RETURN-1 | Accepted | CATALOG-1 | slices/LOAN-RETURN-1.md |
| AUDIT-1 | Accepted | LOAN-RETURN-1 | slices/AUDIT-1.md |
| UTC-1 | Accepted | AUDIT-1 | slices/UTC-1.md |
| MIGRATION-1 | Accepted | UTC-1 | slices/MIGRATION-1.md |
| LOAN-VERTICAL-1 | Accepted | MIGRATION-1 | slices/LOAN-VERTICAL-1.md |
| CI-1 | Accepted | LOAN-VERTICAL-1 | slices/CI-1.md |
| PHASE-1-CLOSE | Accepted | CI-1 | slices/PHASE-1-CLOSE.md |

## Phase 2 — Operational Security

| Slice | Status | Depends On | Slice Spec |
|---|---|---|---|
| WORKFORCE-ELIGIBILITY-1 | Accepted | PHASE-1-CLOSE | slices/WORKFORCE-ELIGIBILITY-1.md |
| KEY-LOOKUP-1 | Accepted | WORKFORCE-ELIGIBILITY-1 | slices/KEY-LOOKUP-1.md |
| REPORTS-1 | Accepted | KEY-LOOKUP-1 | slices/REPORTS-1.md |
| KEY-ROOM-ASSIGNMENT-1 | Accepted | REPORTS-1 | slices/KEY-ROOM-ASSIGNMENT-1.md |
| ADMIN-MAINTENANCE-1 | Accepted | KEY-ROOM-ASSIGNMENT-1 | slices/ADMIN-MAINTENANCE-1.md |
| REPORT-EXPORTS-1 | Accepted | ADMIN-MAINTENANCE-1 | slices/REPORT-EXPORTS-1.md |

## Later Phases
Later phase slices must be created only when human governance selects a concrete operational capability for this building and architectural contracts are mature enough to support implementation without assumptions.
REPORT-EXPORTS-1 is Accepted.
Next Allowed Slice remains STOP until human governance explicitly prepares the next concrete operational capability.
Do not prepare or invent slices for speculative enterprise scale, multi-campus design, policy engines, workflow engines, event platforms, or extensibility frameworks.

## Depends On
- roadmap.md
- implementation-contract.md
- slice-promotion-governance.md

## Depended On By
- documentation/slices/*.md
