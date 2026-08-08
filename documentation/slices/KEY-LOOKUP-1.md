# KEY-LOOKUP-1 - Operational Key and Holder Lookup

## Status
Implementation Complete

## Parent Phase
Phase 2 — Operational Security

## Purpose
Close the first proven daily operational gap by making existing key availability and current-holder information practically discoverable for the key custodian of one approximately five-floor building, without inventing reporting platforms, search engines, or duplicate persistence.

## Objective
The operator can find keys through Operations > Find Key and the persistent global header search using one Application read/search authority against existing SQL Server Loan, Return, Party, Workforce, and Key data; see Available or Issued state with human-readable holder identity for issued keys; navigate from a key result to current operational information; see FirstName, LastName, and UIN instead of opaque Party codes on existing Issue, Receive, Active Loans, History, and Home activity surfaces; and open a practical path from a Workforce Member to that person's currently issued keys. Existing issue and receive behavior remains intact.

## Scope
- One Application read/search authority for operational key and holder lookup against existing SQL Server persistence.
- Infrastructure adapter implementation of that authority using existing KeyAsset, Loan, Party, and WorkforceMember data without a second search store.
- Make Operations > Find Key functional using that Application authority and real SQL-backed results.
- Wire the persistent global header search to the same Application search authority and results.
- Key search supports at minimum exact and partial catalog key code match.
- Include other existing searchable key fields only where current authoritative data already supports them without new domain concepts (for example KeyType code when already persisted on KeyAsset).
- Do not invent key-to-Building or key-to-Room search fields that are not already authoritative on the key model.
- Search results show Available or Issued for each key based on existing open Loan authority.
- For an Issued key, results show current holder FirstName, LastName, and UIN from existing Party authority.
- Provide a direct usable path from a key search result to its current operational information (for example Active Loan / Receive context for Issued keys, or Issue/catalog context for Available keys).
- Replace opaque BorrowerPartyReference / Party code displays with FirstName + LastName and UIN on existing operator-facing Issue Key, Receive Key, Active Loans, History, and Home activity surfaces wherever those surfaces currently expose borrower Party codes.
- Provide a practical UI path from an existing Workforce Member administration or related operator surface to the keys currently issued to that person's Party through existing open Loan authority.
- Web pages consume Application DTOs only; no DbContext access in Web.
- Holder-resolution and search presentation rules are owned once in Application (and Infrastructure query implementation); Web must not duplicate holder-resolution logic across pages.
- Architecture, Application, persistence, and UI-boundary tests required by this slice.
- Dependency injection registration required for the new Application read/search authority.

## Out of Scope
- REPORTS-1 and any reporting, dashboard analytics, or KPI platform work.
- Generalized search engine.
- Elasticsearch or external indexing.
- Fuzzy-search framework.
- New authorization engine.
- Key authorization policy engine.
- Enterprise, multi-campus, multi-tenant, or large-scale search architecture.
- Organization or Department edit/deactivate work.
- Building or Room edit/deactivate work beyond what is required to display existing fields already returned by lookup DTOs.
- Workforce member edit of Organization, Department, ResponsibleManager, or WorkforceType.
- Ending or reassigning WorkAssignments.
- Changes to issue eligibility rules.
- Changes to Loan/Return mutation semantics beyond read/display enrichment required for lookup.
- Automatic audit emission.
- Custody lifecycle mutation.
- Lifecycle state machine work.
- New business entities unless an existing governing contract explicitly requires them.
- Duplicate reporting/search persistence, projections, or caches as a second source of truth.
- Unrelated UI redesign or new visual system.
- Seed data or demo pages.
- In-memory fake persistence, SQLite, or second persistence model.
- Speculative abstractions.
- Placeholders.
- TODO.
- FIXME.
- Commented-out code.
- Git operations unless explicitly requested by the human repository owner.
- Preparation or implementation of any other slice.

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

## Required Previous Slices
- WORKFORCE-ELIGIBILITY-1

