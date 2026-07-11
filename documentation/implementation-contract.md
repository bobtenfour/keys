# Implementation Contract

## Authority
This document is the sole authority for how implementation work is executed.

## Purpose
Prevent improvisation, scope drift, patches, duplicate authority, and incomplete slice closure.

## Permanent Rules
- Implement exactly one approved slice.
- Do not start the next slice automatically.
- Read every governing contract required by the slice before implementation.
- Stop if a required contract is missing, contradictory, or too vague.
- Do not make assumptions to fill architectural gaps.
- Do not modify unrelated files.
- Do not introduce temporary behavior.
- Do not introduce TODO, FIXME, commented code, dead code, placeholders, fallbacks, compatibility hacks, or speculative abstractions.
- Do not place business logic in UI.
- Do not mutate state outside the owning service or workflow.
- Preserve one source of truth and one authority per business decision.

## Mandatory Slice Workflow
1. Open the approved slice specification.
2. Read required governing contracts.
3. Validate contract consistency.
4. Validate prior slice dependencies.
5. Stop if any blocker exists.
6. Implement only the approved scope.
7. Validate architectural boundaries.
8. Run build.
9. Run tests.
10. Verify repository hygiene.
11. Update documentation only if required by the slice.
12. Produce closure report.
13. Stop.

## Required Closure Report
The report must contain only:
- Transversal Gate
- Technical summary
- Files changed
- Architectural decisions
- Risks found
- Acceptance verification
- Build result
- Test result
- Git status

## Mandatory Gates
Every slice must close with:
- Transversal Gate PASS.
- Architecture consistency PASS.
- Authority consistency PASS.
- ERD consistency PASS when the slice touches data model.
- Capability consistency PASS when the slice touches business capabilities.
- Product experience consistency PASS when the slice touches UI.
- Build PASS.
- Tests PASS.
- No new warnings.
- Repository hygiene PASS.

## Decision Outcomes
- ACCEPT: all closure criteria satisfied.
- REWORK: scope is valid but closure evidence is incomplete or correction is required.
- REJECT: work violates architecture, scope, or authority.

## Slice File Invariant
- Accepted, Approved, In Progress, Implementation Complete, and Closed slices must have exactly one file under documentation/slices.
- Planned slices must not have slice files until approved.

## Depends On
- project-governance.md

## Depended On By
- implementation-roadmap.md
- documentation/slices/*.md
