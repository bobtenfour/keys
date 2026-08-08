# REPORTS-1 - Practical Operational Reports and CSV Export

## Status
Accepted

## Parent Phase
Phase 2 — Operational Security

## Purpose
Give the key custodian of one approximately five-floor building practical read-only reports and CSV extraction over information already owned by Key, Loan, Return, Party, and Workforce authorities, without creating a BI platform, reporting database, or invented domain data.

## Objective
The application exposes a Reports navigation section with seven practical read-only reports backed by Application report queries over existing SQL Server authorities; each on-screen tabular report has matching CSV export of the same authoritative result set; unsupported requested fields that lack stored authority are excluded rather than invented; existing issue/receive and lookup behavior remain intact.

## Authorized Reports and Fields

### 1. Current Key Holders
Authorized fields:
- Catalog key code
- Holder FirstName, LastName, UIN (Party)
- Holder WorkforceMember code when resolvable through existing Party → WorkforceMember linkage
- Department code from the resolvable WorkforceMember
- Responsible Manager workforce member code from the resolvable WorkforceMember
- Issue timestamp (Loan.IssuedAtUtc)
- Due timestamp (Loan.DueAtUtc)
- Current loan status (Open)

Excluded fields:
- Room/Department issue justification — Loan does not persist justification kind or justification code; KEY-LOOKUP-1 and WORKFORCE-ELIGIBILITY-1 evaluate justification at issue time only and do not store it.

### 2. Keys by Workforce Member
Authorized fields and behavior:
- Select or search an existing Workforce Member
- Currently issued keys for that member's Party (catalog key code, issue/due timestamps, Open status, holder FirstName/LastName/UIN)
- Returned-key history for that member's Party (catalog key code, issued/returned timestamps, Returned status, holder FirstName/LastName/UIN)

Excluded fields:
- Issue justification history — not persisted on Loan/Return.

### 3. Active Loans
Authorized fields:
- Catalog key code
- Holder FirstName, LastName, UIN
- WorkforceMember code when resolvable
- Department code when resolvable
- Issue timestamp
- Due timestamp
- Open status

Excluded fields:
- Issue justification — not persisted.

### 4. Overdue Keys
Authorized fields:
- Catalog key code
- Holder FirstName, LastName, UIN
- WorkforceMember code when resolvable
- Responsible Manager workforce member code when resolvable
- Department code when resolvable
- Issue timestamp
- Due timestamp
- Days overdue derived from DueAtUtc relative to current UTC instant when DueAtUtc is in the past
- Open status

Excluded fields:
- Room/Department issue justification — not persisted on Loan.

### 5. Key History
Authorized fields:
- Selected catalog key code
- Existing issue/return history rows from Loan and Return authorities (loan code, issued timestamp, due timestamp, returned timestamp when present, status, holder FirstName/LastName/UIN)

Excluded fields:
- Issue justification — not persisted.
- Non-existent custody/lifecycle event streams beyond Loan/Return history.

### 6. Outstanding Keys by Workforce Status
Authorized fields:
- WorkforceMember code
- WorkforceMember Status (Active or Terminated)
- Party FirstName, LastName, UIN
- Department code
- Responsible Manager workforce member code
- Currently held open loan key codes and due timestamps for that member's Party
- Terminated members remain included when open loans exist (existing mandatory return-obligation linkage)

Excluded fields:
- Invented offboarding workflow state beyond WorkforceMember.Status and open Loan presence.

### 7. Key Catalog
Authorized fields:
- Catalog key code
- Key type code
- Key active flag (KeyAsset.IsActive)
- Available or Issued state derived from existing open Loan authority

Excluded fields:
- Building, Room, RoomNumber, or other location fields on the key — KeyAsset has no authoritative location association in the current model; inventing key-location mapping is forbidden.

