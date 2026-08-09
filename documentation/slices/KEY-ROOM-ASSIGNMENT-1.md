# KEY-ROOM-ASSIGNMENT-1 - KeyAsset to Room Opening Assignments

## Status
Accepted

## Parent Phase
Phase 2 — Operational Security

## Purpose
Implement the governed current KeyAsset-to-Room opening relationship so the key custodian of one approximately five-floor building can associate registered physical keys with the Rooms they open, expose those assignments through existing Catalog, Find Key / lookup, and authorized REPORTS-1 key surfaces, and avoid Lock mediation, master/sub-master hierarchy, assignment history, or new reporting families.

## Objective
The application persists and maintains current KeyAsset↔Room opening assignments under Key Catalog authority against existing SQL Server persistence; operators can assign, add, and remove Rooms for an existing KeyAsset including zero Rooms; Building is derived only through Room; Key Catalog, Find Key / operational lookup, and the existing REPORTS-1 Key Catalog report consume the authoritative relationship without duplicating it; Lock does not mediate room-opening; issue/receive and existing Room/Building identity ownership remain intact.

## Scope
- Domain current Key-to-Room Assignment model owned by Key Catalog for physical KeyAsset only (not KeyType).
- Cardinalities: one KeyAsset may open zero, one, or multiple Rooms; one Room may be opened by zero, one, or multiple KeyAssets.
- KeyAsset registration and continued existence remain valid with zero Room assignments.
- Editable current assignments after registration (add, remove, replace current set); no assignment-history model.
- Building for a key is derived exclusively through assigned Rooms; KeyAsset must not independently own Building.
- No master/sub-master hierarchy; multiple Rooms are represented only by multiple current assignments.
- Lock must not mediate, duplicate, or be required for Key-to-Room assignment; remove or neutralize any runtime KeyAsset→Lock room-opening dependence only as required to eliminate contradiction with governing contracts (no Lock feature expansion).
- Application commands and queries to maintain and read current Key-to-Room assignments.
- Infrastructure SQL Server persistence and migration for the current KeyAsset–Room relationship against `ConnectionStrings:KeyInventory` without a second persistence model.
- Dependency injection registration for required adapters and use cases.
- Web UI for assigning Rooms to an existing KeyAsset and editing those assignments later, reusing existing UX patterns.
- Key Catalog list/detail surfaces show Rooms opened by each Key (with Building derived through Room).
- Find Key / operational lookup shows Rooms opened using the same authoritative relationship (no duplicate assignment query logic).
- Existing REPORTS-1 Key Catalog report may show Building/Room/RoomNumber using the new authoritative relationship.
- Existing REPORTS-1 key-oriented surfaces may consume Room assignments only where already authorized by governing contracts; no new report families.
- Architecture, domain, persistence, lookup/report reuse, and UI-boundary tests required by this slice.

## Persistence Requirements
- Persist current KeyAsset-to-Room assignments in SQL Server through the existing `KeyInventory` connection string authority.
- Enforce uniqueness of the current (KeyAsset, Room) pair.
- Do not persist assignment history tables or historical rows as a second source of truth.
- Do not persist Building on KeyAsset; Building is read through Room.
- Do not require Lock foreign keys for Key-to-Room persistence.
- Migration must remain SQL Server-only; no SQLite, InMemory, or second store.

## UI Requirements
- Practical operator path to assign Rooms to an existing KeyAsset.
- Practical path to add or remove Room assignments after registration.
- Key registration remains valid with zero Rooms.
- Key Catalog shows Rooms opened by each Key, with Building derived through Room.
- Find Key / operational lookup shows Rooms opened.
- Existing REPORTS-1 Key Catalog report shows Building/Room when assignments exist.
- Explicit usable empty state when a Key has zero Room assignments.
- Web consumes Application DTOs/commands only; no DbContext access in Web.
- Reuse existing KeyInventory visual language; no unrelated redesign.

## Out of Scope
- Assignment history.
- Master/sub-master modeling or hierarchy engines.
- Lock mediation or Lock feature expansion.
- Duplicate Building ownership on KeyAsset.
- New location hierarchy, Campus, or multi-building enterprise place model.
- Key authorization policy engine.
- Access-control engine or electronic/smart lock integration.
- REPORTS-2.
- New report families beyond existing REPORTS-1 surfaces already authorized to consume Key information.
- Changes to issue eligibility rules beyond consuming Room identity already owned by Location.
- Loan/Return mutation changes.
- Automatic audit emission.
- Workforce, Organization, or Department maintenance expansion.
- Unrelated UI redesign or new visual system.
- Enterprise abstractions, speculative frameworks, placeholders, TODO, FIXME, or commented-out code.
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
- documentation/slices/KEY-LOOKUP-1.md
- documentation/slices/REPORTS-1.md

## Required Previous Slices
- REPORTS-1

## Allowed Files
- documentation/slices/KEY-ROOM-ASSIGNMENT-1.md
- documentation/implementation-roadmap.md
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/KEY-ROOM-ASSIGNMENT-1.md and documentation/implementation-roadmap.md
- REPORTS-2 slice files or new report-family feature folders
- access-control / policy-engine packages and files
- smart-lock / electronic access packages
- Elasticsearch or external index packages
- CI pipeline files
- Docker Compose files
- Testcontainers configuration
- SQLite provider packages or configuration
- EF Core InMemory provider packages or configuration

