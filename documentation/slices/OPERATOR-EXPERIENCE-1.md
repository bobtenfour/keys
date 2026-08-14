# OPERATOR-EXPERIENCE-1 - Single-Site Structural Simplification and First-Use Operator Experience

## Status
Accepted

## Parent Phase
Later Phases — Operator Experience Simplification

## Preparation Record
- Decision: Prepare Next Slice OPERATOR-EXPERIENCE-1
- Date: 2026-08-11
- Authority: Human architectural governance (explicit Prepare Next Slice instruction)
- Evidence: Human product decisions finalize single-site removal of Organization and Building as active business concepts; global Department and RoomNumber uniqueness; removal of ResponsibleManager as active WorkforceMember authority; Identity Role OrganizationCode scoping removal where it exists solely for Organization business authority; first-use self-guiding UX; mutability; human-readable dates; post-create lifecycle; User Guide contract; empty-database acceptance. Roadmap exits STOP after Accepted OPERATOR-AUDIT-1.

## Purpose
Make existing KeyInventory capability intuitive and operationally fluid by removing artificial multi-organization/multi-building and ResponsibleManager prerequisites, and by guiding first-time operators to the first legitimate Issue Key without expanding product scope into new enterprise capabilities.

## Objective
From an empty legitimate KeyInventory business database, an authenticated operator can understand and complete first-time setup through Application/Domain-owned readiness, create the first WorkforceMember without another WorkforceMember, reach and succeed at Issue Key without Organization, Building, ResponsibleManager, bootstrap pairs, or fake personnel, correct authorized mutable fields with OperatorAuditRecord evidence, see human-readable dates, experience clean post-create forms, and use a User Guide that matches the same dependency model as the UI.

## Scope
- Remove Organization and Building as active Domain/Application/Persistence/Web business authorities (not hide, default, or replace with Tenant/Site/Facility/Campus/LocationRoot/hierarchy abstractions).
- Department without Organization; DepartmentCode uniqueness global within KeyInventory.
- Room without Building; RoomNumber uniqueness global; preserve internal RoomCode as immutable technical identity; operators do not invent technical identifiers.
- Remove ResponsibleManager from WorkforceMember creation, validity, eligibility, Issue Key eligibility, and future manager assignment/change requirements; remove bootstrap mutual-manager pair and related create paths from active product behavior.
- Remove OrganizationCode/Organization scoping from active Identity Role semantics where present solely because Organization was a business authority; preserve Role/Permission/RBAC capability itself.
- First-use readiness, prerequisite-aware actions, meaningful empty states, contextual next actions; Application/Domain own readiness/eligibility; Web presents without duplicating business rules.
- Navigation redesign around operator tasks; Administration no longer exposes Organization or Building.
- Field-level mutability matrix implementation for authorized mutable fields only (see Mutability Matrix below).
- Shared human-readable date/time presentation authority across UI and CSV/XLSX/PDF exports; persisted UTC unchanged.
- Coherent post-create server-side lifecycle (success → confirmation → clean create state → next action); failed validation retains input.
- User Guide contract; final screenshots/docs after runtime finalization.
- SQL Server migration with RoomNumber global-uniqueness precheck STOP-on-conflict; preserve surviving business data; drop obsolete Organization/Building columns/FKs/tables and active ResponsibleManager requirements without rewriting historical audit rows.
- Architecture/workflow/UI tests and empty-database first-use acceptance.

## Out of Scope
- Workflow engine, configurable onboarding framework, generic wizard infrastructure, speculative abstractions.
- Generic CRUD, hard delete (unless already explicitly authorized elsewhere), historical Loan/Return/Audit rewriting.
- Fake/default Organization/Building/personnel, bootstrap pairs, self-manager, sole-member manager exception, placeholder manager, hierarchy replacements.
- Compatibility shims retaining nullable legacy Organization/Building relationships “just in case.”
- REPORTS-2, BI/report-builder, authentication redesign, multi-tenant/site models.
- Changing authoritative persisted UTC values merely for presentation.
- Marking this slice Accepted or preparing another slice in the same preparation.
- Git operations unless explicitly requested.
- Accepted slice history content rewrites (WORKFORCE-ELIGIBILITY-1, ADMIN-MAINTENANCE-1, OPERATOR-UX-1, etc. remain historical; active authority is superseded by this slice’s governing-document amendments).

