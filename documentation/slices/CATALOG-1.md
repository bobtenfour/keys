# CATALOG-1 - Key Catalog Foundation

## Status
Accepted

## Parent Phase
Phase 1

## Purpose
Establish the key catalog foundation as the authoritative source for controlled key identity and catalog reference data.

## Objective
The repository contains a minimal key catalog foundation for KeyAsset, KeySeries, KeyType, Lock, and Location concepts while preserving separate ownership for identity, custody, loans, lifecycle state, audit, and authorization.

## Scope
- KeyAsset catalog identity foundation.
- KeySeries foundation.
- KeyType foundation.
- Lock foundation.
- Location foundation.
- Catalog domain invariants required by the Domain Contract and ERD.
- Application ports directly required by the completed catalog domain contract: key asset lookup, key series lookup, key type lookup, lock lookup, and location lookup.
- Architecture tests protecting catalog ownership and layer boundaries.
- Unit tests verifying catalog invariants.

## Out of Scope
- Loan.
- Return.
- Custody Event.
- Audit Event.
- Lifecycle State.
- Lifecycle Event.
- EventStream.
- Event.
- KeyLifecycleProjection.
- KeyCustodyProjection.
- Inventory.
- Maintenance.
- Reporting.
- Policy Engine.
- Authentication.
- Authorization runtime.
- Identity or RBAC changes.
- Catalog command use cases.
- Party management.
- Persistence implementation.
- Repository implementation.
- Service implementation.
- Provider implementation.
- Dependency injection registration.
- Provider-specific infrastructure.
- Provider-specific configuration.
- UI.
- Sample data.
- Demo pages.
- Placeholders.
- TODO.
- FIXME.
- Commented code.

## Required Governing Contracts
- slice-promotion-governance.md
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- roadmap.md
- key-inventory-domain-contract.md
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- architecture-contracts.md
- security-capability-contract.md
- system-integrity-contract.md
- testing-strategy.md

## Required Previous Slices
- IDENTITY-1

## Allowed Files
- documentation/slices/CATALOG-1.md
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/CATALOG-1.md
- database/**
- migrations/**
- authentication files
- authorization runtime files
- identity or RBAC files
- party management files
- loan workflow files
- return workflow files
- custody files
- lifecycle state files
- audit workflow files
- reporting files
- policy engine files
- UI files

## Authority Owner
key-inventory-domain-contract.md

## Architectural Risks
- Letting catalog own current possession.
- Letting catalog own loan or return workflow state.
- Letting catalog own lifecycle state authority.
- Letting catalog own audit history.
- Letting catalog duplicate Party, Identity, RBAC, or Authorization authority.
- Introducing persistence or provider-specific infrastructure before it is explicitly authorized.
- Introducing UI behavior before product experience scope is approved.

## Acceptance Criteria
- KeyAsset foundation exists as authoritative catalog identity only.
- KeySeries, KeyType, Lock, and Location foundations exist within their authorized catalog or location scope.
- KeyAsset does not contain authoritative mutable lifecycle status.
- Catalog does not own current custodian, possession, loan, return, custody, audit, policy, authentication, authorization, Party, Identity, or RBAC authority.
- No UI, persistence implementation, provider-specific infrastructure, provider-specific configuration, sample data, or demo pages are introduced.
- No catalog command use cases, repository implementations, service implementations, provider implementations, or dependency injection registrations are introduced.
- Architecture tests protect catalog ownership and layer boundaries.
- Unit tests verify catalog invariants.
- Build PASS.
- Tests PASS.
- Repository hygiene PASS.

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

## Promotion Record
- Decision: Planned to Approved.
- Date: 2026-07-12.
- Evidence: IDENTITY-1 is Accepted; CATALOG-1 is the next non-terminal roadmap slice; no other slice is Approved or In Progress; required governing contracts are complete, consistent, and unambiguous for this slice specification.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-07-12.
- Evidence: CATALOG-1 implementation passed all required acceptance criteria; catalog authority remained limited to KeyAsset, KeySeries, KeyType, Lock, and Location foundations; no future-slice functionality, UI, persistence implementation, provider-specific infrastructure, repository implementation, service implementation, or dependency injection registration was introduced; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
