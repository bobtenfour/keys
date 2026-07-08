# Business Authority Matrix

Each domain concept has exactly one structural authority and one mutation authority. Query services may read across concepts but must not own mutations outside their area.

| Area | Structural Authority | Mutation Owner | Query Owner | Audit Owner |
| --- | --- | --- | --- | --- |
| Key | Master ERD `Key` | `KeyCatalogWriteService` | `KeyCatalogReadService` | `KeyAuditService` |
| Holder | Master ERD `KeyHolder` | `KeyHolderWriteService` | `KeyHolderReadService` | `KeyAuditService` |
| Assignment | Master ERD `KeyAssignment` | `KeyAssignmentWriteService` | `KeyAssignmentReadService` | `KeyAuditService` |
| Return | Master ERD `KeyReturn` | `KeyReturnWriteService` | `KeyAssignmentReadService` | `KeyAuditService` |
| Audit | Master ERD `KeyAuditEvent` | `KeyAuditService` | `KeyAuditReadService` | `KeyAuditService` |
| Reports | Read models derived from master entities | No mutation owner except saved report settings in future Admin slice | `KeyReportReadService` | Report access audit in future slice |
| Admin | Reference entities: `KeyCategory`, `KeyLocation`, `KeyCondition`, `KeyAccessLevel` | `KeyAdminWriteService` | `KeyAdminReadService` | `KeyAuditService` |
| Help | In-app help content contract | `HelpContentService` | `HelpContentService` | No domain audit unless content becomes editable |
| Security | Roles and capabilities | `SecurityAdministrationService` | `SecurityCapabilityService` | Security audit service in future auth slice |

## Authority Rules

- UI handlers never mutate entities directly.
- DbContext access from pages is forbidden.
- One mutation service owns each state-changing operation.
- Reports are read-only and cannot become a second source of operational truth.
- Audit event creation is centralized through `KeyAuditService`.
- Derived values may be projected or calculated, but their source authority remains the master entity and audit history.