## Allowed Files
- documentation/slices/KEY-LOOKUP-1.md
- documentation/implementation-roadmap.md
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/KEY-LOOKUP-1.md and documentation/implementation-roadmap.md
- src/KeyInventory.Domain/** unless an existing governing contract explicitly requires a domain change for this slice (default: no Domain entity invention)
- reporting feature files or REPORTS-1 slice files
- Elasticsearch, Lucene, or external index packages and configuration
- fuzzy-search framework packages
- authorization engine files
- key authorization policy engine files
- Organization/Department edit or deactivate feature expansion beyond lookup display
- CI pipeline files
- Docker Compose files
- Testcontainers configuration
- SQLite provider packages or configuration
- EF Core InMemory provider packages or configuration

## Authority Owner
Application operational key/holder lookup read authority over existing Key Catalog, Loan/Return, Party, and Workforce Eligibility data; Party remains sole person-identity authority for FirstName, LastName, and UIN; Loan remains sole open-issue authority for current holder derivation.

## Architectural Risks
- Duplicating holder-resolution logic across Razor Pages.
- Putting DbContext or SQL queries in Web.
- Creating a second search/reporting persistence model.
- Inventing key-location search fields not present on the authoritative KeyAsset model.
- Expanding into REPORTS-1, analytics, or generalized search infrastructure.
- Changing issue/receive mutation behavior while enriching display.
- Reintroducing opaque Party codes on some operator surfaces while fixing others.
- Treating WorkforceMember as person-identity authority instead of Party.
- Weakening SQL Server-only persistence testing.

## Acceptance Criteria
- Operations > Find Key returns real SQL-backed results through the Application read/search authority.
- Persistent global header search reaches the same Application authority and the same result semantics as Find Key.
- Search supports exact and partial catalog key code match at minimum.
- Search results show correct Available or Issued state from existing open Loan authority.
- Issued key results display current holder FirstName, LastName, and UIN from existing Party authority rather than opaque Party codes alone.
- Operator has a direct usable path from a key result to current operational information for that key.
- Existing Issue Key, Receive Key, Active Loans, History, and Home activity surfaces display FirstName, LastName, and UIN wherever they currently expose BorrowerPartyReference / borrower Party codes.
- Operator can determine who currently has a specific key through the Find Key / search result path.
- Operator can determine which currently issued keys belong to a specific Workforce Member through a practical UI path backed by existing open Loan and Party linkage.
- No duplicate source of truth for search, holder resolution, or loan state is introduced.
- Existing issue and receive behavior remains intact.
- Web does not access DbContext and does not duplicate holder-resolution logic across pages.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Repository hygiene PASS.
- Human acceptance checkpoint is reached only after Implementation Complete evidence is recorded.

## Required Tests
- Application or persistence tests verify key-code exact and partial search against SQL Server through `ConnectionStrings:KeyInventory`.
- Application or persistence tests verify Available vs Issued classification from open Loan authority.
- Application or persistence tests verify Issued results include Party FirstName, LastName, and UIN for the current borrower Party.
- Application or persistence tests verify currently issued keys for a Workforce Member resolve through that member's Party and open Loans.
- Architecture or UI-boundary tests verify Web does not reference DbContext and consumes Application DTOs for lookup/display enrichment.
- Architecture or Application tests verify holder-resolution is not duplicated as independent page-local query logic.
- Workflow or regression tests verify existing issue and receive use cases still succeed for an eligible Active Workforce Member and still complete returns.
- Architecture tests verify no Elasticsearch/external index, reporting store, or second persistence provider is introduced.

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
- No duplicate search/reporting source of truth introduced
- Existing issue/receive behavior intact
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After KEY-LOOKUP-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work, including REPORTS-1.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-08.
- Evidence: WORKFORCE-ELIGIBILITY-1 is Accepted; human governance explicitly authorized KEY-LOOKUP-1 as the next operational slice to close the proven Find Key / current-holder accountability gap; product scope remains one approximately five-floor building with a small workforce; slice specification defines one Application read/search authority over existing SQL Server Key/Loan/Party/Workforce data, Find Key and header search wiring, Available/Issued and holder display, operational surface identity enrichment, Workforce Member to issued-keys path, out-of-scope exclusion of REPORTS-1 and enterprise search, allowed/forbidden files, acceptance criteria, required tests, dependencies, and human acceptance checkpoint without implementation.
- Deciding authority role: Human Architectural Governance.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-08.
- Evidence: One Application IOperationalKeyLookupUseCase/port implemented; Infrastructure OperationalKeyLookupAdapter queries existing SQL Server KeyAsset, Loan, Party, and WorkforceMember data without a second search store; Operations Find Key and global header search share that authority with exact/partial key-code and type search; results show Available/Issued and Party FirstName/LastName/UIN holders with direct Issue/Receive paths; Issue, Receive, Active Loans, History, and Home activity display human-readable holder identity; Workforce Members link to MemberKeys issued-key path; issue/receive mutation and eligibility unchanged; architecture and SQL workflow tests PASS; build PASS 0 warnings 0 errors; tests PASS 123/123.
- Deciding authority role: Implementation execution under approved slice specification.

## Next Allowed Slice
STOP
