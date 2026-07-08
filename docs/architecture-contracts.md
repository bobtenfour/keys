# KeyInventory Implementation Contracts

## Service Boundaries

| Service | Owns | May Not Own |
| --- | --- | --- |
| `KeyCatalogWriteService` | Register, edit metadata, transfer location, retire | Assignment, return, direct audit storage bypass |
| `KeyCatalogReadService` | Key search, key detail, status summaries | Mutations |
| `KeyHolderWriteService` | Holder create/update/deactivate | Key assignment |
| `KeyHolderReadService` | Holder lookup and history input | Mutations |
| `KeyAssignmentWriteService` | Assign key and enforce assignment invariants | Key metadata edits |
| `KeyAssignmentReadService` | Active assignment lookup, holder assignment history | Mutations |
| `KeyReturnWriteService` | Return key and close active assignment | Assignment creation |
| `KeyAuditService` | Append audit events | Business decision making |
| `KeyAuditReadService` | Timeline and audit history | Mutations |
| `KeyReportReadService` | Reports and dashboards | Operational mutations |
| `SecurityCapabilityService` | Role/capability resolution | Domain mutation decisions |
| `HelpContentService` | In-app help content | Markdown downloads |

## Read / Write Split

Write services enforce invariants and own transactions. Read services shape query models for pages and reports. Pages call services; pages do not contain business logic.

## Mutation Ownership

Each mutation has one owner:

- Register key: `KeyCatalogWriteService`
- Assign key: `KeyAssignmentWriteService`
- Return key: `KeyReturnWriteService`
- Transfer location: `KeyCatalogWriteService`
- Mark lost: `KeyCatalogWriteService`
- Mark damaged: `KeyCatalogWriteService`
- Mark usable: `KeyCatalogWriteService`
- Retire key: `KeyCatalogWriteService`
- Inventory audit finding: future `KeyInventoryAuditWriteService`

## Query Ownership

Operational pages use read services. Reports use `KeyReportReadService`. Help pages use `HelpContentService`.

## Audit Ownership

Only mutation services call `KeyAuditService`. UI handlers never write audit rows directly.

## UI Rule

No business logic in Razor Pages. Page handlers validate input shape, authorize access, call application services, and render results.
