# KeyInventory Architecture Index

This is the single navigation point for project contracts.

| Contract | File | Authority |
| --- | --- | --- |
| Project governance | `docs/project-governance.md` | Methodology, gates, acceptance, completion rules |
| Business authority matrix | `docs/business-authority-matrix.md` | Structural and service ownership |
| ERD governance | `docs/project-erd-governance.md` | ERD change control |
| Master ERD, section KEY | `docs/key-inventory-erd.md` | Entity model, relationships, constraints |
| Domain contract | `docs/key-inventory-domain-contract.md` | Lifecycle, transitions, workflow invariants |
| Capability map | `docs/key-inventory-capability-map.md` | Product capabilities and module boundaries |
| Implementation contracts | `docs/architecture-contracts.md` | Service boundaries and ownership rules |
| Security capability contract | `docs/security-capability-contract.md` | Roles, capabilities, page and handler authorization |
| Product experience contract | `docs/product-experience-contract.md` | Shell, UX behavior, Help Center, accessibility |
| Testing strategy | `docs/testing-strategy.md` | Required test categories and acceptance |
| Roadmap | `docs/roadmap.md` | Ordered implementation slices |
| System integrity contract | `docs/system-integrity-contract.md` | Future structural tests preventing drift |

## Binding Source Inputs

- `documentation/key-inventory-product-brief.md`
- `documentation/key-control-system-roadmap-v2.md` for enterprise traceability principles only where not conflicting with the KeyInventory brief.

## Current Slice Boundary

This package is documentation only. It does not authorize runtime code, UI pages, migrations, seed data, or scaffolded application behavior.
