# WORKFORCE-ELIGIBILITY-1 - Workforce Key Eligibility Foundation

## Status
Approved

## Parent Phase
Phase 2 — Operational Security

## Purpose
Establish governing domain, ERD, capability, authority, architecture, and integrity contracts for WorkforceMember key eligibility so later implementation can proceed without inventing borrower aggregates, temporary borrower fields, or duplicate identity authority.

## Objective
Governing documentation defines Organization, Department, Building, Room with RoomNumber unique within Building, WorkforceMember for WorkforceType Employee and Contractor, ResponsibleManager, WorkAssignment, key-issue eligibility, and termination return-obligation rules while preserving Party identity and existing Loan/Return/custody/lifecycle/audit authority. No runtime implementation is performed by this Approved preparation state.

## Scope
- Governing contract completion for Workforce Eligibility.
- Domain authority for Organization, Department, WorkforceMember, ResponsibleManager, WorkAssignment, and eligibility/termination rules.
- Location boundary authority for Building and Room, including RoomNumber uniqueness within Building.
- ERD logical entities and relationships for the workforce eligibility model.
- Business authority matrix, capability map, architecture boundary, and integrity rule alignment.
- Implementation-roadmap sequencing after PHASE-1-CLOSE.
- Approved slice specification only.

## Out of Scope
- Implementation in `src/**`.
- Changes in `tests/**`.
- UI.
- Persistence mapping or migrations.
- Authentication.
- Authorization runtime.
- HR integration.
- Audit emission.
- Custody lifecycle mutation.
- Loan workflow mutation.
- Return workflow mutation.
- Automatic offboarding implementation.
- Borrower aggregate.
- Temporary borrower fields.
- Duplicate Party identity authority.
- Automatic mutation of Loan, Return, custody, lifecycle, or audit authority on termination.
- Git operations.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- roadmap.md
- key-inventory-domain-contract.md
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- architecture-contracts.md
- system-integrity-contract.md
- slice-promotion-governance.md

## Required Previous Slices
- PHASE-1-CLOSE

## Allowed Files
- documentation/key-inventory-domain-contract.md
- documentation/key-inventory-erd.md
- documentation/business-authority-matrix.md
- documentation/key-inventory-capability-map.md
- documentation/architecture-contracts.md
- documentation/system-integrity-contract.md
- documentation/roadmap.md
- documentation/implementation-roadmap.md
- documentation/slices/WORKFORCE-ELIGIBILITY-1.md

## Forbidden Files
- src/**
- tests/**
- Any runtime implementation file outside the Allowed Files list

## Authority Owner
Workforce Eligibility boundary, with Location boundary ownership of Building and Room place authority, and Party boundary ownership of business identity.

## Architectural Risks
- Creating a Borrower aggregate or temporary borrower fields.
- Duplicating Party identity inside WorkforceMember.
- Letting Workforce Eligibility own Location hierarchy or RoomNumber uniqueness.
- Auto-mutating Loan, Return, custody, lifecycle, or audit on termination.
- Implementing UI, persistence, authentication, HR, or offboarding automation in this slice's preparation state.
- Starting implementation before PHASE-1-CLOSE is Accepted.

## Acceptance Criteria
- Domain contract defines Organization, Department, Building, Room, RoomNumber uniqueness within Building, WorkforceMember, WorkforceType Employee and Contractor, ResponsibleManager, WorkAssignment, eligibility rules, and termination return-obligation rules.
- ERD lists and owns Organization, Department, Building, Room, WorkforceMember, and WorkAssignment under the correct boundaries.
- Business authority matrix assigns each workforce eligibility concern to exactly one authority document.
- Capability map includes Workforce Key Eligibility aligned to the WorkforceMember model.
- Architecture contracts define the Workforce Eligibility boundary and forbidden ownership.
- System integrity contract includes Party non-duplication, RoomNumber uniqueness, eligibility, and non-mutating termination rules.
- Implementation roadmap lists WORKFORCE-ELIGIBILITY-1 as Approved depending on PHASE-1-CLOSE.
- No `src/**` or `tests/**` changes are introduced by this Approved preparation.
- Implementation remains forbidden until the slice is authorized to start.

## Required Tests
- Documentation consistency verification against Required Governing Documents.
- No runtime tests are required while Status remains Approved without implementation start.

## Closure Contract
- Transversal Gate PASS
- Build PASS when implementation later starts
- Tests PASS when implementation later starts
- Repository hygiene PASS
- Documentation updated only if required
- No automatic Loan/Return/custody/lifecycle/audit mutation introduced

## Expected Build Result
Not applicable while Status is Approved without implementation start.

## Expected Test Result
Not applicable while Status is Approved without implementation start.

## Next Allowed Slice
STOP until PHASE-1-CLOSE is Accepted and WORKFORCE-ELIGIBILITY-1 is authorized to move to In Progress.
