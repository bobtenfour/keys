# KeyInventory Operator Guide

Concise guide for day-to-day key custody at a single-site KeyInventory installation.

In-application **Help** (`/Help`) is the operator-invoked visual reference for the same operational story: organization of records, KEY # / MEDECO, Issue → Active custody → Receive, Find (including Room # reverse-search), lifecycle, Audit Trail, and Reports. This markdown guide remains the portable documentation form; it does not duplicate Help HTML.

## What KeyInventory does

KeyInventory helps a key custodian:

- maintain **KEY #** access patterns and the **Room #** values each KEY # opens
- register **MEDECO** physical copies under a KEY #
- issue and return exact physical copies to people
- see who currently holds each physical copy
- correct authorized business records
- review audit history and reports

There is no Organization or Building setup. Departments and Rooms belong directly to this installation.

## KEY # and MEDECO (authoritative vocabulary)

| Term | Meaning |
|---|---|
| **KEY #** | Shared access number. Associated with the set of Rooms that open. Many physical copies may share one KEY #. |
| **MEDECO Key Code** | Identifies the specific physical key copy issued to a person. Unique within a KEY #; may repeat under a different KEY #. |
| **Room #** | Operator-facing room identifier. |

**Example**

- KEY # `66800`
- Rooms opened: `410D`
- Physical copies:
  - MEDECO `26`
  - MEDECO `27`
  - MEDECO `28`

Rooms opened are recorded once on the KEY #. Every MEDECO copy under that KEY # opens the same Rooms. Issue and return always name the exact MEDECO copy (for example KEY # 66800 / MEDECO 26), not merely the KEY #.

## Dependency model (authoritative)

This guide and the Application readiness/eligibility model use the same dependency classifications. Initial configuration is documented here; Home is an operational dashboard and does not permanently host setup UI.

| Relationship | Meaning |
|---|---|
| **Mandatory** | Must exist before the next step can succeed |
| **Parallel** | Can be created independently / in any order |
| **Optional** | Helpful, not required for Issue Key |
| **Consequence** | Created by a successful operation |
| **Lifecycle** | Edit where mutable; Delete only when unused/unreferenced; otherwise Activate / Retire / End / Terminate / Remove |

```mermaid
flowchart TB
  subgraph parallel ["Parallel setup"]
    D[Department]
    R[Room]
    KT[Key Type]
  end

  WM[Workforce Member]
  WA[Work Assignment]
  KN[KEY # access pattern]
  KR[KEY # ↔ Room #]
  COPY[MEDECO physical copy]
  ISSUE[Issue Key]
  ACTIVE[Active Custody]
  RECV[Return / Receive]
  HIST[History / Audit / Reports]

  D -->|mandatory| WM
  R -->|mandatory| WA
  WM -->|mandatory| WA
  KT -->|mandatory| KN
  KN -->|mandatory| COPY
  KN --> KR
  R --> KR
  WM -->|mandatory| ISSUE
  WA -->|mandatory| ISSUE
  COPY -->|mandatory| ISSUE
  ISSUE -->|consequence| ACTIVE
  ACTIVE -->|mandatory for return| RECV
  ISSUE -->|consequence| HIST
  RECV -->|consequence| HIST
```

### Minimum first-use path

