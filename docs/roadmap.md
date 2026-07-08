# KeyInventory Roadmap

Implementation proceeds in ordered slices. One slice equals one commit. Stop after every slice.

## Slice 0 - Architecture Package

Deliverables:

- Governance, ERD, domain, security, UX, testing, roadmap, and integrity contracts.

Acceptance:

- Docs only.
- No runtime code.
- No UI.
- No migrations.
- Git status reported.

## Slice 1 - Solution Foundation

Deliverables:

- ASP.NET Core Razor Pages solution structure.
- Domain, application, infrastructure, web, and test projects.
- No product pages beyond controlled login shell placeholders.
- Build pipeline and test runner.

Acceptance:

- Build passes.
- Empty test suite or smoke tests pass.
- No framework-default product pages leak into authenticated shell.

## Slice 2 - Security Foundation

Deliverables:

- ASP.NET Core Identity cookie session.
- No Remember Me.
- Roles and internal capability resolver.
- Authorization policies.

Acceptance:

- Authorization tests pass.
- Anonymous users cannot access modules.
- Capabilities are application-owned.

## Slice 3 - ERD and Initial Migration

Deliverables:

- EF Core entities for master ERD section KEY.
- Initial migration.
- Database constraints and indexes.

Acceptance:

- Migration tests pass.
- No duplicated authority fields.
- No seed business data.

## Slice 4 - Catalog/Admin

Deliverables:

- Key categories, locations, access levels, conditions.
- Key registration.
- Admin pages with professional UI.

Acceptance:

- `CanManageKeys` and `CanManageSettings` enforced.
- Register key creates audit event.
- UI matches product contract.

## Slice 5 - Assignment Workflow

Deliverables:

- Assign key workflow.
- Active assignment enforcement.
- Assignment history.

Acceptance:

- Double assignment blocked.
- Lost/retired/damaged rules enforced.
- Audit tests pass.

## Slice 6 - Return Workflow

Deliverables:

- Return key workflow.
- Assignment closure.
- Returned condition and location capture.

Acceptance:

- Return requires active assignment.
- Return cannot duplicate.
- Audit tests pass.

## Slice 7 - Status and Location Operations

Deliverables:

- Mark lost, damaged, usable, retired.
- Transfer home location.

Acceptance:

- Legal transitions enforced.
- Illegal transitions rejected.
- Audit tests pass.

## Slice 8 - Search and History

Deliverables:

- Key search.
- Key detail.
- Timeline of assignments, returns, and audit events.

Acceptance:

- Pagination and filters.
- Read-only access for viewers.

## Slice 9 - Reports

Deliverables:

- Assigned keys.
- Lost keys.
- Damaged keys.
- Key history.
- Holder history.
- Location inventory.

Acceptance:

- Reports are read-only.
- No report owns transactional state.

## Slice 10 - Help Center

Deliverables:

- In-app Help index.
- Contextual help topics.

Acceptance:

- No raw markdown downloads.
- Help is rendered as product UI.

## Slice 11 - System Integrity Tests

Deliverables:

- Structural tests for architectural drift.

Acceptance:

- No DbContext in pages.
- No mutation outside services.
- No pages without authorization.
- No workflow without audit.
