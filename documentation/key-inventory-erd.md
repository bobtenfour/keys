# Key Inventory ERD

## Authority
This document is the logical data model authority.

## Purpose
Define the logical entities and relationships required by the domain. It is not a database migration plan.

## Initial Logical Entities
- KeyAsset
- KeySeries
- KeyType
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
| KeyAsset | Key Catalog aggregate | key-inventory-domain-contract.md | Authoritative for catalog identity only | Current baseline |
| KeySeries | Key Catalog classification | key-inventory-domain-contract.md | Authoritative classification | Current baseline |
| KeyType | Key Catalog classification | key-inventory-domain-contract.md | Authoritative classification | Current baseline |
| Lock | Key Catalog aggregate | key-inventory-domain-contract.md | Authoritative | Current baseline |
| Location | Location boundary | key-inventory-domain-contract.md | Authoritative | Current baseline |
| Building | Location boundary | key-inventory-domain-contract.md | Authoritative | WORKFORCE-ELIGIBILITY-1 |
| Room | Location boundary | key-inventory-domain-contract.md | Authoritative | WORKFORCE-ELIGIBILITY-1 |
| Party | Party boundary | key-inventory-domain-contract.md | Authoritative | Current baseline |
| Organization | Workforce Eligibility boundary | key-inventory-domain-contract.md | Authoritative | WORKFORCE-ELIGIBILITY-1 |
| Department | Workforce Eligibility boundary | key-inventory-domain-contract.md | Authoritative | WORKFORCE-ELIGIBILITY-1 |
| WorkforceMember | Workforce Eligibility boundary | key-inventory-domain-contract.md | Authoritative | WORKFORCE-ELIGIBILITY-1 |
| WorkAssignment | Workforce Eligibility boundary | key-inventory-domain-contract.md | Authoritative | WORKFORCE-ELIGIBILITY-1 |
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
### KeyAsset
- Purpose: authoritative catalog identity for one controlled physical key asset.
- Owning aggregate or boundary: Key Catalog aggregate.
- Authoritative or derived: Authoritative for catalog identity only.
- Required relationships: references exactly one KeyType; may reference one KeySeries; may reference one Lock.
- Cardinalities: one KeyType to zero or more KeyAsset records; zero or one KeySeries to zero or more KeyAsset records; zero or one Lock to zero or more KeyAsset records.
- Required uniqueness: CatalogKeyCode is unique across KeyAsset records.
- Required integrity constraints: CatalogKeyCode is required; KeyType reference is required; referenced KeyType, KeySeries, and Lock must be active for new catalog assignment.
- Prohibited authority: must not store current possession, current custodian, loan state, return state, lifecycle state, audit history, maintenance workflow state, authorization state, authentication state, policy state, persistence-provider configuration, or UI state.

### KeySeries
- Purpose: authoritative catalog classification for an organizational keying system, pattern, or managed series.
- Owning aggregate or boundary: Key Catalog classification.
- Authoritative or derived: Authoritative classification.
- Required relationships: may classify zero or more KeyAsset records.
- Cardinalities: one KeySeries to zero or more KeyAsset records; a KeyAsset has zero or one KeySeries.
- Required uniqueness: SeriesCode is unique across KeySeries records.
- Required integrity constraints: SeriesCode is required; inactive KeySeries must not be used for new KeyAsset catalog assignment.

### KeyType
- Purpose: authoritative catalog classification for the physical or operational kind of key.
- Owning aggregate or boundary: Key Catalog classification.
- Authoritative or derived: Authoritative classification.
- Required relationships: classifies zero or more KeyAsset records.
- Cardinalities: one KeyType to zero or more KeyAsset records; a KeyAsset has exactly one KeyType.
- Required uniqueness: TypeCode is unique across KeyType records.
- Required integrity constraints: TypeCode is required; inactive KeyType must not be used for new KeyAsset catalog assignment.
- Prohibited authority: must not encode custody, loan, return, lifecycle, maintenance, authorization, policy, authentication, persistence, or UI state.

