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
- Party
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
| Party | Party boundary | key-inventory-domain-contract.md | Authoritative | Current baseline |
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
| AuditEvent | Audit boundary | key-inventory-domain-contract.md | Authoritative append-only audit evidence | Current baseline |
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
- Required relationships: may have zero or one parent Location; may have zero or more child Locations; may be referenced by zero or more Lock records.
- Cardinalities: one parent Location to zero or more child Locations; one Location to zero or more Lock records.
- Required uniqueness: LocationCode is unique across Location records.
- Required integrity constraints: LocationCode is required; a Location cannot be its own parent; Location hierarchy must not contain cycles; inactive Location must not be used for new Lock assignment.

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
