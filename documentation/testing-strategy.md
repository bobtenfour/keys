# Testing Strategy

## Authority
This document governs testing expectations.

## Purpose
Ensure verification is proportional, deterministic, and aligned with slice scope.

## Rules
- Build warnings are defects.
- Tests must be deterministic.
- Architecture rules must be testable when implemented.
- A slice must not reduce verification quality.
- Tests must verify authority boundaries when the slice introduces or modifies them.
- Do not create fake tests that only assert implementation existence without meaningful contract verification.

## SQL Server Persistence Testing
- Persistence and workflow tests that require a database must use SQL Server only.
- Those tests must resolve the database through the canonical connection string name `KeyInventory` (`ConnectionStrings:KeyInventory`).
- SQLite, EF Core InMemory, Docker-managed databases, and Testcontainers are forbidden as persistence test strategies.
- If `ConnectionStrings:KeyInventory` is missing or does not target SQL Server, persistence and workflow database tests must fail; they must not fall back to another provider.
- Architecture and boundary tests that do not require a database remain provider-independent.

## Minimum Closure
Every implementation slice requires:
- Build PASS.
- Tests PASS.
- Zero new warnings.

## Depends On
- implementation-contract.md
- architecture-contracts.md

## Depended On By
- slices