## Scope
- Application-owned report queries and report DTOs for the seven authorized reports using only fields listed as Authorized above.
- Infrastructure read adapters against existing SQL Server KeyAsset, KeyType, Loan, Return, Party, WorkforceMember, Organization, and Department tables/entities already mapped.
- Reuse existing KEY-LOOKUP-1 Application lookup/read authorities when they already own required information rather than duplicating holder-resolution or open-loan classification logic.
- Web Reports section in existing navigation with practical pages for each authorized report.
- Practical filtering using existing fields where useful (for example key code, workforce member, overdue-only already implied by Overdue report, workforce status).
- CSV export for each tabular report representing the same authoritative result set as the on-screen report.
- Explicit usable empty states for each report.
- Read-only reporting only; no mutation of Loan, Return, Party, Workforce, or Key authorities.
- No Web DbContext access; Web consumes Application report DTOs only.
- Dependency injection registration for report use cases/ports.
- Architecture, Application/persistence, and UI-boundary tests required by this slice.

## Out of Scope
- Excel export.
- PDF export.
- Charts.
- Dashboards or BI platform.
- Scheduled reports.
- Email delivery.
- Report designer.
- Data warehouse.
- Duplicate reporting database or denormalized reporting source of truth.
- New business entities.
- New business rules.
- Persisting issue justification solely to satisfy a report.
- Associating keys to Building/Room solely to satisfy Key Catalog location columns.
- New authorization/policy engine.
- Enterprise analytics.
- REPORTS-2 or any future reporting abstraction layer beyond this practical slice.
- Organization/Department/Building/Room/Workforce edit or deactivate expansion.
- Changes to issue/receive eligibility or mutation semantics.
- Automatic audit emission.
- Unrelated UI redesign or new visual system.
- Elasticsearch or external indexing.
- Speculative abstractions.
- Placeholders.
- TODO.
- FIXME.
- Commented-out code.
- Git operations unless explicitly requested by the human repository owner.
- Preparation or implementation of any other slice.

## Field Exclusion Authority
Unsupported requested elements are excluded only for these exact reasons:
1. Room/Department issue justification on Current Key Holders, Active Loans, Overdue Keys, Keys by Workforce Member history, and Key History — no Loan/Return persisted justification attributes exist under current governing contracts and persistence mappings.
2. Key Catalog location information (Building/Room/RoomNumber/description-as-key-place) — KeyAsset authoritative model stores catalog key code, type, and active flag only; no key-to-location authority exists.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- roadmap.md
- product-vision.md
- key-inventory-domain-contract.md
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- architecture-contracts.md
- system-integrity-contract.md
- product-experience-contract.md
- testing-strategy.md
- slice-promotion-governance.md
- documentation/slices/KEY-LOOKUP-1.md

## Required Previous Slices
- KEY-LOOKUP-1

