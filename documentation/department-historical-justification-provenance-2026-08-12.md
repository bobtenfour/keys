# Department Historical Justification Provenance Audit

## Status
**TARGET (analysis):** Format classification B for runtime authority (below) remains binding.
**IMPLEMENTED (2026-08-12):** One-time migration provenance extract authorized and shipped in `KeyIssuedJustificationProvenanceExtract` (migration-scoped only). Structured Loan justification is relational authority after migration. Runtime Details parsing remains forbidden (Department and Room delete eligibility use Loan structured FKs). Roadmap Next Allowed Slice remains STOP (unchanged).

## Primary question
Can existing historical Issue / KeyIssued Department justification be mapped to a Department deterministically?

## Format classification (exact)

**B. SEMI-STRUCTURED BUT NOT GOVERNED**

### Why not A
- OPERATOR-AUDIT-1 requires only “concise structured Details”; it does **not** define a grammar, field order, separators, escaping, or KeyIssued Details schema.
- No test asserts `Details` contains `Justification=Department/{code}` or locks the segment.
- `DepartmentCode` / justification code charset is not governed: `WorkforceText.Require` only trims; `/` and `;` are not forbidden, so a naive segment extract can be ambiguous.
- Case matching at Issue uses `StringComparison.OrdinalIgnoreCase` in Domain eligibility, but persistence stores the trimmed submitted code as-is — no documented canonical case for reverse mapping.

### Why not C
- There is a single Application writer for KeyIssued Issue justification: `IssueLoanUseCase`.
- Since OPERATOR-AUDIT-1 introduced KeyIssued audit, every generation emits a trailing segment of the form:
  - `Justification={kind}/{justificationCode?.Trim()}`
  - where `kind` is `KeyIssueJustificationKind` (`Department` or `Room`) via enum formatting.
- At Issue time, Department justification is **not free text**: `KeyIssueEligibility.EnsureEligible` requires the code to match the WorkforceMember’s authorized Department (after loading the Department entity). Authoritative source at write = Department entity via membership + validated justification code.

### Why not purely D (as the sole label)
- The **Justification=** segment pattern has been stable across both KeyIssued Details generations.
- The **Details envelope** did change once (MIXED envelope), which reinforces that Details as a whole is not a frozen contract — hence **B**, not **A**.

### Details generations (code history)

| Generation | Slice / commit | Details shape |
|---|---|---|
| Gen-1 | OPERATOR-AUDIT-1 (`f074162`) | `Key={CatalogKeyCode}; WorkforceMember={code}; Justification={kind}/{code}` |
| Gen-2 | KEY-ACCESS-COPY-1 (`f31b538`) | `KEY#={KeyNumber}; MEDECO={Medeco}; KeyAssetId={Guid:D}; WorkforceMember={code}; Justification={kind}/{code}` |

No other Application writers of KeyIssued with Department justification were found.

---

## Historical Issue write path

1. Web/Application invokes `IIssueLoanUseCase.ExecuteAsync(..., justificationKind, justificationCode, ...)`.
2. `IssueLoanUseCase` parses kind (`Department` \| `Room`), loads KeyAsset, WorkforceMember, Party, **Department entity** (`FindDepartmentAsync(member.DepartmentCode)`), active WorkAssignments.
3. `KeyIssueEligibility.EnsureEligible(...)` validates justification against that Department / assigned Room.
4. Domain `Loan` is created **without** justification fields.
5. `_audit.Stage(OperatorAuditActions.KeyIssued, OperatorAuditSubjects.Loan, loan.LoanCode, details)`.
6. `OperatorAuditRecorder` persists append-only `OperatorAuditRecord` atomically with Loan insert.

**Authoritative source at event time:** Department aggregate (for Department kind), selected through membership + eligibility — emitted into Details as the validated `justificationCode` string (DepartmentCode value at that moment).

**Action / subject constants:**
- ActionType = `Key issued` (`OperatorAuditActions.KeyIssued`)
- SubjectType = `Loan`
- SubjectReference = `LoanCode`

---

## Rename semantics (confirmed)

| Rule | Authority | Result |
|---|---|---|
| DepartmentCode editable | Human Governance + active Domain/ERD amendment | Live identity stays DepartmentId |
| Audit immutable | Domain (“Audit history must not be rewritten”); OPERATOR-AUDIT-1 append-only; OX1 historical audit preservation | Old KeyIssued Details keep event-time code (e.g. `FAC`) |
| No delete/recreate on rename | Domain Department invariants | Same DepartmentId |

**No contradiction** with governing history/audit contracts.

Forbidden by Human Governance for this closure: permanently forbidding rename solely because old snapshots contain the old code.

---

## Delete / Retire semantics (target)

| Condition | Action |
|---|---|
| No live WM membership, no structured historical Issue justification reference, no other governed refs | Delete allowed (where entity permits) |
| Any live or structured historical reference | Delete forbidden → Retire |

Preferred structured historical representation (when populated):

