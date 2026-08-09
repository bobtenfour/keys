# Key Inventory Capability Map

## Authority
This document is the authority for product capabilities.

## Purpose
Define what the product must be able to do, independent of implementation order.

## Product Scope Alignment
Capabilities serve one building, a small workforce, employees and regular contractors, rooms and keys, day-to-day issue/return, accountability, and straightforward administration/audit.
Capabilities must not be invented to support speculative enterprise scale.
Workforce Key Eligibility ensures keys are issued to legitimate active workers; it is not a generalized access-control or key-authorization policy engine.

## Capabilities
- Solution Foundation
- Identity and Access
- Key Catalog
- Loan and Return
- Immutable Audit
- Workforce Key Eligibility
- Building and Room Administration
- Operational Accountability
- Straightforward Audit and History

## Capability Details
- Solution Foundation includes authoritative UTC timestamps for business evidence and workflow times, and SQL Server EF Core persistence for the delivered operational model, without owning local-time display or system clock infrastructure.
- Identity and Access includes the authentication foundation required for application use; it does not authorize a generalized enterprise authorization or policy engine.
- Key Catalog includes controlled physical key identity, type classification, and current Key-to-Room opening assignments for this building's keys; KeyType does not own Room assignments; Building is derived through Room; Lock is not required as intermediate room-opening authority; master/sub-master hierarchy is out of scope.
- Existing Key Catalog, Find Key / operational lookup, and existing REPORTS-1 key surfaces may consume Key-to-Room assignments when a later authorized slice implements them; this does not authorize REPORTS-2 or new report families.
- Loan and Return includes create key asset, issue loan, complete return, and list open and returned loans as a usable operational workflow.
- Immutable Audit includes append-only AuditEvent evidence for business and security-relevant actions, immutable after creation, without rewriting audit history and without owning authentication, authorization, policy, custody, lifecycle, loan workflow, or return workflow authority.
- Workforce Key Eligibility includes Organization, Department, Building, Room with RoomNumber unique within Building, Party as persistent person identity with UIN, WorkforceMember as the workforce relationship and eligibility authority for WorkforceType Employee and Contractor, ResponsibleManager, WorkAssignment to Room, key-issue eligibility, and termination return obligations completed through existing Return authority, without a separate Employment aggregate, Borrower aggregate, temporary borrower fields, duplicate person-identity authority, or HR integration.
- Building and Room Administration exists because Building and Room are real operational concepts for this building; it does not expand into Campus or enterprise location hierarchies.
- Organization supports the real employee/contractor distinction and responsible organization where required; it does not authorize multi-organization enterprise hierarchy design.
- Operational Accountability means the product must make clear who has each key through the delivered loan/return and eligibility model.
- Straightforward Audit and History means operators can review operational evidence without an enterprise analytics platform.

## Out of Capability Scope Unless Explicitly Required Later
- Policy Engine
- Generalized authorization engine
- Workflow engine
- Event platform or event-sourcing readiness program
- Smart cabinet / RFID platform integration
- Electronic access control or smart locks
- Master/sub-master key hierarchy platforms
- Multi-campus or multi-tenant architecture
- Advanced institutional approval frameworks justified only by scale
- Enterprise business-intelligence platform
- REPORTS-2 or new report families beyond existing REPORTS-1 surfaces
- Speculative extensibility frameworks

## Rules
- Capabilities do not define implementation sequence.
- Implementation sequence belongs to implementation-roadmap.md.
- Capability ownership must remain aligned with domain concepts.
- Future capability additions require a concrete operational need for this building.

## Depends On
- key-inventory-domain-contract.md
- roadmap.md
- product-vision.md

## Depended On By
- implementation-roadmap.md
- slices