1. Sign in.
2. Create **Department**, **Room #**, and **Key Type** in any order.
3. Create the first **Workforce Member** (no second person required).
4. Create a **Work Assignment** (member ↔ room) — required before Issue.
5. Create a **KEY #** (with Key Type) and assign **Room #** openings to that KEY #.
6. Register at least one **MEDECO** physical copy under that KEY #.
7. **Issue Key** (person + KEY # + available MEDECO; Rooms derived) → **Active custody** → **Return** the same MEDECO copy when returned.

Home remains an operational dashboard (metrics, Daily custody, Recent Activity) during and after initial configuration:

![Home daily operations](images/01-home-operations.png)

## Initial configuration (documentation sequence)

### Purpose
Reach a state where Issue Key is possible without guessing prerequisites. This sequence belongs in documentation; it is not a permanent Home or Administration setup surface.

### Prerequisites
Signed-in operator account.

### Where to go
**Administration** — Departments, Rooms, Workforce Members, Work Assignments, Audit Trail.  
**Catalog** — Key Types, Register Key, KEY # ↔ Room.  
**Operations → Issue Key** — when prerequisites are missing, Issue Key explains the missing Application-owned prerequisites and links to resolve them.

### Steps
1. Create **Department**, **Room**, and **Key Type** in any order (Administration / Catalog).
2. Create the first **Workforce Member**, then a **Work Assignment**.
3. Register a **KEY #** / **MEDECO** copy in Catalog (assign Room openings on the KEY # as needed).
4. Open **Operations → Issue Key**. If anything mandatory is still missing, resolve the items listed there, then issue.

### Expected result
Issue Key becomes usable; Home stays focused on custody metrics and activity (no leftover onboarding checklist).

### Common problems
- Issue blocked with no Work Assignment — create member-to-room assignment.
- Cannot create Workforce Member — create Department first.

### What becomes available next
Workforce Members → Work Assignments → Register Key → Issue.

---

## Creating Departments

### Purpose
Name organizational units used for membership and department-based issue justification. The department code is the operator-facing business identifier; under active identity authority it is editable without destroying the department’s relationships or history (stable internal DepartmentId is not an operator typing target).

### Prerequisites
None (parallel).

### Where to go
Administration → Departments → **+ Add department**

### Steps
1. Enter a unique Department code (example: `FACILITIES`).
2. Create Department.
3. Confirm it appears on the Departments list (form returns clean for another add via redirect).

### Expected result
Active department listed.

![Departments](images/02-departments.png)

### Common problems
Duplicate department code is rejected.

### What becomes available next
Workforce Member creation can use that department.

---

## Creating Rooms

### Purpose
Define places for work assignments and rooms opened by keys.

### Prerequisites
None (parallel). Room numbers are unique across the whole installation.

### Where to go
Administration → Rooms → **+ Add room**

### Steps
1. Enter Room number (operator identity) and optional description.
2. Create Room (system assigns internal room identity automatically).
3. Edit Room number/description later from Rooms → Edit when correction is needed.

### Expected result
Room appears with its room number.

### Common problems
Duplicate room number is rejected globally.

### What becomes available next
Work Assignments and Key↔Room.

---

## Creating Workforce Members

### Purpose
Register a person as an active worker who may receive keys.

### Prerequisites
**Mandatory:** active Department.  
No second workforce member and no manager are required.

### Where to go
Administration → Workforce Members → **+ Add workforce member**

### Steps
1. Enter First name, Last name, UIN (9 digits), Type, Department.
2. Create.
3. Open Details to correct name, UIN, department, or type when needed.

![Workforce Members](images/05-workforce-members.png)

### Expected result
One Active member with name, UIN, type, department.

### Common problems
- Missing department.
- UIN already used by another person.

### What becomes available next
Work Assignment for that member.

---

## Creating Work Assignments

### Purpose
Link a workforce member to a room where they are authorized to work.  
**Mandatory before Issue Key.**

### Prerequisites
Active Workforce Member and active Room.

### Where to go
Administration → Work Assignments → **+ Add**

### Steps
1. Select member and room.
2. Mark primary when appropriate (at most one active primary per member).
3. Create.

### Expected result
Active assignment listed.

### Common problems
Creating assignment before room or member exists.

### What becomes available next
Issue Key eligibility (together with a registered key).

---

## Key Types, KEY #, and MEDECO copies

### Purpose
Classify access patterns, record which Room # values a KEY # opens, and register physical MEDECO copies.

### Prerequisites
Create Key Types under **Catalog → Key Types → Add** before creating a new KEY #. Key Types are not created silently from Register Key.

### Where to go
Catalog → Key Types; Catalog → Register Key; Catalog → Keys → KEY # Rooms

### Steps — Register copy under existing KEY #
1. Choose **Register copy under existing KEY #**.
2. Search and select the existing KEY #.
3. Confirm derived **Key Type** and **Rooms opened**.
4. Enter the **MEDECO** code printed on the physical copy.
5. Register physical copy — next registration opens clean.

### Steps — Create new KEY #
1. Choose **Create new KEY #**.
2. Enter the new KEY #.
3. Select an **existing** Key Type.
4. Enter the first **MEDECO** code printed on the physical copy.
5. Create KEY # and register copy.
6. Assign **Rooms opened** afterward on KEY # Rooms.

### Expected result
KEY # shows Rooms opened; MEDECO copies appear under that KEY # in catalog and Find Key.

### What becomes available next
Issue Key for an available MEDECO copy.

---

## Assigning rooms opened by a KEY #

### Purpose
Record which Room # values a KEY # opens. Every MEDECO copy under that KEY # opens the same Rooms.

### Prerequisites
KEY # and active Room.  
Room assignment is on the KEY #, not repeated on each physical copy.

### Where to go
Catalog → Keys → **KEY # Rooms** (`/Catalog/KeyRooms`)

### Steps
1. Select KEY # and Room #.
2. Assign.
3. Remove only when the opening association should end.

---

## Issuing a Key

### Purpose
Hand an available MEDECO physical copy to an eligible person.

### Prerequisites (mandatory)
- Active Workforce Member with valid Party identity
- Active Department on that member
- At least one active Work Assignment
- Available MEDECO physical copy under a KEY #
- Justification: member’s Department **or** an assigned Work Assignment Room

### Where to go
Operations → Issue Key

![Issue Key](images/03-issue-key.png)

### Steps
1. Search **Key holder** by name or UIN (eligible matches only; nothing is preselected).
2. Select the holder deliberately.
3. Search available **KEY #** or **MEDECO**, then select KEY # deliberately (no full KEY # dropdown).
4. Select an **available MEDECO** copy under that KEY # (MEDECO stays empty until KEY # is chosen; nothing auto-selected).
5. Confirm **Rooms opened** shown as derived from the KEY # (do not re-enter Rooms).
6. Choose For = Department or Room and the matching justification (no default).
7. Confirm Issued / Due as operator local times (human-readable entry).
8. Enter Loan code.
9. Issue Key — next Issue opens clean.

### Expected result
Success message; Active custody shows KEY # + MEDECO for the open issue with human-readable times.

![Active Loans](images/04-active-loans.png)

### Common problems
- Readiness still shows missing Work Assignment or Key / MEDECO copy.
- Selected MEDECO already issued.
- Justification room not on the member’s assignments.

### What becomes available next
Active Custody / Return of that exact MEDECO copy.

---

## Active custody and Return

### Purpose
See who holds which MEDECO copy; complete return of that exact copy.

### Prerequisites
Open loan for the physical copy being returned.

### Where to go
Operations → Active Loans → Receive/Return, or Operations → Receive Key

### Steps
1. Search active issues by **KEY #**, **MEDECO**, holder name, or UIN (bounded matches; nothing is preselected). Deep-link from Active Loans may open one deliberate issue.
2. Select the matching open issue labeled **KEY # / MEDECO · holder — UIN** (for example KEY # 66800 / MEDECO 26 · …).
3. Enter Receive reference and Received (operator local time).
4. Complete return — next Receive opens clean.

### Expected result
That MEDECO copy leaves Active custody; appears in History as returned. Other MEDECO copies under the same KEY # are unaffected.

---

## Header search (global operational search)

### Purpose
Answer practical questions about a person, Room, KEY #, or MEDECO copy by composing current operational facts from existing authorities.

### Where to go
Header search → Search Results (`/Search`). The header owns the Search box; the results page does not show a second Search form.

### How to search
One header box accepts **name**, **UIN**, **Room #**, **KEY #**, or **MEDECO**. No search-type dropdown. Results are typed groups (People, Rooms, KEY #, MEDECO) — only groups with matches appear.

### Person results
Show Full Name, UIN, Department, status, active **Work Assignment** Room # values, and **Current Key Custody** (open custody only). For each current physical copy: KEY #, MEDECO, Rooms opened by that KEY #, Issued time. A person with no current keys is still a valid result (`No keys currently issued.`). Work Assignment is not key custody. History is not listed here. There are no Member details / Member keys buttons on search results.

### Room / KEY # / MEDECO results
- Room #: description when available; KEY # values that open it.
- KEY #: Rooms opened; MEDECO copies with Available/Issued and holder when issued.
- MEDECO: always with parent KEY # (not globally unique); Rooms via KEY #; custody/holder.

### Zero results
`No results found for "…"` plus guidance to search by name, UIN, Room #, KEY #, or MEDECO.

---

## Find Key

### Purpose
Key-specific search for KEY # / MEDECO / Key Type / Room # questions.

### Where to go
Operations → Find Key (`/Operations/Find`).  
(Header search is global operational search — not Find Key.)

### Prerequisites
Catalog data (KEY # Room openings and MEDECO copies).

### How to search
One search box accepts **KEY #**, **MEDECO**, **Key Type**, or **Room #** (operator-facing room number). Matching is the same partial-match style used for KEY # / MEDECO / type. Searching a Room # returns every KEY # that opens that room (including multi-room / master KEY # values), with each MEDECO copy’s availability and current holder.

### What you should be able to answer
- What Room # values does KEY # 66800 open?
- What MEDECO copies exist under KEY # 66800? Which are available or issued?
- Who holds MEDECO 28 under KEY # 66800?
- Which KEY # values open Room X? (search Find by that Room #)
- Which KEY # / MEDECO a person currently holds? (prefer header person search, Member Keys, or Keys by Member report)

---

## Correcting authorized records

Operators may correct:

| Record | What you can correct |
|---|---|
| Room | Room number, description; activate/retire; **Delete** only when unused |
| Department | **Edit** department code (same department kept); activate/retire; **Delete** only when unused |
| Workforce Member | Department, type; terminate; **Delete** only when unused (no assignments/loans) |
| Party | First/Last name; **UIN** via governed correction on the same person |
| Work Assignment | End; primary flag; **Delete** only for active unused assignments |
| Key Type | activate/retire; **Delete** only when no KEY # references it |
| KEY # | activate/retire; **Delete** only with no MEDECO copies and no Room assignments |
| MEDECO copy | activate/retire; **Delete** only with no loan history |
| Key↔Room | assign/remove |

### Delete vs Retire
- **Delete** permanently removes an unused record that has no business relationships and no history that must be preserved. Confirm deliberately; deletion cannot be undone.
- **Retire** (or End / Terminate for relationship/person records) keeps the record so history and references remain meaningful.
- If Delete is unavailable, the record is in use — Retire/End/Terminate instead.
- Deleted records do not exist; retired records still exist and may be Activated when that lifecycle is supported.

UIN correction keeps the same person and history; it rejects UIN already used by someone else and writes a new Audit Trail row with old and new UIN.

---

## Audit Trail

### Purpose
See who performed which business action.

### Where to go
Administration → Audit Trail

![Audit Trail](images/06-audit-trail.png)

### Steps
Filter by date, operator, action, or subject; export CSV / Excel / PDF of the same result.

### Notes
Historical rows that mention older Organization/Building/manager language remain readable. New work does not require those concepts.

---

## Reports and exports

### Where to go
Reports menu (existing REPORTS-1 reports).

### Steps
Filter on screen, then download CSV, Excel, or PDF.  
Timestamps appear in readable `yyyy-MM-dd HH:mm UTC` form in exports; on-screen lists use friendly local/relative presentation.

---

## Common blockers

| Symptom | Likely cause | What to do |
|---|---|---|
| Cannot find person at Issue | Inactive member, missing Work Assignment, or search terms | Verify active Workforce Member + Work Assignment; search by name or UIN |
| No MEDECO available | None registered, or all issued/unavailable | Register a copy or return an open issue |
| Cannot Issue | Missing governed prerequisites | Use the contextual resolution links on Issue Key |
| Cannot delete Department | Relationships or history block Delete | Retire when Delete is unavailable |
| Cannot return | No matching open loan / wrong copy | Select the open KEY # / MEDECO issue on Receive |
| KEY # missing expected Room | Room not assigned to that KEY # | Assign under Catalog → KEY # Rooms |
| Duplicate Room number | Global uniqueness | Choose a different number |
| UIN correction fails | UIN already used | Use a free UIN |

---

## Daily cycle summary

```mermaid
flowchart LR
  ISSUE[Issue] --> ACTIVE[Active Custody]
  ACTIVE --> RECV[Receive]
  RECV --> HIST[History / Audit / Reports]
```
