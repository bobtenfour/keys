# Key Inventory Domain Contract

## Authority
This document is the sole authority for business concepts and aggregate boundaries.

## Purpose
Define the business model without implementation details.

## Core Business Concepts
- Key: controlled physical key asset.
- Key Catalog: authoritative list of controlled keys.
- Location: physical organizational place where a key, lock, or custody action is relevant.
- Party: person or organization participating in custody or authorization.
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
- Persistence foundation for KeyType, KeyAsset, Loan, and Return belongs to MIGRATION-1; Application port adapters and workflow DI belong to LOAN-VERTICAL-1.
- UI behavior belongs to future product experience or UI slices.

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

Invariants:
- A Loan must have a non-empty loan code.
- A Loan must reference a cataloged KeyAsset.
- A Loan must reference a Party borrower without owning Party profile or lifecycle.
- A Loan issue timestamp is required.
- A Loan due timestamp is required and must be later than the issue timestamp.
- A Loan may be Open, Returned, or Cancelled.
- A Loan starts Open.
- An Open Loan may be completed by exactly one Return.
- A Returned Loan must not be returned again.
- A Cancelled Loan must not be returned.
- Cancelling a Loan does not create custody authority.

Allowed behavior:
- Create loan issuance intent.
- Mark an Open Loan as Returned when a valid Return completes it.
- Cancel an Open Loan before return.
- Expose whether the Loan is open for return.

Prohibited authority:
- Loan must not own current possession, current custodian, custody transfer history, catalog identity, Party identity, lifecycle state, lifecycle transition authority, audit history, authentication, authorization, policy, persistence-provider configuration, or UI state.

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
- Persistence foundation for Loan and Return belongs to MIGRATION-1; Application port adapters and workflow DI belong to LOAN-VERTICAL-1.
- UI behavior remains future product experience or UI slice scope.

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
