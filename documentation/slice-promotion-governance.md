# Slice Promotion Governance

## Authority
This document is the sole authority for slice preparation and status transitions.

## Purpose
Define deterministic slice preparation and status rules so implementation sequencing requires no inference.

## Preparation Decision Authority
Architectural governance means the human repository owner or an explicitly delegated human architectural reviewer.

Only architectural governance may decide ACCEPT, REWORK, REJECT, CLOSE, REOPEN, SUPERSEDE, or Prepare Next Slice.

Cursor, Codex, automation, CI, build results, test results, or completion of another slice cannot authorize slice preparation.

## Preparation Execution Authority
Prepare Next Slice is the only governance action between two implementation cycles.

Cursor/Codex may materially update governing documents, roadmap, and slice status only after receiving an explicit Prepare Next Slice instruction from architectural governance.

The instruction must identify the exact next slice to prepare.

Cursor/Codex must verify all preparation preconditions before modifying files.

If any precondition fails, Cursor/Codex must stop without changing status.

Implementation execution may update a slice to Implementation Complete only when the approved slice specification explicitly requires closure documentation and all implementation evidence is present.

## Prepare Next Slice
Prepare Next Slice may:
- Complete existing governing contracts.
- Complete existing ERD authority.
- Complete existing authority mappings.
- Create the slice specification.
- Define acceptance criteria.
- Define required tests.
- Mark the slice Approved.

Prepare Next Slice must not:
- Change roadmap order.
- Promote any slice other than the prepared next slice.
- Create new slice states.
- Begin implementation.

## Preparation Record
A valid preparation updates implementation-roadmap.md and the slice specification in the same execution.

The slice specification must record the decision, date, evidence, and deciding authority role.

Planned to Approved occurs only as the outcome of Prepare Next Slice.

No separate approval document or meeting record is required unless another governing contract explicitly requires one.

## Repository Ownership Boundary
Git workflow, staging, commits, cleanliness, and repository-state verification are controlled exclusively by the human repository owner.

Cursor/Codex must not run Git commands unless the human explicitly requests a specific Git operation.

Git state is not a slice-preparation precondition under this contract.

Preparation-only governance must not change runtime code, tests, or unrelated documentation.

The roadmap must remain the source of implementation sequence.

## Transition Rules

### Planned to Approved
Preconditions:
- All dependency slices listed in the roadmap are Accepted or Closed.
- The slice is next in the roadmap sequence among non-terminal slices.
- Prepare Next Slice has completed existing governing contracts, existing ERD authority, existing authority mappings, slice acceptance criteria, and required tests to the level needed for implementation without inference.
- A slice specification exists or is created as part of Prepare Next Slice.
- If any required governing document is missing any item above, the slice remains Planned.

Required evidence:
- Explicit human architectural governance instruction naming Prepare Next Slice and the exact slice.
- Preconditions verified.
- Required governing documents provide complete domain ownership.
- Required governing documents provide complete domain invariants.
- Required governing documents provide complete logical ERD representation.
- Required governing documents provide complete authority ownership.
- Required governing documents provide complete acceptance criteria for the slice.
- Slice specification defines required tests.
- Required governing documents have no dependency on undefined future contracts.
- Required governing documents contain no ambiguity requiring architectural interpretation.
- Slice specification with scope, out-of-scope items, Required Governing Documents, dependencies, allowed files, forbidden files, acceptance criteria, required tests, and closure contract.
- Roadmap and slice specification updated atomically.

### Approved to In Progress
Preconditions:
- Exactly one roadmap slice has status Approved.
- No slice has status In Progress.
- Required previous slices are Accepted or Closed.
- Implementation Readiness Gate PASS.

Required evidence:
- Approved roadmap row.
- Existing slice specification.
- Only the governing documents declared by the slice specification read and validated.
- No internal inconsistency, declared governing-document contradiction, or prior-slice dependency blocker.

### In Progress to Implementation Complete
Preconditions:
- The implementation changed only files allowed by the slice.
- The implementation satisfies the slice scope and does not include out-of-scope work.
- Required architecture, authority, capability, ERD, product, build, and test gates applicable to the slice pass.

Required evidence:
- Closure report.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Acceptance evidence mapped to each slice acceptance criterion.

### Implementation Complete to Accepted
Preconditions:
- Architectural review confirms all closure evidence.
- No required gate is missing, failed, ambiguous, or contradicted by promotion evidence.
- Acceptance evidence matches the implemented scope.

Required evidence:
- Architectural governance acceptance.
- Slice status update to Accepted in the roadmap and slice specification.
- Acceptance Record in the slice specification.

### Accepted to Closed
Preconditions:
- Architectural governance determines no further action remains for the slice.
- The slice has already been Accepted.
- Closing the slice does not alter implementation sequence or promote another slice.

Required evidence:
- Architectural governance closure decision.

## Automatic Promotion
No slice is prepared or approved automatically.

The next slice may move from Planned to Approved only through explicit Prepare Next Slice governance action under this contract.

## Forbidden Transitions
- Planned to In Progress.
- Planned to Implementation Complete.
- Planned to Accepted.
- Planned to Closed.
- Approved to Accepted.
- Approved to Closed.
- In Progress to Accepted.
- In Progress to Closed.
- Implementation Complete to Closed.
- Any transition that skips required evidence.
- Any transition that promotes more than one slice at the same time.
- Any transition that creates more than one In Progress slice.
- Any transition that changes roadmap order without explicit roadmap governance.
- Any transition inferred from completion of another slice.

## Acceptance Criteria
- Slice preparation authority is explicit.
- Each allowed transition has preconditions.
- Each allowed transition has required evidence.
- Automatic next-slice preparation or approval is forbidden.
- Repository ownership boundary is explicit.
- Forbidden transitions are explicit.
- Roadmap sequence remains unchanged.
- Existing slice statuses remain unchanged unless a valid preparation or transition action is explicitly approved.

## Depends On
- project-governance.md
- implementation-contract.md
- implementation-roadmap.md

## Depended On By
- implementation-contract.md
- implementation-roadmap.md
- documentation/slices/*.md
