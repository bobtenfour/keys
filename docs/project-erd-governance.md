# Project ERD Governance

## Master ERD Rule

KeyInventory has one master ERD. The KEY section is defined in `docs/key-inventory-erd.md`. Module-local ERDs are forbidden.

## Entity Addition Gate

A new entity may be added only when all of the following are defined:

- Business purpose.
- Attributes.
- Primary key.
- Foreign keys.
- Unique constraints.
- Check constraints.
- Cardinalities.
- Ownership in the business authority matrix.
- Mutation owner.
- Audit requirements.
- Tests that enforce its invariants.

## Migration Gate

A migration may be created only after:

- The master ERD is updated.
- The domain contract is updated if workflow behavior changes.
- The business authority matrix is updated if ownership changes.
- The security contract is updated if pages or handlers are affected.
- Migration tests are defined.

## Forbidden ERD Practices

- No duplicated state authority.
- No mutable current-holder field on `Key`.
- No mutable current-assignment field on `Key`.
- No module-specific copy of status, holder, location, or condition rules.
- No denormalized report fields as transactional truth.
- No hard-delete of assignment or audit history.

## Approved Placeholder Policy

Placeholder migrations are not authorized in this slice. Future placeholders may exist only for empty migration baselines and must not create product schema.
