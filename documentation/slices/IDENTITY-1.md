# IDENTITY-1 - Identity & Security Foundation

## Status
Accepted

## Parent Phase
Phase 1

## Purpose
Establish the technical identity and authorization foundation without introducing authentication UI, business identity, or business workflows.

## Objective
The repository contains a minimal technical identity and RBAC foundation for principals, roles, permissions, assignments, service contracts, dependency registration, and tests while keeping Party as a separate business identity.

## Scope
- SecurityPrincipal aggregate foundation.
- Role aggregate foundation.
- Permission aggregate foundation.
- PrincipalRoleAssignment.
- RolePermission.
- Identity service contracts.
- Authorization service contracts.
- Dependency injection registration required for identity service contracts.
- Architecture tests protecting identity and layer boundaries.
- Domain invariants required by the Domain Contract.
- Unit tests.
- Architecture tests.

## Out of Scope
- Authentication UI.
- Login.
- Logout.
- Password reset.
- MFA.
- JWT.
- Cookies.
- OAuth.
- OpenID Connect.
- External providers.
- User administration screens.
- Party management.
- Business workflows.
- Custody.
- Loans.
- Inventory.
- Reporting.
- Policy Engine.
- Audit workflows.
- Persistence implementation.
- Provider-specific infrastructure.
- Provider-specific configuration.
- Sample data.
- Demo pages.
- Placeholders.
- TODO.
- FIXME.
- Commented code.

## Required Governing Contracts
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- project-architecture-index.md
- architecture-contracts.md
- business-authority-matrix.md
- key-inventory-domain-contract.md
- key-inventory-erd.md
- security-capability-contract.md
- system-integrity-contract.md
- testing-strategy.md
- roadmap.md
- product-vision.md

## Required Previous Slices
- SOLUTION-FOUNDATION-2

## Allowed Files
- documentation/slices/IDENTITY-1.md
- src/KeyInventory.Domain/**
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except documentation/slices/IDENTITY-1.md
- database/**
- migrations/**
- authentication UI files
- user administration UI files
- party management files
- business workflow files

## Authority Owner
implementation-contract.md

## Architectural Risks
- Merging technical identity with Party.
- Letting authentication own authorization.
- Letting authorization own Party.
- Introducing framework-default Identity scaffolding without governing authority.
- Introducing business workflows under identity.
- Creating duplicate authority for roles, permissions, or security decisions.
- Adding persistence, provider-specific infrastructure, or provider-specific configuration.

## Acceptance Criteria
- SecurityPrincipal, Role, Permission, PrincipalRoleAssignment, and RolePermission foundations exist only within the identity/security scope.
- Identity and authorization service contracts exist.
- Dependency injection registration required by this slice exists.
- Party remains independent.
- Authentication and authorization remain separate.
- Roles and permissions own authorization only.
- No UI, login, logout, MFA, JWT, cookies, OAuth, OpenID Connect, external providers, user administration screens, Party management, custody, loans, inventory, reporting, policy engine, audit workflows, sample data, or demo pages are introduced.
- Architecture tests protect identity and layer boundaries.
- Unit tests verify identity invariants.
- Build PASS.
- Tests PASS.
- Repository hygiene PASS.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Authority consistency PASS
- ERD consistency PASS
- Security capability consistency PASS
- System integrity consistency PASS
- Build PASS
- Tests PASS
- Repository hygiene PASS

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Acceptance Record
- Identity/RBAC foundation implemented.
- Party remains independent.
- No authentication runtime.
- No authorization runtime.
- Resolvable composition PASS.
- Infrastructure boundary PASS.
- Build PASS.
- Tests PASS.
- Repository hygiene PASS.

## Next Allowed Slice
STOP
