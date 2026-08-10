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
- Workforce Members index shows person identity (FirstName LastName, UIN), Type, Organization, Department, Responsible Manager, Status, and actions; PartyCode and WorkforceMemberCode are not primary operator-facing columns; Add and Detail/Edit are dedicated routes; Terminate lives on the selected member detail page with deliberate confirmation.
- Prefer authoritative selectors over manually typed foreign-reference codes when Application list authorities already expose the choices.

## OPERATOR-AUDIT-1 Product Experience
- Administration includes an Audit Trail list showing Date/Time, Operator, Action, Subject, and Details for persisted operator business mutations.
- Prefer operator-readable action and subject labels; authenticated operator display uses the existing KeyInventory user identity name.
- Practical filters (date range, operator, action, subject/reference) are presentation over Application trail query results.
- Do not turn normal operations pages into audit dashboards; optional subject links to filtered history are allowed when simple.

## Applies When
This document is required only for slices that create or modify UI, navigation, user workflows, messages, or product-facing behavior.

## Depends On
- product-vision.md

## Depended On By
- web/UI slices
