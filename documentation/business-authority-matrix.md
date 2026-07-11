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
| Business concepts and aggregate boundaries | key-inventory-domain-contract.md |
| Logical data model | key-inventory-erd.md |
| ERD evolution | project-erd-governance.md |
| Product capabilities | key-inventory-capability-map.md |
| Security capabilities | security-capability-contract.md |
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
