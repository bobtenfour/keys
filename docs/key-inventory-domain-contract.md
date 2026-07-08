# KeyInventory Domain Contract

## Key Lifecycle States

- `Available`: key is in inventory and may be assigned.
- `Assigned`: key is assigned to one holder.
- `Returned`: key has been returned and is being processed back into inventory.
- `Lost`: key is lost and cannot be assigned.
- `Damaged`: key is damaged; assignment is blocked unless explicitly marked assignable.
- `Retired`: key is permanently removed from circulation.

## Legal Transitions

| From | To | Workflow | Audit Required |
| --- | --- | --- | --- |
| none | Available | Register key | `KeyRegistered` |
| Available | Assigned | Assign key | `KeyAssigned` |
| Assigned | Returned | Return key | `KeyReturned` |
| Returned | Available | Complete return processing | `KeyMadeAvailable` |
| Available | Lost | Mark lost | `KeyMarkedLost` |
| Assigned | Lost | Mark lost while assigned | `KeyMarkedLost` |
| Available | Damaged | Mark damaged | `KeyMarkedDamaged` |
| Returned | Damaged | Return damaged | `KeyMarkedDamaged` |
| Damaged | Available | Mark usable after review | `KeyMarkedUsable` |
| Available | Retired | Retire key | `KeyRetired` |
| Damaged | Retired | Retire damaged key | `KeyRetired` |
| Lost | Retired | Retire lost key | `KeyRetired` |

## Illegal Transitions

- `Retired` to any active state.
- `Lost` to `Assigned`.
- `Damaged` to `Assigned` unless `IsDamagedAssignable = true` and the assignment service records the justification.
- `Available` to `Returned` without an active assignment.
- `Assigned` to `Assigned` for another holder.
- Any transition without audit.

## Assignment Invariants

- A key cannot be assigned to two holders at once.
- Assignment requires an authenticated actor.
- Assignment requires holder, UTC timestamp, and reason.
- Assignment requires key status to be assignable.
- Assignment requires active holder.
- Assignment creates immutable assignment history and audit.

## Return Invariants

- Return requires one active assignment.
- Return closes that assignment.
- Return records actor, UTC timestamp, reason, returned condition, and return location.
- Return creates `KeyReturn` and `KeyAuditEvent`.
- Return cannot be applied twice to the same assignment.

## Lost, Damaged, and Retired Rules

- Lost keys cannot be assigned.
- Retired keys cannot be assigned or reactivated.
- Damaged keys cannot be assigned unless explicitly marked usable by an authorized operation.
- Marking lost, damaged, usable, or retired requires audit.

## Audit Rules

- Every mutation records who acted, what changed, when it occurred in UTC, and why.
- Audit events are append-only.
- Audit history is never hard-deleted.
- UI success feedback occurs only after the mutation and audit event are committed.
