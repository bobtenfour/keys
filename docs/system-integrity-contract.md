# KeyInventory System Integrity Contract

This contract defines future structural tests that prevent architectural drift.

## Required Structural Tests

### No DbContext in Pages

Razor Pages must not inject or reference DbContext directly. Pages call application services.

### No Mutation Outside Services

Mutations must occur only in approved write services. Read services and UI handlers cannot call `SaveChanges`.

### No Pages Without Authorization

Every page and POST handler must have an explicit authorization expectation. Anonymous access is limited to authentication surfaces.

### No Workflow Without Audit

Every mutation workflow must call `KeyAuditService` in the same transaction boundary as the mutation.

### No Duplicate Calculators

Derived values such as current holder, active assignment, status summaries, and overdue status must have one approved calculator/query owner.

### No Orphan Routes

All routable pages must appear in the product navigation contract, contextual workflow contract, or Help contract.

### No Raw Markdown Help Downloads

Help content may originate from controlled content files, but users must receive rendered in-app HTML, not raw `.md` downloads.

### No Default Scaffold UI

Framework-default Identity, Bootstrap, or Razor scaffolding must not leak into the product experience without design-system treatment.

## Failure Policy

Any failing integrity test blocks slice completion. The correction must remove the drift, not suppress the test.
