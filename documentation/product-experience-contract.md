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
- Navigation is task-oriented: Administration (Departments, Rooms, Workforce Members, Work Assignments, Audit Trail); Key/Catalog; Daily custody; Lookup; Reporting.
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
- Catalog distinguishes KEY # (shared access pattern) from MEDECO Key Code (physical copy under that KEY #).
- Room openings are maintained at KEY # level; operators assign Room # values to a KEY #; physical copies do not present independent conflicting Room editors.
- Issue Key identifies **Key holder** (WorkforceMember / Party; label only—no KeyHolder entity), KEY #, available MEDECO/physical copy, and derived Rooms opened (read-only; not re-entered). Internal KeyAssetId is not an operator typing target.
- Return / receive identifies the exact physical copy as KEY # + MEDECO (e.g. 66800 / 26 vs 66800 / 27).
- Find Key and reports distinguish KEY #, MEDECO/copy, holder, Rooms opened, and issue/return state; screen/CSV/XLSX/PDF parity preserved.
- Operations → Find Key (`/Operations/Find`) remains key-specific search: KEY #, MEDECO, Key Type, or Room # through the existing Application key lookup authority; Room # reverse-search returns KEY # values that open that room via KeyAccessPattern↔Room (sole Room-access authority).
- Do not expose Transfer; do not invent New Key terminology beyond KEY # / MEDECO presentation required by this slice.
- Operator guide must explain KEY # → Rooms opened and MEDECO → physical copy held, using the 66800 / 410D / MEDECO 26–28 example pattern; screenshots refresh only after runtime finalization.

## Global Operator Search (active)
- Header search is global operational search (`/Search`), not Find Key. One Application orchestration boundary (`IGlobalOperatorSearchUseCase`) composes typed results from existing authorities. No second search store, no Web DbContext, no full-table Web filtering.
- One search field accepts name, UIN, Room #, KEY #, or MEDECO. Placeholder: `Search name, UIN, Room #, KEY #, or MEDECO...`. No search-type dropdown. Search runs only on submit; no preload; no auto-select/auto-redirect.
- Results are typed groups rendered only when populated: People, Rooms, KEY #, MEDECO Key Code.
- Person result: Full Name, UIN, Department (current workforce membership), workforce status, and **Keys currently issued** from open Loan custody only — each with KEY #, MEDECO, Rooms opened (from KEY #). Person with zero current keys is still a valid result (`None`). History/Audit are not embedded.
- Room result: Room #, description when available, KEY # values that open it via KeyAccessPattern↔Room.
- KEY # result: type, Rooms opened, physical MEDECO copies with Available/Issued and holder identity when issued.
- MEDECO result: always includes parent KEY # (MEDECO is not globally unique), Rooms via KEY #, custody state/holder.
- Zero results: single global empty state (`No results found for "…"`) with guidance to search by name, UIN, Room #, KEY #, or MEDECO — not Find Key’s “No matching keys / Browse Keys” pattern.
- Bounds: explicit maximum per category (aligned with existing Application search conventions).

## Administration / Catalog Record Lifecycle (active)
- Unreferenced Administration/Catalog records may be permanently deleted when Application determines they have no business relationships and no historical/operational references that must be preserved.
- Referenced or historically used records must not be permanently deleted; use Retire / End / Terminate / Remove where that entity supports those lifecycle operations.
- Application owns editable / deletable / retireable / reactivatable eligibility; Web must not decide delete eligibility via DbContext relationship counting, client-side rules, or raw FK/SQL exceptions as the operator contract.
- Delete execution must revalidate eligibility atomically; a relationship created between list rendering and delete execution must reject destructive deletion with a business-readable message.
- Every row representing legitimately editable business data exposes an explicit Edit action on that row. Under active identity authority, DepartmentCode is an editable business identifier (DepartmentId remains hidden technical identity); RoomNumber remains editable; immutable classification/identity codes (for example KeyType TypeCode, KEY # KeyNumber) do not invent Edit.
- Actions columns expose only actions valid for that exact record and state (Edit, Delete, Retire, Activate, End, Terminate, Remove as authorized). Retire is not a universal Delete substitute; Delete is not universally available.
- Permanent Delete requires deliberate confirmation that identifies the exact record and uses permanent-deletion language (not Retire wording).
- Do not cascade-delete related business/history records merely to make a parent deletable.
- Lifecycle relationship detection must use stable entity identities (for example DepartmentId), not mutable business strings, once the normalized ERD is implemented.

## Identity presentation (active)
- Operators see and edit business identifiers (DepartmentCode, RoomNumber, UIN, KEY #, MEDECO) per Domain mutability.
- Operators must not be required to invent or type internal technical identities (DepartmentId, RoomCode, PartyCode, KeyAssetId) in normal workflows.
- Renaming a business identifier must not be presented as delete-and-recreate of the underlying entity.

## Issue / Receive Interaction (active presentation)
- A freshly opened Issue or Receive/Return operation must not silently select business choices for the operator, including when exactly one valid option exists.
- Issue Key holder selection is search-on-demand: no full workforce load into HTML or JavaScript; Application returns a bounded set of eligible candidates matching name or UIN; Web must not evaluate eligibility or auto-select first/only match.
- Issue initial business-choice state is empty for Key holder, KEY #, MEDECO, justification kind, Department, and Room. MEDECO options appear only after KEY # is selected. Rooms opened remain derived from KEY #.
- Successful Issue and Receive use server-side PRG: success confirmation then clean new-operation state. Failed validation retains submitted values. No JavaScript field-by-field reset; no first/only-record defaults; no query-string carry of prior Issue business selections on a fresh Issue open.
- Receive/Return initial Active issue selection is empty (`Select an issued key...`) unless the operator deliberately opens a specific issue deep-link. Option text uses `KEY # … / MEDECO … · Name — UIN …`.
- Issued, Due, and Received remain operator-editable under Application loan/return timestamp parameters (UTC persistence unchanged). Operator entry uses shared `OperatorLocalTimestamp` conversion; absolute display uses shared `OperatorTimestampFormatter.ToAbsoluteDisplay` (`MMM d, yyyy · h:mm tt`). No page-local timestamp formatters; no ISO/raw UTC/offset presentation in normal operator UI.

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
