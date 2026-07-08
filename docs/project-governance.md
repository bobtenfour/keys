# KeyInventory Project Governance

## Methodology

KeyInventory is built slice by slice with contract-first delivery. A slice may begin only after its business contract, ERD impact, service ownership, authorization expectations, audit requirements, UX expectations, and tests are defined.

The product brief in `documentation/key-inventory-product-brief.md` is binding input. These architecture documents are the project control layer for implementation.

## Non-Negotiable Rules

- No code without a contract.
- No UI without the product experience contract.
- No database change without the master ERD.
- No workflow without business invariants.
- No mutation without an audit event.
- No page without authorization.
- No feature without tests.
- No duplicated authority.
- No module-local ERDs.
- No speculative architecture.
- No temporary patches.

## Gates

| Gate | Required Before Proceeding |
| --- | --- |
| Product gate | Product behavior exists in the brief or approved contract. |
| ERD gate | Entity, attributes, relationships, keys, constraints, and derived fields are defined in the master ERD. |
| Domain gate | Workflow states, legal transitions, illegal transitions, and invariants are defined. |
| Service gate | Mutation owner, query owner, and audit owner are assigned. |
| Security gate | Capability and role expectations are defined for every page and handler. |
| UX gate | Page purpose, shell placement, empty state, feedback, table/filter behavior, and accessibility baseline are defined. |
| Test gate | Required tests are listed before implementation starts. |

## Zero-Patch Policy

Temporary bypasses are forbidden. Work that cannot satisfy its contract must stop and update the contract before implementation continues. A patch is any change that knowingly leaves authority duplicated, audit incomplete, authorization partial, UI default-looking, or invariants unenforced.

## Acceptance Rules

A slice is accepted only when:

- Contracted behavior is implemented exactly.
- Domain invariants are covered by tests.
- Authorization is enforced server-side.
- UI visibility matches capabilities.
- Mutations emit audit events.
- Build and tests pass.
- No unrelated concerns are mixed into the slice.

## Completion Rules

One implementation slice equals one coherent commit. Stop after each slice for review. Do not begin the next slice until the current slice passes build, tests, architectural checks, and acceptance criteria.
