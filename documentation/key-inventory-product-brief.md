# KeyInventory — Product Brief

| Field | Value |
| --- | --- |
| **Product** | KeyInventory |
| **Document type** | Binding product brief for implementation |
| **Audience** | Codex (no prior DentalInventory context assumed) |
| **Status** | Draft — ready for implementation |
| **Reference style** | DentalInventory enterprise internal web application |

**Purpose:** Define a complete product brief for **KeyInventory** — a system to track physical keys, holders, locations, status, assignment history, and audit trail. The product must **visually and operationally** follow the same enterprise product style as DentalInventory: a professional, server-rendered internal web application suitable for hospitals, universities, and large organizations.

**Out of scope for this document:** Implementation code, project scaffolding, runtime configuration changes.

---

## 1. DentalInventory visual and product style summary

DentalInventory is an **enterprise internal web application** for operational staff and administrators. It is **not** a developer tool, demo playground, or markdown-driven documentation site. Codex should treat the following as binding UX and presentation requirements for KeyInventory.

### Application character

- **Enterprise internal web application** — polished, calm, authoritative presentation for daily operational use.
- **Server-rendered professional UI** — pages are generated on the server; HTML is the primary delivery surface.
- **Production-safe presentation** — no debug-looking surfaces, no placeholder mockups, no “under construction” admin chrome for ordinary users.
- **No developer-looking UI** — avoid raw JSON viewers, unstyled tables, framework defaults without design system treatment, or experimental component libraries.

### Shell and navigation

- **Top shell navigation** — primary modules appear in a persistent header/nav bar after authentication.
- **Module separation** — distinct areas for **Home**, **Admin**, **Operations**, **Reports**, and **Help**; operators should not see configuration surfaces they cannot use.
- **Role-aware home page** — landing content, quick actions, and summary cards adapt to the signed-in user’s capabilities.
- **Capability-based visibility** — navigation links, buttons, and page regions appear only when the user is authorized; hiding UI is not sufficient without server-side enforcement.

### Visual and interaction patterns

- **Bootstrap-style design system** — Bootstrap as the layout/component baseline, extended with a cohesive token-driven design system (cards, panels, page headers, badges, density zones).
- **Cards** — module landing pages and dashboards use card grids for navigation and summary metrics.
- **Tables** — operational and report pages use consistent table presentation with filters, paging, and empty states.
- **Filters** — list and report pages expose predictable filter controls (status, location, holder, date range, key number).
- **Badges** — key status and condition appear as compact visual badges, not free-text-only labels.
- **Toasts** — successful mutations surface non-blocking success feedback after server confirmation.
- **Page headers** — each page uses a clear title band with optional toolbar actions and status badges.

### Help experience

- **Integrated Help Center** — first-class `/Help` area with structured topics, not external wikis or downloadable markdown files.
- **Contextual help** — operational pages link to relevant help topics; help content is rendered in-app as HTML.
- **No markdown downloads** — help is browsed inside the application; do not expose `.md` file downloads to end users.

### Authentication presentation

- **Session-only authentication style** — sign-in establishes a server session; no “Remember Me” checkbox or persistent login token UX.
- **Anonymous entry** — unauthenticated users see a focused login surface, not operational navigation.
- **Audit-oriented operations** — every mutation records who acted and when; the UI should reinforce accountability, not anonymous changes.

### What to avoid

- Single-page application shells that replace server-rendered workflows.
- Client-only authorization (hiding buttons without server policy enforcement).
- Per-page one-off CSS or interaction frameworks.
- Admin/setup content on operator home pages.
- Optimistic UI that shows success before the server confirms the mutation.

---

## 2. Recommended stack

### Primary recommendation (maximum similarity to DentalInventory)

The safest stack for KeyInventory — and the one that most closely matches DentalInventory’s architecture and UX — is:

