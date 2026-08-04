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
| Location hierarchy | key-inventory-domain-contract.md |
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
| Party identity | key-inventory-domain-contract.md |
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
