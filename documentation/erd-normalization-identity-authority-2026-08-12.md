# ERD Normalization and Identity Authority Review

## Status
**TARGET:** Matrices and Target ERD below (analysis 2026-08-12).
**IMPLEMENTED (2026-08-12):** Migration `20260812224036_DepartmentIdentityNormalization` + Application/Web DepartmentId identity, structured Loan justification, fail-closed migration provenance extract. See `documentation/key-inventory-erd.md` Implementation status. Roadmap Next Allowed Slice remains STOP (unchanged). Sections titled “Current implemented” below are the pre-normalization baseline retained for audit history — not the live schema after DepartmentIdentityNormalization.

## Authority
- Human Governance identity rule (this review): entity identity = stable internal PK; business identifier = unique operator-facing value that may be editable; uniqueness ≠ identity.
- `documentation/key-inventory-erd.md` — logical ERD authority (updated to Target Normalized Model).
- `documentation/key-inventory-domain-contract.md` — Domain identity authority (Department active amendment).
- `documentation/implementation-roadmap.md` — sole slice sequencing authority (unchanged).

## Purpose
Record the complete identity/normalization audit that produced the Target ERD, including matrices, migration feasibility, and unresolved gaps that block implementation.

---

## 1. Current implemented ERD findings

Source: Domain entities + EF `*Entity`/`*Configuration` + migration `20260812015021_KeyAccessCopy1`.