| Layer | Choice | Rationale |
| --- | --- | --- |
| **Web host** | **ASP.NET Core Razor Pages** | Same server-rendered page model as DentalInventory; natural fit for forms, tables, authorization conventions, and integrated Help pages. |
| **Database** | **SQL Server** (production) / **SQLite** (optional local development) | Relational persistence with EF Core migrations; SQLite acceptable for lightweight local dev only. |
| **UI** | **Bootstrap-style design system** | Matches DentalInventory’s Bootstrap + token-driven enterprise presentation. |
| **Rendering** | **Server-rendered pages** | HTML-first delivery; minimal JavaScript beyond Bootstrap and small interaction helpers (toasts, confirmations). |
| **Auth** | **ASP.NET Core Identity (cookie session)** | Session-based sign-in without Remember Me. |
| **Data access** | **Entity Framework Core + migrations** | Schema evolution, audit history preservation, indexed query paths. |

**Recommendation:** Use **ASP.NET Core Razor Pages** as the default implementation path. This yields the highest fidelity to DentalInventory’s enterprise product experience, authorization model, and operational page patterns.

### Acceptable alternative (if explicitly required)

An acceptable but **secondary** stack:

| Layer | Choice |
| --- | --- |
| **Web** | Next.js with server-side rendering |
| **Database** | PostgreSQL |
| **ORM** | Prisma |
| **Rendering** | Server-side rendering for operational pages |

This alternative is acceptable only when the organization standardizes on Node/PostgreSQL. It requires explicit replication of DentalInventory-style UX discipline (shell nav, capability visibility, help center, audit-first workflows) because the default Next.js ecosystem tends toward developer-centric UI unless constrained.

**Binding guidance for Codex:** Prefer **ASP.NET Core Razor Pages + SQL Server/SQLite + Bootstrap + server-rendered pages** unless the user explicitly directs the alternative stack.

---

## 3. Product name

**KeyInventory**

Working title for display surfaces: **Key Inventory** (spaced, title case in UI labels).

---

## 4. Domain purpose

KeyInventory is a system to track **physical keys** and answer operational questions such as:

- What keys exist?
- Who currently holds each key?
- Where does each key belong?
- What is each key’s current status and condition?
- What is the full assignment and return history?
- What audit events occurred for each key?

The system supports registration, assignment, return, loss/damage handling, location transfers, inventory audits, search, and historical reporting — with **append-only history** and **audit events on every state change**.

---

## 5. Core entities

| Entity | Purpose |
| --- | --- |
| **Key** | A physical key record (number, description, status, condition, location, access level). |
| **KeyCategory** | Classification of keys (e.g., building, cabinet, vehicle, storage). |
| **KeyLocation** | Where a key belongs or is stored when not assigned (building, room, cabinet, hook). |
| **KeyHolder** | A person or role entity that may receive key assignments (employee, contractor, department delegate). |
| **KeyAssignment** | An active or historical record of a key assigned to a holder, with actor, timestamp, and reason. |
| **KeyReturn** | A return event that closes an active assignment. |
| **KeyAuditEvent** | Immutable audit log entry for any key state or metadata change. |
| **KeyCondition** | Physical condition classification (good, worn, damaged, unusable). |
| **KeyAccessLevel** | Security/access tier for a key (standard, restricted, high-security). |

---

## 6. Key statuses

| Status | Meaning |
| --- | --- |
| **Available** | Key is in inventory and may be assigned. |
| **Assigned** | Key is currently assigned to a holder. |
| **Returned** | Key was returned and is available again (may transition to Available after processing). |
| **Lost** | Key is lost; cannot be assigned. |
| **Damaged** | Key is damaged; assignment restricted unless explicitly marked usable. |
| **Retired** | Key is permanently removed from active circulation; cannot be assigned. |

---

## 7. Core workflows

| Workflow | Description |
| --- | --- |
| **Register key** | Create a new key with number, category, location, access level, and initial condition. |
| **Assign key** | Assign an available key to a holder; creates assignment record and audit event. |
| **Return key** | Close the active assignment; restore key to available inventory state. |
| **Mark key lost** | Set status to Lost; block future assignment. |
| **Mark key damaged** | Set status/condition to Damaged; block assignment unless marked usable. |
| **Transfer key location** | Change the key’s home location when not conflicting with active assignment rules. |
| **Audit key inventory** | Reconcile expected vs observed keys; record audit findings and events. |
| **Search keys** | Find keys by number, status, holder, location, category, access level. |
| **View key history** | Read-only timeline of assignments, returns, status changes, and audit events. |

