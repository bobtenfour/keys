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

## Forbidden
- Duplicate business rules.
- Cross-layer shortcuts.
- UI-owned business decisions.
- Persistence-owned business rules.
- Domain depending on framework infrastructure.
- Runtime features without owning contracts.

## Depends On
- project-governance.md

## Depended On By
- business-authority-matrix.md
- implementation-contract.md
- slices
