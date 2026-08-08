# Strategic Roadmap

## Authority
This document is the strategic product evolution authority.

## Purpose
Define product phases and business capability progression. It does not govern implementation sequence.

## Rules
This document may define:
- Product phases.
- Business objectives.
- Phase-level acceptance.

This document must not define:
- Implementation slices.
- File changes.
- Project structure.
- Prompts.
- Technical tasks.

## Product Scope Alignment
KeyInventory targets one building, a small workforce, employees and regular contractors, rooms and keys, day-to-day issue/return, accountability, and straightforward administration/audit.
Capability progression must follow concrete operational need for this building.
Strategic phases must not invent speculative enterprise architecture such as policy engines, event platforms, multi-campus hierarchies, smart-cabinet platforms, or enterprise readiness programs unless a later explicit business requirement proves they are necessary.

## Completed — Core Operational Foundation
Business objective achieved: establish the foundation for key catalog, loan/return workflow, immutable auditability, UTC timestamps, SQL Server persistence, CI readiness, authentication foundation, and workforce key eligibility for Employee and Contractor recipients in this building.

Acceptance:
- Complete loan/return cycle.
- Full audit trail.
- Zero orphan records.
- Business rules enforced by the owning authority.
- WorkforceMember eligibility authority is defined for Organization, Department, Building, Room, ResponsibleManager, WorkAssignment, and key-issue eligibility without duplicating Party identity.
- Organization exists only to support the real employee/contractor distinction and responsible organization where required.
- Building and Room exist as real operational place concepts for this building, not as a Campus or enterprise location hierarchy.

## Future Capabilities
Future strategic capabilities are not pre-authorized by this document.
Human product governance selects the next concrete operational capability after asking what the key custodian needs next.
Candidates may include only capabilities that serve this building's day-to-day operations, such as richer custody accountability, overdue handling, inventory checks, maintenance records, or simple operational reporting, and only when a concrete need is stated.
The following are not strategic commitments and must not drive design:
- Policy engines or generalized authorization engines.
- Workflow engines or event platforms.
- Event-sourcing readiness programs.
- Smart cabinet / RFID platform preparation.
- Multi-campus, multi-building-enterprise, multi-tenant, or large-scale distribution architecture.
- Advanced multi-party approval frameworks justified only by institutional scale.
- Enterprise business-intelligence platforms.
- Speculative extensibility frameworks.

## Depends On
- product-vision.md

## Depended On By
- implementation-roadmap.md
