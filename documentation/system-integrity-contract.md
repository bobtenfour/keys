# System Integrity Contract

## Authority
This document protects cross-system consistency.

## Purpose
Prevent hidden coupling, documentation drift, authority duplication, and operational defects across slices.

## Integrity Invariants
- Single source of truth.
- Single business authority.
- Explicit ownership.
- No hidden coupling.
- No business logic in UI.
- No orphan runtime concepts.
- No dead code.
- No untracked architectural decisions.
- Build and tests remain valid after every slice.
- Authoritative business timestamps remain UTC across Domain boundaries; UI and local civil time must not become a second authoritative time model.
- Physical persistence has one authorized model for mapped entities; MIGRATION-1 and LOAN-VERTICAL-1 must not introduce a second business store beside the EF Core foundation.
- Party is the sole persistent person-identity authority and owns FirstName, LastName, and UIN for human workforce recipients; UIN is exactly nine numeric digits and unique on Party.
- WorkforceMember is the sole workforce relationship and eligibility authority and must not own FirstName, LastName, UIN, or other Party person-identity attributes.
- Employment is not a separate aggregate and must not duplicate WorkforceMember relationship authority.
- RoomNumber is required and unique within one Building; Room place authority remains inside the Location boundary.
- Key issue eligibility for a WorkforceMember requires Active status, Party person identity with valid UIN, Department, ResponsibleManager, and at least one active Room WorkAssignment relevant to the key being issued.
- Keys may be issued only for the Department or Room where the WorkforceMember is authorized to work.
- WorkforceMember termination, rehire, Department change, Organization change, and Employee or Contractor WorkforceType transition must not rewrite Party person identity.
- WorkforceMember termination for Employee or Contractor forbids new key issues and creates a mandatory return obligation for currently issued keys without automatically mutating Loan, Return, custody, lifecycle, or audit authority; returns use the existing Return workflow.

## Required Checks
- UI / service / domain / infrastructure consistency.
- Hidden operational dependencies.
- Future roadmap impact.
- Semantically invalid user actions.
- Duplicate sources of truth.
- Documentation consistency.
- Repository hygiene.

## Depends On
- project-governance.md
- architecture-contracts.md
- business-authority-matrix.md

## Depended On By
- implementation-contract.md
- slices
