# WORKFORCE-ELIGIBILITY-1 - Workforce Key Eligibility Foundation

## Status
Accepted

## Parent Phase
Phase 2 — Operational Security

## Purpose
Implement the governed Workforce Eligibility runtime so Organization, Department, Building, Room, Party, WorkforceMember, WorkAssignment, key-issue eligibility evaluation, and termination return-obligation signaling operate through Domain, Application, SQL Server persistence, and task-oriented Razor Pages without inventing borrower aggregates, temporary borrower fields, or duplicate identity authority.

## Objective
The application persists and administers Party, Organization, Department, Building, Room, WorkforceMember, and WorkAssignment under existing authority boundaries; evaluates governed key-issue eligibility; terminates WorkforceMember relationships by blocking future issues and exposing mandatory outstanding return obligations without mutating Loan, Return, Audit, Custody, or Lifecycle; and integrates eligibility into Issue Key. Authentication redesign, HR integration, automatic audit emission, custody/lifecycle mutation, and unrelated UI redesign remain absent.

## Scope
- Domain entities, invariants, uniqueness rules, eligibility evaluation, and termination return-obligation signaling for:
  - Party (FirstName, LastName, unique nine-digit UIN)
  - Organization
  - Department
  - Building
  - Room (Building, RoomNumber unique within Building, Description)
  - WorkforceMember (WorkforceType Employee or Contractor, Organization, Department, ResponsibleManager, Status Active or Terminated, Party reference)
  - WorkAssignment (multiple Room assignments; at most one active primary assignment)
- Application commands, queries, and ports required for administration and key-issue eligibility evaluation.
- Integration of governed eligibility into the existing Issue Key / issue Loan Application use case so issue is allowed only for an eligible Active WorkforceMember and the Loan borrower Party is that member's Party.
- Application exposure of mandatory outstanding return obligations for a Terminated WorkforceMember without mutating Loan, Return, Audit, Custody, or Lifecycle.
- SQL Server EF Core mappings, migration, and Infrastructure persistence adapters for the workforce/location/party entities in this slice against `ConnectionStrings:KeyInventory`.
- Dependency injection registration for DbContext extensions, adapters, and Application use cases required by this slice.
- Task-oriented Razor Pages, reusing existing UX patterns, for:
  - Organizations
  - Departments
  - Buildings
  - Rooms
  - Workforce Members
  - Work Assignments
- Architecture, domain, persistence, workflow, and UI-boundary tests required by this slice.

## Out of Scope
- Authentication redesign.
- Authorization runtime.
- HR integration.
- Automatic audit emission.
- Custody lifecycle mutation.
- Lifecycle state mutation.
- Automatic offboarding implementation.
- Automatic return, cancel, or rewrite of Loan on termination.
- Automatic fabrication of Return records on termination.
- Automatic mutation of Audit, Custody, or Lifecycle on termination.
- Borrower aggregate.
- Temporary borrower fields.
- Duplicate Party identity authority inside WorkforceMember.
- Separate Employment aggregate.
- Unrelated UI redesign or new visual system.
- Dashboards, reporting, notifications, advanced search.
- Seed data or demo pages.
- In-memory fake persistence, SQLite, or second persistence model.
- Speculative abstractions.
- Placeholders.
- TODO.
- FIXME.
- Commented-out code.
- Git operations unless explicitly requested by the human repository owner.
- Roadmap reorder.
- Preparation or implementation of any other slice.

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
- product-experience-contract.md
- testing-strategy.md
- slice-promotion-governance.md

## Required Previous Slices
- PHASE-1-CLOSE

## Allowed Files
- documentation/slices/WORKFORCE-ELIGIBILITY-1.md
- documentation/implementation-roadmap.md
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/WORKFORCE-ELIGIBILITY-1.md and documentation/implementation-roadmap.md
- authentication redesign files
- authorization runtime files
- HR integration files
- custody mutation files
- lifecycle mutation files
- automatic audit emission files
- CI pipeline files
- Docker Compose files
- Testcontainers configuration
- SQLite provider packages or configuration
- EF Core InMemory provider packages or configuration

## Authority Owner
Workforce Eligibility boundary, with Location boundary ownership of Building and Room place authority, and Party boundary ownership of FirstName, LastName, and UIN.

## Architectural Risks
- Creating a Borrower aggregate or temporary borrower fields.
- Duplicating Party FirstName, LastName, or UIN inside WorkforceMember.
- Letting Workforce Eligibility own Location hierarchy or RoomNumber uniqueness.
- Auto-mutating Loan, Return, Audit, Custody, or Lifecycle on termination.
- Placing eligibility or uniqueness business rules in Razor Pages or Infrastructure adapters.
- Introducing authentication redesign, HR integration, automatic audit emission, or unrelated UI redesign.
- Weakening SQL Server-only persistence testing or introducing a second persistence provider.
- Expanding beyond the minimum administration and Issue Key eligibility integration authorized by governing contracts.