---

## 8. Business rules

1. **A key cannot be assigned to two people at once.** At most one active assignment per key.
2. **A lost key cannot be assigned.**
3. **A retired key cannot be assigned.**
4. **A damaged key cannot be assigned** unless explicitly marked usable (condition/status workflow must allow re-entry to assignable state with audit).
5. **Returning a key requires an active assignment.**
6. **Every assignment must record** holder, actor (authenticated user), timestamp (UTC), and reason.
7. **Every return must close the active assignment** and record actor, timestamp, and reason.
8. **Historical assignments must never be deleted** — close or supersede; do not hard-delete assignment history.
9. **Every key state change must create an audit event** — status, condition, location, assignment, return, loss, damage, retirement, audit reconciliation.

---

## 9. Roles and capabilities

### Roles

| Role | Intent |
| --- | --- |
| **SuperUser** | Full system access including user and settings management. |
| **KeyAdministrator** | Configuration of categories, locations, access levels, and key master data. |
| **KeyOperator** | Day-to-day assign, return, transfer, mark lost/damaged, and audit operations. |
| **KeyViewer** | Read-only access to keys, history, and permitted reports. |

### Capabilities

Capabilities drive **UI visibility** and **server-side authorization**:

| Capability | Typical use |
| --- | --- |
| **CanManageKeys** | Register, edit, retire keys; manage key master data. |
| **CanAssignKeys** | Assign keys to holders. |
| **CanReturnKeys** | Process key returns. |
| **CanAuditKeys** | Run inventory audits and record findings. |
| **CanViewReports** | Access Reports module pages. |
| **CanManageUsers** | Administer users and role assignments. |
| **CanManageSettings** | Application and reference-data settings. |

**Mapping guidance:**

- **SuperUser** — all capabilities.
- **KeyAdministrator** — CanManageKeys, CanViewReports, CanManageSettings (not necessarily CanManageUsers unless also granted).
- **KeyOperator** — CanAssignKeys, CanReturnKeys, CanAuditKeys; read key search/history.
- **KeyViewer** — read-only key search/history + CanViewReports where appropriate.

Server-side policy enforcement is authoritative; UI hiding is supplemental.

---

## 10. UI structure

### Home (`/`)

- Role-aware dashboard
- Quick actions (assign, return, search — based on capabilities)
- Key status summary (counts by status)
- Recent activity feed (assignments, returns, audit events)

### Admin (`/Admin/*`)

- Users
- Settings
- Key categories
- Key locations
- Access levels

Visible only to users with admin capabilities (SuperUser, KeyAdministrator as configured).

### Operations (`/Operations/*`)

- Register key
- Assign key
- Return key
- Transfer key
- Mark lost / damaged
- Inventory audit

Primary workspace for KeyOperator; KeyViewer does not see mutation actions.

### Reports (`/Reports/*`)

- Current assigned keys
- Overdue keys (assigned beyond policy threshold if configured)
- Lost keys
- Damaged keys
- Key history
- Holder history
- Location inventory

Read-heavy; export optional in later phase but not required for MVP acceptance.

### Help (`/Help/*`)

- Integrated Help Center index
- Contextual help pages linked from Operations and Admin pages
- In-app HTML content only — no markdown downloads

---

## 11. UX requirements

- **Professional enterprise UI** — consistent with DentalInventory-style shell, typography, spacing, and panels.
- **Consistent filters** — shared filter layout and behavior across Operations and Reports list pages.
- **Pagination** — all large tables paginate; do not render unbounded result sets.
- **Empty states** — clear guidance when no keys, assignments, or report rows exist.
- **Success toasts after mutations** — confirm assign, return, register, mark lost/damaged, transfer, audit save.
- **Clear validation messages** — field-level and summary errors; no silent failures.
- **Keyboard-friendly forms** — logical tab order, labels, focus management on validation failure.
- **Responsive tables** — usable on common desktop widths; horizontal scroll acceptable on narrow viewports.
- **No admin/setup content for ordinary operators** — operators see Operations + permitted Reports + Help only.

