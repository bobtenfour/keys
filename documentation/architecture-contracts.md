# Architecture Contracts

## Authority
This document is the technical architecture boundary authority.

## Purpose
Define layer responsibilities, dependency direction, ownership, and forbidden coupling.

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
- MIGRATION-1 establishes the minimum persistence foundation required for LOAN-VERTICAL-1.
- MIGRATION-1 includes one EF Core `DbContext` in Infrastructure.
- MIGRATION-1 includes the initial migration for only these entities: KeyType, KeyAsset, Loan, and Return.
- KeyAsset persistence may omit optional KeySeries and Lock references until a later authorized slice.
- Authoritative UTC timestamps persist as `DateTimeOffset` values without conversion or normalization.
- Local development persistence uses SQLite.
- A design-time `DbContext` factory may exist in Infrastructure solely to create and apply migrations.
- MIGRATION-1 does not implement Application port adapters, command handlers, repository facades beyond the `DbContext`, business DI registrations, UI, seed data, or demo pages.
- Port adapter implementation and runtime workflow DI belong to LOAN-VERTICAL-1.
- Identity, AuditEvent, Lock, Location, and KeySeries physical tables are out of scope for MIGRATION-1.

## LOAN-VERTICAL-1 Runtime Workflow Contract
- Application owns the LOAN-VERTICAL-1 use cases: create Key Asset, issue Loan, complete Return, list Open Loans, and list Returned Loans.
- Create Key Asset accepts catalog key code and key type code; when the KeyType does not exist, Application creates that KeyType before creating the KeyAsset.
- Issue Loan and Complete Return use existing Domain Loan and Return aggregates and `UtcTimestamp` validation.
- Borrower Party is an opaque required string reference; no Party aggregate is introduced.
- Infrastructure implements persistence adapters against the existing `KeyInventoryDbContext` and MIGRATION-1 entity mappings; adapters translate between Domain aggregates and persistence entities without owning business rules.
- The Web composition root registers SQLite `DbContext`, persistence adapters, and Application use cases required by this slice.
- Web owns Razor Pages for the LOAN-VERTICAL-1 workflow only.
- LOAN-VERTICAL-1 must not introduce authentication, authorization runtime, automatic audit emission, a second persistence model, in-memory fake stores, mock workflows, seed/demo data, or speculative abstractions.

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

## Depends On
- project-governance.md

## Depended On By
- business-authority-matrix.md
- implementation-contract.md
- slices