## Acceptance Criteria
- Domain implements Party with FirstName, LastName, and UIN that is exactly nine numeric digits and unique.
- Domain implements Organization, Department, Building, Room, WorkforceMember, and WorkAssignment with governed invariants and uniqueness rules, including RoomNumber unique within one Building and Department code unique within one Organization.
- WorkforceMember references Party and owns WorkforceType (Employee or Contractor), Organization, Department, ResponsibleManager, and Status (Active or Terminated) without owning person-identity attributes.
- WorkAssignment supports multiple Room assignments for one WorkforceMember and at most one active primary assignment.
- Eligibility evaluation allows key issue only when Status is Active, Department is assigned, ResponsibleManager is assigned, at least one active WorkAssignment exists, referenced Party has valid FirstName/LastName/UIN, and the requested key is justified by an authorized Department or assigned Room.
- Issue Key / issue Loan Application path enforces that eligibility evaluation and uses the eligible WorkforceMember's Party as the Loan borrower Party reference.
- Termination blocks future key issues and exposes mandatory outstanding return obligations for Open Loans of that Party without automatically mutating Loan, Return, Audit, Custody, or Lifecycle.
- SQL Server EF mappings, migration, and adapters persist the entities in this slice through `ConnectionStrings:KeyInventory` without a second persistence model.
- Dependency injection registers the adapters and use cases required by this slice.
- Razor Pages exist for Organizations, Departments, Buildings, Rooms, Workforce Members, and Work Assignments, reuse existing UX patterns, use product language, and contain no business logic beyond presentation and Application invocation.
- Architecture, domain, persistence, workflow, and UI-boundary tests required by this slice PASS.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Repository hygiene PASS.
- Human acceptance checkpoint is reached only after Implementation Complete evidence is recorded.

## Required Tests
- Domain tests verify Party UIN nine-digit and uniqueness rules, and FirstName/LastName requirements for workforce recipients.
- Domain tests verify Organization, Department, Building, Room, WorkforceMember, and WorkAssignment invariants, including RoomNumber uniqueness within Building and at most one active primary WorkAssignment.
- Domain or Application tests verify eligibility rejects inactive Status, missing Department, missing ResponsibleManager, missing active WorkAssignment, invalid Party identity, and unauthorized Department/Room justification.
- Domain or Application tests verify eligibility accepts a fully governed Active WorkforceMember with authorized Department or Room justification.
- Application or workflow tests verify Issue Key / issue Loan succeeds only for an eligible WorkforceMember and stores that member's Party as borrower.
- Application or workflow tests verify termination blocks new issues and exposes mandatory outstanding return obligations without mutating Loan, Return, Audit, Custody, or Lifecycle records.
- Persistence tests verify SQL Server mappings and migration for Party, Organization, Department, Building, Room, WorkforceMember, and WorkAssignment through `ConnectionStrings:KeyInventory`.
- Architecture tests verify layer boundaries, no Borrower aggregate, no Party identity duplication on WorkforceMember, and no authentication redesign / HR / automatic audit emission / custody or lifecycle mutation introduced by this slice.
- Architecture or UI-boundary tests verify Web does not own eligibility or uniqueness business decisions.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Authority consistency PASS
- ERD consistency PASS
- Capability consistency PASS
- Product experience consistency PASS
- System integrity consistency PASS
- Testing strategy consistency PASS
- Build PASS
- Tests PASS
- Repository hygiene PASS
- Documentation updated only if required
- No automatic Loan/Return/Audit/Custody/Lifecycle mutation introduced
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After WORKFORCE-ELIGIBILITY-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: prior governance preparation.
- Evidence: Governing contracts for Workforce Eligibility, Location Building/Room, Party identity, eligibility, and termination were completed; PHASE-1-CLOSE dependency established; slice remained documentation-only preparation until runtime repreparation.
- Deciding authority role: Human Architectural Governance.

## Runtime Repreparation Record
- Decision: Reprepare existing Approved slice for runtime implementation.
- Date: 2026-08-06.
- Evidence: PHASE-1-CLOSE is Accepted; WORKFORCE-ELIGIBILITY-1 is the next authorized executable slice; documentation-only restrictions replaced with explicit minimum runtime scope for Domain, Application, SQL Server persistence, DI, Razor Pages administration, Issue Key eligibility integration, and required tests; Status remains Approved; no src/** or tests/** implementation performed by this repreparation; roadmap order unchanged.
- Deciding authority role: Human Architectural Governance.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-06.
- Evidence: Domain Party, Organization, Department, Building, Room, WorkforceMember, WorkAssignment, KeyIssueEligibility, and termination implemented under Party/Location/Workforce Eligibility boundaries; Application administration and Issue Key eligibility integration use Party borrower reference without Borrower aggregate; SQL Server EF mappings, WorkforceEligibility migration, adapters, and DI registered; Razor Pages for Organizations, Departments, Buildings, Rooms, Workforce Members, and Work Assignments reuse existing UX; termination blocks issues and exposes outstanding return obligations without mutating Loan, Return, Audit, Custody, or Lifecycle; architecture, domain, persistence, workflow, and UI-boundary tests PASS; build PASS 0 warnings 0 errors; tests PASS 115/115.
- Deciding authority role: Implementation execution under approved slice specification.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-08.
- Evidence: WORKFORCE-ELIGIBILITY-1 was Implementation Complete; Party identity, Organization, Department, Building, Room, WorkforceMember, WorkAssignment, key-issue eligibility, Issue Key integration, termination return-obligation exposure, SQL Server persistence, administration Razor Pages, and required tests remained within approved scope; no Borrower aggregate, automatic Loan/Return/Audit/Custody/Lifecycle mutation, authentication redesign, or HR integration was introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
