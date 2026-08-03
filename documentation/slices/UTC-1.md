# UTC-1 - UTC Timestamp Foundation

## Status
Accepted

## Parent Phase
Phase 1

## Purpose
Establish authoritative UTC timestamp representation for Domain business timestamps without introducing clock infrastructure, persistence date/time mapping, local-time display, UI, or future workflow changes.

## Objective
The repository enforces that authoritative Domain business timestamps are UTC instants with zero offset, while preserving separate ownership for catalog, identity, loan/return, audit business rules, persistence, and product experience.

## Scope
- Domain UTC timestamp validation for authoritative business timestamps.
- Shared Domain UTC timestamp helper required by the completed UTC Timestamp Contract.
- Enforcement at existing Domain entry points that already accept authoritative UTC timestamps: Loan, Return, AuditEvent, and PrincipalRoleAssignment.
- Architecture tests protecting UTC timestamp authority and layer boundaries.
- Unit tests verifying UTC offset rejection and acceptance of UTC instants.

## Out of Scope
- System clock abstraction.
- Time provider ports.
- NodaTime or third-party date/time libraries.
- Local civil time models.
- Display time-zone conversion.
- UI date/time formatting.
- Persistence-provider date/time types.
- Database time-zone configuration.
- Migrations.
- CI pipeline changes.
- Authentication.
- Authorization runtime.
- Identity or RBAC feature changes beyond UTC validation on existing timestamp entry points.
- Party management.
- Key catalog business rule changes.
- Loan or return workflow rule changes beyond UTC validation.
- Audit business rule changes beyond UTC validation.
- Automatic audit emission.
- Custody Event.
- Lifecycle State.
- Lifecycle Event.
- Inventory.
- Maintenance.
- Reporting.
- Policy Engine.
- Digital Trust.
- Repository implementation.
- Service implementation.
- Provider implementation.
- Dependency injection registration.
- Sample data.
- Demo pages.
- Placeholders.
- TODO.
- FIXME.
- Commented code.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- roadmap.md
- architecture-contracts.md
- key-inventory-domain-contract.md
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- system-integrity-contract.md
- testing-strategy.md

## Required Previous Slices
- AUDIT-1

## Allowed Files
- documentation/slices/UTC-1.md
- src/KeyInventory.Domain/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/UTC-1.md
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- database/**
- migrations/**
- authentication files
- authorization runtime files
- party management files
- UI files
- CI pipeline files
- persistence mapping files

## Authority Owner
architecture-contracts.md

## Architectural Risks
- Allowing non-UTC offsets as authoritative Domain timestamps.
- Introducing a second local-time authority in Domain.
- Introducing speculative clock or time-provider infrastructure.
- Expanding into persistence date/time mapping or UI formatting.
- Changing loan, return, audit, or identity business rules beyond UTC validation.
- Coupling UTC enforcement to provider-specific libraries.

## Acceptance Criteria
- Domain provides shared UTC timestamp validation that requires `DateTimeOffset.Offset` equal to `TimeSpan.Zero`.
- Loan, Return, AuditEvent, and PrincipalRoleAssignment reject non-UTC authoritative timestamps.
- Loan, Return, AuditEvent, and PrincipalRoleAssignment accept UTC timestamps with zero offset.
- No local-time authoritative Domain model is introduced.
- No system clock abstraction, time provider port, NodaTime dependency, persistence mapping, UI, migration, or CI change is introduced.
- Architecture tests protect UTC timestamp authority and layer boundaries.
- Unit tests verify UTC invariants.
- Build PASS.
- Tests PASS.
- Repository hygiene PASS.

## Required Tests
- Unit tests verify shared Domain UTC validation accepts zero-offset timestamps and rejects non-zero offsets.
- Unit tests verify Loan rejects non-UTC IssuedAtUtc or DueAtUtc.
- Unit tests verify Return rejects non-UTC ReturnedAtUtc.
- Unit tests verify AuditEvent rejects non-UTC OccurredAtUtc.
- Unit tests verify PrincipalRoleAssignment rejects non-UTC EffectiveFromUtc or EffectiveToUtc.
- Architecture tests verify authoritative Domain timestamp public properties use UTC naming and `DateTimeOffset`.
- Architecture tests verify Infrastructure and Web do not introduce UTC business timestamp authority types for this slice.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Authority consistency PASS
- ERD consistency PASS
- Capability consistency PASS
- System integrity consistency PASS
- Build PASS
- Tests PASS
- Repository hygiene PASS

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-07-30.
- Evidence: AUDIT-1 is Accepted; UTC-1 is the next Planned roadmap slice; existing architecture, domain, ERD, capability, integrity, and authority mappings were completed for UTC timestamp ownership, invariants, acceptance criteria, and required tests.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-03.
- Evidence: UTC-1 was Implementation Complete; shared UtcTimestamp validation rejects non-UTC offsets and default(DateTimeOffset), returns validated values unchanged, and is enforced on Loan, Return, AuditEvent, and PrincipalRoleAssignment; no clock abstraction, NodaTime, persistence, DI, or UI was introduced; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
