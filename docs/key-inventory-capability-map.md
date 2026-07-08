# KeyInventory Capability Map

## Catalog / Admin

Capabilities:

- Manage keys.
- Manage categories.
- Manage locations.
- Manage access levels.
- Manage conditions.
- Manage users and role assignments in a future security slice.

Primary roles:

- `SuperUser`
- `KeyAdministrator`

## Operations

Capabilities:

- Register key.
- Assign key.
- Return key.
- Transfer key location.
- Mark lost.
- Mark damaged.
- Mark usable.
- Retire key.
- Run inventory audit.

Primary roles:

- `SuperUser`
- `KeyAdministrator` for catalog-sensitive operations.
- `KeyOperator` for day-to-day operations.

## Reports

Capabilities:

- Current assigned keys.
- Overdue keys.
- Lost keys.
- Damaged keys.
- Key history.
- Holder history.
- Location inventory.

Primary roles:

- `SuperUser`
- `KeyAdministrator`
- `KeyOperator` where operationally appropriate.
- `KeyViewer` where read-only access is granted.

## Help

Capabilities:

- View Help Center.
- View contextual help.

Primary roles:

- All authenticated roles.

## Security

Capabilities:

- Manage users.
- Manage roles.
- Resolve capabilities.
- Enforce page and handler authorization.

Primary roles:

- `SuperUser`

## Audit

Capabilities:

- Create audit events for every mutation.
- View key history.
- View operational audit trails.

Primary roles:

- Audit creation is system-owned through mutation services.
- Audit viewing is granted by reporting or administrative capability.
