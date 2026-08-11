# Security Capability Contract

## Authority
This document governs security capability boundaries.

## Purpose
Separate authentication, authorization, policy, audit, and digital trust responsibilities.

## Security Areas
- Identity: owns technical principal identity and principal lifecycle.
- Authentication: prove identity.
- Authorization: determine allowed action.
- RBAC: role-based authorization authority for roles, permissions, role-permission links, and principal-role assignments.
- Policy: configurable decision authority for advanced rules.
- Audit: immutable evidence of relevant actions; Domain AuditEvent aggregate authority is owned by key-inventory-domain-contract.md; OPERATOR-AUDIT-1 authorizes Application-owned OperatorAuditRecord operational accountability using the authenticated KeyInventory user identity.
- Digital Trust: integrity, acceptance, and non-repudiation concepts.

## Boundary Ownership
- Identity owns SecurityPrincipal, SecurityPrincipalType, technical principal identity, and principal lifecycle.
- Authentication proves a principal identity.
- Authorization determines whether an authenticated principal may perform an action.
- RBAC owns Role, Permission, RolePermission, and PrincipalRoleAssignment.
- Role identity is scoped to the KeyInventory installation: RoleCode is unique across Role records; Role must not carry OrganizationCode or Organization business scoping. Organization was removed as an active business authority by OPERATOR-EXPERIENCE-1; do not replace Role organization scoping with Tenant/Site/Facility abstractions.
- PrincipalRoleAssignment continues to relate SecurityPrincipal, Role, and AuthorizationScopeType without requiring Organization business identity.
- Policy may refine authorization decisions in a future phase but does not own basic RBAC.
- Audit capability covers immutable evidence of relevant actions; AuditEvent aggregate ownership and invariants are defined by key-inventory-domain-contract.md; operational mutation accountability via OperatorAuditRecord is authorized by OPERATOR-AUDIT-1 and must not invent a second Operator/User identity model.
- Digital Trust owns integrity, acceptance, and non-repudiation concepts.
- Party is business identity and is not owned by Identity, Authentication, Authorization, RBAC, Policy, Audit, or Digital Trust.
- SecurityPrincipal may reference Party for human principals but must not duplicate Party profile or business data.
- ASP.NET Identity application authentication remains the runtime login authority already delivered; Domain Role OrganizationCode removal reconciles foundation RBAC with the single-site product model and must not invent a second user model.

## IDENTITY-1 Service Contract Boundary
IDENTITY-1 may define:
- Identity principal query and lifecycle contracts.
- Role and permission query contracts.
- Role-assignment command contracts.
- Authorization decision contract interface only when the decision input and output can be fully defined without runtime policy implementation.

IDENTITY-1 must not define:
- Authentication credentials.
- Authentication provider implementation.
- Authorization runtime enforcement unless explicitly approved by the slice.
- Provider-specific infrastructure.
- JWT, cookies, OAuth, OpenID Connect, ASP.NET Identity, LDAP, external identity providers, or other provider choices.

## Capability Examples
- Policy must be able to compose critical-risk and outside-business-hours conditions.
- Authorization must be able to require supervisor and security-officer approval when policy requires it.
- Digital Trust may use integrity mechanisms such as SHA-256 and hash chaining.
- Digital Trust may use acceptance methods such as electronic signature, PIN, NFC, smart card, and biometrics.

## Rules
- Identity is not Party.
- Authentication is not authorization.
- Authorization is not audit.
- RBAC is not Policy.
- Policy is not basic RBAC.
- Audit is not authentication.
- Integrity proof is not user authentication.
- Security decisions require explicit authority before implementation.

## Depends On
- product-vision.md
- architecture-contracts.md

## Depended On By
- identity slices
- authorization slices
- policy slices
- audit slices
