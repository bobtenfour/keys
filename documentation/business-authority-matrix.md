# Business Authority Matrix

## Authority
This document assigns ownership for business and technical decision areas.

## Purpose
Prevent duplicated authority.

| Area | Owning Authority |
|---|---|
| Product vision | product-vision.md |
| Strategic roadmap | roadmap.md |
| Implementation sequencing | implementation-roadmap.md |
| Implementation process | implementation-contract.md |
| Project governance | project-governance.md |
| Architecture document inventory | project-architecture-index.md |
| Repository hygiene | project-governance.md |
| Technical boundaries | architecture-contracts.md |
| UTC business timestamp representation | architecture-contracts.md |
| Physical persistence mapping and EF migrations | architecture-contracts.md |
| Persistence provider | architecture-contracts.md |
| Runtime connection string name | architecture-contracts.md |
| SQL Server persistence testing | testing-strategy.md |
| Business concepts and aggregate boundaries | key-inventory-domain-contract.md |
| Key catalog identity | key-inventory-domain-contract.md |
| Key catalog classifications | key-inventory-domain-contract.md |
| KEY # / KeyAccessPattern access-pattern identity | key-inventory-domain-contract.md |
| KeyAccessPattern-to-Room current opening assignments | key-inventory-domain-contract.md |
| Physical key copy (KeyAsset) and MEDECO within KEY # | key-inventory-domain-contract.md |
| KeyAsset-to-Room current opening assignments | Superseded active authority; historical KEY-ROOM-ASSIGNMENT-1 only; KEY-ACCESS-COPY-1 moves Room openings to KeyAccessPattern |
| Location hierarchy | key-inventory-domain-contract.md |
| Lock as intermediate room-opening authority | Forbidden; KeyAccessPattern↔Room is sole operational room-opening authority in key-inventory-domain-contract.md |
| Master/sub-master key hierarchy | Forbidden; a master key is only a KEY # with multiple Rooms (KEY-ACCESS-COPY-1); no inheritance engine |
| KeySeries as KEY # or Room-access authority | Forbidden; KeySeries is non-operational classification seed only |
| Loan issuance workflow | key-inventory-domain-contract.md |
| Return completion workflow | key-inventory-domain-contract.md |
| LOAN-VERTICAL-1 runnable workflow orchestration | architecture-contracts.md |
| Logical data model | key-inventory-erd.md |
| ERD evolution | project-erd-governance.md |
| Product capabilities | key-inventory-capability-map.md |
| Security capabilities | security-capability-contract.md |
| Technical principal identity | security-capability-contract.md |
| Authentication | security-capability-contract.md |
| Authorization | security-capability-contract.md |
| Roles | security-capability-contract.md |
| Permissions | security-capability-contract.md |
| Role assignments | security-capability-contract.md |
| Party persistent person identity | key-inventory-domain-contract.md |
| Person FirstName LastName and UIN | key-inventory-domain-contract.md |
| Building place authority | Removed from active model; key-inventory-domain-contract.md |
| Room place authority and global RoomNumber uniqueness | key-inventory-domain-contract.md |
| Workforce eligibility boundary | key-inventory-domain-contract.md |
| Organization | Removed from active model; key-inventory-domain-contract.md |
| Department membership for workforce (DepartmentId identity; global unique editable DepartmentCode) | key-inventory-domain-contract.md |
| Logical entity identity vs business identifier normalization | key-inventory-erd.md |
| WorkforceMember workforce relationship and eligibility | key-inventory-domain-contract.md |
| WorkforceType Employee and Contractor | key-inventory-domain-contract.md |
| ResponsibleManager relationship | Removed from active model; key-inventory-domain-contract.md |
| WorkAssignment to Room | key-inventory-domain-contract.md |
| Key issue eligibility for WorkforceMember | key-inventory-domain-contract.md |
| Single-site structural simplification / Application readiness query | documentation/slices/OPERATOR-EXPERIENCE-1.md |
| First-use/onboarding presentation (no permanent Home setup UI) | product-experience-contract.md |
| Human-readable operator date/time presentation | product-experience-contract.md |
| Post-create form lifecycle | product-experience-contract.md |
| Operator User Guide | documentation/slices/OPERATOR-EXPERIENCE-1.md |
| WorkforceMember termination return obligation | key-inventory-domain-contract.md |
| Employment as separate aggregate | Forbidden; authority belongs to WorkforceMember in key-inventory-domain-contract.md |
| Borrower workflow role definition | key-inventory-domain-contract.md |
| Policy evaluation | security-capability-contract.md |
| Audit evidence | key-inventory-domain-contract.md |
| Audit Event aggregate | key-inventory-domain-contract.md |
| Immutable audit history | key-inventory-domain-contract.md |
| Product experience | product-experience-contract.md |
| Testing requirements | testing-strategy.md |
| Cross-system integrity | system-integrity-contract.md |

## Rule
Every new business decision must be assigned to exactly one authority before implementation.

## Depends On
- architecture-contracts.md
- key-inventory-domain-contract.md

## Depended On By
- implementation-contract.md
- slices