## Allowed Files
- documentation/slices/REPORTS-1.md
- documentation/implementation-roadmap.md
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/REPORTS-1.md and documentation/implementation-roadmap.md
- src/KeyInventory.Domain/** unless an existing governing contract explicitly requires a domain change (default: no Domain invention for reports)
- Excel/PDF export packages introduced for this slice
- charting/BI packages
- data warehouse or second reporting database packages
- Elasticsearch or external index packages
- authorization/policy engine files
- CI pipeline files
- Docker Compose files
- Testcontainers configuration
- SQLite provider packages or configuration
- EF Core InMemory provider packages or configuration

## Authority Owner
Application operational reporting read authority over existing Key Catalog, Loan/Return, Party, and Workforce Eligibility data; Party remains sole person-identity authority; Loan/Return remain sole issue/return history authority; no report owns or mutates business decisions.

## Architectural Risks
- Inventing persisted justification or key-location data to fill report columns.
- Creating a duplicate reporting database or denormalized second source of truth.
- Duplicating KEY-LOOKUP-1 holder-resolution logic instead of reusing Application read authority.
- Putting DbContext access in Web.
- Expanding into dashboards, BI, scheduled delivery, or REPORTS-2 abstractions.
- Changing issue/receive mutation behavior while adding reports.
- Treating CSV as a divergent result set from on-screen reports.
- Enterprise analytics scope creep beyond one-building operational needs.

## Acceptance Criteria
- Reports navigation section exists and reaches the seven authorized report pages.
- Each authorized report returns read-only SQL-backed results through Application report DTOs for only the Authorized fields listed above.
- Excluded fields are not displayed and are not invented.
- Current Key Holders shows currently issued keys with key identification, holder FirstName/LastName/UIN, Department when resolvable, issue/due timestamps, and Open status.
- Keys by Workforce Member shows currently issued keys and returned-key history for a selected/searchable Workforce Member via Party linkage.
- Active Loans shows all currently issued keys with holder identity and due dates.
- Overdue Keys shows open loans with DueAtUtc in the past, holder identity, Responsible Manager and Department when resolvable, issue/due timestamps, and derivable days overdue.
- Key History shows existing Loan/Return history for a selected key with holder identity.
- Outstanding Keys by Workforce Status shows Workforce Members who still hold open-loan keys, including Terminated members with open loans.
- Key Catalog shows registered keys with type, active flag, and Available/Issued state without fabricated location columns.
- CSV export exists for each tabular report and matches the same authoritative result set as the on-screen report.
- Empty states are explicit and usable.
- Web does not access DbContext.
- No duplicate reporting persistence is introduced.
- Existing issue/receive behavior remains intact.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Repository hygiene PASS.
- Human acceptance checkpoint is reached only after Implementation Complete evidence is recorded.

## Required Tests
- Application or persistence tests verify each authorized report query against SQL Server through `ConnectionStrings:KeyInventory` for the Authorized fields only.
- Application or persistence tests verify Overdue Keys includes only past-due open loans and computes days overdue from DueAtUtc.
- Application or persistence tests verify Keys by Workforce Member and Outstanding Keys by Workforce Status resolve through Party and open/returned Loan authority, including Terminated members with open loans.
- Application or persistence tests verify Key Catalog Available/Issued classification from open Loan authority without requiring location fields.
- Tests verify CSV export content matches the corresponding report DTO result set for at least one representative tabular report per export path or a shared CSV formatting authority used by all reports.
- Architecture or UI-boundary tests verify Web report pages do not reference DbContext and consume Application DTOs only.
- Architecture tests verify no reporting database, Excel/PDF/BI platform, or second persistence provider is introduced.
- Workflow or regression tests verify existing issue and receive use cases remain valid.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Authority consistency PASS
- ERD consistency PASS
- Capability consistency PASS
- Product experience consistency PASS
- System integrity consistency PASS
- Product scope consistency PASS
- Testing strategy consistency PASS
- Build PASS
- Tests PASS
- Repository hygiene PASS
- Documentation updated only if required
- No invented justification or key-location data
- No duplicate reporting source of truth
- Existing issue/receive behavior intact
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After REPORTS-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-08.
- Evidence: KEY-LOOKUP-1 is Accepted; human governance explicitly authorized REPORTS-1 as the next operational slice for practical read-only reports and CSV extraction for one approximately five-floor building; each requested report was checked against existing Loan/Return/Party/WorkforceMember/KeyAsset authorities; Room/Department issue justification and Key Catalog location fields were excluded because they lack stored authoritative data; slice specification defines authorized fields, exclusions with exact reasons, Application-owned report queries, SQL Server read adapters, Reports navigation, CSV parity, reuse of existing lookup/read authorities, out-of-scope BI/export/platform items, allowed/forbidden files, acceptance criteria, required tests, dependencies, and human acceptance checkpoint without implementation.
- Deciding authority role: Human Architectural Governance.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-08.
- Evidence: Application IOperationalReportsUseCase/port and ReportCsvFormatter implemented; Infrastructure OperationalReportsAdapter reads existing SQL Server Key/Loan/Return/Party/WorkforceMember authorities without a reporting database; Web Reports navigation and seven read-only report pages consume Application DTOs only with matching CSV export and filters; excluded justification and Key Catalog location fields are not displayed; overdue days derived from DueAtUtc; terminated members with open loans included; issue/receive regression remains valid; architecture and SQL workflow tests PASS; build PASS 0 warnings 0 errors; tests PASS 129/129.
- Deciding authority role: Implementation execution under approved slice specification.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-08.
- Evidence: REPORTS-1 was Implementation Complete; seven authorized read-only reports with Application-owned queries/DTOs, SQL Server reads over existing Key/Loan/Return/Party/Workforce authorities, Reports navigation, matching CSV export, and excluded unsupported justification/location fields remained within approved scope; no reporting database, BI platform, Excel/PDF export, or issue/receive mutation changes were introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
