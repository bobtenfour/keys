# LOAN-RETURN-1 - Loan and Return Foundation

## Status
Implementation Complete

## Parent Phase
Phase 1

## Purpose
Establish the loan and return workflow foundation without introducing custody authority, lifecycle authority, audit workflows, persistence, provider infrastructure, or UI.

## Objective
The repository contains a minimal Loan and Return foundation for controlled key issuance and completion workflow while preserving separate ownership for key catalog identity, Party identity, custody, lifecycle state, audit, authentication, authorization, and policy.

## Scope
- Loan aggregate foundation.
- Return aggregate foundation.
- Loan workflow state required by the Domain Contract and ERD.
- Return completion invariants required by the Domain Contract and ERD.
- Application ports directly required by the completed loan/return domain contract: loan lookup and return lookup.
- Architecture tests protecting loan/return ownership and layer boundaries.
- Unit tests verifying loan and return invariants.

## Out of Scope
- Custody Event.
- Current possession.
- Current custodian.
- Custody transfer history.
- Audit Event.
- Audit workflows.
- Lifecycle State.
- Lifecycle Event.
- Lifecycle transition authority.
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
- Party management.
- Key catalog changes.
- Loan command handlers.
- Return command handlers.
- Repository implementation.
- Service implementation.
- Provider implementation.
- Dependency injection registration.
- Persistence implementation.
- Provider-specific infrastructure.
- Provider-specific configuration.
- UI.
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
- key-inventory-domain-contract.md
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- architecture-contracts.md
- security-capability-contract.md
- system-integrity-contract.md
- testing-strategy.md

## Required Previous Slices
- CATALOG-1

## Allowed Files
- documentation/slices/LOAN-RETURN-1.md
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/LOAN-RETURN-1.md
- database/**
- migrations/**
- authentication files
- authorization runtime files
- identity or RBAC files
- party management files
- key catalog changes
- custody files
- lifecycle state files
- audit workflow files
- reporting files
- policy engine files
- UI files

## Authority Owner
key-inventory-domain-contract.md

## Architectural Risks
- Letting Loan or Return own current possession.
- Letting Loan or Return own custody transfer authority.
- Letting Loan or Return own lifecycle state authority.
- Letting Loan or Return own audit history.
- Letting Loan or Return duplicate Key Catalog, Party, Identity, RBAC, Authorization, or Policy authority.
- Introducing persistence or provider-specific infrastructure before it is explicitly authorized.
- Introducing UI behavior before product experience scope is approved.

## Acceptance Criteria
- Loan foundation exists as authoritative loan issuance intent and completion workflow only.
- Return foundation exists as authoritative return completion workflow only.
- Loan references catalog key identity and borrower Party without owning either authority.
- Return references one open Loan and marks that Loan returned.
- Loan and Return do not own current possession, current custodian, custody transfer history, lifecycle state, audit history, authentication, authorization, Party, Key Catalog, Identity, RBAC, or Policy authority.
- No UI, persistence implementation, provider-specific infrastructure, provider-specific configuration, sample data, or demo pages are introduced.
- No loan/return command handlers, repository implementations, service implementations, provider implementations, or dependency injection registrations are introduced.
- Architecture tests protect loan/return ownership and layer boundaries.
- Unit tests verify loan and return invariants.
- Build PASS.
- Tests PASS.
- Repository hygiene PASS.

## Required Tests
- Unit tests verify Loan requires loan code, key reference, borrower Party reference, issue timestamp, and due timestamp later than issue timestamp.
- Unit tests verify Loan starts Open, can be cancelled only while Open, and cannot be returned after cancellation.
- Unit tests verify Return requires return code, an Open Loan, and a return timestamp not earlier than the Loan issue timestamp.
- Unit tests verify exactly one Return may complete a Loan.
- Architecture tests verify Loan and Return do not expose custody, lifecycle, audit, authentication, authorization, policy, Party profile, or catalog authority state.
- Architecture tests verify Application defines lookup ports only for Loan and Return.
- Architecture tests verify Infrastructure, Web, persistence, provider implementation, service implementation, repository implementation, and dependency injection registration are not introduced for loan/return.

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
- Date: 2026-07-12.
- Evidence: CATALOG-1 is Accepted; LOAN-RETURN-1 is the next Planned roadmap slice; existing domain, ERD, and authority mappings were completed for loan/return ownership, invariants, relationships, acceptance criteria, and required tests.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