## Authority Owner
Key Catalog owns current KeyAsset-to-Room opening assignments; Location boundary remains sole owner of Building and Room identity; Building for a key is derived only through Room.

## Architectural Risks
- Letting Lock mediate or duplicate room-opening authority.
- Placing Building ownership on KeyAsset.
- Letting KeyType own Room assignments.
- Inventing assignment history or master/sub-master hierarchy.
- Duplicating Key-to-Room query logic across Catalog, Lookup, and Reports.
- Expanding REPORTS-1 into REPORTS-2 or new report families.
- Putting DbContext access in Web.
- Weakening SQL Server-only persistence.
- Changing issue/receive mutation semantics while adding assignments.

## Acceptance Criteria
- Domain implements current KeyAsset↔Room opening assignments under Key Catalog authority with governed cardinalities and uniqueness.
- KeyType does not own Room assignments.
- KeyAsset may exist with zero Room assignments.
- Operators can assign Rooms to an existing KeyAsset and later add/remove assignments.
- Only current assignments are authoritative; no assignment-history model is introduced.
- Building is derived exclusively through Room; KeyAsset does not independently own Building.
- No master/sub-master hierarchy is introduced.
- Lock does not mediate, duplicate, or remain required for room-opening authority.
- SQL Server persists the relationship through `ConnectionStrings:KeyInventory` without a second persistence model.
- Key Catalog shows Rooms opened by each Key.
- Find Key / operational lookup shows Rooms opened using the same authoritative relationship.
- Existing REPORTS-1 Key Catalog report shows Building/Room using the authoritative relationship.
- Application owns maintain/read commands and queries; Web consumes Application authorities only.
- Lookup and Reports reuse the authoritative relationship rather than duplicating it.
- Existing issue/receive behavior remains intact.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Repository hygiene PASS.
- Human acceptance checkpoint is reached only after Implementation Complete evidence is recorded.

## Required Tests
- Domain tests verify zero/one/many Room assignments per KeyAsset, many Keys per Room, uniqueness of current (KeyAsset, Room), and rejection of KeyType-owned assignments.
- Domain or Application tests verify assignments are editable and that a KeyAsset remains valid with zero Rooms.
- Domain or architecture tests verify Building is not owned on KeyAsset and Lock is not required for room-opening.
- Persistence tests verify SQL Server mapping/migration for current Key-to-Room assignments through `ConnectionStrings:KeyInventory`.
- Application or UI-boundary tests verify Catalog and Find Key / lookup expose Rooms opened without duplicate assignment authorities.
- Application or report tests verify existing REPORTS-1 Key Catalog report consumes Building/Room from the authoritative relationship.
- Architecture tests verify no assignment history, master/sub-master model, Lock mediation, REPORTS-2, or second persistence provider is introduced.
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
- No Lock mediation or Building duplication
- No assignment history or master/sub-master hierarchy
- No REPORTS-2 or new report families
- Existing issue/receive behavior intact
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After KEY-ROOM-ASSIGNMENT-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-09.
- Evidence: REPORTS-1 is Accepted; human governance completed Key-Room business authority and explicitly authorized KEY-ROOM-ASSIGNMENT-1; governing contracts define Key Catalog ownership of current KeyAsset↔Room assignments, cardinalities, optionality, editability, Building-via-Room derivation, Lock non-mediation, no history, no master/sub-master, and authorized Catalog/Lookup/REPORTS-1 Key Catalog consumption; slice specification defines runtime Domain/Application/Infrastructure/Web scope, persistence and UI requirements, out-of-scope exclusions, allowed/forbidden files, acceptance criteria, required tests, dependencies, risks, and human acceptance checkpoint without implementation.
- Deciding authority role: Human Architectural Governance.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-09.
- Evidence: Domain KeyAsset current OpenedRoomCodes assignments under Key Catalog with zero/one/many Rooms, uniqueness, editability, and no Building ownership; Application IKeyRoomAssignmentUseCase/port maintain and read current assignments; Infrastructure KeyRoomAssignments SQL Server table/migration through ConnectionStrings:KeyInventory with FKs to KeyAssets and Rooms only (no Lock mediation); Catalog Keys and KeyRooms UI maintain and display Rooms; Find Key / operational lookup and REPORTS-1 Key Catalog consume the same IKeyRoomAssignmentPersistencePort authority with Building derived through Room; CSV Rooms Opened parity preserved; no assignment history, master/sub-master, REPORTS-2, or second store; issue/receive unchanged; build PASS 0 warnings 0 errors; tests PASS 140/140.
- Deciding authority role: Implementation execution under approved slice specification.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-09.
- Evidence: KEY-ROOM-ASSIGNMENT-1 was Implementation Complete; Key Catalog-owned current KeyAsset↔Room assignments with zero/one/many cardinalities, editable current-only persistence on SQL Server, Building derived through Room, Catalog/Find Key/REPORTS-1 Key Catalog consumption with CSV parity, and Lock non-mediation remained within approved scope; no assignment history, master/sub-master model, REPORTS-2, second persistence store, or issue/receive mutation changes were introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
