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
- Product scope remains one building and a small operational workforce; future work requires a concrete operational need and must not be justified solely by speculative scale or enterprise extensibility.
- Speculative policy engines, generalized authorization engines, workflow engines, event platforms, Campus hierarchies, and multi-tenant/large-scale platform abstractions must not enter the system without an explicit later business requirement.
- Authoritative business timestamps remain UTC across Domain boundaries; UI and local civil time must not become a second authoritative time model.
- Physical persistence has one authorized model for mapped entities; MIGRATION-1 and LOAN-VERTICAL-1 must not introduce a second business store beside the EF Core foundation.
- Party is the sole persistent person-identity authority and owns FirstName, LastName, and UIN for human workforce recipients; UIN is exactly nine numeric digits and unique on Party.
- WorkforceMember is the sole workforce relationship and eligibility authority and must not own FirstName, LastName, UIN, or other Party person-identity attributes.
- Employment is not a separate aggregate and must not duplicate WorkforceMember relationship authority.
- RoomNumber is required and unique across all Room records; Room place authority remains inside the Location boundary; Building is not an active place authority.
- KEY # / KeyAccessPattern is the sole shared access-pattern identity; physical KeyAsset copies belong to exactly one KeyAccessPattern; MEDECO Key Code is unique within KEY # and is not globally unique.
- Current KeyAccessPattern-to-Room opening assignments are owned by Key Catalog; Location owns Room identity; KeyAsset must not independently own Building, site abstractions, or Room openings; physical copies derive Rooms opened only through parent KEY #.
- KeyAccessPattern↔Room is the sole operational authority for which Rooms a KEY # (and therefore every physical copy under it) opens; Lock must not mediate or duplicate that authority; KeyType must not own Room assignments; KeySeries must not own KEY # or Room access; assignment history is not required; master/sub-master hierarchy is forbidden (a master key is only a KEY # with multiple Rooms).
- CatalogKeyCode must not remain unique physical-copy business identity; opaque composite KEY#+MEDECO strings must not be identity authority; KeyAssetId is the immutable internal physical-copy identity.
- Custody (Issue/Return/open Loan) remains on the physical KeyAsset; at most one open Loan per physical copy; different copies under the same KEY # may be issued simultaneously.
- Key issue eligibility for a WorkforceMember requires Active status, Party person identity with valid UIN, Department, and at least one active Room WorkAssignment; Organization and ResponsibleManager are not eligibility requirements.
- Keys may be issued only for the Department or Room where the WorkforceMember is authorized to work.
- WorkforceMember termination, rehire, Department change, and Employee or Contractor WorkforceType transition must not rewrite Party person identity.
- Organization, Building, and ResponsibleManager must not remain active business authorities after OPERATOR-EXPERIENCE-1; historical OperatorAuditRecord rows remain immutable.
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
