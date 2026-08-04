# LOAN-VERTICAL-1 - First Runnable Workflow

## Status
Implementation Complete

## Parent Phase
Phase 1

## Purpose
Deliver the first complete end-to-end user workflow using existing Domain and Persistence foundations so a user can create a key asset, issue a loan, complete a return, and list open and returned loans.

## Objective
The application runs locally with SQLite persistence and Razor Pages that exercise real Domain Loan/Return and KeyAsset behavior through Application use cases and Infrastructure adapters, without authentication, audit emission, or future-phase capabilities.

## Scope
- Application commands for create Key Asset, issue Loan, and complete Return.
- Application queries for list Open Loans and list Returned Loans.
- Infrastructure persistence adapters for KeyType, KeyAsset, Loan, and Return against the existing DbContext.
- Dependency injection registration for DbContext, adapters, and Application use cases.
- Razor Pages for create key asset, issue loan, complete return, list open loans, and list returned loans.
- Simple navigation among those workflow pages.
- Architecture tests protecting layer boundaries for this vertical.
- Tests verifying the create key, issue loan, complete return, and list workflows.

## Out of Scope
- Authentication runtime.
- Authorization runtime.
- Automatic audit emission.
- Party aggregate.
- Custody.
- Lifecycle.
- Inventory.
- Reporting.
- Dashboards.
- Notifications.
- Advanced search.
- Identity schema expansion.
- AuditEvent persistence.
- KeySeries, Lock, or Location persistence.
- Seed data.
- Demo pages.
- In-memory fake persistence.
- Mock workflows.
- Second persistence model.
- Speculative abstractions.
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
- product-experience-contract.md
- system-integrity-contract.md
- testing-strategy.md

## Required Previous Slices
- MIGRATION-1

## Allowed Files
- documentation/slices/LOAN-VERTICAL-1.md
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/LOAN-VERTICAL-1.md
- src/KeyInventory.Domain/**
- authentication files
- authorization runtime files
- CI pipeline files
- custody files
- lifecycle files
- inventory files
- reporting files
- dashboard files

## Authority Owner
key-inventory-domain-contract.md

## Architectural Risks
- Placing business rules in Razor Pages.
- Introducing fake or duplicate persistence.
- Expanding into authentication, authorization, or audit emission.
- Creating a Party aggregate instead of opaque Party references.
- Mapping KeySeries, Lock, Location, Identity, or AuditEvent before authorized.
- Building dashboards or speculative abstractions beyond the five workflow actions.

## Acceptance Criteria
- A user can create a Key Asset with catalog key code and key type code through the UI.
- Create Key Asset creates a missing KeyType when needed, then creates the KeyAsset.
- A user can issue a Loan against an existing Key Asset with borrower Party reference and UTC issue/due timestamps.
- A user can complete a Return for an Open Loan with UTC return timestamp.
- A user can list Open Loans and list Returned Loans through the UI.
- Persistence adapters use the existing EF Core SQLite DbContext and MIGRATION-1 entity set only.
- Dependency injection registers DbContext, adapters, and Application use cases required by this slice.
- UI uses product language and contains no business logic beyond presentation and invocation of Application use cases.
- No authentication, authorization runtime, automatic audit emission, Party aggregate, seed data, demo pages, or second persistence model is introduced.
- Domain project remains unchanged.
- Architecture tests protect Application, Infrastructure, and Web boundaries for this slice.
- Build PASS.
- Tests PASS.
- Repository hygiene PASS.

## Required Tests
- Tests verify create Key Asset succeeds for a new type code and catalog key code.
- Tests verify issue Loan succeeds for an existing Key Asset and rejects invalid UTC timestamps.
- Tests verify complete Return succeeds for an Open Loan and rejects a non-open Loan.
- Tests verify list Open Loans and list Returned Loans return the expected workflow results.
- Architecture tests verify Web does not reference Domain aggregates for business decisions.
- Architecture tests verify Infrastructure adapters do not own Domain invariant logic beyond mapping and persistence.
- Architecture tests verify authentication, authorization runtime, and audit emission types are not introduced by this slice.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Authority consistency PASS
- ERD consistency PASS
- Capability consistency PASS
- Product experience consistency PASS
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
- Evidence: MIGRATION-1 is Accepted; LOAN-VERTICAL-1 is the next Planned roadmap slice; architecture, domain ownership, product experience, capability, and authority mappings were completed for the first runnable create-key / issue-loan / complete-return / list-loans workflow.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
