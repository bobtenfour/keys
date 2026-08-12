# Key Inventory Domain Contract

## Authority
This document is the sole authority for business concepts and aggregate boundaries.

## Purpose
Define the business model without implementation details.

## Product Scope Alignment
This domain model serves a single-site KeyInventory installation (one organization and one building as fixed product scope, not as configurable multi-organization or multi-building entities), a small workforce, employees and regular contractors, rooms and keys, day-to-day issue/return, accountability, and straightforward administration/audit.
Organization and Building are not active Domain business concepts. Do not replace them with Tenant, Site, Facility, Campus, LocationRoot, or another hierarchy abstraction.
Room is the active place concept for duties, justification, and KeyAccessPattern↔Room openings; Campus and enterprise location hierarchies remain out of scope unless a future explicit business requirement adds them.
Workforce Eligibility ensures keys are issued to legitimate active workers and must not become a generalized access-control or key-authorization policy engine.
OPERATOR-EXPERIENCE-1 owns the active single-site simplification authority that supersedes prior Organization, Building, and ResponsibleManager active-model requirements.
KEY-ACCESS-COPY-1 owns the active KEY # / physical-copy authority that supersedes prior KeyAsset↔Room opening ownership and CatalogKeyCode-as-unique-physical-identity semantics.

## Core Business Concepts
- KEY # / KeyAccessPattern: shared access-pattern identity associated with the Room access set; operator-facing KEY #.
- Physical key copy / KeyAsset: one controlled physical key copy under exactly one KEY #; operator-facing MEDECO Key Code unique within that KEY #.
- Key Catalog: authoritative catalog of KEY # access patterns and physical copies.
- KeyAccessPattern-to-Room Assignment: current authoritative association of a KEY # to a Room that every physical copy under that KEY # opens; owned by Key Catalog.
- Key-to-Room Assignment (historical name): superseded active authority; former KeyAsset↔Room openings replaced by KeyAccessPattern↔Room under KEY-ACCESS-COPY-1.
- Location: physical place where a key, lock, or custody action is relevant (foundation hierarchy; not a substitute Organization/Building business model).
- Room: physical room for this installation; operator-facing identity uses RoomNumber unique across all Room records; internal RoomCode remains immutable technical identity.
- Party: persistent person business identity; sole person-identity authority.
- Department: organizational unit for WorkforceMember membership and Department-based key-issue justification; DepartmentCode unique across all Department records.
- WorkforceMember: workforce relationship and key-eligibility record for WorkforceType Employee or Contractor; not person identity.
- WorkforceType: Employee or Contractor.
- WorkAssignment: assignment of a WorkforceMember to a Room where duties are performed; one assignment may be primary.
- Employment: not a separate aggregate; workforce relationship authority belongs solely to WorkforceMember.
- Borrower: a workflow role only; fulfilled by an eligible active WorkforceMember; not a separate aggregate.
- Organization (removed from active model): historical OperatorAuditRecord references may remain; not an active aggregate.
- Building (removed from active model): historical OperatorAuditRecord references may remain; not an active aggregate.
- ResponsibleManager (removed from active model): historical facts/audit may remain; not an active WorkforceMember authority.
- Loan: controlled issuance of a key to a party.
- Return: controlled completion of a loaned key back into organizational control.
- Custody Event: immutable record of possession transfer or custody-relevant change.
- Audit Event: immutable evidence of a business or security-relevant action.
- Lifecycle State: valid derived state of a key, including Available, Reserved, Issued, InTransit, ReturnedPendingInspection, Lost, Maintenance, Disabled, and Destroyed.
- Lifecycle Event: immutable event that supports key lifecycle derivation, including KeyCreated, KeyUpdated, Issued, Returned, CustodyTransferred, Lost, Recovered, and Destroyed.

