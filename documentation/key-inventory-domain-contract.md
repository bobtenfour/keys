# Key Inventory Domain Contract

## Authority
This document is the sole authority for business concepts and aggregate boundaries.

## Purpose
Define the business model without implementation details.

## Core Business Concepts
- Key: controlled physical key asset.
- Key Catalog: authoritative list of controlled keys.
- Location: physical organizational place where a key, lock, or custody action is relevant.
- Party: person or organization participating in custody or authorization.
- Loan: controlled issuance of a key to a party.
- Return: controlled completion of a loaned key back into organizational control.
- Custody Event: immutable record of possession transfer or custody-relevant change.
- Audit Event: immutable evidence of a business or security-relevant action.
- Lifecycle State: valid derived state of a key, including Available, Reserved, Issued, InTransit, ReturnedPendingInspection, Lost, Maintenance, Disabled, and Destroyed.
- Lifecycle Event: immutable event that supports key lifecycle derivation, including KeyCreated, KeyUpdated, Issued, Returned, CustodyTransferred, Lost, Recovered, and Destroyed.

## Domain Invariants
- A key must have one authoritative catalog identity.
- Possession must be traceable.
- Loan and return must not create orphan custody.
- Custody transfers must support Party and storage endpoints, including employees, contractors, security personnel, storage locations, and other authorized Party types.
- Audit history must not be rewritten.
- Current state must be derivable from authoritative records when the relevant phase introduces that model.

## Forbidden
This document must not define:
- Database schema.
- EF mappings.
- UI behavior.
- Controller routes.
- Service registrations.
- Package choices.

## Depends On
- product-vision.md

## Depended On By
- key-inventory-erd.md
- key-inventory-capability-map.md
- business-authority-matrix.md
- slices
