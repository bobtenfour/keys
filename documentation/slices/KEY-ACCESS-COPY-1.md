# KEY-ACCESS-COPY-1 - KEY # Access Pattern and Physical MEDECO Copies

## Status
Accepted

## Progress Record
- Decision: Approved → In Progress
- Date: 2026-08-11
- Authority: Human architectural governance (explicit Authorize KEY-ACCESS-COPY-1 Approved → In Progress)
- Evidence: Pre-implementation gate verified against Approved slice and reconciled contracts; sole authorized implementation slice.

## Closure Record
- Decision: In Progress → Implementation Complete
- Date: 2026-08-11
- Authority: Implementation execution under approved slice
- Evidence: Build PASS 0/0; tests PASS 184/184 including KeyAccessCopy1WorkflowTests; migration `KeyAccessCopy1` STOP gate when KeyAssets/KeyRoomAssignments/Loans have rows; empty DB `KeyInventory_KAC1` migrated and interactive workflow validated (KEY # 66800 / Room 410D / MEDECO 26–28 simultaneous issue; return 26 leaves 27 issued; MASTER1 multi-room; Find/Catalog/Active/Reports distinguish KEY # vs MEDECO); KeyInventoryDev left unmigrated with existing CatalogKeyCode rows (STOP path preserved). Next Allowed Slice remains STOP.

## Parent Phase
Later Phases — Key Access Pattern and Physical Copy Authority

## Preparation Record
- Decision: Prepare Next Slice KEY-ACCESS-COPY-1, Planned to Approved
- Date: 2026-08-11
- Authority: Human architectural governance (explicit Prepare Next Slice KEY-ACCESS-COPY-1 instruction with final business authority for KEY #, MEDECO, Room cardinality, custody, and normalized model)
- Evidence: OPERATOR-EXPERIENCE-1 is Accepted; human business evidence distinguishes KEY # (shared Room/access authority) from MEDECO Key Code (physical copy unique within KEY #); prior KEY NUMBER / MEDECO PHYSICAL COPY DOMAIN AUDIT classified current model as structurally incomplete; human forbids silent KeySeries reinterpretation; preparation chooses distinct KeyAccessPattern aggregate; governing Domain/ERD/architecture/authority/capability/product-experience/integrity/operator-guide contracts reconciled in the same preparation; no implementation; Status Approved only.

## Purpose
Make KeyInventory model the real operator distinction between a shared KEY # / access pattern (what Rooms open) and individual physical MEDECO key copies (what is issued to a person), without flattening into spreadsheet rows or duplicating Room access per copy.

## Objective
After implementation, KeyInventory persists and operates a normalized model:

Room(s) ↔ KEY # / KeyAccessPattern ↔ Physical KeyAsset copies (MEDECO) ↔ Issue / Return / Holder

Operators can create KEY # values, assign Rooms at KEY # level, register multiple MEDECO copies under one KEY #, issue and return exact physical copies (including simultaneous issues of different copies under the same KEY #), find and report KEY # vs copy distinctions, and audit access-pattern changes separately from custody—without dual Room-access authority, without global MEDECO uniqueness, and without Transfer.

## Scope
- Introduce Domain aggregate **KeyAccessPattern** as sole KEY # / access-pattern authority (operator-facing **KEY #** / `KeyNumber`, unique installation-wide).
- Retain **KeyAsset** as one physical key copy under exactly one KeyAccessPattern; introduce immutable internal **KeyAssetId**; operator-facing **MEDECO Key Code** unique within that KeyAccessPattern only.
- Move current Room-opening authority from KeyAsset↔Room to **KeyAccessPattern↔Room**; remove/retire KeyAsset-level Room assignment authority so exactly one source of truth answers “What Rooms does this KEY # open?”
- Physical copies derive Rooms opened solely from parent KeyAccessPattern.
- **KeyType** owned at KeyAccessPattern level; physical copies derive type from parent (no duplicated mutable type authority).
- **KeySeries** is not elevated and must not become KEY #, Room-access, or copy identity authority.
- Retire **CatalogKeyCode** as unique physical-copy business identity; do not use opaque composite strings (e.g. `66800-28`) as identity authority.
- Loan/Return remain custody against the physical KeyAsset (KeyAssetId); at most one open Loan per copy; simultaneous open Loans allowed for different copies under the same KEY #.
- Application use cases, ports, SQL Server migration, Catalog/Issue/Return/Find/Reports/export surfaces, OperatorAuditRecord subjects, readiness, and operator guide text for KEY # / MEDECO.
- Presentation terminology: KEY #, MEDECO Key Code, Room #, person identity; Issue/Return lifecycle labels retained.
- Required tests proving cardinality, uniqueness, derivation, custody, lookup, reporting, audit level, single Room-access authority, and no Web DbContext.

## Out of Scope
- Transfer / Transferred From (unresolved; do not add, placeholder, or reinterpret Return+Issue as Transfer).
- New Key / Pick up terminology beyond KEY # and MEDECO presentation required by this slice.
- Master/sub-master hierarchy, inheritance, policy, or recursive access engines (a master key is only a KEY # with multiple Rooms).
- REPORTS-2 or new report families.
- Elevating or silently reinterpreting KeySeries as KEY #.
- Global MEDECO uniqueness.
- Guessing migration semantic mapping of existing CatalogKeyCode values.
- Rewriting historical OperatorAuditRecord, Loan, or Return business meaning.
- Workflow engines, wizard frameworks, second persistence store, SQLite/InMemory.
- Marking this slice In Progress or Accepted in preparation.
- Preparing another slice.
- Git operations unless explicitly requested.

## Structural Choice (KEY # authority)
**Choice B — distinct KeyAccessPattern aggregate.**

Reasons (architecture/domain semantics; not a residual human fork):
- Domain defines KeySeries as optional **classification**, not Room-access authority.
- Current contracts place Room openings on physical KeyAsset, not KeySeries.
- KeySeries is not persisted or used at runtime; elevating it would force a silent reinterpretation of “classification” into operational access-pattern aggregate authority.
- Human preparation instruction forbids silent KeySeries reinterpretation and forbids two competing KEY # authorities.
- An explicit KeyAccessPattern aggregate with operator-facing KeyNumber (KEY #) is the clear sole access-pattern authority.

KeySeries remains a non-operational Domain classification seed only; this slice must not persist KeySeries as KEY #, Room access, or copy identity.

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
- documentation/system-integrity-contract.md
- documentation/slices/KEY-ACCESS-COPY-1.md
- documentation/operator/keyinventory-operator-guide.md
- documentation/slices/OPERATOR-EXPERIENCE-1.md (dependency Accepted; do not rewrite history)
- documentation/slices/KEY-ROOM-ASSIGNMENT-1.md (historical KeyAsset↔Room; do not rewrite Accepted history)
- documentation/slices/CATALOG-1.md / LOAN-RETURN-1.md / KEY-LOOKUP-1.md / REPORTS-1.md / REPORT-EXPORTS-1.md (historical evidence only)

## Required Previous Slices
- OPERATOR-EXPERIENCE-1

## Allowed Files
- documentation/slices/KEY-ACCESS-COPY-1.md
- documentation/implementation-roadmap.md
- documentation/roadmap.md
- documentation/key-inventory-domain-contract.md
- documentation/key-inventory-erd.md
- documentation/architecture-contracts.md
- documentation/product-experience-contract.md
- documentation/business-authority-matrix.md
- documentation/key-inventory-capability-map.md
- documentation/system-integrity-contract.md
- documentation/project-architecture-index.md (index sync only if required)
- documentation/operator/**
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/**

## Forbidden Files
- Accepted slice history content rewrites
- CI/demo redesign unrelated to KeyAccessPattern / physical-copy migration
- Introduction of SQLite, EF InMemory, second database, Redis, workflow engines, Transfer features, REPORTS-2

## Authority Owner
- Domain/Key Catalog: KeyAccessPattern (KEY #), KeyAccessPattern↔Room openings, KeyAsset physical copies (MEDECO within KEY #), KeyType classification at pattern level.
- Domain/Loan-Return: custody Issue/Return against KeyAsset only.
- Application: all mutations, availability, lookup, reports, readiness, audit staging—no Web-duplicated rules.
- Infrastructure: SQL Server schema migration and adapters; STOP on non-deterministic CatalogKeyCode semantic mapping.
- Web: presentation of KEY #, MEDECO, Room #, person identity; Issue/Return/Find/Catalog/Reports consume Application authorities only.

## Identifier Contract
| Identifier | Authority | Uniqueness | Notes |
|---|---|---|---|
| KEY # (`KeyNumber`) | KeyAccessPattern | Installation-wide unique | Operator-facing shared access number |
| MEDECO Key Code | KeyAsset (physical copy) | Unique within parent KeyAccessPattern only | Not globally unique |
| Operational copy identity | Pair (KEY #, MEDECO) | — | Presentation/business pair; not a parsed composite string |
| KeyAssetId | KeyAsset | Globally unique immutable internal identity | GUID (or equivalent); not operator-typed |
| CatalogKeyCode | Retired as unique business identity | — | Must not remain sole physical identity; no opaque `KEY#-MEDECO` composite as authority |
| KeySeries.SeriesCode | Not KEY # authority | — | Must not compete |

## Cardinality Matrix
| Rule | Requirement |
|---|---|
| One KEY # → physical copies | Zero or more KeyAssets |
| One physical copy → KEY # | Exactly one KeyAccessPattern |
| One KEY # → Rooms | Zero or more Rooms (many-to-many) |
| One Room → KEY # values | Zero or more KeyAccessPatterns (many-to-many) |
| MEDECO within KEY # | Unique |
| MEDECO across KEY # | May repeat |
| Open Loans per physical copy | At most one |
| Open Loans under one KEY # | Many (different copies) |
| Rooms for every copy under KEY # | Identical; derived from parent only |

## Room-access Authority
- Sole authority: current KeyAccessPattern↔Room assignments owned by Key Catalog.
- KeyAsset must not own independent Room openings after this slice.
- Lock, KeyType, and KeySeries must not own Room openings.
- No dual KeyAsset↔Room and KeyAccessPattern↔Room authority.
- Master key = another KEY # whose Room set contains multiple Rooms; no special hierarchy engine.

## Custody Authority
- Issue, Return, holder, open custody operate on KeyAsset (physical copy).
- Loan references KeyAssetId (not KEY # alone, not CatalogKeyCode as identity).
- Availability for Issue is Application/Domain over KeyAsset + open Loan rules (not Web).

## Issue UX Contract
Operator identifies:
1. Key holder (WorkforceMember / Party; human-readable identity + UIN; no KeyHolder entity)
2. KEY #
3. Available physical copy / MEDECO under that KEY #
4. Rooms opened (derived from KEY #; read-only; not re-entered)

Do not expose internal KeyAssetId as a typing target.

### Active presentation note (does not rewrite Acceptance Record)
Key holder selection is bounded search-on-demand over Application-eligible candidates (name/UIN). Fresh Issue must not auto-select holder, KEY #, MEDECO, or justification. See product-experience-contract.md Issue / Receive Interaction.

## Return UX Contract
Operator identifies the exact physical copy as KEY # + MEDECO (and existing Loan selection path), distinguishing e.g. 66800/26 from 66800/27. Lifecycle remains existing Return completion of an Open Loan.

### Active presentation note (does not rewrite Acceptance Record)
Fresh Receive must not auto-select first/only active issue. Deliberate deep-link selection remains allowed. See product-experience-contract.md Issue / Receive Interaction.

## Lookup Contract
Existing Find/lookup Application authorities must answer without a second search store:
- Rooms opened by KEY #
- Physical copies under KEY #; available vs issued
- Who holds MEDECO X under KEY # Y
- Which KEY # / MEDECO a person currently holds
- Which KEY # values open Room #

### Active implementation note (does not rewrite Acceptance Record)
Room reverse-search (“Which KEY # values open Room #?”) is satisfied by the existing `IOperationalKeyLookupUseCase` / Find path (2026-08-12 structural contract closure). Operators search by operator-facing **Room #** (`RoomNumber`); traversal is RoomNumber → RoomCode → KeyAccessPatternRoomAssignment → KEY # → MEDECO copies / custody. No second search store, no Web DbContext, no KeyAsset↔Room authority. Header search is separately owned by global operator search (`IGlobalOperatorSearchUseCase` / `/Search`) and may also present Room subjects; Find Key remains the key-specific Operations surface. In-application `/Help` documents Find behavior as presentation-only operator reference (no second lookup authority).

## Reporting Contract
Existing REPORTS-1 (+ CSV/XLSX/PDF parity) must distinguish KEY #, MEDECO/physical copy, Holder, Rooms opened, Issue/Return state. No REPORTS-2.

## Audit Contract
- OperatorAuditRecord continues to record authenticated operator.
- New/changed actions identify subject level: KEY # created/maintained; physical copy registered; Room access added/removed at KEY #; physical copy issued; physical copy returned.
- Historical audit rows under the prior model remain immutable and readable (no rewrite).

## Migration / Data-Preservation Contract
1. Structural SQL Server migration may create KeyAccessPattern, KeyAccessPattern↔Room, KeyAssetId, MedecoKeyCode, and Loan FK to KeyAssetId.
2. Existing KeyAssets / KeyRoomAssignments / Loans that require semantic interpretation of CatalogKeyCode as KEY # vs MEDECO vs composite vs arbitrary **must STOP** rather than guess, parse, auto-split, or fabricate KEY # groupings.
3. No silent parsing; no automatic splitting; no duplication of access relationships; no data loss; no historical Loan/Return/Audit rewriting of business meaning.
4. If the database is empty or contains only controlled demo/test data with an explicitly authorized reset/reseed procedure, that reset may be used **separately** from production/real-data migration authority and must be documented in implementation closure—not conflated with guessing production CatalogKeyCode semantics.
5. After migration, exactly one Room-access authority remains (KEY # level).

## Master-key Treatment
A master key is a KEY # associated with multiple Rooms. No nested master/sub-master engine.

## Terminology Contract (presentation)
| Concept | Operator-facing term |
|---|---|
| KeyAccessPattern.KeyNumber | KEY # |
| KeyAsset.MedecoKeyCode | MEDECO Key Code |
| Room.RoomNumber | Room # |
| Holder | Person name + UIN |
| Issue / Return | Issue Key / Return (or existing Receive label only if not changed by this slice; prefer distinguishing exact copy) |

Do not implement Transfer/New Key terminology in this slice.

## Operator Guide Contract
`documentation/operator/keyinventory-operator-guide.md` must explain:
- KEY # → what Rooms it opens
- MEDECO → which physical copy a person has
- Example: KEY # 66800; Rooms opened: 410D; Copies MEDECO 26–28 (and peers)
Screenshots must not be fabricated during preparation; refresh screenshots only after runtime finalization of this slice.

## Architectural Risks
- Elevating KeySeries as a competing KEY # authority
- Leaving KeyAsset↔Room dual authority
- Composite string identity
- Global MEDECO uniqueness invention
- Custody moved to KEY #
- Migration guessing CatalogKeyCode semantics
- Transfer feature creep
- Web-duplicated availability/eligibility
- REPORTS-2

## Acceptance Criteria
- KeyAccessPattern is sole KEY # / Room-access authority; KeyNumber unique installation-wide.
- KeyAsset is physical copy with KeyAssetId + MedecoKeyCode unique within parent KEY #; MEDECO may repeat under another KEY #.
- Every copy under a KEY # derives the same Room set; KeyAsset cannot own conflicting Room assignments.
- KeyType owned on KeyAccessPattern; copies derive type; KeyType does not own Rooms.
- KeySeries is not used as KEY # / Room / copy authority.
- CatalogKeyCode is not unique physical business identity; no opaque composite identity authority.
- Different copies of same KEY # may be issued simultaneously; one copy cannot have multiple open Loans.
- Issue and Return identify exact physical copy; UI shows KEY # + MEDECO + derived Rooms; no internal ID typing; no Room re-entry on Issue.
- Find/lookup answers KEY #-level and copy-level questions listed in Lookup Contract.
- Reports/exports distinguish KEY # / MEDECO / holder / rooms / state with screen/CSV/XLSX/PDF parity.
- Audit distinguishes access-pattern maintenance from custody Issue/Return; historical rows unre-written.
- Migration obeys STOP-on-ambiguous-mapping and demo-reset separation rules.
- No Web DbContext; Application owns rules; normalized relational ownership preserved.
- Build 0 warnings / 0 errors; required tests PASS.

## Required Tests
- One KEY # owns many physical copies.
- MEDECO may repeat under different KEY # values.
- Duplicate MEDECO within same KEY # rejected.
- Each physical copy belongs to exactly one KEY #.
- One KEY # opens many Rooms; one Room opened by multiple KEY # values.
- Every copy under a KEY # derives the same Room set.
- Physical copy cannot own independent conflicting Room assignments.
- Different copies of same KEY # can be simultaneously issued.
- One copy cannot have multiple open Loans.
- Issue identifies exact physical copy; Return identifies exact physical copy.
- Find distinguishes KEY # and copy; Room→KEY # lookup works.
- Reporting distinguishes KEY # / MEDECO / holder / rooms.
- Audit distinguishes access-pattern changes from custody changes.
- No duplicate Room-access authority after migration path under test.
- No Web project reference to DbContext in architecture tests.
- Normalized ownership (pattern → copies → loans) preserved in Domain/Application tests.

## Closure Contract
- Transversal Gate PASS
- Architecture / Authority / ERD / Capability / Product experience consistency PASS
- Build PASS (0 warnings, 0 errors)
- Tests PASS
- Repository hygiene PASS
- Documentation updated only as required by this slice
- Migration STOP evidence recorded if real data mapping was required and blocked

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-11.
- Evidence: KEY-ACCESS-COPY-1 was Implementation Complete; KeyAccessPattern remains sole KEY # / Room-access authority; KeyAsset remains physical MEDECO-copy custody authority with KeyAssetId and MEDECO unique within KEY #; KeyType remains classification at KEY # level; KeySeries remains non-authority; CatalogKeyCode unique-physical-identity retired; KeyAsset↔Room dual authority retired; migration STOP preserved for ambiguous legacy CatalogKeyCode data without parsing/guessing; Issue/Return/Find/Reports distinguish KEY # vs MEDECO; simultaneous issue of distinct copies under one KEY # validated; master key represented only as KEY # with multiple Rooms; no Transfer, REPORTS-2, KeySeries elevation, or successor-slice preparation introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Active Presentation Amendment — Register Key / Key Type authority (does not rewrite Acceptance Record)
- Decision: SUPERSEDE Register Key free-text Key Type / silent Key Type creation presentation only.
- Date: 2026-08-13.
- Authority: Human Governance via active `documentation/product-experience-contract.md` (Operator Interaction Architecture; KEY-ACCESS-COPY-1 Product Experience).
- Scope: Presentation and Application CreateKeyAsset / CreateKeyType orchestration. KeyAccessPattern remains KEY # / Room-access authority; KeyAsset remains physical MEDECO copy; KeyType remains classification. Migration STOP unchanged. Roadmap Next Allowed Slice remains STOP.
- Active rules: Register copy under existing KEY # derives Type/Rooms; Create new KEY # requires selecting an existing Key Type; Key Types created on Catalog → Key Types → Add; no silent Key Type creation from Register.
- Historical Accepted evidence above remains unchanged.

## Next Allowed Slice
STOP

## Governance Notes
- Do not prepare another slice until human governance issues Prepare Next Slice after this Accepted baseline.
