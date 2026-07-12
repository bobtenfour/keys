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
| LOAN-RETURN-1 | Planned | CATALOG-1 | Created when approved |
| AUDIT-1 | Planned | LOAN-RETURN-1 | Created when approved |
| UTC-1 | Planned | AUDIT-1 | Created when approved |
| MIGRATION-1 | Planned | UTC-1 | Created when approved |
| CI-1 | Planned | MIGRATION-1 | Created when approved |
| PHASE-1-CLOSE | Planned | CI-1 | Created when approved |

## Later Phases
Later phase slices must be created only when their architectural contracts are mature enough to support implementation without assumptions.

## Depends On
- roadmap.md
- implementation-contract.md
- slice-promotion-governance.md

## Depended On By
- documentation/slices/*.md