### Lock
- Purpose: authoritative catalog identity for one controlled physical lock.
- Owning aggregate or boundary: Key Catalog aggregate.
- Authoritative or derived: Authoritative.
- Required relationships: references exactly one Location; may be referenced by zero or more KeyAsset records.
- Cardinalities: one Location to zero or more Lock records; one Lock to zero or more KeyAsset records; a KeyAsset has zero or one intended Lock.
- Required uniqueness: LockCode is unique across Lock records.
- Required integrity constraints: LockCode is required; Location reference is required; referenced Location must be active for new Lock assignment.
- Prohibited authority: must not store possession, custody, loan, return, lifecycle, maintenance, audit, authorization, authentication, policy, persistence-provider configuration, or UI state.

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
- Purpose: physical building place that contains Rooms.
- Owning aggregate or boundary: Location boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may contain zero or more Room records.
- Cardinalities: one Building to zero or more Room records.
- Required uniqueness: BuildingCode is unique across Building records.
- Required integrity constraints: BuildingCode is required; only an active Building may contain active Rooms used for WorkAssignment or Room-based key-issue justification.
- Prohibited authority: must not own WorkforceMember, WorkAssignment, Loan, Return, custody, audit, authentication, or UI.
- Lifecycle phase: WORKFORCE-ELIGIBILITY-1.

### Room
- Purpose: physical room within one Building used for WorkAssignment and Room-based key-issue justification.
- Owning aggregate or boundary: Location boundary.
- Authoritative or derived: Authoritative.
- Required relationships: references exactly one Building; may be referenced by zero or more WorkAssignment records.
- Cardinalities: one Building to zero or more Room records; one Room to zero or more WorkAssignment records.
- Required uniqueness: RoomCode is unique across Room records; RoomNumber is unique within one Building.
- Required integrity constraints: RoomNumber is required as the operator-facing room identifier; Room must reference exactly one Building; only an active Room in an active Building may be used for active WorkAssignment or Room-based key-issue justification.
- Prohibited authority: must not own WorkforceMember eligibility decisions, Organization, Department, Loan, Return, custody, audit, authentication, or UI; Room must not exist outside Location boundary place authority.
- Lifecycle phase: WORKFORCE-ELIGIBILITY-1.

## Workforce Eligibility Logical Contract
### Party
- Purpose: persistent business identity for persons or organizations; sole person-identity authority.
- Owning aggregate or boundary: Party boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may be referenced by zero or more WorkforceMember records over time; may be referenced by zero or more Loan records as borrower.
- Cardinalities: one Party to zero or more WorkforceMember records; one Party to at most one Active WorkforceMember at a time in this workforce scope; one Party to zero or more Loan records.
- Required uniqueness: UIN is unique across Party records that carry UIN.
- Required integrity constraints: for a human Party used as a workforce key recipient, FirstName, LastName, and UIN are required; UIN is exactly nine numeric digits; Party remains independent of WorkforceMember Status; Loan borrower is a Party reference, not a Borrower entity.
- Prohibited authority: must not own Organization, Department, WorkforceType, ResponsibleManager hierarchy, WorkAssignment, key-issue eligibility decisions, Loan/Return mutation, custody, lifecycle, audit emission, authentication, or UI.

### Organization
- Purpose: institutional organization that owns Departments and workforce membership for key eligibility.
- Owning aggregate or boundary: Workforce Eligibility boundary.
- Authoritative or derived: Authoritative.
- Required relationships: may own zero or more Department records; may be referenced by zero or more WorkforceMember records.
- Cardinalities: one Organization to zero or more Department records; one Organization to zero or more WorkforceMember records.
- Required uniqueness: OrganizationCode is unique across Organization records.
- Required integrity constraints: OrganizationCode is required; only an active Organization may own active Departments or active WorkforceMember membership.
- Prohibited authority: must not own Party, Location, Building, Room, Loan, Return, custody, audit, authentication, or UI.
- Lifecycle phase: WORKFORCE-ELIGIBILITY-1.

### Department
- Purpose: organizational unit within one Organization for WorkforceMember membership and Department-based key-issue justification.
- Owning aggregate or boundary: Workforce Eligibility boundary.
- Authoritative or derived: Authoritative.
- Required relationships: references exactly one Organization; may be referenced by zero or more WorkforceMember records.
- Cardinalities: one Organization to zero or more Department records; one Department to zero or more WorkforceMember records.
- Required uniqueness: DepartmentCode is unique within one Organization.
- Required integrity constraints: DepartmentCode is required; only an active Department in an active Organization may be used for active WorkforceMember membership or Department-based issue justification.
- Prohibited authority: must not own Party, Location, Building, Room, Loan, Return, custody, audit, authentication, or UI.
- Lifecycle phase: WORKFORCE-ELIGIBILITY-1.

