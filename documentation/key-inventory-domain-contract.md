# Key Inventory Domain Contract

## Authority
This document is the sole authority for business concepts and aggregate boundaries.

## Purpose
Define the business model without implementation details.

## Product Scope Alignment
This domain model serves one building, a small workforce, employees and regular contractors, rooms and keys, day-to-day issue/return, accountability, and straightforward administration/audit.
Organization exists only to support the real employee/contractor distinction and responsible organization where required.
Building and Room are real operational place concepts; Campus and enterprise location hierarchies are out of scope unless a future explicit business requirement adds them.
Workforce Eligibility ensures keys are issued to legitimate active workers and must not become a generalized access-control or key-authorization policy engine.

## Core Business Concepts
- Key: controlled physical key asset.
- Key Catalog: authoritative list of controlled keys.
- Location: physical place where a key, lock, or custody action is relevant.
- Building: physical building place owned by Location boundary.
- Room: physical room within one Building; operator-facing identity uses RoomNumber unique within that Building.
- Party: persistent person or organization business identity; sole person-identity authority.
- Organization: organization that owns Departments and workforce membership for key eligibility.
- Department: organizational unit within one Organization.
- WorkforceMember: workforce relationship and key-eligibility record for WorkforceType Employee or Contractor; not person identity.
- WorkforceType: Employee or Contractor.
- ResponsibleManager: reference from a WorkforceMember to another active authorized WorkforceMember.
- WorkAssignment: assignment of a WorkforceMember to a Room where duties are performed; one assignment may be primary.
- Employment: not a separate aggregate; workforce relationship authority belongs solely to WorkforceMember.
- Borrower: a workflow role only; fulfilled by an eligible active WorkforceMember; not a separate aggregate.
- Loan: controlled issuance of a key to a party.
- Return: controlled completion of a loaned key back into organizational control.
- Custody Event: immutable record of possession transfer or custody-relevant change.
- Audit Event: immutable evidence of a business or security-relevant action.
- Lifecycle State: valid derived state of a key, including Available, Reserved, Issued, InTransit, ReturnedPendingInspection, Lost, Maintenance, Disabled, and Destroyed.
- Lifecycle Event: immutable event that supports key lifecycle derivation, including KeyCreated, KeyUpdated, Issued, Returned, CustodyTransferred, Lost, Recovered, and Destroyed.

## Domain Invariants
- A key must have one authoritative catalog identity.
- Possession must be traceable.
- Loan and return must not create orphan custody.
- Custody transfers must support Party and storage endpoints, including employees, contractors, security personnel, storage locations, and other authorized Party types.
- Audit history must not be rewritten.
- Current state must be derivable from authoritative records when the relevant phase introduces that model.
- Authoritative business timestamps are UTC instants.
- Domain entry points that accept authoritative timestamps require UTC offset zero and must not accept local-time offsets as authoritative business time.
- Required authoritative timestamps must reject `default(DateTimeOffset)`.

## Catalog Contract
### Aggregate Roots
- KeyAsset is the Key Catalog aggregate root for one controlled physical key asset.
- Lock is the Key Catalog aggregate root for one controlled physical lock.
- Location is the Location boundary aggregate root for one physical organizational place.

### Entities and Classifications
- KeySeries is a catalog classification for grouping KeyAsset records that share an organizational keying system, pattern, or managed series.
- KeyType is a catalog classification for the physical or operational type of a KeyAsset.

### KeyAsset
Purpose: define the authoritative catalog identity of one controlled physical key asset.

Identity: KeyAsset is identified by one catalog key code that is unique across all KeyAsset records.

Ownership: Key Catalog owns creation, catalog detail updates, activation, and retirement rules for KeyAsset.

Invariants:
- A KeyAsset must have a non-empty catalog key code.
- A KeyAsset must reference exactly one KeyType.
- A KeyAsset may reference one KeySeries.
- A KeyAsset may reference one Lock that it is intended to operate.
- A KeyAsset must not reference an inactive KeyType, inactive KeySeries, inactive Lock, or inactive Location for new catalog assignment.
- A retired KeyAsset remains catalog-identifiable and must not be reused as a different physical key.

Allowed lifecycle-neutral behavior:
- Create catalog identity.
- Update catalog descriptive attributes.
- Assign or change KeyType, KeySeries, and intended Lock references.
- Activate or retire catalog availability for future use.

