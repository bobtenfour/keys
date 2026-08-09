# Architecture Contracts

## Authority
This document is the technical architecture boundary authority.

## Purpose
Define layer responsibilities, dependency direction, ownership, and forbidden coupling.

## Product Scope Boundary
Architecture serves one building and a small operational workforce.
Do not introduce abstractions solely for hypothetical multi-campus, multi-building-enterprise, multi-tenant, large-scale, cross-organization, distributed, or future-platform requirements.
Do not introduce policy engines, generalized authorization engines, workflow engines, event platforms, or extensibility frameworks unless a concrete KeyInventory business requirement later proves they are necessary.
Workforce Eligibility evaluates legitimate active-worker key issue eligibility; it is not a generalized access-control or key-authorization policy engine.
Building and Room must not be expanded into Campus or enterprise location hierarchies without a future explicit business requirement.
Key Catalog owns current KeyAsset-to-Room opening assignments; Location owns Building and Room identity; Building for a key is derived only through Room.
Key-to-Room Assignment is the operational authority for which Rooms a physical key opens; Lock must not be required or used as an intermediate room-opening authority.
Master/sub-master hierarchy is out of scope; multiple Rooms are represented by multiple current assignments.

## Operational Report Export Boundary
Existing REPORTS-1 tabular reports may be represented as on-screen tables and as CSV, XLSX, and PDF downloads of the same Application-owned filtered result set.
Application owns report queries and DTO result sets; export formatters consume those results and must not independently query SQL Server or invent a second reporting store.
XLSX must be a genuine Excel workbook and PDF must be a genuine readable PDF; CSV behavior established by REPORTS-1 remains authoritative for the CSV representation.
This boundary does not authorize REPORTS-2, new report families, BI platforms, report designers, scheduled delivery, email delivery, or enterprise analytics frameworks.

## Layers
### Domain
Owns business rules, domain invariants, and aggregate consistency.

### Application
Owns use cases, orchestration, ports, and transaction boundaries.

### Infrastructure
Owns persistence, external systems, integration adapters, and technical implementations of ports.

### Web
Owns presentation, request/response binding, navigation, and product experience.

## Dependency Rules
- Domain references no project.
- Application may reference Domain.
- Infrastructure may reference Application and Domain only when required for implementation.
- Web may reference Application and Infrastructure only through composition and presentation needs.
- Domain must never depend on Infrastructure or Web.
- Business logic must not exist in Web.

## Composition Root
Runtime composition belongs to the application host. Service registration must not become business authority.

## Persistence Foundation Contract
- Infrastructure owns physical persistence mapping and EF Core migrations.
- Logical entity ownership remains in `key-inventory-domain-contract.md` and `key-inventory-erd.md`.
- Persistence must not own business rules, workflow decisions, or UI behavior.
- SQL Server is the sole authorized persistence provider.
- SQLite, in-memory EF providers, and any second persistence provider are forbidden.
- The canonical runtime connection string name is `KeyInventory` under configuration path `ConnectionStrings:KeyInventory`.
- Runtime and design-time persistence use EF Core SQL Server (`UseSqlServer`) with that connection string only.
- MIGRATION-1 establishes the minimum persistence foundation required for LOAN-VERTICAL-1.
- MIGRATION-1 includes one EF Core `DbContext` in Infrastructure.
- MIGRATION-1 includes the initial migration for only these entities: KeyType, KeyAsset, Loan, and Return.
- KeyAsset persistence may omit optional KeySeries references until a later authorized slice.
- KeyAsset persistence must not treat Lock as required intermediate authority for Rooms opened by a key; Key-to-Room Assignment is the contracted room-opening authority when later implemented.
- Authoritative UTC timestamps persist as `DateTimeOffset` values without conversion or normalization.
- A design-time `DbContext` factory may exist in Infrastructure solely to create and apply migrations against SQL Server using `ConnectionStrings:KeyInventory`.
- MIGRATION-1 does not implement Application port adapters, command handlers, repository facades beyond the `DbContext`, business DI registrations, UI, seed data, or demo pages.
- Port adapter implementation and runtime workflow DI belong to LOAN-VERTICAL-1.
- Identity, AuditEvent, Lock, Location, and KeySeries physical tables are out of scope for MIGRATION-1.

