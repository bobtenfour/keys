# Key Control System --- Professional Roadmap v2

## Vision

Transform the current key control system into an enterprise-grade
physical asset custody platform with complete traceability, immutable
auditability, and policy-driven security.

------------------------------------------------------------------------

# Phase 1 --- Core Foundation

## Objectives

-   Institutional structure
-   RBAC
-   Key catalog
-   Loan / Return workflow
-   Immutable auditing
-   UTC timestamps
-   Migration strategy
-   CI/CD

**Acceptance criteria**

-   Complete loan/return cycle
-   Full audit trail
-   Zero orphan records
-   All business rules enforced

------------------------------------------------------------------------

# Phase 2 --- Operational Security

## Add

-   Authorization workflows
-   Time restrictions
-   Alerts
-   Notifications
-   Dashboards
-   Reports
-   Inventory verification

**Acceptance criteria**

-   High-risk keys require approval
-   Automatic overdue alerts
-   Operational dashboard available

------------------------------------------------------------------------

# Phase 3 --- Custody Chain

## New Module

### Key Custody

Every possession transfer becomes a permanent event.

Examples

-   Employee → Employee
-   Employee → Contractor
-   Contractor → Security

Never overwrite custody.

Only append events.

**Acceptance criteria**

-   Full chain of custody reconstruction
-   Current custodian always derivable

------------------------------------------------------------------------

# Phase 4 --- State Machine

Replace mutable status with a formal state machine.

States

-   Available
-   Reserved
-   Issued
-   In Transit
-   Returned Pending Inspection
-   Lost
-   Maintenance
-   Disabled
-   Destroyed

Create a transition matrix.

Forbidden transitions must be impossible.

------------------------------------------------------------------------

# Phase 5 --- Event Sourcing

Replace mutable operational history with immutable events.

Core events

-   KeyCreated
-   KeyUpdated
-   Issued
-   Returned
-   CustodyTransferred
-   Lost
-   Recovered
-   Destroyed

Current state is derived from the latest valid event.

No history rewriting.

------------------------------------------------------------------------

# Phase 6 --- Physical Inventory

Modules

-   Inventory Session
-   Inventory Count
-   Inventory Discrepancy
-   Investigation

Support periodic and surprise inventories.

------------------------------------------------------------------------

# Phase 7 --- Maintenance Lifecycle

Modules

-   Maintenance Request
-   Maintenance Execution
-   Cylinder Replacement
-   Rekey
-   Duplicate Creation
-   Retirement
-   Destruction

Maintain complete maintenance history.

------------------------------------------------------------------------

# Phase 8 --- Unified Parties

Replace employee-only ownership.

Root entity

Party

Derived entities

-   Employee
-   Contractor
-   Visitor
-   Vendor
-   External Company

Allows future integrations without schema redesign.

------------------------------------------------------------------------

# Phase 9 --- Smart Cabinet Integration

Support electronic cabinets.

Entities

-   Storage Device
-   Cabinet
-   Slot
-   Locker
-   RFID Cabinet

Future-ready architecture.

------------------------------------------------------------------------

# Phase 10 --- Policy Engine

Replace hardcoded authorization flags.

Examples

IF

Risk \>= Critical

AND

Outside Business Hours

THEN

Require

-   Supervisor
-   Security Officer

Policies become configurable instead of coded.

------------------------------------------------------------------------

# Phase 11 --- Advanced Authorization

Support

-   Dual approval
-   N-of-M approval
-   Emergency override
-   Escalation
-   Expiration

No workflow changes required.

------------------------------------------------------------------------

# Phase 12 --- Digital Trust

Separate

Integrity

-   SHA-256
-   Hash chain

Acceptance

-   Electronic Signature
-   PIN
-   NFC
-   Smart Card
-   Biometrics

Integrity is not authentication.

Authentication is not integrity.

------------------------------------------------------------------------

# Phase 13 --- Business Intelligence

Operational KPIs

-   Active loans
-   Overdue loans
-   Lost keys
-   SLA compliance

Analytical KPIs

-   Most requested keys
-   Heat maps
-   Department utilization
-   Risk trends
-   Custody duration
-   Incident rate
-   Maintenance cost
-   Replacement frequency

------------------------------------------------------------------------

# Phase 14 --- Enterprise Readiness

-   Disaster recovery
-   Backup validation
-   Penetration testing
-   Performance testing
-   HA deployment
-   Monitoring
-   Runbooks
-   Administrator guide
-   User guide

------------------------------------------------------------------------

# Final Architecture

The system evolves from a traditional CRUD application into:

-   Event-driven
-   Immutable by design
-   Policy-based
-   Fully auditable
-   Enterprise scalable
-   Multi-site ready
-   Smart cabinet ready
-   Analytics ready

The single source of truth becomes the event stream. Current state is
always derived, never manually maintained.
