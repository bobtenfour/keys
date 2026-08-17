# Product Experience Contract

## Authority
This document governs user-facing product quality.

## Purpose
Prevent framework-default, demo-like, or inconsistent UI experience.

## Rules
- UI must support the business workflow, not expose technical structure.
- No framework demo pages.
- No placeholder user experience.
- No business logic in UI.
- No ambiguous user actions.
- Error messages must be actionable when UI exists.
- Screens must reflect product language, not database language.

## Active Structural Amendment — Classification defines KEY # access (2026-08-16)
- Create Key: existing KEY # shows Classification + Access read-only; new Regular requires exactly one Room selector; new Master hides Room selector and shows Access: All Rooms.
- No `/Catalog/KeyRooms` assign/remove surface.
- Issue / Find / Search / Reports / Keys show Regular Room or Master “Access: All Rooms”; prefer Access / Room labels over plural “Rooms opened” for Regular.

## LOAN-VERTICAL-1 Product Experience
- Provide Razor Pages for: create key asset, issue loan, complete return, list open loans, and list returned loans.
- Use product language (Key, Loan, Return), not database or persistence language.
- Provide simple navigation among only these workflow pages.
- Surface Domain and Application validation failures as actionable page messages.
- Do not add login, dashboards, demo scaffolding, or unrelated marketing pages.

## OPERATOR-UX-1 Product Experience
- Issue Key must present an operator workflow (Key, Issue to, For, Issued, Due) rather than implementation-oriented codes or raw UTC ISO strings as the normal UI.
- Operator-facing person selectors use FirstName LastName and UIN; internal WorkforceMemberCode/PartyCode remain submitted values when required and are not normal operator typing targets when selectors exist.
- Administration list/create/maintain surfaces use list-first index pages with a primary Add action and dedicated create/detail task pages; do not present a wall of independent form cards as the primary administration pattern.
- Workforce Members index shows person identity (FirstName LastName, UIN), Type, Department, Status, and actions (Organization and Responsible Manager columns removed by OPERATOR-EXPERIENCE-1); PartyCode and WorkforceMemberCode are not primary operator-facing columns; Add and Detail/Edit are dedicated routes; Terminate lives on the selected member detail page with deliberate confirmation.
- Prefer authoritative selectors over manually typed foreign-reference codes when Application list authorities already expose the choices.

## OPERATOR-AUDIT-1 Product Experience
- Administration includes an Audit Trail list showing Date/Time, Operator, Action, Subject, and Details for persisted operator business mutations.
- Prefer operator-readable action and subject labels; authenticated operator display uses the existing KeyInventory user identity name.
- Practical filters (date range, operator, action, subject/reference) are presentation over Application trail query results.
- Do not turn normal operations pages into audit dashboards; optional subject links to filtered history are allowed when simple.

## OPERATOR-EXPERIENCE-1 Product Experience
- Product is single-site: no Organization or Building administration, selectors, columns, or prerequisites.
- Workforce Members show person identity, Type, Department, Status, and actions; no Organization or Responsible Manager fields.
- Navigation is task-oriented in primary order **Home → Catalog → Operations → Reports → Administration → Help**. Catalog exposes Create Key, Keys, and KEY # / Rooms (no Key Types). Operations owns Issue Key, Receive Key, Active Loans, History, and Find Key. Administration owns Departments, Rooms, Workforce Members, Room Assignments, and Audit Trail.
- Application-owned readiness/eligibility remains the sole business readiness authority; Web must not duplicate Domain eligibility formulas or invent a second readiness engine.
- First-time configuration is a transient installation state, not a permanent application capability or navigation surface. Home must not present a permanent first-time setup section, setup checklist, onboarding wizard, or Administration/Catalog duplicate of setup cards.
- Home is an operational dashboard (metrics, Daily custody, Recent Activity). Permanent onboarding/setup presentation is forbidden on Home.
- Administration and Catalog expose ordinary capabilities (not labeled or structured as permanent setup steps).
- When an operator opens a capability whose mandatory prerequisites are missing, communicate those missing prerequisites contextually at that capability boundary using Application readiness/eligibility signals, with authorized routes to resolve them. Once prerequisites exist, obsolete onboarding/setup messaging must not remain.
- The logical empty-installation dependency sequence belongs in the operator User Guide (including the dependency diagram); documentation does not justify permanent setup UI.
- One shared human-readable date/time presentation authority covers Home, Administration, Issue/Receive, Active Loans, History, member details, Audit Trail, Reports, and CSV/XLSX/PDF; raw SQL/ISO/UTC serialization is forbidden in normal operator UI; persisted UTC is unchanged.
- Successful create uses server-side lifecycle: success confirmation, clean form state, logical next action; failed validation retains input; no field-by-field JavaScript clearing.
- User Guide in `documentation/operator/` must present the same dependency model as Application readiness (including WorkAssignment as mandatory for Issue Key).

