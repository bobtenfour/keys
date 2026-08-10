# OPERATOR-AUDIT-1 - Operator Accountability Audit Trail

## Status
Accepted

## Parent Phase
Phase 2 — Operational Security

## Purpose
Provide durable operator accountability so the system can answer who performed what business action, when, and which business record was affected, using the existing authenticated KeyInventory user identity.

## Objective
Application owns append-only operational audit emission for authorized business mutations; Infrastructure persists OperatorAuditRecord rows atomically with the mutation on SQL Server; Web exposes a filtered Administration Audit Trail with operator-readable values and optional CSV/XLSX/PDF reuse of existing export formatters.

## Scope
- One append-only OperatorAuditRecord operational audit authority with AuditRecordId, OccurredAtUtc, authenticated Operator reference, ActionType, SubjectType, SubjectReference, and concise structured Details.
- Application-owned audit staging for the listed audited mutations.
- Authenticated operator identity from existing ASP.NET Identity session (UserName); distinct from WorkforceMember subject identity.
- SQL Server persistence via KeyInventoryDbContext + EF migration.
- Read-only Audit Trail query authority with practical filters.
- Administration > Audit Trail UI.
- CSV/XLSX/PDF export of the same Audit Trail result via existing ReportExportTable exporters when compatible.
- Architecture/workflow tests listed in Required Tests.
- Governing-document synchronization for operational audit emission authority.

## Persistence Requirements
- Append-only OperatorAuditRecords table on existing SQL Server KeyInventory database.
- No second database, audit store, event bus, outbox (unless proven necessary), or external logging platform.
- Mutation SaveChanges and staged audit rows commit together on the shared DbContext.
- No Application delete/update path for audit records.

## UI Requirements
- Administration Audit Trail list with Date/Time, Operator, Action, Subject, Details.
- Filters: date range, operator, action, subject/reference where practical.
- Operator-readable action/subject labels; avoid raw internal codes when better display values exist.
- Optional subject deep-link to filtered history where simple and already supported.
- Do not audit page views, searches, report viewing, or exports.

## Out of Scope
- Event sourcing, CQRS frameworks, Kafka/RabbitMQ, workflow/approval/policy engines.
- Second Operator/User identity model or Domain SecurityPrincipal bridge requirement for this slice.
- Domain AuditEvent automatic emission through the AUDIT-1 aggregate (remains foundation; OperatorAuditRecord is the authorized operational trail).
- Supervisor approvals, dual authorization, approval queues.
- Cryptographic signing, blockchain, temporal tables, HTTP/page-view auditing.
- REPORTS-2 or enterprise compliance frameworks.
- Git operations unless explicitly requested.
- Accepted slice history rewrites.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- architecture-contracts.md
- key-inventory-domain-contract.md
- security-capability-contract.md
- system-integrity-contract.md
- product-experience-contract.md
- slice-promotion-governance.md
- documentation/slices/OPERATOR-UX-1.md

## Required Previous Slices
- OPERATOR-UX-1

## Allowed Files
- documentation/slices/OPERATOR-AUDIT-1.md
- documentation/implementation-roadmap.md
- documentation/architecture-contracts.md
- documentation/key-inventory-domain-contract.md
- documentation/security-capability-contract.md
- documentation/product-experience-contract.md
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/**

## Forbidden Files
- documentation/** except Allowed Files governing documents listed above
- Accepted slice history content rewrites
- CI pipeline files
- Docker Compose / Testcontainers / SQLite / EF InMemory introductions

## Authority Owner
Application owns audit requirement and trail query; Infrastructure owns append-only SQL Server persistence and atomic SaveChanges with mutations; Web owns Audit Trail presentation and export download wiring; Domain AuditEvent remains AUDIT-1 foundation aggregate without required use for this operational trail; authenticated operator identity remains ASP.NET Identity UserName.

## Architectural Risks
- Confusing operator identity with WorkforceMember subject identity.
- Best-effort audit after successful mutation.
- Web DbContext audit writes.
- Inventing approval workflows or a second user model.
- Expanding Domain AuditEvent emission without bridging Identity Party requirements.

## Acceptance Criteria
- Authenticated operator captured on audited mutations; WorkforceMember subjects remain distinct.
- Listed Key, Key↔Room, Issue, Return, Workforce, WorkAssignment, Org/Dept/Building/Room/KeyType mutations audited.
- OccurredAtUtc persisted as UTC; records immutable through Application; no delete path.
- Failed mutation does not create successful audit; required audit failure does not leave successful mutation where shared SaveChanges atomicity applies.
- Audit Trail reads SQL Server data; filters work; exports reuse ReportExportTable path when compatible.
- No Web DbContext; no duplicate identity authority.
- Issue/Receive/Lookup/Reporting/Admin maintenance remain valid.
- Build PASS; Tests PASS; runtime verification of create/issue/return/maintain-or-terminate with Audit Trail evidence.
- Human acceptance checkpoint only after Implementation Complete.

## Required Tests
- Operator capture; WorkforceMember not confused with operator.
- Key register; Key-Room add/remove; Issue; Return audited.
- Workforce create/maintain/terminate; WorkAssignment create/end; Org/Dept/Building/Room/KeyType mutations audited.
- OccurredAtUtc; immutability; no Application delete; failed mutation / atomicity cases.
- Audit Trail read/filters; no Web DbContext; no duplicate identity; regressions.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Product experience consistency PASS
- Build PASS
- Tests PASS
- Runtime verification PASS
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After OPERATOR-AUDIT-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-10.
- Evidence: OPERATOR-UX-1 is Accepted; human governance explicitly required durable operator accountability; OPERATOR-AUDIT-1 specifies authenticated-operator append-only trail, atomic Application-owned emission, SQL Server persistence, Audit Trail UI/filters/exports reuse, tests, and human acceptance checkpoint; implementation continues in the same continuous structural execution.
- Deciding authority role: Human Architectural Governance.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-10.
- Evidence: OperatorAuditRecord append-only authority stages authenticated ASP.NET Identity operator evidence with business mutations on shared SQL Server SaveChanges; listed Key/Key↔Room/Issue/Return/Workforce/WorkAssignment/Org/Dept/Building/Room/KeyType mutations audited; Administration Audit Trail with filters and CSV/XLSX/PDF via ReportExportTable; Domain AuditEvent remains AUDIT-1 foundation without Identity Party bridge; architecture/workflow tests PASS; build PASS 0 warnings 0 errors; tests PASS 174/174; Development runtime as user verified Organization created, Key registered, Key issued, Key returned, Workforce Member maintained on Audit Trail with operator=user.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-10.
- Evidence: OPERATOR-AUDIT-1 was Implementation Complete; append-only OperatorAuditRecord operational accountability using the authenticated KeyInventory user identity, Application-owned mutation audit staging with Infrastructure SQL Server atomic persistence, Administration Audit Trail read/filters/exports via existing ReportExportTable exporters, and distinct operator vs WorkforceMember subject identity remained within approved scope; no second Operator/User model, event sourcing, approval/policy engines, Web post-use-case audit writes, second database, or REPORTS-2 were introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
