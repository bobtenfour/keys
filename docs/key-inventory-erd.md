# KeyInventory Master ERD - Section KEY

This ERD is the structural authority for the KeyInventory foundational slice.

## Entities

### Key

Physical key record.

Attributes:

- `KeyId` primary key.
- `KeyNumber` required, unique.
- `Description` optional.
- `KeyCategoryId` foreign key to `KeyCategory`.
- `HomeKeyLocationId` foreign key to `KeyLocation`.
- `KeyAccessLevelId` foreign key to `KeyAccessLevel`.
- `KeyConditionId` foreign key to `KeyCondition`.
- `KeyStatus` required lifecycle value.
- `IsDamagedAssignable` required boolean, default false.
- `CreatedUtc` required UTC timestamp.
- `CreatedByUserId` required actor reference.
- `UpdatedUtc` optional UTC timestamp.
- `UpdatedByUserId` optional actor reference.

Constraints:

- `KeyNumber` is unique.
- `KeyStatus` must be one of `Available`, `Assigned`, `Returned`, `Lost`, `Damaged`, `Retired`.
- `Retired` keys cannot be assigned.
- `Lost` keys cannot be assigned.
- `Damaged` keys cannot be assigned unless `IsDamagedAssignable = true`.

Forbidden persisted fields:

- `CurrentHolderName`
- `CurrentHolderId`
- `CurrentAssignmentId`
- `IsCurrentlyAssigned`
- `AssignmentCount`
- `LastReturnedUtc`

Derived fields:

- Current holder derives from the active `KeyAssignment`.
- Current assignment derives from `KeyAssignment` where `ReturnedUtc` is null.
- Assignment count derives from assignment history.
- Last return derives from `KeyReturn`.

### KeyCategory

Classification of keys.

Attributes:

- `KeyCategoryId` primary key.
- `Code` required, unique.
- `Name` required.
- `Description` optional.
- `IsActive` required.

### KeyLocation

Home or storage location for a key when not assigned.

Attributes:

- `KeyLocationId` primary key.
- `Code` required, unique.
- `Name` required.
- `Building` optional.
- `Room` optional.
- `Cabinet` optional.
- `Hook` optional.
- `IsActive` required.

### KeyHolder

Person or role entity that may receive key assignments.

Attributes:

- `KeyHolderId` primary key.
- `HolderType` required: `Employee`, `Contractor`, `DepartmentDelegate`.
- `DisplayName` required.
- `ExternalReference` optional, unique when present.
- `DepartmentName` optional.
- `Email` optional.
- `Phone` optional.
- `IsActive` required.

Constraints:

- Inactive holders cannot receive new assignments.

### KeyAssignment

Active or historical key assignment.

Attributes:

- `KeyAssignmentId` primary key.
- `KeyId` foreign key to `Key`.
- `KeyHolderId` foreign key to `KeyHolder`.
- `AssignedUtc` required UTC timestamp.
- `AssignedByUserId` required actor reference.
- `AssignmentReason` required.
- `DueUtc` optional UTC timestamp.
- `ReturnedUtc` optional UTC timestamp, derived from accepted return processing.
- `ClosedByKeyReturnId` optional foreign key to `KeyReturn`.

Constraints:

- At most one active assignment per key where `ReturnedUtc` is null.
- `ReturnedUtc`, when present, must be greater than or equal to `AssignedUtc`.
- Assignment history is append-preserved and cannot be hard-deleted.

### KeyReturn

Return event closing an active assignment.

Attributes:

- `KeyReturnId` primary key.
- `KeyAssignmentId` foreign key to `KeyAssignment`, unique.
- `ReturnedUtc` required UTC timestamp.
- `ReturnedByUserId` required actor reference.
- `ReturnReason` required.
- `ReturnedConditionId` foreign key to `KeyCondition`.
- `ReturnLocationId` foreign key to `KeyLocation`.

Constraints:

- One assignment may have zero or one return.
- Return requires an active assignment.
- Return timestamp must be greater than or equal to assignment timestamp.

### KeyAuditEvent

Immutable audit log entry for every key mutation.

Attributes:

- `KeyAuditEventId` primary key.
- `KeyId` foreign key to `Key`.
- `KeyAssignmentId` optional foreign key to `KeyAssignment`.
- `KeyReturnId` optional foreign key to `KeyReturn`.
- `EventType` required.
- `ActorUserId` required.
- `OccurredUtc` required UTC timestamp.
- `Reason` required for state-changing events.
- `BeforeJson` optional.
- `AfterJson` optional.
- `CorrelationId` required.

Constraints:

- Append-only.
- No update after insert.
- No delete.
- Every mutation on `Key`, `KeyAssignment`, or `KeyReturn` creates at least one audit event.

### KeyCondition

Physical condition classification.

Attributes:

- `KeyConditionId` primary key.
- `Code` required, unique.
- `Name` required.
- `IsAssignableByDefault` required.
- `IsActive` required.

Required codes:

- `Good`
- `Worn`
- `Damaged`
- `Unusable`

### KeyAccessLevel

Security/access tier.

Attributes:

- `KeyAccessLevelId` primary key.
- `Code` required, unique.
- `Name` required.
- `SortOrder` required, unique.
- `IsActive` required.

Required codes:

- `Standard`
- `Restricted`
- `HighSecurity`

## Relationships

- `KeyCategory` 1:N `Key`
- `KeyLocation` 1:N `Key` as home location
- `KeyLocation` 1:N `KeyReturn` as return location
- `KeyAccessLevel` 1:N `Key`
- `KeyCondition` 1:N `Key`
- `KeyCondition` 1:N `KeyReturn`
- `Key` 1:N `KeyAssignment`
- `KeyHolder` 1:N `KeyAssignment`
- `KeyAssignment` 0:1 `KeyReturn`
- `Key` 1:N `KeyAuditEvent`
- `KeyAssignment` 0:N `KeyAuditEvent`
- `KeyReturn` 0:N `KeyAuditEvent`

## Invariants

- A key has one structural category, home location, access level, condition, and lifecycle status.
- Assignment and return history is never hard-deleted.
- Current holder is derived only from active assignment.
- Audit event history is immutable.
- Reports read from keys, assignments, returns, holders, and audit events; reports do not own business state.