## KEY-ACCESS-COPY-1 Product Experience
- Catalog distinguishes KEY # (shared access pattern) from MEDECO (individual key under that KEY #).
- Catalog distinguishes physical condition (Active / Lost / Destroyed) from custody (Available / Issued). Lost/Destroyed must not be labeled Available merely because no Loan exists.
- Room openings are maintained at KEY # level; operators assign Room # values to a KEY #; keys do not present independent conflicting Room editors.
- Classification Regular|Master is selected when creating a new KEY #; no Key Types administration page or KeyType entity exists in the active product.
- Issue Key identifies an Active+Available key first (KEY # + MEDECO via searchable combobox), then **Key holder** (WorkforceMember / Party; label only—no KeyHolder entity), with derived Classification and Rooms opened (read-only; not re-entered). Internal KeyAssetId is not an operator typing target.
- Return / receive identifies the exact key via open-custody searchable combobox as KEY # + MEDECO (e.g. 66800 / 26 vs 66800 / 27). Normal Receive closes as Returned. Lost/Destroyed closures are not presented as Returned.
- History distinguishes Loan closure Returned vs Lost vs Destroyed.
- Find Key and reports distinguish KEY #, Classification, MEDECO, condition, custody, holder, Rooms opened, and issue/return/closure state; screen/CSV/XLSX/PDF parity preserved.
- Operations → Find Key (`/Operations/Find`) remains key-specific search: KEY #, MEDECO, Classification, or Room # through the existing Application key lookup authority; Room # reverse-search returns KEY # values that open that room via KeyAccessPattern↔Room (sole Room-access authority).
- Do not expose Transfer; do not invent New Key terminology beyond KEY # / MEDECO presentation required by this slice.
- Operator guide must explain KEY # → Rooms opened and MEDECO → individual key held, using the 66800 / 410D / MEDECO 26–28 example pattern; screenshots refresh only after runtime finalization.

## Global Operator Search (active)
- Header search is global operational search (`/Search`), not Find Key. One Application orchestration boundary (`IGlobalOperatorSearchUseCase`) composes typed results from existing authorities. No second search store, no Web DbContext, no full-table Web filtering.
- Header Search is the only global-query input surface in this workflow. `/Search` presents results only — no duplicate Search textbox/button/card.
- One search field accepts name, UIN, Room #, KEY #, or MEDECO. Placeholder: `Search name, UIN, Room #, KEY #, or MEDECO...`. No search-type dropdown. Search runs only on submit; no preload; no auto-select/auto-redirect.
- Results page begins with Search Results / Search results for "<query>" then typed groups rendered only when populated: People, Rooms, KEY #, MEDECO Key Code.
- Person result: Full Name; UIN; Department (current workforce membership); workforce status; **Room Assignment** rooms from active Room Assignment authority; and **Current Key Custody** from open Loan custody only — each with KEY #, MEDECO, Rooms opened (from KEY #), Issued timestamp. Person with zero current keys is still a valid result (`No keys currently issued.`). Room Assignment is not custody. History/Audit are not embedded. Do not render Member details / Member keys / View details / View keys as information substitutes.
- Room result: Room #, description when available, KEY # values that open it via KeyAccessPattern↔Room (inline; no navigation substitute for that answer).
- KEY # result: Classification (Regular|Master), Rooms opened, physical MEDECO copies with Available/Issued and holder identity when issued.
- MEDECO result: always includes parent KEY # (MEDECO is not globally unique), Rooms via KEY #, custody state/holder.
- Zero results: single global empty state (`No results found for "…"`) with guidance to search by name, UIN, Room #, KEY #, or MEDECO — not Find Key’s “No matching keys / Browse Keys” pattern.
- Bounds: explicit maximum per category (aligned with existing Application search conventions).

## Administration / Catalog Record Lifecycle (active)
- Unreferenced Administration/Catalog records may be permanently deleted when Application determines they have no business relationships and no historical/operational references that must be preserved.
- Referenced or historically used records must not be permanently deleted; use Retire / End / Terminate / Remove where that entity supports those lifecycle operations.
- Application owns editable / deletable / retireable / reactivatable eligibility; Web must not decide delete eligibility via DbContext relationship counting, client-side rules, or raw FK/SQL exceptions as the operator contract.
- Delete execution must revalidate eligibility atomically; a relationship created between list rendering and delete execution must reject destructive deletion with a business-readable message.
- Every row representing legitimately editable business data exposes an explicit Edit action on that row. Under active identity authority, DepartmentCode is an editable business identifier (DepartmentId remains hidden technical identity); RoomNumber remains editable; immutable classification/identity codes (for example KEY # KeyNumber) do not invent Edit. KeyType TypeCode Edit is not applicable — KeyType is not an active entity.
- Actions columns expose only actions valid for that exact record and state (Edit, Delete, Retire, Activate, End, Terminate, Remove as authorized). Retire is not a universal Delete substitute; Delete is not universally available.
- Permanent Delete requires deliberate confirmation that identifies the exact record and uses permanent-deletion language (not Retire wording).
- Do not cascade-delete related business/history records merely to make a parent deletable.
- Lifecycle relationship detection must use stable entity identities (for example DepartmentId), not mutable business strings, once the normalized ERD is implemented.

## Identity presentation (active)
- Operators see and edit business identifiers (DepartmentCode, RoomNumber, UIN, KEY #, MEDECO) per Domain mutability.
- Operators must not be required to invent or type internal technical identities (DepartmentId, RoomCode, PartyCode, KeyAssetId) in normal workflows.
- Renaming a business identifier must not be presented as delete-and-recreate of the underlying entity.

## Issue / Receive Interaction (active presentation)
- Growing selector collections use the shared **searchable combobox** pattern (Application-owned bounded search; browse/search panel; explicit operator selection; no silent first/only auto-select).
- A freshly opened Issue or Receive/Return operation must not silently select business choices for the operator, including when exactly one valid option exists.
- **Issue is physical-key-first:** operator searches/selects an issuable physical copy (KEY # + MEDECO) via searchable combobox first; Classification and Rooms opened derive read-only; then selects **Key holder** via searchable combobox over eligible candidates (name/UIN). No full workforce or full available-catalog HTML dump; Web must not evaluate eligibility or auto-select first/only match.
- Issue initial business-choice state is empty for physical copy, Key holder, justification kind, Department, and Room.
- Successful Issue and Receive use server-side PRG: success confirmation then clean new-operation state. Failed validation retains submitted values. No JavaScript field-by-field reset; no first/only-record defaults; no query-string carry of prior Issue business selections on a fresh Issue open.
- **Receive/Return uses open-custody searchable combobox:** bounded search of active issues by KEY #, MEDECO, holder name, or UIN. No full active-issue dropdown. Operator explicitly selects the matching issue. Deep-link from Active Loans / Member Keys may pre-select one deliberate issue only. Display uses `KEY # … / MEDECO … · Name — UIN …`. Internal LoanId/KeyAssetId are not operator targets.
- Issued, Due, and Received remain operator-editable under Application loan/return timestamp parameters (UTC persistence unchanged). Operator entry uses shared `OperatorLocalTimestamp` conversion; absolute display uses shared `OperatorTimestampFormatter.ToAbsoluteDisplay` (`MMM d, yyyy · h:mm tt`). No page-local timestamp formatters; no ISO/raw UTC/offset presentation in normal operator UI.

## Operator Interaction Architecture (active presentation amendment)
- **One capability — one primary interaction surface.** Header owns Global Search input; `/Search` owns result presentation only and must not render a second global Search form.
- **Page purpose.** Every page answers one operator question or completes one business operation; do not retain controls/navigation that do not serve that purpose.
- **No navigation as information substitute.** Global Search Person results must present Identity, Room Assignment rooms, and Current Key Custody directly — never Member details / Member keys / View details / View keys as substitutes.
- **Interaction taxonomy.** New fact → enter; existing reference → select/find via searchable combobox when collections grow; large collection → bounded search-on-demand; derived fact → display; system fact → system-owned; business decision → explicit operator choice; lifecycle → Application-authorized action only.
- **Searchable combobox rule.** Administration/Catalog/Operations selectors for growing collections (Workforce Member, Room, KEY #, issuable MEDECO, open custody) use the shared searchable combobox; do not invent page-local unbounded dropdowns.
- **Create Key.** Exactly two modes controlled by top mode selectors (not submit actions): **New Key** and **Replace Lost Key**. **New Key** is one business operation that always requires KEY # and MEDECO on the same screen. Application resolves whether the typed/searched KEY # already exists — the operator does not choose “existing” versus “new”. Existing KEY #: show Classification and Rooms as read-only information; Create Key adds only the new key under that KEY #. Non-existing KEY #: remain on the same New Key card, require Classification (Regular|Master, never inferred), Rooms (existing Room authority, searchable), and MEDECO; Create Key atomically creates the KEY # with Classification/Rooms and its first key — failure must not leave an orphan KEY #. Final New Key action: **Create Key**. **Replace Lost Key** remains distinct because of replacement lineage: Lost-key searchable combobox + New MEDECO; KEY #/Classification/Rooms derived; action **Replace Key**. Mode switching clears incompatible state. No worker/holder on Create Key. Issue Key remains exclusively the separate Operations custody operation for assigning an already-existing Available key. Obsolete operator-facing wording forbidden: “Register Key” (for this capability), “Add New Key”, “Create New KEY #”, “Add Key”, “Register physical copy”, “physical MEDECO copy”, “MEDECO Key Code”, “Register copy under existing KEY #”. Internal route `/Catalog/Register` and `RegisterNewKeyAsync` may remain. No Key Types admin page; no KeyType entity; no silent Key Type creation.
- Operator-facing Department labels use **Department** only (not “Department Code”); internal `DepartmentCode` identity/authority is unchanged.
- **Room Assignments / Assign Room.** Authorized decisions only: select Workforce member (searchable combobox by name/UIN) and Room (searchable combobox restricted to that member’s Department). After member selection, show Name, UIN, and Department as contextual identity. Submit label **Assign Room**. Successful Assign Room uses PRG back to a clean Assign Room form. No WorkAssignmentCode entry, no Primary designation, no technical identifiers, no separate Search button, no auto-selection.
- Room Assignment rooms and Key-access rooms remain distinct labels and authorities; no inference between them.
- **Workforce Member Create.** Successful create uses PRG to the member’s View/Details. Details show identity and membership as information (not a populated Create form). Edit is an explicit separate action. Terminate is a secondary lifecycle action and must not appear as the immediate continuation of successful creation.
- **No Key Types admin.** Catalog navigation must not present Key Types; Classification lives on KEY # as Regular|Master.

## Help Presentation (active)
- `/Help` is operator-invoked reference guidance in the existing Razor Pages shell; it is not Home onboarding, first-time setup UI, or a permanent readiness surface.
- Help is presentation-only: static guidance, deterministic inline SVG diagrams, real runtime screenshots, and links to authoritative capability pages. It must not query DbContext, evaluate readiness/eligibility, reconstruct lifecycle capabilities, or contain alternate business rules.
- Task-oriented launcher and chapter navigation (Orient / Configure / Operate / Govern) are required; do not resurrect a permanent Home setup checklist or duplicate Application readiness engines.
- Structural concepts use SVG/CSS; actual application surfaces use captured local runtime screenshots under `wwwroot/images/help/`. No Mermaid runtime, no external diagram libraries, no AI-generated application screenshots.
- Help must match current KEY # / MEDECO, Find Room # reverse-search, Issue/Receive interaction, lifecycle, Audit Trail, and Reports behavior; contextual links go to capability owners (`/Administration/*`, `/Catalog/*`, `/Operations/*`, `/Reports`).
- Responsive and accessible presentation is required (semantic headings, keyboard-accessible anchors, useful alt text, diagram text explanations, color not sole semantic signal).

## Applies When
This document is required only for slices that create or modify UI, navigation, user workflows, messages, or product-facing behavior.

## Depends On
- product-vision.md

## Depended On By
- web/UI slices
