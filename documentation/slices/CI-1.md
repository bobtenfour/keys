# CI-1 - Continuous Integration Readiness

## Status
Accepted

## Parent Phase
Phase 1

## Purpose
Establish continuous integration readiness for the KeyInventory solution so every push and pull request verifies restore, build, and tests under the existing SQL Server persistence testing contract.

## Objective
The repository contains an authoritative CI pipeline definition that restores `KeyInventory.sln`, builds with zero warnings treated as errors, and runs the existing test suite using `ConnectionStrings:KeyInventory` against SQL Server only, without changing product business behavior or introducing a second persistence provider.

## Scope
- CI pipeline definition that triggers on push and pull request to the default integration branch set used by the repository.
- Pipeline steps to restore `KeyInventory.sln`.
- Pipeline steps to build `KeyInventory.sln` with warnings treated as errors.
- Pipeline steps to run all tests in the solution.
- Pipeline configuration that supplies `ConnectionStrings:KeyInventory` from CI secrets or variables to an authorized SQL Server instance.
- Minimal pipeline documentation comments required for operators to configure the connection string secret or variable.
- Verification that the pipeline does not introduce SQLite, EF Core InMemory, Docker-managed database test strategies, or Testcontainers.

## Out of Scope
- Product feature implementation.
- Changes to Domain business rules.
- Changes to Application use cases.
- Changes to Infrastructure persistence mappings or migrations.
- Changes to Web UI or product experience.
- Authentication or authorization runtime changes.
- Automatic audit emission.
- Custody, lifecycle, inventory, reporting, or dashboards.
- Workforce Eligibility implementation.
- Party aggregate implementation.
- Deployment, release, or hosting pipelines.
- Docker Compose or containerized application hosting.
- Docker-managed databases or Testcontainers as persistence test strategies.
- SQLite or EF Core InMemory providers.
- Local developer machine bootstrap beyond CI pipeline configuration.
- Seed data or demo data.
- PHASE-1-CLOSE work.
- Placeholders.
- TODO.
- FIXME.
- Commented-out code.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- roadmap.md
- architecture-contracts.md
- system-integrity-contract.md
- testing-strategy.md
- slice-promotion-governance.md

## Required Previous Slices
- LOAN-VERTICAL-1

## Allowed Files
- documentation/slices/CI-1.md
- .github/workflows/**

## Forbidden Files
- documentation/** except documentation/slices/CI-1.md
- src/**
- tests/**
- database/**
- migrations/**
- authentication files
- authorization runtime files
- UI product pages except as already present and untouched
- Docker Compose files
- Testcontainers configuration
- SQLite provider packages or configuration
- EF Core InMemory provider packages or configuration

## Authority Owner
testing-strategy.md

## Architectural Risks
- Introducing a second persistence provider to make CI green.
- Using Docker-managed databases or Testcontainers contrary to testing-strategy.md.
- Weakening warning or test gates to pass CI.
- Changing product code to accommodate CI instead of configuring the pipeline.
- Embedding secrets in pipeline files.
- Expanding into deployment or release automation beyond CI verification.
- Skipping persistence or workflow tests when `ConnectionStrings:KeyInventory` is absent instead of failing.

## Acceptance Criteria
- A CI pipeline definition exists under `.github/workflows/`.
- The pipeline restores `KeyInventory.sln`.
- The pipeline builds `KeyInventory.sln` with warnings treated as errors.
- The pipeline runs all solution tests.
- The pipeline supplies `ConnectionStrings:KeyInventory` from CI secrets or variables.
- Persistence and workflow database tests continue to require SQL Server through `ConnectionStrings:KeyInventory`.
- The pipeline does not introduce SQLite, EF Core InMemory, Docker-managed database test strategies, or Testcontainers.
- No `src/**` or `tests/**` product behavior changes are introduced by this slice.
- Build PASS with zero warnings and zero errors when the pipeline runs with a valid SQL Server connection string.
- Tests PASS when the pipeline runs with a valid SQL Server connection string.
- Human acceptance checkpoint is reached only after Implementation Complete evidence is recorded.

## Required Tests
- Existing architecture tests continue to PASS under CI.
- Existing unit and workflow tests continue to PASS under CI when `ConnectionStrings:KeyInventory` targets SQL Server.
- CI verification confirms the pipeline fails when the build emits warnings.
- CI verification confirms persistence and workflow database tests fail when `ConnectionStrings:KeyInventory` is missing or does not target SQL Server.
- CI verification confirms no SQLite, InMemory, Docker-managed database, or Testcontainers persistence strategy is introduced by the pipeline.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Authority consistency PASS
- System integrity consistency PASS
- Testing strategy consistency PASS
- Build PASS
- Tests PASS
- Repository hygiene PASS
- Documentation updated only if required
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After CI-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any PHASE-1-CLOSE preparation or later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-04.
- Evidence: LOAN-VERTICAL-1 is Accepted; CI-1 is the next Planned roadmap slice; Phase 1 strategic objective includes CI readiness; testing-strategy.md and implementation-contract.md define SQL Server-only persistence testing, warnings-as-defects, and mandatory build/test closure; slice specification defines scope, out-of-scope items, allowed files, forbidden files, acceptance criteria, required tests, dependencies, risks, and human acceptance checkpoint without implementation.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-04.
- Evidence: CI-1 was Implementation Complete; GitHub Actions workflow restores and builds KeyInventory.sln with warnings as errors, runs all tests, and fail-fast validates ConnectionStrings:KeyInventory from secret KEYINVENTORY_CONNECTION_STRING against SQL Server only; no SQLite, LocalDB, Docker, Testcontainers, or fallback providers were introduced; no src/** or tests/** product changes were introduced; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