## Domain Invariants
- A KEY # (KeyAccessPattern) must have one authoritative KeyNumber unique across the installation.
- A physical key copy (KeyAsset) must have one immutable internal KeyAssetId and exactly one parent KEY #.
- MEDECO Key Code must be unique within its KEY # and must not be required to be globally unique.
- All physical copies under one KEY # open exactly the same Room set, derived solely from that KEY #’s Room assignments.
- Possession must be traceable to the physical copy, not merely to the KEY #.
- Loan and return must not create orphan custody.
- Custody transfers must support Party and storage endpoints, including employees, contractors, security personnel, storage locations, and other authorized Party types.
- Audit history must not be rewritten.
- Current state must be derivable from authoritative records when the relevant phase introduces that model.
- Authoritative business timestamps are UTC instants.
- Domain entry points that accept authoritative timestamps require UTC offset zero and must not accept local-time offsets as authoritative business time.
- Required authoritative timestamps must reject `default(DateTimeOffset)`.

## Catalog Contract
### Aggregate Roots
- KeyAccessPattern is the Key Catalog aggregate root for one KEY # / shared access pattern.
- KeyAsset is the Key Catalog aggregate root for one controlled physical key copy under exactly one KeyAccessPattern.
- Lock is the Key Catalog aggregate root for one controlled physical lock.
- Location is the Location boundary aggregate root for one physical organizational place.

### Entities and Classifications
- KeyType is a catalog classification for the physical or operational kind of a KEY # / KeyAccessPattern; physical copies derive KeyType from their parent KeyAccessPattern.
- KeySeries is a non-operational Domain classification seed only. KeySeries must not be KEY # authority, Room-access authority, or physical-copy identity. KEY-ACCESS-COPY-1 must not elevate or silently reinterpret KeySeries.

### KeyAccessPattern
Purpose: define the authoritative shared KEY # / access-pattern identity and the Room access set opened by every physical copy under that KEY #.

