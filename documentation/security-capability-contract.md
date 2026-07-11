# Security Capability Contract

## Authority
This document governs security capability boundaries.

## Purpose
Separate authentication, authorization, policy, audit, and digital trust responsibilities.

## Security Areas
- Authentication: prove identity.
- Authorization: determine allowed action.
- Policy: configurable decision authority for advanced rules.
- Audit: immutable evidence of relevant actions.
- Digital Trust: integrity, acceptance, and non-repudiation concepts.

## Capability Examples
- Policy must be able to compose critical-risk and outside-business-hours conditions.
- Authorization must be able to require supervisor and security-officer approval when policy requires it.
- Digital Trust may use integrity mechanisms such as SHA-256 and hash chaining.
- Digital Trust may use acceptance methods such as electronic signature, PIN, NFC, smart card, and biometrics.

## Rules
- Authentication is not authorization.
- Authorization is not audit.
- Audit is not authentication.
- Integrity proof is not user authentication.
- Security decisions require explicit authority before implementation.

## Depends On
- product-vision.md
- architecture-contracts.md

## Depended On By
- identity slices
- authorization slices
- policy slices
- audit slices