| Entity / Table | PK | Business identifier(s) | Mutable business attrs | FKs (DeleteBehavior) | Unique | Lifecycle | Historical refs | Notes |
|---|---|---|---|---|---|---|---|---|
| Department | **DepartmentCode** | DepartmentCode | IsActive only | none | PK | IsActive | WM soft `DepartmentCode`; audit Details | Code is accidental persistence identity |
| Room | **RoomCode** | RoomNumber (unique) | RoomNumber, Description, IsActive | none | RoomNumber | IsActive | WA soft RoomCode; KAPRA FK RoomCode; audit Room justification | Correct dual-identity pattern |
| Party | **PartyCode** | UIN (unique) | First/Last, UIN, IsActive | none | UIN | IsActive | WM FK PartyCode; Loan soft borrower | Correct: technical PK + mutable UIN AK |
| WorkforceMember | **WorkforceMemberCode** | (none operator-facing beyond Party) | DepartmentCode, Type, Status | Party Restrict | filtered unique Active PartyCode | Status | WA soft WM code; audit | **DepartmentCode soft — no FK** |
| WorkAssignment | **WorkAssignmentCode** | — | IsPrimary, IsActive | **none** | filtered unique active primary | IsActive (End) | issue Room justification via RoomCode | **WM/Room soft — no FK** |
| KeyType | **TypeCode** | TypeCode | IsActive | none | PK | IsActive | KAP.KeyTypeCode FK | Classification code = PK |
| KeyAccessPattern | **KeyNumber** | KeyNumber (KEY #) | IsActive; KeyTypeCode | KeyType Restrict | PK | IsActive | KeyAsset FK KeyNumber; KAPRA PK part | KEY # is PK by KAC1 authority |
| KeyAccessPatternRoomAssignment | **(KeyNumber, RoomCode)** | — | (current assoc only) | KAP Restrict; Room Restrict | composite PK | Remove | not history store | Sole Room-access authority |
| KeyAsset | **KeyAssetId** (Guid) | (KeyNumber, MedecoKeyCode) | IsActive | KAP via KeyNumber Restrict | unique (KeyNumber, Medeco) | IsActive | Loan FK KeyAssetId | Custody identity correct |
| Loan | **LoanCode** | — | Status, timestamps | KeyAsset Restrict | — | Open/Returned/Cancelled | Return; audit | **No justification columns**; borrower soft |
| Return | **ReturnCode** | — | ReturnedAtUtc | Loan Restrict; LoanCode unique | LoanCode unique | — | — | 1:1 with Loan |
| OperatorAuditRecord | **AuditRecordId** | — | immutable row | none | — | append-only | Details free text | Snapshots / soft refs |

Identity tables (AspNet*): technical auth only; Cascade among Identity children; no business FKs to Party/WM.

---

## 2. Documentation vs implementation drift

| Topic | Documentation (pre-review) | Implementation | Drift |
|---|---|---|---|
| Department identity | Domain/ERD/OX1: DepartmentCode is identity / IMMUTABLE IDENTITY | DepartmentCode PK; no separate id | **Aligned with old contracts; contradicts Human Governance 2026-08-12** |
| Room identity | RoomCode immutable technical; RoomNumber mutable unique | Matches | None |
| KeyAsset identity | KeyAssetId + MEDECO within KEY # | Matches | None |
| KeyAccessPattern identity | KeyNumber = KEY # identity | KeyNumber PK | None (KeyNumber immutable by Domain — no rename API) |
| KeyRoomAssignment | Retired active authority | Table removed in KAC1 | None |
| WM → Department | Relationship required | Soft string, **no FK** | **Integrity drift** |
| WA → WM / Room | Relationships required | Soft strings, **no FK** | **Integrity drift** |
| Loan → Party | Borrower Party reference | Soft string, **no FK** | **Integrity drift** |
| Issue justification | Domain: required at issue; not Loan-owned | Audit Details only | **Semantic gap** (no structured Loan snapshot) |
| OX1 mutability “No hard delete” | Historical Accepted matrix | Superseded by active lifecycle amendment | Documented amendment exists |
| ERD lists Lock/Location/Lifecycle/Custody/AuditEvent | Logical future/baseline | Not all persisted | Expected logical vs physical lag |

---

## 3. Normalization findings (1NF / 2NF / 3NF + practical)

**1NF:** Persisted attributes are atomic. No repeating groups in business tables. PASS.

**2NF:** Association KAPRA uses full composite key; no partial-key attributes. PASS.

**3NF / practical defects:**
1. **DepartmentCode as PK** while Human requires editable business code → identity/business conflation (structural).
2. **Soft references** (WM.DepartmentCode, WA.WM/Room, Loan.Borrower) without FK → referential integrity not enforced; lifecycle/delete detection cannot rely solely on relational identity.
3. **Issue justification** not a structured historical snapshot on Loan → delete/rename safety depends on mutable-string audit search (contradicts lifecycle identity rule).
4. **No duplicate AUTHORITATIVE Room-access** — KeyAsset↔Room retired; KAPRA sole. PASS for KAC1.
5. **KeyType/Rooms on KeyAsset** — Domain derives from parent; persistence does not duplicate KeyType on KeyAsset. PASS.
6. **Availability** — derived from open Loan + IsActive; not a competing persisted authority. PASS.

---

## 4. Identity matrix (mutable business identifier)

| Entity | Stable identity | Business identifier | BI unique? | Editable? | Current PK | Target PK | Relationship target | Migration required? |
|---|---|---|---|---|---|---|---|---|
| Department | DepartmentId (Guid) | DepartmentCode | Yes | **Yes** | DepartmentCode | DepartmentId | DepartmentId | **YES** |
| Room | RoomCode | RoomNumber | Yes | Yes | RoomCode | RoomCode | RoomCode | **NO** |
| Party | PartyCode | UIN | Yes | Yes (governed) | PartyCode | PartyCode | PartyCode | **NO** (add Loan FK) |
| WorkforceMember | WorkforceMemberCode | — (person via Party) | — | Dept/Type yes | WorkforceMemberCode | WorkforceMemberCode | WorkforceMemberCode | **YES** (DepartmentId FK) |
| WorkAssignment | WorkAssignmentCode | — | — | IsPrimary; End | WorkAssignmentCode | WorkAssignmentCode | WorkAssignmentCode | **YES** (add FKs) |
| KeyType | TypeCode | TypeCode | Yes | **No** (immutable classification) | TypeCode | TypeCode | TypeCode | **NO** |
| KeyAccessPattern | KeyNumber | KeyNumber (KEY #) | Yes | **No** (immutable by KAC1/Domain) | KeyNumber | KeyNumber | KeyNumber | **NO** |
| KeyAsset | KeyAssetId | Medeco within KEY # | Yes within KEY # | Medeco no; IsActive yes | KeyAssetId | KeyAssetId | KeyAssetId | **NO** |
| Loan | LoanCode | — | — | workflow timestamps/status | LoanCode | LoanCode | LoanCode | **YES** (justification snapshots + Party FK) |
| Return | ReturnCode | — | — | — | ReturnCode | ReturnCode | ReturnCode | **NO** |
| KAPRA | (KeyNumber, RoomCode) | — | pair unique | Remove | composite | composite | KeyNumber + RoomCode | **NO** |
| OperatorAuditRecord | AuditRecordId | — | — | immutable | AuditRecordId | AuditRecordId | soft/snapshot | **NO** (forward Details may include DepartmentId) |

---

## 5. Relationship / cardinality matrix

| Relationship | Cardinality | Owner | Current storage | Target |
|---|---|---|---|---|
| Department → WorkforceMember | 1 → 0..N | WM holds FK | soft DepartmentCode | WM.DepartmentId → Department |
| Party → WorkforceMember | 1 → 0..N; ≤1 Active | WM holds FK | FK PartyCode | unchanged |
| WorkforceMember → WorkAssignment | 1 → 0..N | WA holds FK | soft WM code | FK Restrict |
| Room → WorkAssignment | 1 → 0..N | WA holds FK | soft RoomCode | FK Restrict |
| KeyType → KeyAccessPattern | 1 → 0..N | KAP holds FK | FK KeyTypeCode | unchanged |
| KeyAccessPattern → KeyAsset | 1 → 0..N | KeyAsset holds FK | FK KeyNumber | unchanged (KeyNumber immutable) |
| KeyAccessPattern ↔ Room | M ↔ N via KAPRA | KAPRA | composite PK + FKs | unchanged |
| KeyAsset → Loan | 1 → 0..N; ≤1 Open | Loan holds FK | FK KeyAssetId | unchanged |
| Party → Loan | 1 → 0..N | Loan holds FK | soft BorrowerPartyReference | FK PartyCode Restrict |
| Loan → Return | 1 → 0..1 | Return holds FK | FK LoanCode unique | unchanged |
| Department → Issue justification | historical 0..N | Loan snapshot | audit Details only | Loan.JustificationDepartmentId (+ kind) |
| Room → Issue justification | historical 0..N | Loan snapshot | audit Details; RoomCode stable | Loan.JustificationRoomCode |

---

## 6. Historical snapshot matrix

| Historical field | Current storage | Entity ref or snapshot? | Must follow rename? | Must preserve original? | Governing authority |
|---|---|---|---|---|---|
| Issue Department justification | OperatorAudit Details `Justification=Department/{code}` | **Snapshot** (text) | **No** | **Yes** | Domain eligibility + Audit immutability |
| Issue Room justification | OperatorAudit Details `Justification=Room/{roomCode}` | Snapshot (RoomCode is stable technical id) | N/A for RoomNumber rename | Yes | Domain + Audit |
| Loan borrower | Loan.BorrowerPartyReference | Live entity ref (soft) | Follows PartyCode (immutable) | PartyCode stable | Loan contract |
| Loan physical copy | Loan.KeyAssetId | Live entity ref (FK) | N/A | KeyAssetId immutable | KAC1 / Loan |
| OperatorAudit SubjectReference | string | Soft / display | No rewrite | Yes | OPERATOR-AUDIT-1 |
| OperatorAudit Details | free text | Snapshot / display | No rewrite | Yes | OPERATOR-AUDIT-1 |
| Reports / history exports | read models from Loan/Party/KeyAsset/Audit | Presentation from current Party names + historical audit text | Names follow current Party; audit text preserved | Audit original | REPORTS-1 / product experience |
| Target Loan justification | Loan.JustificationKind + JustificationDepartmentId + JustificationRoomCode | **Historical snapshot columns**; DepartmentId FK Restrict for integrity | DepartmentCode rename does not rewrite snapshot code in audit; FK uses DepartmentId | Preserve issue-time kind + ids | Domain (this review) |

**Decision (Issue justification):** At Issue time, eligibility is evaluated against **live** Department/Room. What is persisted for history is a **snapshot** of the authorizing Department/Room identity used at that moment — not a mutable live membership pointer. Target stores that snapshot on Loan using **stable ids** (`DepartmentId` / `RoomCode`) plus audit Details for operator-readable text. Audit rows are never rewritten when DepartmentCode changes.

---

## 7. Derived-data authority review

| Value | Classification | Notes |
|---|---|---|
| KeyAsset.OpenedRooms | DERIVED | From parent KeyAccessPattern / KAPRA |
| KeyAsset.KeyType | DERIVED | From parent KeyAccessPattern |
| Operational Available/Issued | DERIVED | Open Loan + IsActive |
| Current holder | DERIVED | Open Loan → Party |
| Building / Organization | removed | Historical audit only |
| WM.DepartmentId (target) | AUTHORITATIVE live membership | Not a snapshot |
| Loan.JustificationDepartmentId (target) | HISTORICAL SNAPSHOT | Not current membership |
| Party names on reports | AUTHORITATIVE current Party / presentation | Do not rewrite audit |
| Duplicate KeyAsset↔Room | FORBIDDEN | Must remain absent |

---

## 8. Entity determinations

### Department — REQUIRED CHANGE
- Real identity must be **DepartmentId** (Guid, immutable).
- **DepartmentCode** = unique editable business attribute.
- FAC → FACILITIES = same DepartmentId, new code; not delete/recreate.
- Current PK DepartmentCode is **business-facing** and incorrectly immutable by OX1 Accepted matrix.
- Relationships: WM membership = ENTITY REFERENCE → DepartmentId. Issue justification = HISTORICAL SNAPSHOT → DepartmentId on Loan + display in audit. Presentation shows DepartmentCode.
- Editing code today would require FK/string propagation across WM + audit search — **not normalized**.

### Room — NO CHANGE (identity)
- RoomCode already stable technical PK; RoomNumber editable unique AK.
- Do not change merely for symmetry with Department.
- Add missing **WorkAssignment → Room FK** (integrity), not new RoomId.

### WorkforceMember — PARTIAL CHANGE
- WorkforceMemberCode remains stable PK (system).
- UIN is Party alternate key (mutable governed), **not** WM PK.
- Change: Department relationship → DepartmentId FK Restrict.
- Person relationships use PartyCode (already).

### WorkAssignment — INTEGRITY CHANGE
- WorkAssignmentCode remains PK (association entity with surrogate code).
- Not Retire semantics; End / Delete-when-unused per lifecycle amendment.
- Add FKs to WorkforceMemberCode and RoomCode Restrict.
- No Department FK (not applicable).

### KeyType — NO CHANGE
- TypeCode is immutable classification code and PK by contract.
- Not KEY # authority. Editable TypeCode not authorized.

### KEY # / KeyAccessPattern — NO CHANGE
- KeyNumber is operator-facing KEY # and, by KEY-ACCESS-COPY-1 Domain authority, **immutable identity**.
- No KeyAccessPatternId required while KeyNumber remains immutable.
- KeyAsset and KAPRA correctly reference KeyNumber.
- Do not make KeyNumber editable without a new Human decision (would force KeyAccessPatternId).

### MEDECO / KeyAsset — NO CHANGE
- KeyAssetId stable PK; (KeyNumber, MedecoKeyCode) unique; Loan on KeyAssetId.
- Preserve custody authority.

### Loan — REQUIRED STRUCTURAL ADDITION
- Keep KeyAssetId FK.
- Add Party FK for borrower.
- Add immutable justification snapshot columns (kind + DepartmentId and/or RoomCode).
- Justification is **B — historical snapshot**, not live membership.

### Audit — NO REWRITE
- Append-only; Details remain historical display snapshots.
- Forward Details may include DepartmentId for interpretability after renames; never rewrite old rows.

---

## 9. Target ERD (summary)

See authoritative detail in `documentation/key-inventory-erd.md` § Target Normalized Relational Model.

```
Department(DepartmentId PK, DepartmentCode AK UNIQUE, IsActive)
Room(RoomCode PK, RoomNumber AK UNIQUE, Description, IsActive)
Party(PartyCode PK, UIN AK UNIQUE, FirstName, LastName, IsActive)
WorkforceMember(WorkforceMemberCode PK, PartyCode FK, DepartmentId FK, WorkforceType, Status)
WorkAssignment(WorkAssignmentCode PK, WorkforceMemberCode FK, RoomCode FK, IsPrimary, IsActive)
KeyType(TypeCode PK, IsActive)
KeyAccessPattern(KeyNumber PK, KeyTypeCode FK, IsActive)
KeyAccessPatternRoomAssignment(KeyNumber FK, RoomCode FK, PK(KeyNumber,RoomCode))
KeyAsset(KeyAssetId PK, KeyNumber FK, MedecoKeyCode, IsActive, UNIQUE(KeyNumber,MedecoKeyCode))
Loan(LoanCode PK, KeyAssetId FK, BorrowerPartyReference FK→Party,
     JustificationKind, JustificationDepartmentId FK?, JustificationRoomCode FK?,
     IssuedAtUtc, DueAtUtc, Status)
Return(ReturnCode PK, LoanCode FK UNIQUE, ReturnedAtUtc)
OperatorAuditRecord(AuditRecordId PK, … Details snapshot …)
```

All business FKs: Restrict. No business-history cascade.

---

## 10. Current → Target structural delta

| CURRENT | TARGET | WHY | MIGRATION CONSEQUENCE |
|---|---|---|---|
| DepartmentCode PK | DepartmentId PK + DepartmentCode UNIQUE | Code must be editable without changing identity | Add Guid per row; rewrite WM refs |
| WM.DepartmentCode soft | WM.DepartmentId FK Restrict | Live membership must use stable identity | Join map DepartmentCode→DepartmentId; STOP if orphan codes |
| WA no FKs | WA→WM, WA→Room FK Restrict | Relationship integrity | Add FKs; STOP if orphans |
| Loan borrower soft | Loan→Party FK Restrict | Borrower is Party entity ref | Add FK; STOP if orphans |
| Justification audit-only | Loan snapshot columns + audit text | Rename-safe history + delete eligibility | Forward-only DETERMINISTIC; **backfill AMBIGUOUS** |
| Room / Party / KeyType / KEY # / KeyAsset / KAPRA / Return | unchanged identity | Already correct or immutable by contract | NO CHANGE |
| KeyRoomAssignment dual authority | remains absent | KAC1 | NO CHANGE |

---

## 11. Migration feasibility matrix

| Change | Classification | Notes |
|---|---|---|
| DepartmentId per Department row | **DETERMINISTIC** | New Guid per existing row |
| WM.DepartmentId from DepartmentCode | **DETERMINISTIC** if every WM.DepartmentCode exists in Departments; else **STOP** | Precheck required |
| WA FKs to WM/Room | **DETERMINISTIC** if all codes exist; else **STOP** | Precheck required |
| Loan→Party FK | **DETERMINISTIC** if all BorrowerPartyReference exist; else **STOP** | Precheck required |
| Loan justification columns nullable for existing rows | **DETERMINISTIC** | Forward-only |
| Backfill justification DepartmentId from audit Details | **AMBIGUOUS — STOP** | Free-text Details; Human forbade guessing/parsing unless separately authorizing Application-owned deterministic extractor |
| Rewrite historical audit on rename | **FORBIDDEN** | Immutability |
| Compatibility / dual-write columns | **FORBIDDEN** | Human rule |

---

## 12. Domain / Application / Infrastructure / Web impact (no implementation)

| Layer | Required when implementing |
|---|---|
| Domain | Department(DepartmentId, DepartmentCode); `RenameCode`; WM holds DepartmentId; Loan justification snapshot value object/fields; remove assumption DepartmentCode is identity |
| Application | Update Department use case; create/list/lifecycle ports use DepartmentId; eligibility uses DepartmentId; Issue persists Loan justification snapshots; delete eligibility uses FKs not mutable-string search; Edit DepartmentCode row action |
| Infrastructure | Migration with STOP prechecks; EF configs/FKs Restrict; adapters map ids |
| Web | Department Edit for code; selectors still show DepartmentCode; posts use DepartmentId where relationship |

No speculative abstractions. No new slice prepared here.

---

## 13. Unresolved authority gaps (block implementation)

Superseded detail: `documentation/department-historical-justification-provenance-2026-08-12.md`.

1. **Historical Issue justification format classification: B (semi-structured but not governed).** Deterministic backfill from `OperatorAuditRecord.Details` is **not authorized** without an explicit Human **Migration Provenance Extract** (or Human mapping). Runtime Details string search is forbidden as delete authority. Permanently forbidding DepartmentCode rename because of old snapshots is **rejected**.
2. **Legacy delete-protection linkage** to DepartmentId remains STOP until Human authorizes Migration Provenance Extract or Human mapping into structured Loan justification (or equivalent) rows.
3. Implementation of the broader DepartmentId model remains unauthorized until gap #2 is closed and Human authorizes implementation (roadmap remains STOP).

---

## 14. Acceptance Gate (this review)

| Criterion | Result |
|---|---|
| Explicit identity authority per entity | PASS (documented) |
| Mutable BI not accidental PK (target) | PASS (Department corrected in target) |
| FK semantic ownership | PASS (target) |
| Cardinalities defined | PASS |
| No duplicate Room-access authority | PASS |
| Historical vs live distinguished | PASS |
| Lifecycle can use stable relationships (target) | PASS pending justification backfill choice |
| KEY # / MEDECO intact | PASS |
| Loan custody KeyAssetId | PASS |
| KAPRA sole Room access | PASS |
| Migration feasibility known | PASS (including AMBIGUOUS backfill) |
| Contracts reconciled | PASS (active amendments) |
| Unresolved dependency | **GAP #1 recorded — implementation STOP** |

Build/tests are not acceptance for this review.

## Decision (updated after Human authorization + implementation)
**Human authorized Migration Provenance Extract; normalized ERD implementation completed locally** (`DepartmentIdentityNormalization`). Runtime Details parsing remains forbidden. Roadmap Next Allowed Slice remains STOP.

## Next permitted action
Human review / accept implementation. Do not prepare a slice unless Human authorizes.