### WorkforceMember
- Purpose: workforce relationship and key-eligibility record for WorkforceType Employee or Contractor; not person identity. Employment is not a separate entity.
- Owning aggregate or boundary: Workforce Eligibility boundary.
- Authoritative or derived: Authoritative.
- Required relationships: references exactly one Party; references exactly one Organization; references exactly one Department belonging to that Organization; references exactly one ResponsibleManager WorkforceMember different from itself; may have zero or more WorkAssignment records; may be referenced as ResponsibleManager by zero or more WorkforceMember records.
- Cardinalities: one Party to zero or more WorkforceMember records; one Party to at most one Active WorkforceMember at a time; one Organization to zero or more WorkforceMember records; one Department to zero or more WorkforceMember records; one ResponsibleManager WorkforceMember to zero or more managed WorkforceMember records; one WorkforceMember to zero or more WorkAssignment records.
- Required uniqueness: WorkforceMemberCode is unique across WorkforceMember records.
- Required integrity constraints: WorkforceType, Organization, Department, ResponsibleManager, and Status are required; WorkforceType is Employee or Contractor; Status is Active or Terminated; ResponsibleManager must reference an active authorized WorkforceMember; termination, rehire, Department change, Organization change, and WorkforceType transition must not rewrite Party person identity; termination must not auto-mutate Loan, Return, custody, lifecycle, or audit records.
- Prohibited authority: must not own FirstName, LastName, UIN, Party lifecycle, Borrower aggregate, Employment aggregate, Loan/Return mutation, custody, lifecycle, audit emission, authentication, authorization decisions, or UI.
- Lifecycle phase: WORKFORCE-ELIGIBILITY-1.

### WorkAssignment
- Purpose: assign a WorkforceMember to a Room for authorized work and Room-based key-issue justification.
- Owning aggregate or boundary: Workforce Eligibility boundary.
- Authoritative or derived: Authoritative.
- Required relationships: references exactly one WorkforceMember; references exactly one Room.
- Cardinalities: one WorkforceMember to zero or more WorkAssignment records; one Room to zero or more WorkAssignment records.
- Required uniqueness: a WorkforceMember must not have overlapping active assignments to the same Room; at most one active WorkAssignment per WorkforceMember may be marked primary.
- Required integrity constraints: referenced Room must be active and belong to an active Building for an active assignment.
- Prohibited authority: must not own Location hierarchy, Building, RoomNumber uniqueness, WorkforceMember termination processing, Loan/Return mutation, custody, audit, or UI.
- Lifecycle phase: WORKFORCE-ELIGIBILITY-1.

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
- Required invariants: RoleCode is unique within Organization; must not own authentication credentials, Party lifecycle, policy evaluation, audit history, or business workflow state.
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
- Purpose: authoritative controlled issuance intent and workflow state for one cataloged key loaned to one Party.
- Owning aggregate or boundary: Loan aggregate.
- Authoritative or derived: Authoritative for loan issuance intent and completion workflow, not possession.
- Required relationships: references exactly one KeyAsset; references exactly one borrowing Party; may be referenced by zero or one Return; when Workforce Eligibility is in force, borrowing Party must be the Party of an eligible active WorkforceMember and issue justification/ResponsibleManager are required by domain eligibility rules without creating a Borrower entity.
- Cardinalities: one KeyAsset to zero or more Loan records; one Party to zero or more Loan records; one Loan to zero or one Return.
- Required uniqueness: LoanCode is unique across Loan records.
- Required integrity constraints: LoanCode is required; KeyAsset reference is required; Party borrower reference is required; IssuedAtUtc is required; DueAtUtc is required; DueAtUtc must be later than IssuedAtUtc; LoanStatus must be Open, Returned, or Cancelled; an Open Loan may have zero Returns; a Returned Loan must have exactly one Return; a Cancelled Loan must have zero Returns; WorkforceMember termination must not rewrite LoanStatus automatically.
- Prohibited authority: must not store current possession, current custodian, custody transfer history, catalog identity authority, Party profile data, WorkforceMember ownership, lifecycle state, lifecycle transition authority, audit history, authorization state, authentication state, policy state, persistence-provider configuration, or UI state.

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
