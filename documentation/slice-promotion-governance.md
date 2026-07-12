# Slice Promotion Governance

## Authority
This document is the sole authority for slice status transitions.

## Purpose
Define deterministic promotion rules so implementation sequencing requires no inference.

## Promotion Authority
Only architectural governance may promote, close, reject, supersede, or reopen a slice.

Implementation execution may update a slice to Implementation Complete only when the approved slice specification explicitly requires closure documentation and all implementation evidence is present.

## Repository State Requirements
- Promotion reviews must start from a clean Git working tree unless the only pending changes are the promotion record being reviewed.
- Runtime code, tests, and unrelated documentation must not change during promotion-only governance.
- Generated build outputs must not remain modified after promotion review.
- The roadmap must remain the source of implementation sequence.

## Transition Rules

### Planned to Approved
Preconditions:
- All dependency slices listed in the roadmap are Accepted or Closed.
- The slice is next in the roadmap sequence among non-terminal slices.
- Governing contracts needed by the slice exist and are not contradictory, incomplete, or ambiguous.
- A slice specification exists or is created as part of the approval action.

Required evidence:
- Architectural governance approval.
- Slice specification with scope, out-of-scope items, required contracts, dependencies, allowed files, forbidden files, acceptance criteria, and closure contract.
- Repository hygiene PASS.

### Approved to In Progress
Preconditions:
- Exactly one roadmap slice has status Approved.
- No slice has status In Progress.
- Required previous slices are Accepted or Closed.
- Implementation Readiness Gate PASS.
- Git working tree is clean.

Required evidence:
- Approved roadmap row.
- Existing slice specification.
- Required governing contracts read and validated.
- No contract contradiction, missing authority, incomplete scope, or ambiguous acceptance criteria.

### In Progress to Implementation Complete
Preconditions:
- The implementation changed only files allowed by the slice.
- The implementation satisfies the slice scope and does not include out-of-scope work.
- Required architecture, authority, capability, ERD, product, build, test, and repository hygiene gates applicable to the slice pass.

Required evidence:
- Closure report.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Repository hygiene PASS.
- Acceptance evidence mapped to each slice acceptance criterion.

### Implementation Complete to Accepted
Preconditions:
- Architectural review confirms all closure evidence.
- No required gate is missing, failed, ambiguous, or contradicted by repository state.
- Acceptance evidence matches the implemented scope.

Required evidence:
- Architectural governance acceptance.
- Slice status update to Accepted in the roadmap and slice specification.
- Acceptance Record in the slice specification.
- Repository hygiene PASS.

### Accepted to Closed
Preconditions:
- Architectural governance determines no further action remains for the slice.
- The slice has already been Accepted.
- Closing the slice does not alter implementation sequence or promote another slice.

Required evidence:
- Architectural governance closure decision.
- Repository hygiene PASS.

## Automatic Promotion
No slice is promoted automatically.

The next slice may move from Planned to Approved only by explicit architectural governance action under this contract.

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
- Slice promotion authority is explicit.
- Each allowed transition has preconditions.
- Each allowed transition has required evidence.
- Automatic next-slice promotion is forbidden.
- Repository state requirements are explicit.
- Forbidden transitions are explicit.
- Roadmap sequence remains unchanged.
- Existing slice statuses remain unchanged unless a valid promotion action is explicitly approved.

## Depends On
- project-governance.md
- implementation-contract.md
- implementation-roadmap.md

## Depended On By
- implementation-contract.md
- implementation-roadmap.md
- documentation/slices/*.md