## Required Governing Documents
- documentation/implementation-roadmap.md
- documentation/slice-promotion-governance.md
- documentation/implementation-contract.md
- documentation/product-vision.md
- documentation/roadmap.md
- documentation/key-inventory-domain-contract.md
- documentation/key-inventory-erd.md
- documentation/architecture-contracts.md
- documentation/product-experience-contract.md
- documentation/business-authority-matrix.md
- documentation/key-inventory-capability-map.md
- documentation/security-capability-contract.md
- documentation/system-integrity-contract.md
- documentation/slices/OPERATOR-EXPERIENCE-1.md
- documentation/slices/OPERATOR-AUDIT-1.md (dependency; do not rewrite Accepted history)
- documentation/slices/ADMIN-MAINTENANCE-1.md (superseded active Org/Building/manager maintenance surfaces; do not rewrite Accepted history)
- documentation/slices/WORKFORCE-ELIGIBILITY-1.md (historical; do not rewrite Accepted history)
- documentation/slices/OPERATOR-UX-1.md (historical UX baseline; do not rewrite Accepted history)

## Required Previous Slices
- OPERATOR-AUDIT-1

## Allowed Files
- documentation/slices/OPERATOR-EXPERIENCE-1.md
- documentation/implementation-roadmap.md
- documentation/product-vision.md
- documentation/roadmap.md
- documentation/key-inventory-domain-contract.md
- documentation/key-inventory-erd.md
- documentation/architecture-contracts.md
- documentation/product-experience-contract.md
- documentation/business-authority-matrix.md
- documentation/key-inventory-capability-map.md
- documentation/security-capability-contract.md
- documentation/system-integrity-contract.md
- documentation/project-architecture-index.md (index sync only if required)
- documentation/operator/** (User Guide and screenshots after runtime finalization)
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/**

## Forbidden Files
- Accepted slice history content rewrites
- CI pipeline redesign unrelated to this slice
- Docker/demo topology changes unless required solely for broken demo references to Org/Building/ResponsibleManager
- Introduction of SQLite, EF InMemory, second database, Redis, workflow engines, or multi-tenant frameworks

## Authority Owner
- Domain/Location: Room place authority with global RoomNumber uniqueness; Building removed as active authority.
- Domain/Workforce Eligibility: Department (global), WorkforceMember (no Organization, no ResponsibleManager), WorkAssignment, key-issue eligibility, termination signaling.
- Domain/Party: FirstName, LastName, UIN (UIN mutation authority deferred — see Mutability Matrix STOP portion).
- Domain/Identity RBAC: Role without OrganizationCode scoping; Permission/RolePermission/PrincipalRoleAssignment preserved.
- Application: all mutations, readiness query surfaces needed by Web, atomic WM registration without manager/org, audit staging.
- Infrastructure: SQL Server persistence and migration safety.
- Web: self-guiding presentation, navigation, human-readable dates, post-create lifecycle, User Guide consumption — no business-rule duplication, no DbContext.

## Architectural Risks
- Reintroducing Organization/Building under another name.
- Leaving ResponsibleManager optional instead of removed (forbidden — removal is complete for active model).
- Web-duplicated eligibility/readiness rules.
- Silent RoomNumber conflict resolution during migration.
- Inventing WorkAssignment-optional Issue eligibility without Domain authority (current Domain requires ≥1 active WorkAssignment for Issue; retained unless separately decided).
- Generic CRUD or hard delete.
- Wizard/workflow-engine frameworks.

## Dependency Graph (authoritative after this slice’s Domain amendments)

### Classification legend
- **MANDATORY** — required before the dependent operation can succeed under Domain/Application authority
- **OPTIONAL** — useful but not required for the dependent operation
- **PARALLEL** — may be created independently of each other
- **CONSEQUENCE** — produced by a prior successful operation; not a setup prerequisite
- **LIFECYCLE** — activation/retirement/end/terminate path, not initial create prerequisite

### Entity / capability dependencies

| Capability | Depends on | Classification | Notes |
|---|---|---|---|
| Department create | (none business) | PARALLEL | Global DepartmentCode |
| Room create | (none business) | PARALLEL | Global RoomNumber; RoomCode system-generated |
| Key Type create | (none business) | PARALLEL | |
| Workforce Member create | Department (active) | MANDATORY | No Organization; no ResponsibleManager; no second WM |
| Work Assignment create | Active WorkforceMember + Active Room | MANDATORY for its own create | WM↔Room; cannot precede either |
| Key registration | Active Key Type | MANDATORY | |
| Key↔Room assignment | Key + Active Room | OPTIONAL for Issue Key | Required for “rooms opened by key” completeness |
| Issue Key | Active WM with valid Party; active Department; ≥1 active WorkAssignment; registered Key; justification Department or assigned Room | MANDATORY set | Verified against Domain eligibility after Org/Building/Manager removal: **WorkAssignment remains required**; ResponsibleManager/Organization not required |
| Active Custody / Open Loans | Successful Issue | CONSEQUENCE | |
| Receive Key | Open Loan | MANDATORY for receive | |
| Find Key | Catalog data (and assignments when present) | OPTIONAL setup | Available whenever keys exist |
| History | Prior loans/returns | CONSEQUENCE | |
| Audit Trail | Prior audited mutations | CONSEQUENCE | Always navigable; empty until mutations |
| Reports | Operational data | CONSEQUENCE | Available; empty/partial until data exists |
| Activate/Retire/End/Terminate | Existing record | LIFECYCLE | |

### Minimum legitimate first-use sequence (not a wizard; derived order)
1. Authenticate (existing Identity).
2. Create in any order (**PARALLEL**): Department, Room, Key Type.
3. Create first Workforce Member (needs Department).
4. Create Work Assignment (needs that Workforce Member + Room) — **MANDATORY before Issue** under current eligibility.
5. Register Key (needs Key Type).
6. Optionally assign Key↔Room (PARALLEL with respect to Issue; recommended for Find Key).
7. Issue Key → Active Custody (**CONSEQUENCE**) → Receive when due → History / Audit / Reports.

## Navigation Model (prepared)
Task-oriented, not architectural-entity oriented:

| Group | Tasks |
|---|---|
| Setup / Administration | Departments; Rooms; Workforce Members; Work Assignments; Audit Trail |
| Key / Catalog | Key Types; Register Key; Key↔Room |
| Daily custody | Issue Key; Receive Key; Active Loans |
| Lookup | Find Key; Member issued keys; History |
| Reporting | Existing REPORTS-1 reports + exports |

- No Organization or Building navigation.
- Preserve list-first + dedicated Add/Details from OPERATOR-UX-1.
- Separate setup guidance from daily custody operations on Home and empty states.

## First-use UX Contract
- Home and major task pages present readiness using Application-owned signals (missing Department, Room, Workforce Member, Work Assignment, Key Type, Key, etc.).
- Each major task states: purpose; missing prerequisites; why required; link to create them; what becomes possible next.
- Empty states are instructional, not blank failures.
- After success, show confirmation and the next logical action from the dependency graph.
- Operator must not learn normal prerequisites only via failed POST validation.
- No workflow engine, onboarding framework, or generic wizard infrastructure.

## Mutability Matrix (preparation authority)

| Entity | Field | Classification | Application authority | Validation / invariants | Audit | Historical consequence |
|---|---|---|---|---|---|---|
| Department | DepartmentCode | IMMUTABLE IDENTITY | none | global unique at create | create/activate/retire only | — |
| Department | IsActive | LIFECYCLE CONTROLLED | Activate/Retire Department | only active Dept for membership/justification | audited | no history rewrite |
| Room | RoomCode | IMMUTABLE IDENTITY | system-generated at create | unique | create only | — |
| Room | RoomNumber | MUTABLE BUSINESS ATTRIBUTE | Update RoomNumber | required; global unique | audited with old→new | loans/assignments keep RoomCode refs |
| Room | Description | MUTABLE BUSINESS ATTRIBUTE | Update Room Description | length/empty rules per Domain | audited | — |
| Room | IsActive | LIFECYCLE CONTROLLED | Activate/Retire Room | active Room required for WA / Key↔Room / room justification | audited | — |
| WorkforceMember | WorkforceMemberCode | IMMUTABLE IDENTITY | system-generated | unique | create | — |
| WorkforceMember | Party reference | IMMUTABLE IDENTITY | set at create only | one Party; at most one Active WM per Party | — | — |
| WorkforceMember | Department | MUTABLE BUSINESS ATTRIBUTE | Update WorkforceMember Department | active Department; Active WM only | audited | no Party rewrite |
| WorkforceMember | WorkforceType | MUTABLE BUSINESS ATTRIBUTE | Update WorkforceType | Employee/Contractor; Active WM only | audited | — |
| WorkforceMember | Status | LIFECYCLE CONTROLLED | Terminate (existing) | Terminated blocks issue; no reactivate | audited | returns via Return workflow |
| WorkforceMember | Organization / ResponsibleManager | removed from active model | none | — | historical audit rows remain readable | no rewrite of past audit |
| Party | FirstName | MUTABLE BUSINESS ATTRIBUTE | Update Party person name | required non-empty | audited on Party subject | does not rewrite loans; display uses current Party |
| Party | LastName | MUTABLE BUSINESS ATTRIBUTE | Update Party person name | required non-empty | audited | same |
| Party | UIN | MUTABLE BUSINESS IDENTIFIER | Correct Party UIN (governed) | nine numeric digits; installation-wide unique; reject collision with another Party; PartyId/PartyCode stable; no replacement Party | audited with old UIN → new UIN; do not rewrite historical audit rows | preserves Loans/Returns/relationships/history |
| Party | PartyCode | IMMUTABLE IDENTITY | system-generated | unique | create | — |
| WorkAssignment | WorkAssignmentCode | IMMUTABLE IDENTITY | system/create | unique | create | — |
| WorkAssignment | WorkforceMember / Room | IMMUTABLE IDENTITY (for existing row) | create new; End old to change room | active WM + active Room at create | create/end audited | — |
| WorkAssignment | IsPrimary | MUTABLE BUSINESS ATTRIBUTE | Mark/Clear Primary | ≤1 active primary per WM | audited | — |
| WorkAssignment | IsActive | LIFECYCLE CONTROLLED | End | ended not deleted | audited | — |
| KeyType | TypeCode | IMMUTABLE IDENTITY | create | unique | create | — |
| KeyType | IsActive | LIFECYCLE CONTROLLED | Activate/Retire | retire rules for active keys | audited | existing keys remain |
| KeyAsset | CatalogKeyCode | IMMUTABLE IDENTITY | register | unique | register audited | — |
| KeyAsset | KeyType / series refs | IMMUTABLE IDENTITY after register unless already authorized elsewhere | none new | — | — | no unauthorized catalog rewrite |
| Key↔Room | assignment pair | LIFECYCLE CONTROLLED | Assign / Remove | active Room; unique pair | audited | not assignment history store |

No hard delete. No Loan/Return/Audit mutation via maintenance.

## Date/Time Presentation Contract
- Persisted authoritative timestamps remain UTC.
- One shared Web presentation authority formats operator-facing date/time for Home, Administration, Issue/Receive, Active Loans, History, member details, Audit Trail, Reports, and CSV/XLSX/PDF exports.
- Reuse/extend OPERATOR-UX-1 local presentation patterns (`OperatorLocalTimestamp` / `OperatorTimestampFormatter` lineage); no raw SQL/ISO/UTC serialization in normal operator UI.
- Inputs use human/browser controls; Web→Application boundary converts to UTC.

## Post-Create Lifecycle Contract
Server-side only (PRG or equivalent coherent Razor Pages lifecycle):
1. Persist via Application succeeds.
2. Success confirmation retained as flash/message as appropriate.
3. Create form model cleared: textboxes empty, dropdowns at intentional defaults, validation state cleared.
4. Present logical next action from dependency graph.
Failed validation: preserve user input; do not clear. No field-by-field JavaScript clearing.
Audit all active create surfaces: Department, Room, Workforce Member, Work Assignment, Key Type, Key registration, Key↔Room, and any other active create form.

## User Guide Contract
- Location: `documentation/operator/` (concise visual operator guide; not an engineering manual).
- Produced **after** final runtime behavior; preparation defines required contents now.
- Must match UI readiness/dependency model exactly (same MANDATORY/OPTIONAL/PARALLEL/CONSEQUENCE classifications).
- Required topics: purpose; first-time setup; verified dependency model; Issue → Active Custody → Receive; Find Key; authorized corrections; Audit Trail; Reports/exports; common blockers.
- Each workflow: purpose; prerequisites; where to go; steps; expected result; what becomes available next; common problems.
- Primary diagram must show parallel branches (Department / Room / Key Type) and actual Issue prerequisites including WorkAssignment.
- Real final UI screenshots only; Mermaid allowed for workflow/dependency diagrams.
- No internal IDs, SQL, DbContext, migrations, or implementation details.

## Migration / Data-Preservation Contract
- SQL Server EF migration on existing KeyInventory database authority.
- Preserve valid Departments, Rooms, Parties, WorkforceMembers, WorkAssignments, Keys, KeyRoomAssignments, Loans, Returns, OperatorAuditRecords, reporting data.
- **Before destructive steps:** verify existing RoomNumber values satisfy global uniqueness; on conflict STOP and report conflicts; no automatic rename; no silent discard.
- Remove obsolete Organization/Building columns, FKs, and tables where the surviving model no longer requires them.
- Remove active ResponsibleManager requirements from schema/model; do not rewrite historical OperatorAuditRecord rows that mention Organization, Building, or manager facts.
- No nullable legacy Org/Building relationships retained “just in case.”
- No second SQL Server; no SQLite.

## Acceptance Criteria
- No Organization or Building required or exposed for normal operation.
- Department created with global uniqueness; Room created without Building with global RoomNumber uniqueness.
- First WorkforceMember created without another WorkforceMember; no ResponsibleManager; no fake personnel.
- Actual prerequisites visible/intelligible via self-guiding UI aligned to dependency graph.
- Key Type/Key created; Key↔Room can be established; first legitimate Issue reached and succeeds when WorkAssignment and other genuine eligibility requirements exist.
- Active Custody reflects Issue; Receive succeeds.
- Dates human-readable in UI and exports; persisted UTC unchanged.
- Authorized corrections work and emit OperatorAuditRecord for authenticated operator; Party UIN governed correction preserves Party identity and audits old→new UIN.
- Successful create forms reset cleanly; failed validation retains input.
- Audit Trail / Reports / CSV/XLSX/PDF remain correct; Find Key / rooms opened / History preserved.
- Final User Guide matches actual UI and dependency model.
- Build PASS (0 warnings/errors); Tests PASS; empty-database first-use walkthrough PASS; DI resolvable; no Web DbContext; no dead Org/Building routes/services.

## Required Tests
- Domain tests: Department global uniqueness; RoomNumber global uniqueness; Room without Building; WorkforceMember without Organization/ResponsibleManager; eligibility without Organization/ResponsibleManager; WorkAssignment still required for Issue; Role without OrganizationCode.
- Application/persistence tests: migration safety precheck behavior (conflict reporting); create first WM alone; Issue path; mutation audit for authorized edits; removal of Org/Building/manager ports from active DI.
- Architecture tests: no Web DbContext; no active Org/Building/ResponsibleManager product surfaces; navigation/admin allow-list; readiness presentation does not embed duplicated eligibility formulas beyond Application signals.
- Workflow/UI tests: post-create clean state; failed validation retention; human-readable timestamps on representative surfaces; empty-database first-use path.
- Regression: Find Key, Issue, Receive, Active Loans, History, Audit Trail, REPORTS-1 exports.

## Closure Contract
- Transversal Gate PASS
- Build PASS
- Tests PASS
- Empty-database first-use acceptance PASS
- User Guide completed after runtime finalization
- Documentation updated only as required by this slice
- Human acceptance checkpoint STOP after Implementation Complete (do not self-Accept)

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After OPERATOR-EXPERIENCE-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-11.
- Evidence: Single-site removal of Organization, Building, and ResponsibleManager as active authorities; global DepartmentCode and RoomNumber uniqueness; Role without OrganizationCode scoping; manager-free first WorkforceMember; WorkAssignment retained as Issue Key mandatory prerequisite; Application-owned readiness; governed record corrections including Party UIN as MUTABLE BUSINESS IDENTIFIER with old→new OperatorAuditRecord evidence; human-readable dates/exports; post-create clean form lifecycle; migration OperatorExperience1SingleSite with duplicate RoomNumber/DepartmentCode STOP prechecks and historical audit preservation; normalized relational model retained; User Guide at documentation/operator/keyinventory-operator-guide.md with runtime screenshots; empty-database first-use walkthrough PASS; build PASS 0 warnings 0 errors; tests PASS 178/178.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-11.
- Evidence: OPERATOR-EXPERIENCE-1 was Implementation Complete; single-site product decisions (Organization/Building removed as active concepts; ResponsibleManager removed; global Department/RoomNumber uniqueness; Party UIN governed correction; Application readiness/first-use UX; human-readable date/time presentation; post-create lifecycle; mutability matrix; OperatorExperience1SingleSite migration/data-preservation; normalized relational ownership; User Guide contract matching the dependency model) remained within approved scope; no Tenant/Site/Facility/Campus/LocationRoot replacements, bootstrap/fake personnel, workflow-engine/wizard frameworks, generic CRUD, Web DbContext persistence, or successor-slice preparation were introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Active Presentation Amendment (does not rewrite Acceptance Record)
- Decision: SUPERSEDE permanent first-use/onboarding Home presentation only.
- Date: 2026-08-11.
- Authority: Human Governance via active `documentation/product-experience-contract.md`.
- Scope: Presentation only. Application-owned readiness/eligibility remains business authority. Issue eligibility and Domain/Application prerequisite rules are unchanged.
- Supersedes for active product experience: permanent Home first-time setup section; permanent Home readiness checklist; permanent Administration onboarding/setup panels that duplicate setup as a lasting capability surface.
- Active rule: Home is operational; prerequisite feedback is contextual at capability boundaries; empty-installation sequence belongs in the operator User Guide.
- Historical Accepted evidence above remains unchanged.

## Active Presentation Amendment — Issue / Receive interaction (does not rewrite Acceptance Record)
- Decision: SUPERSEDE Issue/Receive auto-selection and unbounded holder dropdown presentation only.
- Date: 2026-08-11.
- Authority: Human Governance via active `documentation/product-experience-contract.md` (Issue / Receive Interaction).
- Scope: Presentation/interaction only. Application eligibility, KeyAsset custody, KEY # / MEDECO model, UTC persistence, and loan/return invariants unchanged.
- Active rules: clean initial Issue/Receive business-choice state; bounded Application-owned eligible key-holder search; PRG clean post-success state; shared absolute operator timestamp display with operator-editable Issued/Due/Received via shared local-time conversion.
- Historical Accepted evidence above remains unchanged.

## Active Presentation Amendment — Operator interaction architecture (does not rewrite Acceptance Record)
- Decision: SUPERSEDE duplicate Global Search form, navigation-as-information-substitute on Person search, unbounded Issue KEY # / Receive active-issue / Work Assignment dropdowns, and Register Key free-text Key Type silent-create presentation.
- Date: 2026-08-13.
- Authority: Human Governance via active `documentation/product-experience-contract.md` (Operator Interaction Architecture; Global Operator Search; Issue / Receive Interaction).
- Scope: Presentation/interaction and Application orchestration over existing authorities only. Domain/ERD KEY # / MEDECO / Work Assignment facts unchanged. Roadmap Next Allowed Slice remains STOP.
- Active rules: header-only global query input; `/Search` results-only; Person results show Identity / Work Assignment / Current Key Custody inline; Issue KEY # and Receive active issues use bounded search; Work Assignment Add uses bounded member/room search; Register Key explicit existing-vs-new KEY # modes with existing Key Type selection; no silent Key Type creation from Register.
- Historical Accepted evidence above remains unchanged.

## Active Lifecycle Amendment — Administration / Catalog delete vs retire (does not rewrite Acceptance Record)
- Decision: SUPERSEDE the active mutability matrix rule “No hard delete” for unused/unreferenced Administration and Catalog configuration records only.
- Date: 2026-08-12.
- Authority: Human Governance via active `documentation/product-experience-contract.md` (Administration / Catalog Record Lifecycle).
- Scope: Application-owned lifecycle eligibility and presentation for Administration/Catalog row actions. Does not rewrite historical Accepted records. Does not authorize cascade deletion of business history. Does not change KEY-ACCESS-COPY-1 KEY # / MEDECO custody authority.
- Active rules:
  - Unreferenced record → permanent Delete permitted when Application revalidates no relationships/history that must be preserved.
  - Referenced or historically used record → permanent Delete forbidden; Retire / End / Terminate / Remove where supported.
  - Edit is an explicit row action when attributes are legitimately mutable under active identity authority.
  - `IConfigurationLifecycleUseCase` is the Application lifecycle authority consumed by Web list/delete surfaces.
- Historical Accepted evidence and the historical matrix wording above remain unchanged as Accepted history; active product behavior follows this amendment.

## Active Structural Amendment — Department identity normalization (does not rewrite Acceptance Record)
- Decision: SUPERSEDE the Accepted mutability matrix classification of Department.DepartmentCode as IMMUTABLE IDENTITY for active product/Domain/ERD authority.
- Date: 2026-08-12.
- Authority: Human Governance identity rule via `documentation/key-inventory-erd.md` and `documentation/erd-normalization-identity-authority-2026-08-12.md`.
- Scope: Logical identity only in this amendment. Runtime/migration implementation is not authorized by this amendment alone. Roadmap Next Allowed Slice remains STOP.
- Active rules:
  - DepartmentId = immutable entity identity and relationship target.
  - DepartmentCode = unique editable business identifier (operator-facing); rename must not rewrite prior OperatorAuditRecord rows.
  - Live WorkforceMember membership references DepartmentId.
  - Issue Department justification on Loan (when implemented) = JustificationDepartmentId + immutable JustificationDepartmentCodeSnapshot; OperatorAuditRecord Details remain display history only.
  - Legacy KeyIssued Details format is classification B (semi-structured, not governed); deterministic relational backfill is not authorized until Human Migration Provenance Extract or Human mapping (`documentation/department-historical-justification-provenance-2026-08-12.md`). Runtime Details parsing for delete eligibility is forbidden. Permanently forbidding rename because of old snapshots is rejected.
  - Room RoomCode/RoomNumber dual identity, KEY # KeyNumber immutability, and KeyAssetId custody remain unchanged.
- Historical Accepted matrix rows that labeled DepartmentCode IMMUTABLE IDENTITY remain as Accepted history only.

## Next Allowed Slice
STOP
