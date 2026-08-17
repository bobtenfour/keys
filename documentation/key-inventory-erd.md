# Key Inventory ERD

## Authority
This document is the logical data model authority.

## Purpose
Define the logical entities and relationships required by the domain. It is not a database migration plan.

## Active Structural Amendment — Identity normalization (2026-08-12)
- Decision: SUPERSEDE DepartmentCode-as-entity-identity for the active logical model.
- Authority: Human Governance identity rule — entity identity is a stable internal PK; business identifiers may be unique and editable and must not become persistence identity merely because they are unique.
- Evidence / full audit: `documentation/erd-normalization-identity-authority-2026-08-12.md`; provenance: `documentation/department-historical-justification-provenance-2026-08-12.md`.
- Active Department identity: **DepartmentId** (immutable PK) + **DepartmentCode** (unique editable business identifier).
- Room dual-identity (RoomCode + RoomNumber), Party (PartyCode + UIN), KeyAssetId, KeyNumber-as-immutable-KEY # remain.
- Does not rewrite Accepted OPERATOR-EXPERIENCE-1 / KEY-ACCESS-COPY-1 historical records.

## Active Structural Amendment — Room Department, Regular/Master, no KeyType (2026-08-14)
- Decision: SUPERSEDE KeyType entity and master=multi-room inference for the active logical model.
- Authority: Human Governance normalized model.
- Active rules:
  - **Room.DepartmentId** FK → Department (required; Room belongs to exactly one Department).
  - **KeyAccessPattern.Classification** = `Regular` | `Master` (sole KEY # classification; not inferred from Room count).
  - **No KeyType entity** in the active logical model.
  - **WorkAssignment consistency:** Room.DepartmentId must match WorkforceMember.DepartmentId.
  - KeyAsset has no holder/Department columns; Available/Issued derived from open Loan on KeyAssetId.
- Does not rewrite Accepted OPERATOR-EXPERIENCE-1 / KEY-ACCESS-COPY-1 historical records.

## Active Structural Amendment — Classification defines KEY # access (2026-08-16)
- Decision: SUPERSEDE `KeyAccessPatternRoomAssignments` many-to-many join as Room-access authority.
- Active schema:
  - **KeyAccessPattern.RoomCode** nullable FK → Room (Regular required; Master NULL).
  - Master access = all current Rooms derived from Classification; not stored per Room.
  - KeyAsset has no Room columns.
- Migration: `20260816180000_ClassificationDefinesKeyAccess`.
- Does not rewrite Accepted KEY-ACCESS-COPY-1 historical records.

## Implementation status (2026-08-12)
- **IMPLEMENTED** via migration `DepartmentIdentityNormalization`: DepartmentId PK; WM/WA/Loan FKs; structured Loan justification; one-time KeyIssued provenance extract (migration-scoped only); Department Edit/rename Application authority.
- **TARGET** above remains the governing logical model; runtime must not parse OperatorAuditRecord.Details for Department relationships or delete eligibility.

## Target Normalized Relational Model (active)
Logical persistence shape for current business tables. PK = primary key; AK = alternate/unique business key; FK = foreign key (Restrict unless noted).

### Department
- **PK:** DepartmentId (stable internal identity, Guid)
- **AK:** DepartmentCode (unique, operator-facing, **editable**)
- **Lifecycle:** IsActive (Activate/Retire)
- **Relationships:** referenced by WorkforceMember.DepartmentId (live membership); Room.DepartmentId (Room ownership); may be referenced by Loan.JustificationDepartmentId (historical issue snapshot)

### Room
- **PK:** RoomCode (immutable technical identity)
- **AK:** RoomNumber (unique, operator-facing, editable)
- **FK:** **DepartmentId → Department** (required; exactly one Department)
- **Attrs:** Description (editable), IsActive
- **Relationships:** WorkAssignment.RoomCode FK; KeyAccessPatternRoomAssignment.RoomCode FK; Loan.JustificationRoomCode historical snapshot

### Party
- **PK:** PartyCode (immutable technical identity)
- **AK:** UIN (unique, governed mutable business identifier)
- **Attrs:** FirstName, LastName, IsActive
- **Relationships:** WorkforceMember.PartyCode FK; Loan.BorrowerPartyReference FK → PartyCode

### WorkforceMember
- **PK:** WorkforceMemberCode
- **FK:** PartyCode → Party; **DepartmentId → Department**
- **Attrs:** WorkforceType, Status (Active/Terminated)
- **Cardinality:** Party 1 → 0..N WM (≤1 Active); Department 1 → 0..N WM

### WorkAssignment
- **PK:** WorkAssignmentId (technical Guid; not operator-facing)
- **FK:** WorkforceMemberCode → WorkforceMember; RoomCode → Room
- **Attrs:** IsActive (End)
- **Consistency:** Room.DepartmentId must equal WorkforceMember.DepartmentId (cross-department WA forbidden)
- **Uniqueness:** at most one active WorkAssignment per (WorkforceMemberCode, RoomCode)
- Association entity; no independent WorkAssignmentCode; no Primary designation

### KeyAccessPattern (KEY #)
- **PK / business identity:** KeyNumber (operator-facing KEY #; **immutable** by KEY-ACCESS-COPY-1)
- **Classification:** Regular | Master (required enum on KEY #; replaces KeyType entity)
- **Lifecycle:** IsActive
- Owns current Room openings via KeyAccessPatternRoomAssignment
- Classification is not inferred from Room assignment count

### KeyAccessPatternRoomAssignment
- **PK:** (KeyNumber, RoomCode)
- **FK:** KeyNumber → KeyAccessPattern; RoomCode → Room
- Sole current Room-access authority; Remove does not rewrite Loan/audit history

### KeyAsset (MEDECO)
- **PK:** KeyAssetId (Guid, immutable)
- **AK:** (KeyNumber, MedecoKeyCode) unique
- **FK:** KeyNumber → KeyAccessPattern; optional ReplacesKeyAssetId → KeyAsset (replacement lineage)
- **Attrs:** MedecoKeyCode (unique within KEY #), Condition (Active | Lost | Destroyed), ReplacesKeyAssetId
- Rooms and Classification **derived** from parent KEY # (not independently persisted authorities)
- No holder column; no Department column; no IsActive / Retired physical-key flag
- Available/Issued derived: Active + open Loan ⇒ Issued; Active + no open Loan ⇒ Available; Lost/Destroyed ⇒ not Available/not Issued
- Replacement is an operation/relationship via ReplacesKeyAssetId on the new KeyAsset, not a Condition value

### Loan
- **PK:** LoanCode
- **FK:** KeyAssetId → KeyAsset; BorrowerPartyReference → Party
- **Historical snapshot (immutable after issue):** JustificationKind; JustificationDepartmentId (nullable FK → Department when kind=Department); JustificationRoomCode (nullable FK → Room when kind=Room)
- Snapshot ≠ live WorkforceMember.DepartmentId membership
- Status (Open | Returned | Lost | Destroyed | Cancelled), IssuedAtUtc, DueAtUtc
- Open Loan is the authority for Issued custody of that KeyAsset
- Lost/Destroyed close Open Loan without a Return; Returned requires Return

### Return
- **PK:** ReturnCode
- **FK:** LoanCode → Loan (unique)

### OperatorAuditRecord
- **PK:** AuditRecordId
- Append-only; SubjectReference/Details are soft refs and display snapshots; never rewritten on DepartmentCode rename

### Cardinality summary (target)
- Department 1 — 0..N WorkforceMember; Department 1 — 0..N Room
- Party 1 — 0..N WorkforceMember (≤1 Active)
- WorkforceMember 1 — 0..N WorkAssignment
- Room 1 — 0..N WorkAssignment; Room 1 — 0..N KeyAccessPatternRoomAssignment; Room N — 1 Department
- KeyAccessPattern 1 — 0..N KeyAsset; KeyAccessPattern M — N Room (via assignment)
- KeyAsset 1 — 0..N Loan (≤1 Open)
- Party 1 — 0..N Loan
- Loan 1 — 0..1 Return

## Initial Logical Entities
- KeyAccessPattern
- KeyAccessPatternRoomAssignment
- KeyAsset
- KeyRoomAssignment (historical; retired active authority under KEY-ACCESS-COPY-1)
- KeySeries (non-operational seed; not KEY # authority)
- KeyType (historical; removed from active model — superseded by Regular|Master Classification)
- Lock
- Location
- Building
- Room
- Party
- Organization
- Department
- WorkforceMember
- WorkAssignment
- SecurityPrincipal
- SecurityPrincipalType
- Role
- Permission
- RolePermission
- PrincipalRoleAssignment
- AuthorizationScopeType
- Loan
- Return
- LifecycleState
- LifecycleTransition
- EventStream
- Event
- EventType
- KeyLifecycleProjection
- CustodyEvent
- CustodyEndpointType
- StorageLocation
- KeyCustodyProjection
- AuditEvent
- InventorySession
- InventoryCount
- InventoryDiscrepancy
- MaintenanceRequest

## Entity Ownership Matrix
| Entity | Owning aggregate or boundary | Authority document | Authoritative or derived | Lifecycle phase |
|---|---|---|---|---|
| KeyAccessPattern | Key Catalog aggregate | key-inventory-domain-contract.md | Authoritative KEY # / access-pattern identity, Regular\|Master Classification, and Room openings | KEY-ACCESS-COPY-1; Classification amendment 2026-08-14 |
| KeyAccessPatternRoomAssignment | Key Catalog aggregate | key-inventory-domain-contract.md | Authoritative current KEY #↔Room opening association | KEY-ACCESS-COPY-1 |
| KeyAsset | Key Catalog aggregate | key-inventory-domain-contract.md | Authoritative physical-copy identity (KeyAssetId + MEDECO within KEY #); no holder/Department | KEY-ACCESS-COPY-1 supersedes CatalogKeyCode-as-unique-identity |
| KeyRoomAssignment | Key Catalog aggregate | key-inventory-domain-contract.md | Retired active KeyAsset↔Room authority | Historical KEY-ROOM-ASSIGNMENT-1 |
| KeySeries | Key Catalog classification | key-inventory-domain-contract.md | Non-operational seed; not KEY # / Room / copy authority | KEY-ACCESS-COPY-1 |
| KeyType | Key Catalog classification | key-inventory-domain-contract.md | Removed from active model; superseded by Regular\|Master on KeyAccessPattern | Superseded 2026-08-14 |
| Lock | Key Catalog aggregate | key-inventory-domain-contract.md | Authoritative | Current baseline |
| Location | Location boundary | key-inventory-domain-contract.md | Authoritative | Current baseline |
| Building | Location boundary | key-inventory-domain-contract.md | Removed from active model (historical audit may reference) | OPERATOR-EXPERIENCE-1 |
| Room | Location boundary | key-inventory-domain-contract.md | Authoritative; DepartmentId required; no Building; global RoomNumber | OPERATOR-EXPERIENCE-1; Room→Department 2026-08-14 |
| Party | Party boundary | key-inventory-domain-contract.md | Authoritative | Current baseline |
| Organization | Workforce Eligibility boundary | key-inventory-domain-contract.md | Removed from active model (historical audit may reference) | OPERATOR-EXPERIENCE-1 |
| Department | Workforce Eligibility boundary | key-inventory-domain-contract.md | Authoritative; no Organization; global DepartmentCode | OPERATOR-EXPERIENCE-1 |
| WorkforceMember | Workforce Eligibility boundary | key-inventory-domain-contract.md | Authoritative; no Organization/ResponsibleManager | OPERATOR-EXPERIENCE-1 |
| WorkAssignment | Workforce Eligibility boundary | key-inventory-domain-contract.md | Authoritative WM↔Room; Room.Department must match WM.Department | OPERATOR-EXPERIENCE-1; consistency 2026-08-14 |
| SecurityPrincipal | Identity boundary | security-capability-contract.md | Authoritative | IDENTITY-1 |
| SecurityPrincipalType | Identity boundary | security-capability-contract.md | Authoritative vocabulary | IDENTITY-1 |
| Role | Authorization boundary | security-capability-contract.md | Authoritative | IDENTITY-1 |
| Permission | Authorization boundary | security-capability-contract.md | Authoritative | IDENTITY-1 |
| RolePermission | Authorization boundary | security-capability-contract.md | Authoritative | IDENTITY-1 |
| PrincipalRoleAssignment | Authorization boundary | security-capability-contract.md | Authoritative | IDENTITY-1 |
| AuthorizationScopeType | Authorization boundary | security-capability-contract.md | Authoritative vocabulary | IDENTITY-1 |
| Loan | Loan aggregate | key-inventory-domain-contract.md | Authoritative for loan issuance intent and completion workflow, not possession | Current baseline |
| Return | Return aggregate | key-inventory-domain-contract.md | Authoritative for return workflow, not possession | Current baseline |
| LifecycleState | Lifecycle boundary | key-inventory-domain-contract.md | Authoritative vocabulary | Current baseline |
| LifecycleTransition | Lifecycle boundary | key-inventory-domain-contract.md | Authoritative transition rules | Current baseline |
| EventStream | Lifecycle boundary | key-inventory-domain-contract.md | Authoritative append-only event stream | Current baseline |
| Event | Lifecycle boundary | key-inventory-domain-contract.md | Authoritative append-only lifecycle event | Current baseline |
| EventType | Lifecycle boundary | key-inventory-domain-contract.md | Authoritative vocabulary | Current baseline |
| KeyLifecycleProjection | Lifecycle projection boundary | key-inventory-erd.md | Derived, rebuildable, non-authoritative | Current baseline |
| CustodyEvent | Custody boundary | key-inventory-domain-contract.md | Authoritative append-only custody record | Current baseline |
| CustodyEndpointType | Custody boundary | key-inventory-domain-contract.md | Authoritative vocabulary | Current baseline |
| StorageLocation | Custody boundary | key-inventory-domain-contract.md | Authoritative custody endpoint | Current baseline |
| KeyCustodyProjection | Custody projection boundary | key-inventory-erd.md | Derived, rebuildable, non-authoritative | Current baseline |
| AuditEvent | Audit boundary | key-inventory-domain-contract.md | Authoritative append-only audit evidence | AUDIT-1 |
| InventorySession | Inventory boundary | key-inventory-domain-contract.md | Authoritative | Future roadmap slice |
| InventoryCount | Inventory boundary | key-inventory-domain-contract.md | Authoritative | Future roadmap slice |
| InventoryDiscrepancy | Inventory boundary | key-inventory-domain-contract.md | Authoritative | Future roadmap slice |
| MaintenanceRequest | Maintenance boundary | key-inventory-domain-contract.md | Authoritative | Future roadmap slice |

## Lifecycle Logical Contract
- LifecycleState preserves the valid derived lifecycle states: Available, Reserved, Issued, InTransit, ReturnedPendingInspection, Lost, Maintenance, Disabled, and Destroyed.
- EventType preserves the core lifecycle event concepts: KeyCreated, KeyUpdated, Issued, Returned, CustodyTransferred, Lost, Recovered, and Destroyed.
- EventStream and Event are append-only lifecycle authority.
- LifecycleTransition is the authority for valid lifecycle transition rules.
- KeyLifecycleProjection is derived from authoritative lifecycle events, rebuildable, and non-authoritative.
- KeyAsset must not contain an authoritative mutable lifecycle status.

## Catalog Logical Contract
### KeyAccessPattern
- Purpose: authoritative KEY # / shared access-pattern identity, Regular|Master Classification, and Room access set for all physical copies under that KEY #.
- Owning aggregate or boundary: Key Catalog aggregate.
- Authoritative or derived: Authoritative for KeyNumber, Classification, and current KeyAccessPattern↔Room openings.
- Required relationships: has exactly one Classification (Regular|Master); may have zero or more KeyAsset physical copies; may have zero or more KeyAccessPatternRoomAssignment records.
- Cardinalities: one KeyAccessPattern to zero or more KeyAsset records; one KeyAccessPattern to zero or more Rooms via assignments; one Room to zero or more KeyAccessPatterns.
- Required uniqueness: KeyNumber is unique across KeyAccessPattern records (installation-wide).
- Required integrity constraints: KeyNumber required; Classification required as Regular or Master and not inferred from Room count; Building derived only through assigned Rooms; must not independently own Building; must not own custody.
- Prohibited authority: possession, loan/return, lifecycle, audit history, master/sub-master hierarchy engine, KeyType entity, KeySeries reinterpretation as this entity, Master inferred from multi-room.

### KeyAccessPatternRoomAssignment
- Purpose: current authoritative association of one KEY # to one Room that every physical copy under that KEY # opens.
- Owning aggregate or boundary: Key Catalog aggregate.
- Authoritative or derived: Authoritative for current assignment only; sole Room-opening authority after KEY-ACCESS-COPY-1.
- Required relationships: references exactly one KeyAccessPattern; references exactly one Room.
- Cardinalities: many-to-many between KeyAccessPattern and Room.
- Required uniqueness: the pair (KeyAccessPattern, Room) is unique among current assignments.
- Required integrity constraints: both references required; active operational assignment requires an active Room; KeyAsset does not participate as assignment owner; Classification does not grant Room access.
- Prohibited authority: dual KeyAsset↔Room authority; Lock mediation; assignment history as second truth; REPORTS-2.

### KeyAsset
- Purpose: authoritative physical key under exactly one KeyAccessPattern.
- Owning aggregate or boundary: Key Catalog aggregate.
- Authoritative or derived: Authoritative for KeyAssetId, MEDECO within KEY #, Condition (Active|Lost|Destroyed), and optional ReplacesKeyAssetId; Rooms and Classification are derived from parent KeyAccessPattern; Available/Issued derived from Condition + open Loan.
- Required relationships: references exactly one KeyAccessPattern; may reference one Lost source KeyAsset when created as Replacement.
- Cardinalities: one KeyAccessPattern to zero or more KeyAsset records; each KeyAsset exactly one KeyAccessPattern.
- Required uniqueness: KeyAssetId unique globally; MEDECO unique within parent KeyAccessPattern (not globally).
- Required integrity constraints: KeyAssetId immutable; MEDECO required; Condition required; must not own independent Room assignments; must not store holder or Department; must not persist Available/Issued; physical IsActive/Retire/Activate removed; CatalogKeyCode is not unique physical business identity; opaque composite KEY#+MEDECO strings forbidden as identity authority.
- Prohibited authority: possession/holder/Department columns, loan/return state as KeyAsset fields, independent Room openings, independent mutable Classification, KeySeries-as-KEY #, physical Retire/Activate.

### KeyRoomAssignment
- Purpose (historical): former current association of KeyAsset to Room.
- Status: retired from active logical authority by KEY-ACCESS-COPY-1; must not remain a second source of truth beside KeyAccessPatternRoomAssignment.

### KeySeries
- Purpose: non-operational Domain classification seed only.
- Status: must not be KEY #, Room-access, or physical-copy identity authority under KEY-ACCESS-COPY-1.

### KeyType
- Purpose (historical): former catalog classification entity for KEY #.
- Status: removed from the active logical model (2026-08-14); superseded by KeyAccessPattern.Classification Regular|Master. Must not remain as FK target or admin entity.

### Lock
- Purpose: optional catalog identity for one controlled physical lock device.
- Owning aggregate or boundary: Key Catalog aggregate.
- Authoritative or derived: Authoritative for Lock device identity only when used.
- Required relationships: may reference one Location.
- Cardinalities: one Location to zero or more Lock records when Location is used.
- Required uniqueness: LockCode is unique across Lock records.
- Required integrity constraints: LockCode is required; when Location is referenced it must be active for new Lock assignment.
- Authority reconciliation: Lock is not intermediate or required authority for KeyAccessPattern↔Room opening associations; KeyAccessPatternRoomAssignment is the sole operational room-opening authority.
- Prohibited authority: must not store possession, custody, loan, return, lifecycle, maintenance, audit, authorization, authentication, policy, KeyAccessPattern↔Room opening authority, persistence-provider configuration, or UI state.

### Location
- Purpose: authoritative physical organizational place where a key, lock, or custody action is relevant.
- Owning aggregate or boundary: Location boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may have zero or one parent Location; may have zero or more child Locations; may be referenced by zero or more Lock records; may own Building and Room place records under Location boundary authority.
- Cardinalities: one parent Location to zero or more child Locations; one Location to zero or more Lock records.
- Required uniqueness: LocationCode is unique across Location records.
- Required integrity constraints: LocationCode is required; a Location cannot be its own parent; Location hierarchy must not contain cycles; inactive Location must not be used for new Lock assignment.
- Prohibited authority: must not own Organization, Department, WorkforceMember, WorkAssignment, Loan, Return, custody, audit, authentication, or UI authority; a second independent place model outside Location boundary is forbidden.

### Building
- Purpose: formerly physical building place; removed from the active logical model by OPERATOR-EXPERIENCE-1.
- Owning aggregate or boundary: none active.
- Authoritative or derived: Not active; historical OperatorAuditRecord references may remain.
- Prohibited authority: must not be reintroduced as Tenant/Site/Facility/Campus/LocationRoot without a new human business decision.
- Lifecycle phase: OPERATOR-EXPERIENCE-1 (removal).

### Room
- Purpose: physical room for this installation used for WorkAssignment, Room-based key-issue justification, and KeyAccessPattern↔Room opening associations.
- Owning aggregate or boundary: Location boundary.
- Authoritative or derived: Authoritative for Room identity; Key Catalog owns KeyAccessPatternRoomAssignment references to Room.
- Required relationships: references exactly one Department via DepartmentId; may be referenced by zero or more WorkAssignment records; may be referenced by zero or more KeyAccessPatternRoomAssignment records; does not reference Building.
- Cardinalities: one Department to zero or more Room records; one Room to zero or more WorkAssignment records; one Room to zero or more KeyAccessPatternRoomAssignment records.
- Required uniqueness: RoomCode is unique across Room records; RoomNumber is unique across all Room records.
- Required integrity constraints: RoomNumber is required as the operator-facing room identifier (Room #); RoomCode is immutable technical identity; DepartmentId is required; only an active Room may be used for active WorkAssignment, Room-based key-issue justification, or active KeyAccessPatternRoomAssignment.
- Prohibited authority: must not own WorkforceMember eligibility decisions, Loan, Return, custody, audit, authentication, Key Catalog identity, KeyAccessPattern↔Room assignment ownership, or UI; Room must not exist outside Location boundary place authority.
- Lifecycle phase: OPERATOR-EXPERIENCE-1; Room↔KEY # cardinality governed by KEY-ACCESS-COPY-1; Room→Department amendment 2026-08-14.

## Workforce Eligibility Logical Contract
### Party
- Purpose: persistent business identity for persons; sole person-identity authority.
- Owning aggregate or boundary: Party boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may be referenced by zero or more WorkforceMember records over time; may be referenced by zero or more Loan records as borrower.
- Cardinalities: one Party to zero or more WorkforceMember records; one Party to at most one Active WorkforceMember at a time in this workforce scope; one Party to zero or more Loan records.
- Required uniqueness: UIN is unique across Party records that carry UIN.
- Required integrity constraints: for a human Party used as a workforce key recipient, FirstName, LastName, and UIN are required; UIN is exactly nine numeric digits; Party remains independent of WorkforceMember Status; Loan borrower is a Party reference, not a Borrower entity.
- Prohibited authority: must not own Department, WorkforceType, WorkAssignment, key-issue eligibility decisions, Loan/Return mutation, custody, lifecycle, audit emission, authentication, or UI.

### Organization
- Purpose: formerly institutional organization; removed from the active logical model by OPERATOR-EXPERIENCE-1.
- Owning aggregate or boundary: none active.
- Authoritative or derived: Not active; historical OperatorAuditRecord references may remain.
- Prohibited authority: must not be reintroduced as Tenant/Site or another scoping abstraction without a new human business decision.
- Lifecycle phase: OPERATOR-EXPERIENCE-1 (removal).

### Department
- Purpose: organizational unit for WorkforceMember membership and Department-based key-issue justification.
- Owning aggregate or boundary: Workforce Eligibility boundary.
- Authoritative or derived: Authoritative.
- Identity (active, 2026-08-12): **DepartmentId** is the immutable entity identity and relationship target. **DepartmentCode** is the unique operator-facing business identifier and is editable without changing DepartmentId.
- Required relationships: may be referenced by zero or more WorkforceMember records via DepartmentId; may appear on Loan issue-justification snapshots via JustificationDepartmentId; does not reference Organization.
- Cardinalities: one Department to zero or more WorkforceMember records.
- Required uniqueness: DepartmentId unique; DepartmentCode unique across all Department records.
- Required integrity constraints: DepartmentCode is required; only an active Department may be used for active WorkforceMember membership or new Department-based issue justification; renaming DepartmentCode must not delete/recreate the Department or rewrite immutable audit history.
- Prohibited authority: must not own Party, Location, Room, Loan, Return, custody, audit, authentication, or UI; DepartmentCode must not serve as persistence PK / relationship identity.
- Lifecycle phase: OPERATOR-EXPERIENCE-1 foundation; identity normalization amendment 2026-08-12.

### WorkforceMember
- Purpose: workforce relationship and key-eligibility record for WorkforceType Employee or Contractor; not person identity. Employment is not a separate entity.
- Owning aggregate or boundary: Workforce Eligibility boundary.
- Authoritative or derived: Authoritative.
- Required relationships: references exactly one Party; references exactly one Department; may have zero or more WorkAssignment records; does not reference Organization or ResponsibleManager.
- Cardinalities: one Party to zero or more WorkforceMember records; one Party to at most one Active WorkforceMember at a time; one Department to zero or more WorkforceMember records; one WorkforceMember to zero or more WorkAssignment records.
- Required uniqueness: WorkforceMemberCode is unique across WorkforceMember records.
- Required integrity constraints: WorkforceType, Department, and Status are required; WorkforceType is Employee or Contractor; Status is Active or Terminated; first WorkforceMember may exist without any other WorkforceMember; termination, rehire, Department change, and WorkforceType transition must not rewrite Party person identity; termination must not auto-mutate Loan, Return, custody, lifecycle, or audit records.
- Prohibited authority: must not own FirstName, LastName, UIN, Party lifecycle, Borrower aggregate, Employment aggregate, ResponsibleManager hierarchy, Loan/Return mutation, custody, lifecycle, audit emission, authentication, authorization decisions, or UI.
- Lifecycle phase: OPERATOR-EXPERIENCE-1.

### WorkAssignment
- Purpose: assign a WorkforceMember to a Room for authorized work and Room-based key-issue justification.
- Owning aggregate or boundary: Workforce Eligibility boundary.
- Authoritative or derived: Authoritative.
- Required relationships: references exactly one WorkforceMember; references exactly one Room.
- Cardinalities: one WorkforceMember to zero or more WorkAssignment records; one Room to zero or more WorkAssignment records.
- Required uniqueness: a WorkforceMember must not have overlapping active assignments to the same Room.
- Required integrity constraints: referenced Room must be active for an active assignment; Room.DepartmentId must equal WorkforceMember.DepartmentId (cross-department Work Assignments forbidden).
- Prohibited authority: must not own Location hierarchy, RoomNumber uniqueness, WorkforceMember termination processing, Loan/Return mutation, custody, audit, or UI; must not expose WorkAssignmentId as an operator business identifier; must not invent Primary designation.
- Lifecycle phase: OPERATOR-EXPERIENCE-1; department consistency amendment 2026-08-14; WorkAssignmentCode/Primary removal amendment 2026-08-14.

## Identity and RBAC Logical Contract
### SecurityPrincipal
- Purpose: represent a technical principal that may be authenticated and authorized.
- Owning aggregate or boundary: Identity boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may reference Party for human principals; references SecurityPrincipalType; may have PrincipalRoleAssignment records.
- Required invariants: PrincipalName is unique; human principals may reference Party but Party remains an independent business identity; system and integration principals may exist without Party; must not duplicate Party profile or business data; must not own authentication credentials, Party lifecycle, policy evaluation, audit history, or business workflow state.
- Lifecycle phase: IDENTITY-1.

### SecurityPrincipalType
- Purpose: define the technical principal type vocabulary.
- Owning aggregate or boundary: Identity boundary.
- Authoritative or derived: Authoritative vocabulary.
- Required relationships: classifies SecurityPrincipal.
- Required invariants: must distinguish human, system, and integration principal concepts without creating Party ownership.
- Lifecycle phase: IDENTITY-1.

### Role
- Purpose: group permissions for authorization.
- Owning aggregate or boundary: Authorization boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may have RolePermission records; may have PrincipalRoleAssignment records.
- Required invariants: RoleCode is unique across Role records for this installation (no OrganizationCode scoping); must not own authentication credentials, Party lifecycle, policy evaluation, audit history, or business workflow state.
- Lifecycle phase: IDENTITY-1.

### Permission
- Purpose: define an authorization capability that can be assigned to roles.
- Owning aggregate or boundary: Authorization boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may have RolePermission records.
- Required invariants: PermissionCode is globally unique; must not own authentication credentials, Party lifecycle, policy evaluation, audit history, or business workflow state.
- Lifecycle phase: IDENTITY-1.

### RolePermission
- Purpose: authorize a role through a permission association.
- Owning aggregate or boundary: Authorization boundary.
- Authoritative or derived: Authoritative.
- Required relationships: relates one Role to one Permission.
- Required invariants: cannot contain duplicate Role/Permission pairs; must not own authentication credentials, Party lifecycle, policy evaluation, audit history, or business workflow state.
- Lifecycle phase: IDENTITY-1.

### PrincipalRoleAssignment
- Purpose: assign a role to a technical principal for an authorization scope and effective period.
- Owning aggregate or boundary: Authorization boundary.
- Authoritative or derived: Authoritative.
- Required relationships: relates SecurityPrincipal, Role, and AuthorizationScopeType.
- Required invariants: cannot contain duplicate active Principal/Role/Scope assignments; EffectiveToUtc must be later than EffectiveFromUtc when present; must not own authentication credentials, Party lifecycle, policy evaluation, audit history, or business workflow state.
- Lifecycle phase: IDENTITY-1.

### AuthorizationScopeType
- Purpose: define the authorization scope vocabulary used by role assignments.
- Owning aggregate or boundary: Authorization boundary.
- Authoritative or derived: Authoritative vocabulary.
- Required relationships: classifies PrincipalRoleAssignment scope.
- Required invariants: must not own authentication credentials, Party lifecycle, policy evaluation, audit history, or business workflow state.
- Lifecycle phase: IDENTITY-1.

## Custody Logical Contract
- CustodyEvent is append-only custody authority.
- Custody transfers support Party and StorageLocation endpoints.
- CustodyEndpointType defines the supported endpoint vocabulary for custody transfers.
- A custody transfer source and destination cannot be identical.
- Current possession is derived from the latest valid CustodyEvent.
- KeyCustodyProjection is derived from authoritative custody events, rebuildable, and non-authoritative.
- Loan, Return, KeyAsset, StorageDevice, reports, and UI must not own current-custodian authority.
- This ERD defines no second possession model.

## Loan and Return Logical Contract
### Loan
- Purpose: authoritative controlled issuance intent and workflow state for one physical KeyAsset copy loaned to one Party.
- Owning aggregate or boundary: Loan aggregate.
- Authoritative or derived: Authoritative for loan issuance intent and completion workflow, not possession.
- Required relationships: references exactly one physical KeyAsset by KeyAssetId; references exactly one borrowing Party by PartyCode; may be referenced by zero or one Return; when Workforce Eligibility is in force, borrowing Party must be the Party of an eligible active WorkforceMember and issue justification is required by domain eligibility rules without creating a Borrower entity.
- Issue justification persistence (active, 2026-08-12): Loan stores an **immutable historical snapshot** of the authorizing Department and/or Room identity used at Issue time. For Department: `JustificationKind=Department`, `JustificationDepartmentId` (FK), `JustificationDepartmentCodeSnapshot` (event-time code). For Room: `JustificationKind=Room`, `JustificationRoomCode` (FK to stable RoomCode); no RoomNumber snapshot field. Unrelated justification fields must be null. This snapshot is not live WorkforceMember membership and must not be updated when DepartmentCode or RoomNumber changes. OperatorAuditRecord Details remain immutable operator-readable snapshots and are **not** relational delete authority (see `department-historical-justification-provenance-2026-08-12.md`).
- Cardinalities: one KeyAsset to zero or more Loan records with at most one Open Loan per KeyAsset; one Party to zero or more Loan records; one Loan to zero or one Return; multiple Open Loans may exist under one KEY # when they reference different physical copies.
- Required uniqueness: LoanCode is unique across Loan records.
- Required integrity constraints: LoanCode is required; KeyAssetId reference is required; KEY # alone must not be the Loan subject; Party borrower reference is required; IssuedAtUtc is required; DueAtUtc is required; DueAtUtc must be later than IssuedAtUtc; LoanStatus must be Open, Returned, Lost, Destroyed, or Cancelled; an Open Loan may have zero Returns; a Returned Loan must have exactly one Return; Lost/Destroyed closed Loans must have zero Returns; a Cancelled Loan must have zero Returns; WorkforceMember termination must not rewrite LoanStatus automatically; justification snapshot ids use stable DepartmentId / RoomCode.
- Prohibited authority: must not store current possession, current custodian, custody transfer history, catalog identity authority, Party profile data, WorkforceMember ownership, lifecycle state, lifecycle transition authority, audit history, authorization state, authentication state, policy state, persistence-provider configuration, or UI state; must not move custody to KEY # / KeyAccessPattern.

### Return
- Purpose: authoritative completion record for one Loan back into organizational control.
- Owning aggregate or boundary: Return aggregate.
- Authoritative or derived: Authoritative for return workflow, not possession.
- Required relationships: references exactly one Loan.
- Cardinalities: one Loan to zero or one Return; one Return to exactly one Loan.
- Required uniqueness: ReturnCode is unique across Return records; Loan reference is unique across Return records.
- Required integrity constraints: ReturnCode is required; Loan reference is required; ReturnedAtUtc is required; ReturnedAtUtc must not be earlier than the Loan IssuedAtUtc; referenced Loan must be Open when Return is created; Return completion marks the Loan as Returned; WorkforceMember-termination return obligations are satisfied only by creating valid Return records through this Return authority.
- Prohibited authority: must not store current possession, current custodian, custody transfer history, catalog identity authority, Party profile data, WorkforceMember ownership, lifecycle state, lifecycle transition authority, audit history, authorization state, authentication state, policy state, persistence-provider configuration, or UI state.

## Audit Logical Contract
### AuditEvent
- Purpose: authoritative immutable evidence that one business or security-relevant action occurred.
- Owning aggregate or boundary: Audit boundary.
- Authoritative or derived: Authoritative append-only audit evidence.
- Required relationships: references exactly one acting SecurityPrincipal; may reference one Party; may reference one subject KeyAsset; may reference one subject Loan; may reference one subject Return.
- Cardinalities: one SecurityPrincipal to zero or more AuditEvent records; zero or one Party to zero or more AuditEvent records; zero or one KeyAsset to zero or more AuditEvent records; zero or one Loan to zero or more AuditEvent records; zero or one Return to zero or more AuditEvent records.
- Required uniqueness: AuditEventCode is unique across AuditEvent records.
- Required integrity constraints: AuditEventCode is required; ActionType is required; OccurredAtUtc is required; acting SecurityPrincipal reference is required; AuditEvent records are immutable after creation; audit history must not be rewritten, replaced, or deleted through AuditEvent authority.
- Prohibited authority: must not store current possession, current custodian, custody transfer history, catalog identity authority, Party profile data, loan workflow authority, return workflow authority, lifecycle state, lifecycle transition authority, authentication credentials, authorization decisions, roles, permissions, assignments, policy evaluation results, Digital Trust integrity mechanisms, persistence-provider configuration, or UI state.

## Temporal Logical Contract
- Authoritative logical temporal attributes are UTC instants.
- Authoritative temporal attributes use UTC naming (`Utc` or `AtUtc`), including IssuedAtUtc, DueAtUtc, ReturnedAtUtc, OccurredAtUtc, EffectiveFromUtc, and EffectiveToUtc.
- The logical model does not define authoritative local-time attributes.
- Local display time and client time-zone conversion are not logical ERD authority.
- Persistence-provider date/time types remain outside this logical contract.

## Projection Contract
All projection entities are derived, rebuildable, and non-authoritative. They may never be manually edited or become fallback business authority.

This rule applies to:
- KeyLifecycleProjection.
- KeyCustodyProjection.
- LoanProjection if introduced later.
- AuthorizationProjection if introduced later.
- Reporting and BI projections.

## Deferred Concepts
- Lifecycle and custody concepts remain governed by key-inventory-domain-contract.md.
- Future-phase entities from the broader conceptual model remain governed by the roadmap and must be expanded only in their owning future slices.
- This ERD represents only the minimum logical model required for the current baseline to remain internally coherent.

## Rules
- Logical model only.
- No provider-specific database types.
- No EF-specific mapping.
- No migration sequencing.
- Relationships must reflect business meaning.
- Derived values must not become independent authority.
- Each logical entity has exactly one owning aggregate or architectural boundary.
- No entity ownership cycle is allowed.

## Depends On
- key-inventory-domain-contract.md
- project-erd-governance.md

## Depended On By
- slices that implement persistence or domain entities
