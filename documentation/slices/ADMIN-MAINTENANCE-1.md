# ADMIN-MAINTENANCE-1 - Administrative Record Maintenance

## Status
Accepted

## Parent Phase
Phase 2 — Operational Security

## Purpose
Allow the key custodian of one approximately five-floor building to maintain existing administrative records through task-oriented Application and Web paths—activation/retirement where already owned, WorkforceMember relationship reassignment where already owned, and WorkAssignment end/primary maintenance where already owned—without database intervention, hard delete, generic CRUD frameworks, or invented history/archive models.

## Objective
Operators can activate and retire Organization, Department, Building, Room, and KeyType through Application commands persisted on existing SQL Server tables; can change Organization/Department, ResponsibleManager, and WorkforceType on an Active WorkforceMember without rewriting Party identity; can end WorkAssignments and change primary designation under existing primary uniqueness; and continue using existing WorkforceMember termination unchanged. Existing Loan/Return history and references remain valid. Hard delete, Room descriptive/number edits, identity-code renames, and reactivation of Terminated WorkforceMember records are not introduced.

## Scope
### Entities and exact operations in scope
- **Organization** (Workforce Eligibility): Activate; Retire. Identity `OrganizationCode` is not editable.
- **Department** (Workforce Eligibility): Activate only when the owning Organization is active (domain `Activate(Organization)`); Retire. Identity `(OrganizationCode, DepartmentCode)` is not editable; Department is not moved to another Organization.
- **Building** (Location): Activate; Retire. Identity `BuildingCode` is not editable.
- **Room** (Location): Activate only when the owning Building is active (domain `Activate(Building)`); Retire. Identity `RoomCode` is not editable; Building reference is not reassigned; RoomNumber is not changed; Description is not edited in this slice.
- **KeyType** (Key Catalog): Activate; Retire only when no active KeyAsset records require it for new catalog assignment (domain `Retire(hasActiveKeyAssets)`). Identity `TypeCode` is not editable.
- **WorkforceMember** (Workforce Eligibility): For Status Active only — change Organization and Department together (`AssignOrganizationAndDepartment`); change ResponsibleManager (`AssignResponsibleManager`); change WorkforceType Employee or Contractor (`ChangeWorkforceType`). Existing Terminate remains available and must not be redesigned. Reactivation of Terminated WorkforceMember is forbidden; rehire remains creation of a new Active WorkforceMember for the same Party through the existing create path.
- **WorkAssignment** (Workforce Eligibility): End an active assignment (`End`); MarkPrimary / ClearPrimary subject to at most one active primary WorkAssignment per WorkforceMember. Room on an existing WorkAssignment is not reassigned; room change is end then create via existing create path.

### Editable / maintained fields (exact)
| Entity | Maintained fields / state | Not editable in this slice |
|---|---|---|
| Organization | `IsActive` via Activate/Retire | `OrganizationCode` |
| Department | `IsActive` via Activate/Retire | `OrganizationCode`, `DepartmentCode` |
| Building | `IsActive` via Activate/Retire | `BuildingCode` |
| Room | `IsActive` via Activate/Retire | `RoomCode`, `BuildingCode`, `RoomNumber`, `Description` |
| KeyType | `IsActive` via Activate/Retire | `TypeCode` |
| WorkforceMember | `OrganizationCode`+`DepartmentCode`, `ResponsibleManagerWorkforceMemberCode`, `WorkforceType`; Status → Terminated via existing Terminate | `WorkforceMemberCode`, Party reference, Party FirstName/LastName/UIN; Terminated → Active |
| WorkAssignment | `IsActive` via End; `IsPrimary` via MarkPrimary/ClearPrimary | `WorkAssignmentCode`, `WorkforceMemberCode`, `RoomCode` |

### Activation / deactivation rules (exact)
- Organization: Activate sets active; Retire sets inactive. Only an active Organization may own active Departments or active WorkforceMember membership (existing invariant for subsequent use).
- Department: Activate requires the owning Organization and that Organization is active; Retire sets inactive. Only an active Department in an active Organization may be used for active membership or Department-based issue justification (existing invariant).
- Building: Activate sets active; Retire sets inactive. Only an active Building may contain active Rooms used for WorkAssignment or Room-based issue justification (existing invariant).
- Room: Activate requires the owning Building and that Building is active; Retire sets inactive. Only an active Room in an active Building may be used for active WorkAssignment, Room-based issue justification, or active Key-to-Room assignment (existing invariant).
- KeyType: Activate sets active; Retire is rejected while active KeyAsset records require it for new catalog assignment; retiring KeyType does not retire existing KeyAsset records.
- WorkAssignment: End sets inactive and clears primary; does not delete the row.
- No cascade hard-delete or invented cascade retire of dependent rows is authorized by this slice; eligibility and assignment rules continue to enforce active references at use time.

### Application
- Commands/queries only for the operations listed above (activate/retire, WorkforceMember relationship updates, WorkAssignment end/primary).
- Reuse existing list/read ports and Terminate WorkforceMember use case; do not duplicate termination semantics.
- Web consumes Application authorities only; no DbContext in Web.

