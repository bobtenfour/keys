# Security / Capability Contract

## Roles

| Role | Intent |
| --- | --- |
| `SuperUser` | Full system access including users and settings. |
| `KeyAdministrator` | Key master data and configuration. |
| `KeyOperator` | Day-to-day assignment, return, audit, and status operations. |
| `KeyViewer` | Read-only key search, history, and permitted reports. |

## Internal Capabilities

| Capability | Meaning |
| --- | --- |
| `CanManageKeys` | Register, edit, transfer, mark lost/damaged/usable, retire keys. |
| `CanAssignKeys` | Assign keys to holders. |
| `CanReturnKeys` | Process returns. |
| `CanAuditKeys` | Run inventory audits and record findings. |
| `CanViewReports` | Access report pages. |
| `CanManageUsers` | Manage users and role assignments. |
| `CanManageSettings` | Manage categories, locations, conditions, and access levels. |
| `CanViewKeyHistory` | View key timeline and audit history. |

## Role Mapping

| Role | Capabilities |
| --- | --- |
| `SuperUser` | All capabilities |
| `KeyAdministrator` | `CanManageKeys`, `CanManageSettings`, `CanViewReports`, `CanViewKeyHistory` |
| `KeyOperator` | `CanAssignKeys`, `CanReturnKeys`, `CanAuditKeys`, `CanViewKeyHistory` |
| `KeyViewer` | `CanViewReports`, `CanViewKeyHistory` |

## Page Authorization Expectations

- `/`: authenticated; content role-aware.
- `/Admin/*`: requires `CanManageSettings` or `CanManageUsers` as appropriate.
- `/Operations/Register`: requires `CanManageKeys`.
- `/Operations/Assign`: requires `CanAssignKeys`.
- `/Operations/Return`: requires `CanReturnKeys`.
- `/Operations/Audit`: requires `CanAuditKeys`.
- `/Reports/*`: requires `CanViewReports`.
- `/Help/*`: authenticated.

## Handler Authorization Expectations

Every POST handler requires the matching capability. Direct URL access must fail for unauthorized users.

## Provider Mapping

ASP.NET Core Identity cookie sessions are the preferred provider. Role and capability resolution must remain application-owned so future identity providers can map into the same internal capabilities.

## Forbidden Security Practices

- No hardcoded provider-specific business logic.
- No UI-only authorization.
- No Remember Me login UX.
- No operational navigation for anonymous users.