---

## 12. Security requirements

- **Session-only authentication** — cookie-based ASP.NET Identity session (or equivalent in approved alternative stack).
- **No Remember Me** — do not offer persistent login.
- **Capability-based UI visibility** — nav and actions gated by resolved capabilities.
- **Server-side authorization** — every page and POST handler enforces policy; direct URL access must fail for unauthorized users.
- **Admin functions hidden from unauthorized users** — and blocked server-side if accessed directly.
- **Audit all mutations** — assignment, return, status/condition/location changes, registration, retirement, audit reconciliation.

---

## 13. Database requirements

- **Use migrations** — EF Core (or Prisma migrate for alternative stack); no manual schema drift.
- **No seed business data** except optional evaluation admin (local smoke only; disabled in production).
- **Preserve audit history** — append-only audit events; no destructive deletes of assignments or audit rows.
- **Indexes** on key number, status, holder (active assignment), and location for search/report performance.
- **Prevent duplicate active assignment** at database and/or service layer (unique constraint or transactional invariant on active assignment per key).

---

## 14. Testing requirements

- **Domain invariant tests** — double assignment, lost/retired/damaged rules, return-without-assignment failures.
- **Authorization tests** — page and handler access by role/capability.
- **UI visibility tests** — nav and action buttons absent for unauthorized roles (where test host supports it).
- **Audit history tests** — every mutation creates expected audit events; history not deleted.
- **Workflow tests** — assign, return, mark lost, mark damaged, transfer, audit happy paths and rule violations.
- **Build/test must pass** before declaring implementation complete.

---

## 15. Acceptance criteria

- [ ] Can create a key
- [ ] Can assign a key
- [ ] Can return a key
- [ ] Cannot double-assign a key
- [ ] Cannot assign a lost key
- [ ] Cannot assign a retired key
- [ ] Key history is preserved (assignments and audit events)
- [ ] Operator sees operational UI only (no Admin configuration surfaces)
- [ ] Admin sees admin/configuration UI
- [ ] Help Center works (index + at least one contextual help topic)
- [ ] Visual quality matches DentalInventory-style enterprise UI (shell nav, cards, tables, badges, toasts, professional presentation)

---

## 16. Prompt for Codex

Copy the prompt below verbatim when instructing Codex to implement KeyInventory.

---

```
Build a new enterprise web application named KeyInventory using docs/key-inventory-product-brief.md as the binding product brief.

Read the entire brief first. Assume no prior knowledge of DentalInventory except what the brief describes.

Implementation requirements:
- Use ASP.NET Core Razor Pages, EF Core, SQL Server (SQLite acceptable for local dev only), Bootstrap-style enterprise UI, and server-rendered pages unless explicitly directed otherwise.
- Match DentalInventory-style enterprise product experience: top shell navigation, role-aware home, Admin / Operations / Reports / Help separation, cards, tables, filters, badges, toasts, integrated Help Center, contextual help, session-only auth (no Remember Me), capability-based UI visibility with server-side authorization.
- Implement all core entities, statuses, workflows, business rules, roles, capabilities, UI structure, UX, security, database, and testing requirements defined in the brief.
- Enforce invariants: no double active assignment; lost/retired/damaged keys not assignable per rules; returns require active assignment; append-only assignment and audit history; audit event on every key state change.
- Do not seed business data except optional local evaluation admin (disabled in production).
- Add domain, application, infrastructure, web, and test projects with migrations and passing tests before completion.

Deliverables:
- Runnable KeyInventory solution
- Migrations applied for local dev
- Tests covering domain invariants, authorization, workflows, and audit history
- Help Center with in-app HTML help (no markdown downloads for users)

Stop when all acceptance criteria in the brief are satisfied and build/tests pass.
```

---

*End of product brief.*
