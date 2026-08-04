# Key Inventory Capability Map

## Authority
This document is the authority for product capabilities.

## Purpose
Define what the product must be able to do, independent of implementation order.

## Capabilities
- Solution Foundation
- Identity and Access
- Key Catalog
- Loan and Return
- Immutable Audit
- Custody Chain
- State Machine
- Event Sourcing Readiness
- Physical Inventory
- Maintenance Lifecycle
- Workforce Key Eligibility
- Unified Parties
- Operational Security
- Reporting and Dashboards
- Business Intelligence
- Policy Engine
- Advanced Authorization
- Smart Cabinet Integration
- Digital Trust
- Enterprise Operations

## Capability Details
- Solution Foundation includes authoritative UTC timestamps for business evidence and workflow times, and the minimum EF Core persistence foundation for KeyType, KeyAsset, Loan, and Return, without owning local-time display or system clock infrastructure.
- Loan and Return includes the first runnable LOAN-VERTICAL-1 workflow: create key asset, issue loan, complete return, and list open and returned loans, without authentication, authorization runtime, automatic audit emission, custody, or Party aggregate authority.
- Workforce Key Eligibility includes Organization, Department, Building, Room with RoomNumber unique within Building, Party as persistent person identity with UIN, WorkforceMember as the workforce relationship and eligibility authority for WorkforceType Employee and Contractor, ResponsibleManager, WorkAssignment to Room, key-issue eligibility, and termination return obligations completed through existing Return authority, without a separate Employment aggregate, Borrower aggregate, temporary borrower fields, duplicate person-identity authority, HR integration, automatic Loan/Return/custody/lifecycle/audit mutation, UI, authentication, or persistence implementation in this preparation.
- Immutable Audit includes append-only AuditEvent evidence for business and security-relevant actions, immutable after creation, without rewriting audit history and without owning authentication, authorization, policy, custody, lifecycle, loan workflow, or return workflow authority.
- Reporting and Dashboards includes KPI families for active loans, overdue loans, lost keys, SLA compliance, request frequency, department utilization, risk trends, custody duration, incident rate, maintenance cost, and replacement frequency.
- Enterprise Operations includes future high availability, administrator guidance, and user guidance.

## Rules
- Capabilities do not define implementation sequence.
- Implementation sequence belongs to implementation-roadmap.md.
- Capability ownership must remain aligned with domain concepts.

## Depends On
- key-inventory-domain-contract.md
- roadmap.md

## Depended On By
- implementation-roadmap.md
- slices
