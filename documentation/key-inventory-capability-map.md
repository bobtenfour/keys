# Key Inventory Capability Map

## Authority
This document is the authority for product capabilities.

## Purpose
Define what the product must be able to do, independent of implementation order.

## Product Scope Alignment
Capabilities serve a single-site installation, a small workforce, employees and regular contractors, rooms and keys, day-to-day issue/return, accountability, and straightforward administration/audit.
Capabilities must not be invented to support speculative enterprise scale.
Workforce Key Eligibility ensures keys are issued to legitimate active workers; it is not a generalized access-control or key-authorization policy engine.

## Capabilities
- Solution Foundation
- Identity and Access
- Key Catalog
- Loan and Return
- Immutable Audit
- Workforce Key Eligibility
- Room Administration
- Department Administration
- Operator Experience and First-Use Guidance
- Operational Accountability
- Straightforward Audit and History

## Capability Details
- Solution Foundation includes authoritative UTC timestamps for business evidence and workflow times, and SQL Server EF Core persistence for the delivered operational model, without owning local-time display or system clock infrastructure.
- Identity and Access includes the authentication foundation required for application use; Role identity is installation-scoped without OrganizationCode business scoping; it does not authorize a generalized enterprise authorization or policy engine.
- Key Catalog includes KEY # / KeyAccessPattern (shared access-pattern identity), KeyType classification at KEY # level, physical KeyAsset copies identified by MEDECO Key Code unique within KEY #, and current KeyAccessPattern↔Room opening assignments; physical copies derive Rooms from their KEY #; KeyType does not own Room assignments; Lock is not required as intermediate room-opening authority; KeySeries is not KEY # or Room-access authority; master/sub-master hierarchy is out of scope (a master key is a KEY # with multiple Rooms).
- Existing Key Catalog, Find Key / operational lookup, and existing REPORTS-1 key surfaces consume KeyAccessPattern↔Room authority and distinguish KEY # from MEDECO/physical copy; this does not authorize REPORTS-2 or new report families.
- Loan and Return includes register physical copies under a KEY #, issue loan against a physical copy, complete return of that copy, and list open and returned loans as a usable operational workflow; custody never moves to KEY # alone.
- Immutable Audit includes append-only AuditEvent evidence for business and security-relevant actions, immutable after creation, without rewriting audit history and without owning authentication, authorization, policy, custody, lifecycle, loan workflow, or return workflow authority.
- Workforce Key Eligibility includes Department with global DepartmentCode, Room with global RoomNumber, Party as persistent person identity with UIN, WorkforceMember as the workforce relationship and eligibility authority for WorkforceType Employee and Contractor, WorkAssignment to Room, key-issue eligibility (including at least one active WorkAssignment), and termination return obligations completed through existing Return authority, without Organization, Building, ResponsibleManager, Employment aggregate, Borrower aggregate, temporary borrower fields, duplicate person-identity authority, or HR integration.
- Room Administration exists because Room is the real operational place concept for this installation; it does not expand into Building, Campus, or enterprise location hierarchies.
- Department Administration exists for workforce membership and Department-based issue justification without Organization ownership.
- Operator Experience and First-Use Guidance (OPERATOR-EXPERIENCE-1) makes prerequisites intelligible in-product, guides operators to the first legitimate Issue Key, presents human-readable dates, authorized record corrections, clean post-create forms, and a matching User Guide—without workflow engines or wizard frameworks.
- Organization and Building are not active product capabilities.
- Operational Accountability means the product must make clear who has each key through the delivered loan/return and eligibility model.
- Straightforward Audit and History means operators can review operational evidence without an enterprise analytics platform.
- Existing REPORTS-1 tabular reports support operator download as CSV, XLSX, and PDF of the same filtered authoritative result set shown on screen; this does not authorize REPORTS-2, new report families, or an enterprise BI platform.

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
