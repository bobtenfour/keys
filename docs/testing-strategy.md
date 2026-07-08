# KeyInventory Testing Strategy

## Domain Invariant Tests

Required:

- Cannot double-assign a key.
- Cannot assign lost key.
- Cannot assign retired key.
- Cannot assign damaged key unless explicitly assignable.
- Cannot return without active assignment.
- Cannot return same assignment twice.
- Retired key cannot re-enter circulation.

## Service Boundary Tests

Required:

- UI handlers call services rather than DbContext directly.
- Assignment service owns assignment mutation.
- Return service owns return mutation.
- Catalog service owns key metadata and status mutations.
- Audit service is called by every mutation service.

## Authorization Tests

Required:

- Every page requires authentication except login.
- Admin pages require admin capabilities.
- Operations POST handlers require matching capabilities.
- Reports require report capability.
- Direct unauthorized access fails.

## UI Visibility Tests

Required:

- Operator cannot see Admin navigation.
- Viewer cannot see mutation actions.
- Administrator sees configuration navigation.
- Home quick actions match capabilities.

## Audit Tests

Required:

- Register creates audit event.
- Assign creates audit event.
- Return creates audit event.
- Mark lost/damaged/usable/retired creates audit event.
- Assignment and audit history are not deleted by normal workflows.

## Migration Tests

Future migration tests must verify:

- Unique active assignment enforcement.
- Required indexes for key number, status, active assignment holder, and location.
- Audit table append-only protections where supported.

No migration is authorized in the architecture package slice.

## System Integrity Tests

Future structural tests must enforce `docs/system-integrity-contract.md`.
