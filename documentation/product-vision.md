# Product Vision

## Authority
This document is the highest product authority for KeyInventory.

## Purpose
Define what KeyInventory is, who it serves, what problems it solves, and what it must never become.

## Product Scope
KeyInventory is a modest physical key-management product for a single-site installation (one organization and one building as fixed product scope—not as operator-configurable multi-organization or multi-building entities) of approximately five floors, a relatively small workforce of employees and regular contractors, physical rooms and their keys, day-to-day issue and return operations, clear accountability for who has each key, and straightforward administration with audit/history.

KeyInventory is not an enterprise-wide key-management platform for a large institution.

## Vision
KeyInventory helps a key custodian control physical keys at one site: catalog keys, issue and return them to legitimate active workers, know who has each key, administer the small operational model required for that work, and retain clear audit/history.
Organization and Building are not active configurable business concepts; Rooms and Departments are administered directly for the installation.

## Development Model
Development delivers incremental vertical capability.
Each business capability is built one at a time and left complete, functional, persistent, testable, and usable through the UI before the next capability begins.
The normal product-development question is: "What concrete capability does the key custodian need next?"
It is not: "What architecture might a large institution need later?"

## Product Scope Rule
Future slices require a concrete operational need for this building.
A future slice must not be justified solely by speculative scale, multi-campus design, multi-building-enterprise design, multi-tenant design, large-scale distribution, or future-platform extensibility.
Do not introduce policy engines, generalized authorization engines, workflow engines, event platforms, extensibility frameworks, or similar infrastructure unless a concrete KeyInventory business requirement later proves they are necessary.
Do not reintroduce Organization or Building as configurable business concepts, and do not replace them with Tenant, Site, Facility, Campus, LocationRoot, or other hierarchy abstractions without a future explicit business requirement.
Do not expand Room administration into Campus or enterprise location hierarchies without a future explicit business requirement.
Do not expand Workforce Eligibility into a generalized access-control or key-authorization policy engine.

## Target Users
- Key custodians and facilities operators for this installation
- Operations staff who issue and return keys
- Employees and regular contractors who receive keys under controlled workflows
- Administrators who maintain catalog, workforce, and room data for this installation
- Reviewers who need straightforward audit/history

## Product Outcomes
KeyInventory must:
- Control physical key cataloging for this installation.
- Control day-to-day key issuance and return.
- Keep clear accountability for who has each key.
- Support workforce eligibility so keys are issued only to legitimate active workers.
- Preserve audit/history evidence for operational actions.
- Remain complete and usable after each delivered capability.
- Grow only by concrete operational need for this building.

## Product Non-Goals
KeyInventory is not:
- An enterprise-wide institutional key platform.
- A multi-campus or multi-tenant key platform.
- A building access control system.
- A badge management system.
- A locksmith ERP.
- A generic asset inventory system.
- A visitor management system.
- A patient, HR, payroll, or identity master system.
- A speculative policy, workflow, event, or extensibility platform.

## Boundary
This document never defines implementation, files, projects, database schema, UI screens, runtime workflow details, or slice sequencing.

## Depends On
None.

## Depended On By
- roadmap.md
- key-inventory-domain-contract.md
- product-experience-contract.md
