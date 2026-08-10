# OPERATOR-UX-1 - Operator Issue Key UX and Administration Layout

## Status
Accepted

## Parent Phase
Phase 2 — Operational Security

## Purpose
Correct demonstrated operator usability defects for the key custodian of one approximately five-floor building: Issue Key presenting implementation-oriented codes/UTC strings instead of an operator workflow; Administration stacking form-card walls that waste desktop horizontal space; and Workforce Members requiring multi-card Party/code orchestration instead of a list-first create/maintain flow.

## Objective
Issue Key presents operator-readable Key / Issue to / For / Issued / Due controls backed by existing Application authorities with local date-time at the Web boundary and UTC preserved into Application; Administration uses list-first indexes with dedicated create/detail task pages; Application owns atomic Party + WorkforceMember creation with generated internal codes; Domain eligibility and identifier immutability remain unchanged.

## Scope
- Issue Key form presentation redesign around operator concepts (Key, Issue to, For, Issued, Due).
- Workforce member selector: human-readable `FirstName LastName — UIN {uin}`; submit WorkforceMemberCode internally; do not require WorkforceMemberCode/PartyCode as normal operator input.
- Key selector: catalog key identity with Key↔Room context when assignments exist.
- Justification: Department/Room kind with selectors populated from authoritative eligible Department and active WorkAssignment Room data already available through Application list/query authorities; Domain eligibility remains authoritative on submit; Web must not reimplement KeyIssueEligibility.
- Date/time: local operator-facing controls; convert to zero-offset UTC only at the Web→Application boundary; no second time authority.
- Loan code / former Issue reference: automatic generation and removal from normal operator input are BLOCKED — existing contracts require a non-empty unique loan code but neither authorize generation nor explicitly mandate human entry semantics for generation substitution. Retain required operator entry using Domain/product language **Loan code** until human governance authorizes generation.
- Application atomic Create Workforce Member: one transaction creates Party identity + Active WorkforceMember from FirstName, LastName, UIN, WorkforceType, Organization, Department, ResponsibleManager; Web calls only that Application authority.
- Generated opaque immutable identifiers: `PartyCode = PARTY-{GUID}`, `WorkforceMemberCode = WM-{GUID}`; not operator-entered; not primary UI columns.
- Workforce Members list-first index, dedicated Add page, dedicated Detail/Edit page with ADMIN-MAINTENANCE-1 fields, related Work Assignments / issued keys, and Terminate with confirmation on detail only.
- Align Organizations, Departments, Buildings, Rooms, and Work Assignments to the same list-first + dedicated Add pattern using already-authorized operations; Key Types already list-first remains unchanged.
- Architecture and workflow tests listed in Required Tests.

## Persistence Requirements
- No new business entities or schema redesign.
- Thin read exposure of existing Party rows for selector/list display is allowed.
- Enriching workforce member list items with Party FirstName/LastName/UIN through existing persistence joins is allowed.
- Atomic Party + WorkforceMember persistence uses one SQL Server transaction (all-or-nothing).
- No Web DbContext access.

## UI Requirements
- Issue Key hierarchy: Key, Issue to, For, Issued, Due, then Loan code (blocked-generation retained field), then Issue Key / Cancel.
- No Notes field (not authorized).
- Workforce Members: index = header + Add + search/filter + full-width list; Add = dedicated coherent form without PartyCode/WorkforceMemberCode inputs; Detail = identity header + compact maintenance + related operational information + separated Terminate.
- Other Administration create surfaces: list-first index + dedicated Add page where create is already authorized.
- Use cards/panels only for meaningful grouping; use available desktop width; preserve responsive/mobile behavior.

## Out of Scope
- Domain business rule changes beyond recording identifier-generation and atomic-create authority already decided by human governance.
- New business capabilities or entities.
- Automatic loan-code generation (BLOCKED pending authority).
- Entire-application redesign or cosmetic churn.
- Generic form/CRUD frameworks or JavaScript frameworks.
- Enterprise design system.
- Duplicated eligibility or identity authority in Web.
- Receive Key redesign beyond what regression requires.
- Speculative lookup infrastructure beyond exposing existing Party/Org/Dept/Member/Room list authorities.
- Sequences, counters, configurable code-generation engines, database-specific generators, or user-configurable code formats for Party/WorkforceMember identifiers.
- REPORTS-2 or unrelated operational slices.
- Git operations unless explicitly requested.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- product-experience-contract.md
- architecture-contracts.md
- system-integrity-contract.md
- key-inventory-domain-contract.md
- slice-promotion-governance.md
- documentation/slices/REPORT-EXPORTS-1.md

## Required Previous Slices
- REPORT-EXPORTS-1

