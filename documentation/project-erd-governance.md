# ERD Governance

## Authority
This document governs evolution of the logical data model.

## Purpose
Ensure ERD changes are intentional, traceable, and aligned with domain authority.

## Rules
- No entity without domain ownership.
- No relationship without business meaning.
- No duplicated data authority.
- No derived data stored unless explicitly justified.
- No persistence-specific optimization in the logical ERD.
- ERD changes require domain contract consistency.

## Depends On
- key-inventory-domain-contract.md

## Depended On By
- key-inventory-erd.md
- slices that change data model