| Field | Role |
|---|---|
| `Loan.JustificationDepartmentId` | Which Department participated (stable; Restrict) |
| `Loan.JustificationDepartmentCodeSnapshot` | Event-time business label (immutable snapshot; not naming authority) |

Runtime delete eligibility must use these (and live FKs), **not** `OperatorAuditRecord.Details` string search.

---

## Target Loan justification model (new records)

Kinds (existing Domain): `Department` \| `Room` (`KeyIssueJustificationKind`).

### Department justification
- `JustificationKind = Department`
- `JustificationDepartmentId` = required FK → Department (Restrict)
- `JustificationDepartmentCodeSnapshot` = required immutable event-time DepartmentCode
- `JustificationRoomCode` = null

### Room justification
- `JustificationKind = Room`
- `JustificationRoomCode` = required FK → Room (Restrict) — RoomCode is already stable technical identity; **no RoomNumber snapshot field required** (not in current audit as RoomNumber; not added for symmetry)
- `JustificationDepartmentId` = null
- `JustificationDepartmentCodeSnapshot` = null

### Invariants (CHECK / Application)
- Exactly one justification shape populated per kind.
- Kind None forbidden on persisted issued Loan.
- Snapshot code for Department is write-once at Issue; never updated on Department rename.
- Borrower remains Party FK; custody remains KeyAssetId.

### Forward audit (new KeyIssued)
- Keep append-only OperatorAuditRecord.
- Details may remain operator-readable; must not be relational delete authority.
- Recommended forward Details include stable ids, e.g. `DepartmentId={guid}` alongside snapshot code, without making Details the integrity authority.
- Department rename emits a **new** audit row: Department subject = DepartmentId (or stable ref), Details old→new DepartmentCode; prior rows untouched.

---

## Legacy migration determination

**FORWARD-ONLY WITH LEGACY SNAPSHOT** for OperatorAuditRecord.Details (immutable display history).

**DETERMINISTIC BACKFILL not authorized** under classification **B** and Human rule §4 (ungoverned grammar / charset / no test lock).

**Permanently forbidding rename: REJECTED** by Human Governance in this audit.

### Legacy delete-protection authority (critical)

Because backfill-by-parse is not authorized:

- Legacy KeyIssued Department use exists only as semi-structured Details text today.
- That text must **not** become runtime relational authority.
- Therefore a **durable structured reference** (Loan justification columns, or an equivalent Issue-justification structure keyed by LoanCode + DepartmentId + CodeSnapshot + SourceAuditRecordId) is **structurally necessary** for lifecycle delete protection of historically used Departments.

**Population of that durable structure from legacy rows is STOPPED** until Human explicitly authorizes one of:

1. **Migration Provenance Extract (one-time)** — elevate the de-facto `IssueLoanUseCase` `Justification=Department/{code}` segment to migration-only authority, with STOP on: non-Key-issued rows, missing segment, kind≠Department, code containing `;`, ambiguous `/` splits, orphan code, or non-unique Department match under Domain’s OrdinalIgnoreCase rule; write Loan (or equivalent) structured fields; never use Details search at runtime thereafter; or
2. **Human mapping** of legacy LoanCode → DepartmentId for rows that cannot pass (1).

Until (1) or (2), implementation of DepartmentId + editable DepartmentCode may proceed for live FKs and **new** Issues, but **Delete eligibility cannot claim full historical coverage** for pre-cutover Issue-only Departments — that residual is an explicit STOP for complete lifecycle acceptance unless Human accepts a documented residual (not recommended).

---

## Relationship to prior ERD normalization gap

Supersedes the open A/B/C choice in `erd-normalization-identity-authority-2026-08-12.md` §13 that included “forbid rename”:

| Prior option | This audit |
|---|---|
| A deterministic extractor | Not authorized until Human elevates format (Migration Provenance Extract) |
| B forward-only + residual delete gap | Insufficient alone for lifecycle rule |
| C forbid rename while audit matches current code | **Rejected** |

---

## Acceptance Gate

| Criterion | Result |
|---|---|
| How new Department refs stored | Loan.JustificationDepartmentId + CodeSnapshot; WM.DepartmentId live |
| How historical labels preserved | Immutable audit Details + Loan CodeSnapshot for new/structured rows |
| Existing history backfill deterministic? | **No** under B without Human migration authorization |
| Historical use prevents Delete | Via structured DepartmentId refs only; legacy linkage STOP pending Human |
| DepartmentCode rename freely | Yes; audit not rewritten |
| No runtime Details parsing as relational authority | Yes (forbidden) |
| No historical audit rewrite | Yes |
| Orphan/ambiguous migration behavior | Explicit STOP rules if Human authorizes extract |

## Decision (updated)
**PROVENANCE CLASSIFICATION B STANDS for runtime authority.** Human authorized **one-time Migration Provenance Extract** (implemented in `KeyIssuedJustificationProvenanceExtract`, migration-scoped only). Structured Loan justification is relational authority after migration. Runtime Details parsing remains forbidden.

## Next permitted action
None for provenance analysis; implementation review is the next Human action. Roadmap remains STOP.
