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

## Applies When
This document is required only for slices that create or modify UI, navigation, user workflows, messages, or product-facing behavior.

## Depends On
- product-vision.md

## Depended On By
- web/UI slices