Prohibited authority:
- KeyAsset must not store authoritative possession, current custodian, loan state, return state, lifecycle state, audit history, maintenance workflow state, policy decision state, authentication data, authorization data, or UI state.

### KeySeries
Purpose: group catalog keys that share an organizational keying system, pattern, or managed series.

Ownership: Key Catalog owns creation, update, activation, and retirement rules for KeySeries.

Relationships: KeySeries may classify zero or more KeyAsset records.

Uniqueness rules:
- KeySeries code is unique across all KeySeries records.

Invariants:
- A KeySeries must have a non-empty series code.
- A KeySeries must not be retired while active KeyAsset records reference it for new catalog assignment.
- Retiring a KeySeries does not retire existing KeyAsset records.

### KeyType
Purpose: classify the physical or operational kind of a KeyAsset.

Ownership: Key Catalog owns creation, update, activation, and retirement rules for KeyType.

Classification rules:
- KeyType is catalog reference data.
- KeyType classifies zero or more KeyAsset records.
- KeyType must not encode custody, loan, return, lifecycle, maintenance, authorization, or policy state.

Uniqueness rules:
- KeyType code is unique across all KeyType records.

Invariants:
- A KeyType must have a non-empty type code.
- A KeyType must not be retired while active KeyAsset records require it for new catalog assignment.
- Retiring a KeyType does not retire existing KeyAsset records.

### Lock
Purpose: define a controlled physical lock that may be operated by cataloged keys.

Ownership: Key Catalog owns creation, catalog detail updates, activation, and retirement rules for Lock.

Relationships:
- A Lock must reference one Location.
- A Lock may be referenced by zero or more KeyAsset records.

Uniqueness rules:
- Lock code is unique across all Lock records.

Invariants:
- A Lock must have a non-empty lock code.
- A Lock must not reference an inactive Location for new catalog assignment.
- Retiring a Lock does not retire related KeyAsset records.
- Lock must not own possession, custody, loan, return, lifecycle, maintenance, audit, authorization, or UI authority.

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
Purpose: define one physical building place used to contain Rooms.

Ownership: Location boundary owns Building creation, activation, and retirement.

Identity: Building is identified by one building code that is unique across all Building records.

Invariants:
- A Building must have a non-empty building code.
- Only an active Building may contain active Rooms used for WorkAssignment or Room-based key-issue justification.

### Room
Purpose: define one physical room within a Building where workforce duties and key-issue justification occur.

Ownership: Location boundary owns Room creation, activation, retirement, and RoomNumber uniqueness within Building.

Identity: Room is identified by one room code that is unique across all Room records.

Required attributes:
- RoomNumber is required and is the operator-facing room identifier within its Building.
- Room must reference exactly one Building.

Uniqueness rules:
- RoomNumber is unique within one Building.
- The same RoomNumber may exist in different Buildings.

Invariants:
- Only an active Room in an active Building may be used for active WorkAssignment or Room-based key-issue justification.
- Room must not exist outside Location boundary place authority; a second independent place model is forbidden.

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
- Workforce eligibility, Organization, Department, WorkforceMember, ResponsibleManager, WorkAssignment, Building, and RoomNumber authority alignments belong to WORKFORCE-ELIGIBILITY-1 and later authorized slices.

## Party and Workforce Eligibility Contract
### Boundary Ownership
- Party boundary owns persistent person and organization business identity, including person FirstName, LastName, and UIN, and Party lifecycle.
- Location boundary owns Building and Room place authority, including RoomNumber uniqueness within Building.
- Workforce Eligibility boundary owns Organization, Department, WorkforceMember as the workforce relationship and eligibility authority, ResponsibleManager relationship rules, WorkAssignment, key-issue eligibility evaluation, and termination return-obligation signaling.
- Employment is not a separate aggregate and has no independent authority; relationship periods, eligibility status, Organization, Department, WorkforceType, ResponsibleManager, and WorkAssignment belong to WorkforceMember.
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

Relationships:
- A WorkforceMember must reference exactly one Party.
- A Party may be referenced by zero or more WorkforceMember records over time.
- A Party may have at most one Active WorkforceMember at a time in this workforce scope.
- Loan continues to reference the borrowing Party; Borrower is not a Party subtype and not a separate aggregate.

Prohibited authority:
- Party must not own WorkforceMember Status, Organization, Department, WorkforceType, ResponsibleManager hierarchy, WorkAssignment, key-issue eligibility decisions, Loan/Return workflow, custody, lifecycle, audit, authentication, or UI.