Identity: KeyAccessPattern is identified by one KeyNumber (operator-facing KEY #) that is unique across all KeyAccessPattern records for the installation.

Ownership: Key Catalog owns creation, KeyNumber uniqueness, KeyType reference, activation/retirement, and current KeyAccessPattern↔Room assignments.

Invariants:
- A KeyAccessPattern must have a non-empty KeyNumber unique installation-wide.
- A KeyAccessPattern must reference exactly one KeyType.
- A KeyAccessPattern may have zero, one, or multiple physical KeyAsset copies.
- A KeyAccessPattern may have zero, one, or multiple current Room assignments.
- All physical copies under a KeyAccessPattern open exactly the same Room set; Room access must not vary by copy.
- A master/broader-access key is represented only as another KeyAccessPattern whose Room set contains multiple Rooms; no master/sub-master hierarchy engine exists.
- A KeyAccessPattern must not reference an inactive KeyType for new catalog assignment.
- KeyAccessPattern must not own custody, Loan, Return, holder, or possession state.

Allowed lifecycle-neutral behavior:
- Create KEY # access pattern.
- Assign or change KeyType.
- Assign, change, or clear current KeyAccessPattern↔Room assignments.
- Activate or retire the access pattern for future copy registration / issue eligibility as catalog rules require.

Prohibited authority:
- KeyAccessPattern must not store authoritative possession, current custodian, loan state, return state, lifecycle state, audit history, authentication, authorization, policy, or UI state.
- KeyAccessPattern must not use Lock or Location hierarchy as Room-opening authority beyond KeyAccessPattern↔Room.
- KeyAccessPattern must not own Building or any site abstraction independently of Room.

### KeyAsset
Purpose: define one controlled physical key copy that may be issued to a person.

Identity: KeyAsset is identified by one immutable internal KeyAssetId. Operator-facing physical-copy identity is MEDECO Key Code unique within the parent KeyAccessPattern. The operational business pair is (KEY #, MEDECO Key Code). CatalogKeyCode is not the unique physical-copy business identity under KEY-ACCESS-COPY-1. Opaque composite strings (for example `66800-28`) must not be identity authority.

Ownership: Key Catalog owns physical-copy registration, MEDECO uniqueness within KEY #, activation, and retirement. Custody remains outside Key Catalog (Loan/Return).

Invariants:
- A KeyAsset must have a non-empty immutable KeyAssetId.
- A KeyAsset must reference exactly one KeyAccessPattern.
- A KeyAsset must have a non-empty MEDECO Key Code unique among KeyAsset records that share the same KeyAccessPattern.
- MEDECO Key Code may repeat under a different KeyAccessPattern.
- A KeyAsset derives KeyType and Rooms opened solely from its parent KeyAccessPattern.
- A KeyAsset must not own independent Room assignments and must not store a conflicting Room set.
- A retired KeyAsset remains catalog-identifiable and must not be reused as a different physical copy.

Allowed lifecycle-neutral behavior:
- Register physical copy under an existing KEY #.
- Activate or retire catalog availability for future issue.

Prohibited authority:
- KeyAsset must not store authoritative possession, current custodian, loan state, return state, lifecycle state, audit history, maintenance workflow state, policy decision state, authentication data, authorization data, or UI state.
- KeyAsset must not own Room-opening authority, Building, or KeyType as an independent mutable authority.
- KeyAsset must not reference KeySeries as active KEY # or access authority.

### KeyAccessPattern-to-Room Assignment
Purpose: define the current authoritative association between one KEY # / KeyAccessPattern and one Room that every physical copy under that KEY # opens.

Ownership: Key Catalog owns current KeyAccessPattern↔Room assignments. Location boundary owns Room identity (Building is not an active place authority).

Cardinality:
- One KeyAccessPattern may open zero, one, or multiple Rooms.
- One Room may be opened by zero, one, or multiple KeyAccessPatterns.
- The relationship belongs to KeyAccessPattern, not KeyAsset, not KeyType, and not KeySeries.

Authority rules:
- Only current KeyAccessPattern↔Room assignments are authoritative for Rooms opened.
- Physical copies derive Rooms opened exclusively through their parent KeyAccessPattern.
- Former KeyAsset↔Room assignment authority is retired from the active model; dual authority is forbidden.
- Assignment history is not required and must not be invented as a second source of truth.
- Assignments are editable after KEY # creation.
- Lock must not be introduced or required as an intermediate authority.
- Existing Key Catalog, Find Key / operational lookup, and existing REPORTS-1 surfaces consume this authority; this does not authorize REPORTS-2.

Invariants:
- An assignment must reference an existing KeyAccessPattern and an existing Room.
- An active assignment used for operational display must reference an active Room.
- Duplicate active assignment of the same KeyAccessPattern to the same Room is forbidden.

### KeySeries
Purpose (historical/seed only): former optional classification for organizational keying system, pattern, or managed series.

Active authority under KEY-ACCESS-COPY-1:
- KeySeries is not KEY #.
- KeySeries is not Room-access authority.
- KeySeries is not physical-copy identity.
- Implementation must not persist or expose KeySeries as a competing access-pattern authority.

### KeyType
Purpose: classify the physical or operational kind of a KEY # / KeyAccessPattern.

Ownership: Key Catalog owns creation, update, activation, and retirement rules for KeyType.

Classification rules:
- KeyType is catalog reference data.
- KeyType is referenced by KeyAccessPattern; physical KeyAsset copies derive KeyType from the parent KeyAccessPattern.
- KeyType must not own KeyAccessPattern↔Room assignments.
- KeyType must not encode custody, loan, return, lifecycle, maintenance, authorization, or policy state.
- KeyType must not be overloaded with KEY # semantics.

Uniqueness rules:
- KeyType code is unique across all KeyType records.

Invariants:
- A KeyType must have a non-empty type code.
- A KeyType must not be retired while active KeyAccessPattern records require it for new catalog assignment.
- Retiring a KeyType does not retire existing KeyAccessPattern or KeyAsset records.

### Lock
Purpose: optional catalog identity for a controlled physical lock device.

Ownership: Key Catalog owns creation, catalog detail updates, activation, and retirement rules for Lock when Lock is used.

Authority reconciliation:
- Lock is not the operational authority for which Rooms a KEY # or physical KeyAsset opens.
- KeyAccessPattern↔Room Assignment is the sole operational authority for Rooms opened by a KEY # (and therefore by every physical copy under it).
- Lock must not mediate, duplicate, or be required for KeyAccessPattern↔Room assignment.
- Existing KeyAsset→Lock→Location and former KeyAsset↔Room wording is superseded for room-opening authority by KeyAccessPattern↔Room.

Relationships:
- A Lock may reference one Location.
- Lock is not required on KeyAsset for catalog registration or KeyAccessPattern↔Room assignment.

Uniqueness rules:
- Lock code is unique across all Lock records.

Invariants:
- A Lock must have a non-empty lock code.
- A Lock must not reference an inactive Location for new Lock catalog assignment when Location is used.
- Retiring a Lock does not retire related KeyAccessPattern, KeyAsset, or KeyAccessPattern↔Room assignments.
- Lock must not own possession, custody, loan, return, lifecycle, maintenance, audit, authorization, KeyAccessPattern↔Room assignment, or UI authority.

### Location
Purpose: define a physical organizational place where a key, lock, or custody action is relevant.

Ownership: The Location boundary owns creation, update, activation, retirement, and hierarchy rules for Location.

Hierarchy rules:
- A Location may have no parent or exactly one parent Location.
- A Location may have zero or more child Locations.
- A Location must not be its own parent.
- A Location hierarchy must not contain cycles.
- A Location may be retired only when no active child Location requires it for hierarchy assignment.

Uniqueness rules:
- Location code is unique across all Location records.

Activation rules:
- A Location must have a non-empty location code.
- New Lock assignment must reference an active Location.
- Retiring a Location does not retire existing Lock or KeyAsset records.

### Building
Status: removed from the active Domain model by OPERATOR-EXPERIENCE-1.
Building is not an active aggregate, selector, or prerequisite. Historical OperatorAuditRecord rows that reference Building remain immutable and readable. Do not replace Building with Tenant, Site, Facility, Campus, LocationRoot, or another abstraction.

### Room
Purpose: define one physical room for this installation where workforce duties, key-issue justification, and KeyAccessPattern↔Room opening associations occur.

Ownership: Location boundary owns Room creation, activation, retirement, RoomNumber mutation under authorized Application commands, and global RoomNumber uniqueness. Key Catalog owns KeyAccessPattern↔Room assignments that reference Room.

Identity: Room is identified by one room code (RoomCode) that is unique across all Room records and is an immutable technical identity generated by the system. Operators use RoomNumber; they must not be required to invent RoomCode.

Required attributes:
- RoomNumber is required and is the operator-facing room identifier for the installation.

Uniqueness rules:
- RoomNumber is unique across all Room records within KeyInventory.
- RoomCode is unique across all Room records.

Relationships:
- A Room may be opened by zero, one, or multiple KEY # / KeyAccessPattern records through current KeyAccessPattern↔Room assignments.
- A Room does not reference Building.

Invariants:
- Only an active Room may be used for active WorkAssignment, Room-based key-issue justification, or active KeyAccessPattern↔Room assignment.
- Room must not exist outside Location boundary place authority; a second independent place model is forbidden.
- Room must not own Key Catalog identity or KeyAccessPattern↔Room assignment authority.

### Catalog Authority Exclusions
Catalog authority may never store:
- Current possession.
- Current custodian.
- Loan or return workflow state.
- Lifecycle state or lifecycle transition authority.
- Custody events.
- Audit events.
- Maintenance workflow state.
- Inventory count or discrepancy state.
- Authentication credentials.
- Authorization decisions, roles, permissions, or assignments.
- Policy evaluation results.
- Persistence-provider configuration.
- UI behavior or presentation state.

### Future Slice Ownership
- Custody possession and custody transfer authority belong to future custody slices.
- Lifecycle state, lifecycle transitions, and lifecycle event authority belong to future lifecycle slices.
- Loan and return workflow authority belongs to loan/return slices.
- Maintenance workflow authority belongs to future maintenance slices.
- Persistence foundation for KeyType, KeyAsset, Loan, and Return belongs to MIGRATION-1; Application port adapters, workflow DI, and LOAN-VERTICAL-1 UI belong to LOAN-VERTICAL-1.
- UI behavior outside LOAN-VERTICAL-1 belongs to future product experience or UI slices.
- Workforce eligibility single-site authority (Department, Room without Building, WorkforceMember without Organization/ResponsibleManager, WorkAssignment, global RoomNumber) belongs to OPERATOR-EXPERIENCE-1 for active model changes; WORKFORCE-ELIGIBILITY-1 remains historical Accepted foundation.
- Historical runtime KeyAsset↔Room assignment belonged to KEY-ROOM-ASSIGNMENT-1; active Room-opening authority is KeyAccessPattern↔Room under KEY-ACCESS-COPY-1.
- KEY # / physical-copy normalized model runtime belongs to KEY-ACCESS-COPY-1.

## Party and Workforce Eligibility Contract
### Boundary Ownership
- Party boundary owns persistent person business identity, including person FirstName, LastName, and UIN, and Party lifecycle.
- Location boundary owns Room place authority, including global RoomNumber uniqueness. Building is not an active Location business authority.
- Workforce Eligibility boundary owns Department, WorkforceMember as the workforce relationship and eligibility authority, WorkAssignment, key-issue eligibility evaluation, and termination return-obligation signaling. Organization and ResponsibleManager are not active authorities.
- Employment is not a separate aggregate and has no independent authority; relationship periods, eligibility status, Department, WorkforceType, and WorkAssignment belong to WorkforceMember.
- Workforce Eligibility must not own Party identity attributes, Location hierarchy, Loan workflow mutation, Return workflow mutation, custody, lifecycle, audit emission, authentication, authorization runtime, HR integration, or UI.
- Workforce Eligibility must not expand into a generalized access-control engine, key-authorization policy engine, or Campus/enterprise location hierarchy.

### Party
Purpose: persistent business identity for a person or organization participating in custody, authorization, or key workflows.

Ownership: Party boundary is the sole authority for persistent person identity.

Required person-identity attributes for a human Party used as a workforce key recipient:
- FirstName.
- LastName.
- UIN.

UIN rules:
- UIN is exactly nine numeric digits.
- UIN is unique across all Party records that carry UIN.
- UIN is a mutable business identifier corrected through a governed Application Correct Party UIN operation on the same Party (PartyCode remains stable); collision with another Party is rejected; relationships, Loans/Returns, and historical OperatorAuditRecords are preserved; the correction emits new OperatorAuditRecord evidence with old and new UIN and must not rewrite historical audit rows.

Relationships:
- A WorkforceMember must reference exactly one Party.
- A Party may be referenced by zero or more WorkforceMember records over time.
- A Party may have at most one Active WorkforceMember at a time in this workforce scope.
- Loan continues to reference the borrowing Party; Borrower is not a Party subtype and not a separate aggregate.

Prohibited authority:
- Party must not own WorkforceMember Status, Department, WorkforceType, WorkAssignment, key-issue eligibility decisions, Loan/Return workflow, custody, lifecycle, audit, authentication, or UI.

### Organization
Status: removed from the active Domain model by OPERATOR-EXPERIENCE-1.
Organization is not an active aggregate, selector, membership owner, or eligibility prerequisite. Historical OperatorAuditRecord rows that reference Organization remain immutable and readable. Do not replace Organization with Tenant, Site, or another scoping abstraction.

### Department
Purpose: organizational unit used for WorkforceMember membership and key-issue justification within this single-site installation.

Identity: Department is identified by one department code that is unique across all Department records.

Ownership: Workforce Eligibility boundary owns Department creation, activation, and retirement for this scope.

Relationships:
- A Department does not reference Organization.

Invariants:
- A Department must have a non-empty department code.
- Only an active Department may be used for active WorkforceMember membership or Department-based key-issue justification.

### WorkforceMember
Purpose: workforce relationship and key-eligibility record for WorkforceType Employee or Contractor. WorkforceMember is not person identity.

Identity: WorkforceMember is identified by one workforce member code that is unique across all WorkforceMember records.

Ownership: Workforce Eligibility boundary owns WorkforceMember as the sole workforce relationship and eligibility authority.

Required attributes:
- WorkforceType.
- Department.
- Status.
- Reference to exactly one Party.

WorkforceType values:
- Employee.
- Contractor.

Status values:
- Active.
- Terminated.

Relationships:
- A WorkforceMember must reference exactly one Party.
- A WorkforceMember must reference exactly one Department.
- A WorkforceMember may have zero or more WorkAssignment records.
- A WorkforceMember must not require Organization or ResponsibleManager.

Relationship lifecycle rules:
- Termination, rehire, Department change, and Employee or Contractor WorkforceType transition are workforce relationship changes owned by WorkforceMember.
- These relationship changes must not rewrite or replace Party person identity.
- Rehire after termination creates a new Active WorkforceMember for the same Party; it must not reactivate a Terminated WorkforceMember by mutating person identity.
- The first WorkforceMember may be created when no other WorkforceMember exists.

Prohibited authority:
- WorkforceMember must not own FirstName, LastName, UIN, or other Party person-identity attributes.
- WorkforceMember must not own Party lifecycle, Loan/Return mutation, custody, lifecycle, audit emission, authentication credentials, authorization decisions, or UI.
- A Borrower aggregate must not be created; Borrower remains a workflow role fulfilled by an eligible active WorkforceMember.
- A separate Employment aggregate must not be created.
- ResponsibleManager, bootstrap mutual-manager pairs, self-manager, sole-member manager exceptions, and placeholder managers are forbidden as active authority.

### WorkAssignment
Purpose: assign a WorkforceMember to a Room where the member is authorized to work for key-issue justification.

Ownership: Workforce Eligibility boundary owns active and ended WorkAssignment records.

Relationships:
- A WorkAssignment must reference exactly one WorkforceMember.
- A WorkAssignment must reference exactly one Room.
- Referenced Room must be active for an active assignment.
- A WorkforceMember may have multiple Room assignments.
- At most one active WorkAssignment for a WorkforceMember may be marked primary.

Cardinalities:
- one WorkforceMember to zero or more WorkAssignment records.
- one Room to zero or more WorkAssignment records.

### Key Issue Eligibility Rules
A key may be issued to a WorkforceMember only when all of the following are true:
- WorkforceMember Status is Active.
- Referenced Party has valid FirstName, LastName, and UIN.
- UIN satisfies the nine-digit uniqueness rules on Party.
- WorkforceType and Department are present and valid on the WorkforceMember.
- Department is active and matches the WorkforceMember's Department.
- The WorkforceMember has at least one active WorkAssignment (verified retained requirement; Issue Key does not require Organization or ResponsibleManager).
- The key issue is justified only by the Department or by a Room on an active WorkAssignment for that WorkforceMember.
- The borrowing Party on the Loan is the Party referenced by that eligible WorkforceMember.

Prohibited eligibility authority:
- Eligibility evaluation must not create a Borrower aggregate.
- Eligibility evaluation must not mutate Loan, Return, custody, lifecycle, or audit records by itself.
- Eligibility evaluation must not replace Party business-identity authority.
- Eligibility evaluation must not invent temporary borrower fields or duplicate identity authority.

### Termination and Return Obligation
When WorkforceMember Status becomes Terminated for an Employee or Contractor:
- New key issues to that WorkforceMember are forbidden.
- A mandatory return obligation exists for every currently issued Open Loan whose borrowing Party is that WorkforceMember's Party.
- Termination ends the workforce relationship and must not rewrite Party person identity.
- Termination must not automatically mutate Loan, Return, custody, lifecycle, or audit authority.
- Required key returns must complete through the existing Return workflow owned by the Loan and Return aggregates.

## Loan and Return Contract
### Aggregate Roots
- Loan is the Loan aggregate root for one controlled issuance of one cataloged key to one Party.
- Return is the Return aggregate root for completion of one Loan back into organizational control.

### Loan
Purpose: record controlled issuance intent and workflow state for a cataloged key loaned to a Party.

Identity: Loan is identified by one loan code that is unique across all Loan records.

Ownership: The Loan aggregate owns loan creation, issuance workflow state, due date, borrower reference, key reference, and cancellation rules.

Relationships:
- A Loan must reference exactly one physical KeyAsset (KeyAssetId).
- A Loan must not treat KEY # alone as the issued subject; custody is always the physical copy.
- A Loan must reference exactly one borrowing Party.
- A Loan may have zero or one Return.
- Borrower is a workflow role fulfilled by an eligible active WorkforceMember when Workforce Eligibility is in force; Loan still stores the borrowing Party reference only.

Invariants:
- A Loan must have a non-empty loan code.
- A Loan must reference a cataloged physical KeyAsset.
- At most one Open Loan may exist for a given KeyAsset at a time.
- Different KeyAsset copies under the same KEY # may each have an Open Loan simultaneously.
- A Loan must reference a Party borrower without owning Party profile or lifecycle.
- When Workforce Eligibility is in force for issue authorization, the borrowing Party must be the Party of a WorkforceMember that satisfies Key Issue Eligibility Rules at issue time.
- When Workforce Eligibility is in force, issue justification must reference the authorizing Department or Room without transferring ownership of those authorities into Loan.
- A Loan issue timestamp is required.
- A Loan due timestamp is required and must be later than the issue timestamp.
- A Loan may be Open, Returned, or Cancelled.
- A Loan starts Open.
- An Open Loan may be completed by exactly one Return.
- A Returned Loan must not be returned again.
- A Cancelled Loan must not be returned.
- Cancelling a Loan does not create custody authority.
- WorkforceMember termination must not auto-complete, cancel, or rewrite Loan state.

Allowed behavior:
- Create loan issuance intent.
- Mark an Open Loan as Returned when a valid Return completes it.
- Cancel an Open Loan before return.
- Expose whether the Loan is open for return.

Prohibited authority:
- Loan must not own current possession, current custodian, custody transfer history, catalog identity, Party identity, Workforce Eligibility ownership, WorkforceMember termination processing, lifecycle state, lifecycle transition authority, audit history, authentication, authorization, policy, persistence-provider configuration, or UI state.

### Return
Purpose: record controlled completion of a Loan back into organizational control.

Identity: Return is identified by one return code that is unique across all Return records.

Ownership: The Return aggregate owns return completion data for one Loan.

Relationships:
- A Return must reference exactly one Loan.
- A Return must reference the returned KeyAsset through the Loan.
- A Return must reference the returning Party through the Loan.

Invariants:
- A Return must have a non-empty return code.
- A Return must reference an Open Loan.
- WorkforceMember termination return obligations are completed only by creating valid Return records through Return authority; termination itself must not fabricate Return records.
- A Return timestamp is required and must not be earlier than the Loan issue timestamp.
- Exactly one Return may complete a Loan.
- Creating a Return marks the referenced Loan as Returned.
- Return completion must not create orphan loan state.

Allowed behavior:
- Complete an Open Loan.
- Record return timestamp for the completed Loan.

Prohibited authority:
- Return must not own current possession, current custodian, custody transfer history, catalog identity, Party identity, lifecycle state, lifecycle transition authority, audit history, authentication, authorization, policy, persistence-provider configuration, or UI state.

### Loan and Return Authority Exclusions
Loan and Return authority may never store:
- Current possession.
- Current custodian.
- Custody Event authority.
- Key Catalog authority.
- Party profile or Party lifecycle.
- Lifecycle State or Lifecycle Event authority.
- Audit Event authority.
- Maintenance workflow state.
- Inventory count or discrepancy state.
- Authentication credentials.
- Authorization decisions, roles, permissions, or assignments.
- Policy evaluation results.
- Persistence-provider configuration.
- UI behavior or presentation state.

### Loan and Return Future Slice Ownership
- Custody transfer authority remains future custody slice scope.
- Lifecycle event and state derivation remain future lifecycle slice scope.
- Audit Event foundation is owned by the Audit boundary under AUDIT-1.
- OPERATOR-AUDIT-1 authorizes Application-owned append-only `OperatorAuditRecord` emission for authenticated-operator business mutations as operational accountability history; that trail must not reconstruct current Domain state, must not become event sourcing, and must remain distinct from inventing a second Operator identity aggregate.
- Automatic emission of Domain `AuditEvent` aggregates from workflow handlers remains deferred unless a later slice explicitly bridges authenticated operators to SecurityPrincipal without violating Party independence.
- Authorization enforcement remains future authorization slice scope.
- Persistence foundation for Loan and Return belongs to MIGRATION-1; Application port adapters, workflow DI, and LOAN-VERTICAL-1 UI belong to LOAN-VERTICAL-1.
- UI behavior outside LOAN-VERTICAL-1 remains future product experience or UI slice scope.

## Audit Contract
### Aggregate Roots
- AuditEvent is the Audit boundary aggregate root for one immutable evidence record of a business or security-relevant action.

### AuditEvent
Purpose: record immutable evidence that a business or security-relevant action occurred.

Identity: AuditEvent is identified by one audit event code that is unique across all AuditEvent records.

Ownership: The Audit boundary owns creation of immutable AuditEvent evidence and the append-only audit history for those records.

Relationships:
- An AuditEvent must reference exactly one acting SecurityPrincipal without owning Identity, Authentication, or Authorization authority.
- An AuditEvent may reference one Party without owning Party profile or Party lifecycle.
- An AuditEvent may reference one subject KeyAsset without owning Key Catalog authority.
- An AuditEvent may reference one subject Loan without owning Loan workflow authority.
- An AuditEvent may reference one subject Return without owning Return workflow authority.

Invariants:
- An AuditEvent must have a non-empty audit event code.
- An AuditEvent must have a non-empty action type describing the evidenced action.
- An AuditEvent occurred timestamp is required.
- An AuditEvent must reference an acting SecurityPrincipal without owning principal lifecycle, credentials, roles, permissions, or authorization decisions.
- An AuditEvent is immutable after creation.
- Audit history must not be rewritten, replaced, or deleted through AuditEvent authority.
- An AuditEvent must not mutate Loan, Return, Key Catalog, Party, Identity, Authorization, Custody, or Lifecycle state.

Allowed behavior:
- Create immutable audit evidence.
- Expose audit evidence for lookup.

Prohibited authority:
- AuditEvent must not own current possession, current custodian, custody transfer history, catalog identity, Party identity, loan workflow state, return workflow state, lifecycle state, lifecycle transition authority, authentication credentials, authorization decisions, roles, permissions, role assignments, policy evaluation, Digital Trust integrity mechanisms, persistence-provider configuration, or UI state.

### Audit Authority Exclusions
Audit authority may never store:
- Current possession.
- Current custodian.
- Custody Event authority.
- Key Catalog authority.
- Party profile or Party lifecycle.
- Loan issuance or return completion workflow authority.
- Lifecycle State or Lifecycle Event authority.
- Maintenance workflow state.
- Inventory count or discrepancy state.
- Authentication credentials.
- Authorization decisions, roles, permissions, or assignments.
- Policy evaluation results.
- Digital Trust hash chains, signatures, or acceptance methods.
- Persistence-provider configuration.
- UI behavior or presentation state.

### Audit Future Slice Ownership
- Automatic audit emission from command handlers or workflows remains future slice scope.
- Custody transfer authority remains future custody slice scope.
- Lifecycle event and state derivation remain future lifecycle slice scope.
- Authorization enforcement remains future authorization slice scope.
- Digital Trust integrity and non-repudiation mechanisms remain future Digital Trust slice scope.
- AuditEvent physical persistence remains future authorized persistence scope beyond MIGRATION-1.
- UI behavior remains future product experience or UI slice scope.

## Forbidden
This document must not define:
- Database schema.
- EF mappings.
- UI behavior.
- Controller routes.
- Service registrations.
- Package choices.

## Depends On
- product-vision.md

## Depended On By
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- slices
