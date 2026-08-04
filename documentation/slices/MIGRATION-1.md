# MIGRATION-1 - Persistence Foundation

## Status
Accepted

## Parent Phase
Phase 1

## Purpose
Establish the minimum EF Core persistence foundation required for LOAN-VERTICAL-1 without implementing workflow commands, port adapters, UI, or unrelated entity schemas.

## Objective
Infrastructure contains one EF Core DbContext, entity mappings, and an initial migration for KeyType, KeyAsset, Loan, and Return using SQL Server as the sole persistence provider, while Domain remains the sole business authority.

## Scope
- EF Core package references required by Infrastructure persistence.
- One Infrastructure DbContext.
- Entity mappings for KeyType, KeyAsset, Loan, and Return only.
- Initial EF Core migration for those entities.
- Design-time DbContext factory required to create and apply the migration.
- Architecture tests protecting persistence ownership and layer boundaries.
- Tests verifying the migration model contains only the authorized entities.

## Out of Scope
- Application command handlers.
- Application port adapters.
- Repository facades beyond the DbContext.
- Runtime business DI registrations.
- Web UI.
- Razor Pages.
- Seed data.
- Demo pages.
- Identity persistence tables.
- AuditEvent persistence tables.
- KeySeries persistence tables.
- Lock persistence tables.
- Location persistence tables.
- Party aggregate.
- Authentication runtime.
- Authorization runtime.
- Automatic audit emission.
- Custody.
- Lifecycle.
- Inventory.
- CI pipeline changes.
- SQLite.
- Second persistence model or in-memory business store.
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
- project-erd-governance.md

## Required Previous Slices
- UTC-1

## Allowed Files
- documentation/slices/MIGRATION-1.md
- src/KeyInventory.Infrastructure/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/MIGRATION-1.md
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- src/KeyInventory.Web/**
- authentication files
- authorization runtime files
- UI files
- CI pipeline files

## Authority Owner
architecture-contracts.md

## Architectural Risks
- Letting persistence own business rules.
- Mapping entities beyond the LOAN-VERTICAL-1 minimum set.
- Introducing port adapters or UI before LOAN-VERTICAL-1.
- Introducing a second business persistence model.
- Converting or normalizing UTC timestamps in persistence mapping.
- Expanding into Identity or AuditEvent schema before authorized.

## Acceptance Criteria
- Infrastructure contains one EF Core DbContext.
- Initial migration maps KeyType, KeyAsset, Loan, and Return only.
- KeySeries, Lock, Location, Identity, and AuditEvent tables are not introduced.
- Persistence uses SQL Server only through `ConnectionStrings:KeyInventory`.
- Authoritative UTC timestamp properties map as DateTimeOffset without conversion.
- No Application port adapters, command handlers, business DI registrations, UI, seed data, or demo pages are introduced.
- Domain and Application projects are unchanged.
- Architecture tests protect persistence ownership and layer boundaries.
- Build PASS.
- Tests PASS.
- Repository hygiene PASS.

## Required Tests
- Architecture tests verify Infrastructure owns the DbContext and migration types.
- Architecture tests verify Domain and Application do not reference EF Core persistence types.
- Architecture tests verify Web does not introduce persistence authority types in this slice.
- Tests verify the EF model includes KeyType, KeyAsset, Loan, and Return entity mappings.
- Tests verify the EF model does not include KeySeries, Lock, Location, SecurityPrincipal, or AuditEvent entity mappings.

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
- Date: 2026-08-03.
- Evidence: UTC-1 is Accepted; MIGRATION-1 is the next Planned roadmap slice; existing architecture, domain, capability, integrity, and authority mappings were completed for the minimum KeyType, KeyAsset, Loan, and Return persistence foundation required before LOAN-VERTICAL-1.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-03.
- Evidence: MIGRATION-1 was Implementation Complete; EF Core SQLite foundation with one DbContext, KeyType/KeyAsset/Loan/Return mappings, InitialCreate migration, and design-time factory remained within approved scope; no Application port adapters, workflow DI, UI, seed data, Identity/AuditEvent schema, or second persistence model was introduced; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