## LOAN-VERTICAL-1 Runtime Workflow Contract
- Application owns the LOAN-VERTICAL-1 use cases: create Key Asset, issue Loan, complete Return, list Open Loans, and list Returned Loans.
- Create Key Asset accepts catalog key code and key type code; when the KeyType does not exist, Application creates that KeyType before creating the KeyAsset.
- Issue Loan and Complete Return use existing Domain Loan and Return aggregates and `UtcTimestamp` validation.
- Borrower Party is an opaque required string reference; no Party aggregate is introduced.
- Infrastructure implements persistence adapters against the existing `KeyInventoryDbContext` and MIGRATION-1 entity mappings; adapters translate between Domain aggregates and persistence entities without owning business rules.
- The Web composition root registers the SQL Server `DbContext` using `ConnectionStrings:KeyInventory`, persistence adapters, and Application use cases required by this slice.
- Web owns Razor Pages for the LOAN-VERTICAL-1 workflow only.
- LOAN-VERTICAL-1 must not introduce authentication, authorization runtime, automatic audit emission, a second persistence model, SQLite, in-memory fake stores, mock workflows, seed/demo data, or speculative abstractions.

## UTC Timestamp Contract
- Authoritative business timestamps are UTC instants.
- Authoritative Domain timestamps are represented as `DateTimeOffset` values with `Offset` equal to `TimeSpan.Zero`.
- Required authoritative timestamps must reject `default(DateTimeOffset)`.
- Domain entry points that accept authoritative timestamps must reject non-UTC offsets.
- Authoritative temporal attributes use UTC naming (`Utc` or `AtUtc`).
- Local civil time, display time zones, and user-facing time conversion are not Domain authority and must not become authoritative business time.
- Persistence-provider date/time types, database time-zone configuration, and UI formatting remain outside this contract's runtime ownership and belong to later authorized slices.
- A system clock abstraction, time provider port, or NodaTime dependency is not required by this contract and must not be introduced unless a later slice explicitly authorizes it.

### Shared UTC Validation Helper
- The Domain provides one shared UTC validation helper for authoritative timestamps.
- The helper accepts a `DateTimeOffset`.
- The helper requires `Offset == TimeSpan.Zero`.
- The helper rejects `default(DateTimeOffset)`.
- The helper never converts or normalizes values.
- On success, the helper returns the validated value unchanged.

### UTC Validation Failure Semantics
- Invalid timestamps are contract violations.
- Validation fails immediately.
- The concrete exception type is intentionally left unspecified by UTC-1.

## Workforce Eligibility Boundary Contract
- Party boundary owns persistent person and organization business identity, including person FirstName, LastName, and UIN.
- Workforce Eligibility boundary owns Organization, Department, WorkforceMember as the workforce relationship and eligibility authority, ResponsibleManager relationship rules, WorkAssignment, key-issue eligibility evaluation, and termination return-obligation signaling.
- WorkforceMember is the workforce relationship, not person identity.
- Employment is not a separate aggregate; relationship authority must not be duplicated under an Employment entity.
- Location boundary owns Building and Room place authority, including required RoomNumber uniqueness within one Building.
- WorkforceMember references Party and must not own or duplicate Party person-identity attributes.
- Borrower remains a workflow role only; a Borrower aggregate, temporary borrower fields, and duplicate identity authority are forbidden.
- Workforce Eligibility must not own Loan workflow mutation, Return workflow mutation, custody, lifecycle, audit emission, authentication, authorization runtime, HR integration, persistence implementation, or UI.
- WorkforceMember termination, rehire, Department change, Organization change, and Employee or Contractor WorkforceType transition are relationship changes and must not rewrite Party person identity.
- WorkforceMember termination may forbid new key issues and signal a mandatory return obligation; it must not automatically mutate Loan, Return, custody, lifecycle, or audit authority.
- Required key returns after termination complete only through the existing Return workflow.
- WORKFORCE-ELIGIBILITY-1 is structural preparation and Approved slice specification only until Phase 1 close prerequisites are Accepted; implementation of this boundary is forbidden until the slice is authorized to start.

## Forbidden
- Duplicate business rules.
- Cross-layer shortcuts.
- UI-owned business decisions.
- Persistence-owned business rules.
- Domain depending on framework infrastructure.
- Runtime features without owning contracts.
- Authoritative Domain timestamps with non-UTC offsets.
- Required authoritative timestamps equal to `default(DateTimeOffset)`.
- Converting or normalizing non-UTC timestamps into UTC inside Domain validation.
- Speculative enterprise-scale architecture introduced without a concrete operational need for this building.
- Policy engines, generalized authorization engines, workflow engines, event platforms, or extensibility frameworks introduced without an explicit later business requirement.

## Depends On
- project-governance.md

## Depended On By
- business-authority-matrix.md
- implementation-contract.md
- slices