### Organization
Purpose: organization that owns Departments and workforce membership for key eligibility; not an enterprise multi-organization hierarchy authority.

Identity: Organization is identified by one organization code that is unique across all Organization records.

Ownership: Workforce Eligibility boundary owns Organization creation, activation, and retirement for this scope.

Invariants:
- An Organization must have a non-empty organization code.
- Only an active Organization may own active Departments or active WorkforceMember membership.

### Department
Purpose: organizational unit within one Organization used for WorkforceMember membership and key-issue justification.

Identity: Department is identified by one department code that is unique within its Organization.

Ownership: Workforce Eligibility boundary owns Department creation, activation, and retirement for this scope.

Relationships:
- A Department must reference exactly one Organization.

Invariants:
- A Department must have a non-empty department code.
- Only an active Department in an active Organization may be used for active WorkforceMember membership or Department-based key-issue justification.

### WorkforceMember
Purpose: workforce relationship and key-eligibility record for WorkforceType Employee or Contractor. WorkforceMember is not person identity.

Identity: WorkforceMember is identified by one workforce member code that is unique across all WorkforceMember records.

Ownership: Workforce Eligibility boundary owns WorkforceMember as the sole workforce relationship and eligibility authority.

Required attributes:
- WorkforceType.
- Organization.
- Department.
- ResponsibleManager.
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
- A WorkforceMember must reference exactly one Organization.
- A WorkforceMember must reference exactly one Department belonging to that Organization.
- A WorkforceMember must reference exactly one ResponsibleManager WorkforceMember.
- A WorkforceMember may have zero or more WorkAssignment records.
- ResponsibleManager must reference a different WorkforceMember from the subject WorkforceMember.
- ResponsibleManager must reference an active authorized WorkforceMember.

Relationship lifecycle rules:
- Termination, rehire, Department change, Organization change, and Employee or Contractor WorkforceType transition are workforce relationship changes owned by WorkforceMember.
- These relationship changes must not rewrite or replace Party person identity.
- Rehire after termination creates a new Active WorkforceMember for the same Party; it must not reactivate a Terminated WorkforceMember by mutating person identity.

Prohibited authority:
- WorkforceMember must not own FirstName, LastName, UIN, or other Party person-identity attributes.
- WorkforceMember must not own Party lifecycle, Loan/Return mutation, custody, lifecycle, audit emission, authentication credentials, authorization decisions, or UI.
- A Borrower aggregate must not be created; Borrower remains a workflow role fulfilled by an eligible active WorkforceMember.
- A separate Employment aggregate must not be created.

### WorkAssignment
Purpose: assign a WorkforceMember to a Room where the member is authorized to work for key-issue justification.

Ownership: Workforce Eligibility boundary owns active and ended WorkAssignment records.

Relationships:
- A WorkAssignment must reference exactly one WorkforceMember.
- A WorkAssignment must reference exactly one Room.
- Referenced Room must be active and belong to an active Building for an active assignment.
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
- WorkforceType, Organization, Department, and ResponsibleManager are present and valid on the WorkforceMember.
- Department is active and belongs to the WorkforceMember's Organization.
- ResponsibleManager references another active authorized WorkforceMember.
- The WorkforceMember has at least one active WorkAssignment to a Room relevant to the key being issued.
- The key issue is justified only by the Department or Room where the WorkforceMember is authorized to work.
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
- A Loan must reference exactly one KeyAsset catalog identity.
- A Loan must reference exactly one borrowing Party.
- A Loan may have zero or one Return.
- Borrower is a workflow role fulfilled by an eligible active WorkforceMember when Workforce Eligibility is in force; Loan still stores the borrowing Party reference only.

Invariants:
- A Loan must have a non-empty loan code.
- A Loan must reference a cataloged KeyAsset.
- A Loan must reference a Party borrower without owning Party profile or lifecycle.
- When Workforce Eligibility is in force for issue authorization, the borrowing Party must be the Party of a WorkforceMember that satisfies Key Issue Eligibility Rules at issue time.
- When Workforce Eligibility is in force, issue justification must reference the authorizing Department or Room and the ResponsibleManager without transferring ownership of those authorities into Loan.
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
- Audit Event foundation is owned by the Audit boundary under AUDIT-1; automatic audit emission from loan or return workflow handlers remains future slice scope.
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