### Infrastructure
- Persist state changes through existing SQL Server `ConnectionStrings:KeyInventory` and `KeyInventoryDbContext` entity tables already mapped for these concepts.
- Migration only if an existing schema gap blocks persisting already-owned fields (none expected for IsActive / relationship fields already present).
- No second persistence provider, reporting store, archive store, or soft-delete history table.

### Web
- Practical task-oriented paths on existing Administration (and Catalog Key Types where KeyType is surfaced) to perform the authorized operations on existing records.
- Explicit usable feedback for rejected retire/activate/relationship changes.
- Reuse existing KeyInventory visual language; no unrelated redesign.

## Out of Scope
- Generic CRUD framework, generic repository, or generalized administration engine.
- Hard delete / physical deletion of Organization, Department, Building, Room, KeyType, WorkforceMember, WorkAssignment, or any other record.
- Archive, version, or assignment/admin history models.
- Editing identity codes (`OrganizationCode`, `DepartmentCode`, `BuildingCode`, `RoomCode`, `TypeCode`, `WorkforceMemberCode`, `WorkAssignmentCode`).
- Editing Room `Description` or `RoomNumber` (business decision missing; see Preparation Record).
- Reassigning Room to another Building.
- Reassigning Department to another Organization.
- Changing `RoomCode` on an existing WorkAssignment.
- Reactivating a Terminated WorkforceMember.
- Redesigning or replacing existing Terminate WorkforceMember behavior.
- Party FirstName/LastName/UIN maintenance (Party boundary; not in this slice's entity list).
- KeyAsset catalog descriptive updates, KeyAsset activate/retire, or Key-to-Room assignment changes (already owned elsewhere; not this slice's entity list).
- New business entities.
- Enterprise multi-organization / multi-campus administration.
- Authorization-policy expansion or authentication redesign.
- REPORTS-2 or new report families.
- Issue/receive mutation changes beyond consuming already-active administrative state.
- Automatic audit emission.
- Unrelated UI redesign.
- Speculative frameworks, placeholders, TODO, FIXME, or commented-out code.
- Git operations unless explicitly requested by the human repository owner.
- Preparation or implementation of any other slice.

## Persistence Requirements
- All maintenance mutations persist through the existing KeyInventory SQL Server connection string authority.
- Existing identity keys and historical Loan/Return rows that reference Party/Key codes must remain valid; maintenance must not rewrite Loan/Return history.
- Do not introduce hard-delete cascades.
- Do not introduce a second store for administrative versions or archives.

## UI Requirements
- Operator can activate/retire Organization, Department, Building, Room, and KeyType for existing records from practical Administration/Catalog paths.
- Operator can update Organization/Department, ResponsibleManager, and WorkforceType for an Active WorkforceMember.
- Operator can end WorkAssignments and maintain primary designation under existing uniqueness.
- Existing Terminate WorkforceMember path remains usable and unchanged in meaning.
- Empty/error states for blocked retire (for example KeyType still required by active keys) are explicit.
- Web uses Application commands/DTOs only.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- roadmap.md
- product-vision.md
- key-inventory-domain-contract.md
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- architecture-contracts.md
- system-integrity-contract.md
- product-experience-contract.md
- testing-strategy.md
- slice-promotion-governance.md
- documentation/slices/WORKFORCE-ELIGIBILITY-1.md
- documentation/slices/KEY-ROOM-ASSIGNMENT-1.md

## Required Previous Slices
- KEY-ROOM-ASSIGNMENT-1

## Allowed Files
- documentation/slices/ADMIN-MAINTENANCE-1.md
- documentation/implementation-roadmap.md
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/ADMIN-MAINTENANCE-1.md and documentation/implementation-roadmap.md
- Accepted slice history files other than roadmap status line for this slice
- Generic CRUD / repository framework packages and files
- Hard-delete cascade schema packages invented for this slice
- Archive/history/versioning packages
- REPORTS-2 slice files or new report-family feature folders
- Authorization-policy engine packages and files
- CI pipeline files
- Docker Compose files
- Testcontainers configuration
- SQLite provider packages or configuration
- EF Core InMemory provider packages or configuration

## Authority Owner
Existing boundary owners retain authority: Workforce Eligibility owns Organization, Department, WorkforceMember, and WorkAssignment maintenance; Location owns Building and Room activation/retirement; Key Catalog owns KeyType activation/retirement. No new ownership boundary is created.

## Architectural Risks
- Inventing hard-delete or cascade-delete semantics not present in governing contracts.
- Inventing Room Description/RoomNumber edit or identity-code rename without explicit business authority.
- Building a generic CRUD administration framework.
- Reactivating Terminated WorkforceMember contrary to rehire-as-new-member rule.
- Mutating Party identity during WorkforceMember relationship changes.
- Auto-mutating Loan/Return on administrative retire or termination.
- Expanding into REPORTS-2, enterprise admin, or authorization-policy engines.
- Putting DbContext access in Web.

## Acceptance Criteria
- Organization, Department, Building, Room, and KeyType can be activated and retired through Application-backed UI for existing records.
- Department and Room activation enforce owning Organization/Building active rules.
- KeyType retirement is rejected while active KeyAssets require the type; retirement does not retire KeyAssets.
- Active WorkforceMember Organization/Department, ResponsibleManager, and WorkforceType can be updated without rewriting Party identity.
- Existing Terminate WorkforceMember behavior remains unchanged in meaning and continues to forbid new issues and expose return obligations without auto-mutating Loan/Return.
- WorkAssignment can be ended; primary designation can be changed with at most one active primary per WorkforceMember.
- No hard-delete UI or persistence path is introduced for in-scope entities.
- Identity codes, RoomNumber, Room Description, and WorkAssignment Room are not editable through this slice.
- Existing Loan/Return historical rows remain valid.
- Web consumes Application authorities only.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Repository hygiene PASS.
- Human acceptance checkpoint is reached only after Implementation Complete evidence is recorded.

## Required Tests
- Domain or Application tests for Organization/Department/Building/Room/KeyType Activate and Retire rules, including Department/Room owning-parent active checks and KeyType retire-with-active-KeyAsset rejection.
- Application workflow tests for WorkforceMember Organization/Department, ResponsibleManager, and WorkforceType changes on Active members only; Terminated member relationship change rejection.
- Regression tests that existing Terminate WorkforceMember behavior remains valid and does not mutate Loan/Return.
- Application workflow tests for WorkAssignment End and primary change with at most-one-primary invariant.
- SQL Server persistence tests that IsActive and relationship field updates persist through `ConnectionStrings:KeyInventory` without deleting rows.
- Architecture boundary tests verify no generic CRUD framework, hard-delete path, archive/history model, REPORTS-2, or second persistence provider is introduced.
- UI-boundary tests verify Administration/Catalog maintenance pages consume Application use cases only (no DbContext).

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Authority consistency PASS
- ERD consistency PASS
- Capability consistency PASS
- Product experience consistency PASS
- System integrity consistency PASS
- Product scope consistency PASS
- Testing strategy consistency PASS
- Build PASS
- Tests PASS
- Repository hygiene PASS
- Documentation updated only if required
- No hard delete
- No generic CRUD framework
- No invented Room Description/RoomNumber edit
- Existing Terminate behavior intact
- Existing Loan/Return history intact
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After ADMIN-MAINTENANCE-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-09.
- Evidence: KEY-ROOM-ASSIGNMENT-1 is Accepted; human governance explicitly authorized ADMIN-MAINTENANCE-1; governing contracts already own Organization/Department/Building/Room/KeyType creation+activation+retirement, WorkforceMember relationship changes and termination, and WorkAssignment active/ended plus primary rules; inspection found no explicit Room Description or RoomNumber post-create edit authority and no hard-delete authority, so those operations are excluded; slice specification defines exact entities, fields, activation rules, Application/SQL Server/Web scope, out-of-scope exclusions, allowed/forbidden files, acceptance criteria, required tests, dependencies, risks, and human acceptance checkpoint without implementation.
- Deciding authority role: Human Architectural Governance.
- Missing business decisions excluded from this slice (not invented):
  1. Whether Room.Description may be changed after Room creation (domain has UpdateDescription; Location Room ownership text does not grant update).
  2. Whether RoomNumber may be changed after Room creation (required/unique; no explicit post-create edit authority; no domain mutator).
  3. Whether identity codes may be renamed after creation for Organization, Department, Building, Room, or KeyType.
  4. Whether retiring an Organization or Building must cascade-retire dependent Departments/Rooms (not specified; not invented).
  5. Physical deletion rules for any of the listed entities (not specified; hard delete forbidden in this slice).

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-09.
- Evidence: Application maintenance use cases persist Organization/Department/Building/Room/KeyType Activate and Retire (with owning-parent and KeyType active-KeyAsset rules), Active WorkforceMember Organization/Department/ResponsibleManager/WorkforceType updates without Party rewrite, WorkAssignment End and primary maintenance, and existing Terminate unchanged; Infrastructure updates existing SQL Server rows without migration, hard delete, or second store; Administration and Catalog Key Types surfaces expose Active/Retired/Terminated/Ended states and maintenance actions; Issue/Receive/Lookup/Reporting regression remains valid; architecture and SQL workflow tests PASS; build PASS 0 warnings 0 errors; tests PASS 147/147.
- Deciding authority role: Implementation execution under approved slice specification.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-09.
- Evidence: ADMIN-MAINTENANCE-1 was Implementation Complete; Organization/Department/Building/Room/KeyType Activate and Retire with owning-parent and KeyType active-KeyAsset rules, Active WorkforceMember relationship maintenance without Party rewrite, WorkAssignment End and primary maintenance, existing Terminate unchanged, SQL Server row updates without hard delete or second store, and Administration/Catalog task-oriented maintenance UI remained within approved scope; no identity-code renames, Room Description/RoomNumber edit, cascade-retire invention, generic CRUD framework, REPORTS-2, or issue/receive mutation changes were introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
