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