## Allowed Files
- documentation/slices/OPERATOR-UX-1.md
- documentation/implementation-roadmap.md
- documentation/product-experience-contract.md
- documentation/architecture-contracts.md
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/**

## Forbidden Files
- documentation/** except Allowed Files governing documents listed above
- Accepted slice history content rewrites
- Domain business-rule changes in KeyInventory.Domain except if a governing contradiction forces STOP
- CI pipeline files
- Docker Compose / Testcontainers / SQLite / EF InMemory introductions

## Authority Owner
Application remains owner of issue eligibility, workforce, catalog, list authorities, atomic Party+WorkforceMember creation, and PartyCode/WorkforceMemberCode generation; Web owns presentation, local date-time conversion at the boundary, and selector orchestration from Application results; Domain remains owner of UTC validation and KeyIssueEligibility; Party and WorkforceMember Domain ownership remain distinct.

## Architectural Risks
- Inventing loan-code generation without authority.
- Duplicating KeyIssueEligibility in Web.
- Letting local civil time become a second authoritative time model.
- Web DbContext for selector data.
- Web orchestration of separate Party and WorkforceMember writes.
- Partial Party persistence when WorkforceMember creation fails.
- Introducing sequence/engine-based identifier generation.

## Acceptance Criteria
- Issue Workforce selector uses human-readable Party identity; internal codes are not normal Issue input.
- Justification choices come from authoritative eligible Department/Room data; Web does not duplicate eligibility.
- Raw UTC ISO strings are not the normal Issue UI; UTC authority remains preserved through submission.
- Loan code remains required operator input with Domain/product language; generation/removal BLOCKED and recorded.
- Application provides atomic Party + WorkforceMember create; Web calls only that authority.
- PartyCode and WorkforceMemberCode are generated as `PARTY-{GUID}` / `WM-{GUID}`, unique, non-empty, immutable after create, and not normal operator inputs.
- Workforce Members index is list-first with human-readable columns; Add and Detail/Edit are dedicated routes; Terminate is on detail with confirmation; reactivation remains forbidden.
- Member issued-key path remains valid.
- Other Administration surfaces with the form-wall defect are aligned to list-first + dedicated Add where already authorized.
- No Web DbContext.
- Issue/Receive, workforce eligibility, lookup, and reporting remain valid.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Local runtime verification of Workforce Members index/Add/Detail/Terminate, desktop width, and narrow viewport.
- Human acceptance checkpoint only after Implementation Complete.

## Required Tests
- Atomic Party + WorkforceMember creation; rollback/no partial Party when member persistence fails.
- Generated PartyCode and WorkforceMemberCode unique and non-empty; codes are not normal operator inputs.
- Workforce Members index list-first; Add dedicated route; Detail/Edit dedicated route; human-readable identity; termination from detail; terminated reactivation forbidden; issued-key path valid.
- Issue Workforce selector human-readable identity; justification from authoritative data; local UTC boundary; no Web DbContext.
- Issue/Receive, workforce eligibility, lookup, and reporting regression.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Product experience consistency PASS
- System integrity / UTC boundary PASS
- Build PASS
- Tests PASS
- Runtime verification PASS
- Loan-code generation remains BLOCKED pending authority
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After OPERATOR-UX-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-09.
- Evidence: REPORT-EXPORTS-1 is Accepted; human governance authorized focused operator-UX correction from runtime visual validation; Issue Key and Administration layout/selectors inspected; loan-code automatic generation recorded BLOCKED (no authorized generation semantics); slice specifies Issue operator workflow, shared responsive admin grid, selector readability from existing authorities, UTC boundary preservation, tests, and human acceptance checkpoint; implementation continues in the same continuous structural execution.
- Deciding authority role: Human Architectural Governance.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-09.
- Evidence: Human governance resolved atomic create and identifier-generation blockers; Application `IRegisterWorkforceMemberUseCase` / bootstrap register create Party + Active WorkforceMember in one SQL Server transaction with generated `PARTY-{GUID}` / `WM-{GUID}` codes; Web Workforce Members uses list-first Index, dedicated Add, dedicated Details with ADMIN-MAINTENANCE-1 maintenance, Work Assignments/Rooms, issued keys, and Terminate confirmation; Organizations/Departments/Buildings/Rooms/Work Assignments aligned to list-first + dedicated Add; Issue Key operator UX preserved with Loan code still operator-entered; architecture/workflow tests cover atomicity, rollback, generated codes, routes, termination, and regressions; build PASS 0 warnings 0 errors; tests PASS 169/169; runtime on http://localhost:7161 verified Workforce Members index (full-width list, no PartyCode/WM columns), Add (no code inputs), Details (identity header + Terminate confirmation), Organizations list-first; desktop shell ≈1152px; narrow 390px no page overflow with horizontal table scroll inside table-shell.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-10.
- Evidence: OPERATOR-UX-1 was Implementation Complete; Issue Key operator workflow with human-readable Issue to identity, Key↔Room context, Department/Room justification selectors from Application authorities without Web eligibility duplication, local date-time controls with UTC preserved at the Web→Application boundary, Loan code retained with generation BLOCKED, atomic Application Party + WorkforceMember registration with generated `PARTY-{GUID}` / `WM-{GUID}` codes, Workforce Members list-first Index with dedicated Add and Detail/Edit including Terminate confirmation on detail, and Organizations/Departments/Buildings/Rooms/Work Assignments list-first + dedicated Add alignment remained within approved scope; no Domain ownership merge, Web DbContext, Web write orchestration, generic CRUD/code-generation framework, loan-code generation, or new business entities were introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
