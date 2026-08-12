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
KeyInventory targets a single-site installation (one organization and one building as product scope, not as configurable multi-organization or multi-building business concepts), a small workforce, employees and regular contractors, rooms and keys, day-to-day issue/return, accountability, and straightforward administration/audit.
Capability progression must follow concrete operational need for this installation.
Strategic phases must not invent speculative enterprise architecture such as policy engines, event platforms, multi-campus hierarchies, smart-cabinet platforms, or enterprise readiness programs unless a later explicit business requirement proves they are necessary.

## Completed — Core Operational Foundation
Business objective achieved: establish the foundation for key catalog, loan/return workflow, immutable auditability, UTC timestamps, SQL Server persistence, CI readiness, authentication foundation, and workforce key eligibility for Employee and Contractor recipients.

Acceptance:
- Complete loan/return cycle.
- Full audit trail.
- Zero orphan records.
- Business rules enforced by the owning authority.
- WorkforceMember eligibility foundation delivered under prior slices; active single-site simplification and first-use operator experience are owned by OPERATOR-EXPERIENCE-1 once implemented.

## Selected Next Strategic Capability — Operator Experience Simplification
Human product governance selected OPERATOR-EXPERIENCE-1: remove Organization and Building as active configurable business concepts; remove ResponsibleManager as active workforce authority; make first-use and daily custody intuitive without inventing enterprise abstractions.

## Selected Next Strategic Capability — KEY # Access Pattern and Physical Copies
After OPERATOR-EXPERIENCE-1 acceptance, human product governance selected KEY-ACCESS-COPY-1: distinguish shared KEY # / Room access-pattern authority from physical MEDECO copies under Issue/Return custody, without Transfer, master-key engines, or spreadsheet flattening.
Implementation sequencing authority remains documentation/implementation-roadmap.md.

## Future Capabilities
Further strategic capabilities beyond KEY-ACCESS-COPY-1 are not pre-authorized by this document.
Human product governance selects the next concrete operational capability after KEY-ACCESS-COPY-1 acceptance by asking what the key custodian needs next.
Candidates may include only capabilities that serve this installation's day-to-day operations, such as richer custody accountability, overdue handling, inventory checks, maintenance records, or simple operational reporting, and only when a concrete need is stated.
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
