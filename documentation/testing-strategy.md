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
