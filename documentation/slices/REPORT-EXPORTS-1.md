# REPORT-EXPORTS-1 - CSV, Excel, and PDF Export for Existing Reports

## Status
Accepted

## Parent Phase
Phase 2 — Operational Security

## Purpose
Give the key custodian of one approximately five-floor building CSV, Excel (.xlsx), and PDF downloads for every existing REPORTS-1 tabular report so filtered on-screen results can be taken into spreadsheet and printable document workflows without inventing REPORTS-2, a BI platform, or a second reporting store.

## Objective
Every existing REPORTS-1 tabular report exposes CSV, XLSX, and PDF downloads that represent the same Application-owned filtered result set as the on-screen report; export formatters consume report DTOs only and do not query SQL Server; CSV behavior remains intact; XLSX is a genuine workbook and PDF is a genuine readable document including valid zero-row exports; filters apply identically across screen and all three download formats.

## Scope
- Shared small export boundary for the seven existing REPORTS-1 tabular reports only:
  1. Current Key Holders
  2. Keys by Workforce Member
  3. Active Loans
  4. Overdue Keys
  5. Key History
  6. Outstanding Keys by Workforce Status
  7. Key Catalog
- Preserve existing Application report queries/DTOs as the sole result authority.
- Preserve existing CSV formatting behavior and parity with on-screen results.
- Add genuine XLSX workbook generation consuming the same authoritative result tables.
- Add genuine PDF document generation consuming the same authoritative result tables.
- Web download actions for CSV | Excel | PDF on each existing report page, preserving applied filters.
- Correct MIME types, file extensions, and operator-readable filenames.
- Valid exports for zero-row results (headers present; no fabricated data rows).
- Dependency decision: ClosedXML for XLSX and QuestPDF for PDF (Community license), selected as the minimum maintained .NET dependencies for these two concrete formats; one library per format.
- Architecture, formatter-boundary, parity, MIME/filename, zero-row, and regression tests required by this slice.
- Dependency injection registration for export ports/adapters.

## Persistence Requirements
- No new persistence model, reporting database, or export cache store.
- Export formatters must not query `KeyInventoryDbContext` or SQL Server.
- Existing report reads continue through the REPORTS-1 Application/Infrastructure read path only.

## UI Requirements
- Each of the seven existing report pages exposes clearly visible CSV, Excel, and PDF download actions.
- Applied filters are preserved on every export action.
- No unrelated UI redesign; reuse existing Reports visual language.

## Out of Scope
- REPORTS-2.
- New report families or new report pages beyond export of the seven existing reports.
- Reporting database, warehouse, or denormalized second source of truth.
- BI platform, dashboards, charts, KPI widgets.
- Report designer.
- Scheduled reporting or email delivery.
- Custom XLSX or PDF engine built without ClosedXML/QuestPDF.
- Multiple competing libraries for the same format.
- Generalized enterprise export framework beyond the small shared boundary for these seven reports.
- Web DbContext access.
- Export formatters that independently query persistence.
- Issue/receive mutation changes.
- Automatic audit emission.
- Unrelated UX redesign.
- Speculative frameworks, placeholders, TODO, FIXME, or commented-out code.
- Git operations unless explicitly requested by the human repository owner.
- Preparation or implementation of any other slice.

## Required Governing Documents
- implementation-contract.md
- implementation-roadmap.md
- project-governance.md
- roadmap.md
- product-vision.md
- key-inventory-capability-map.md
- architecture-contracts.md
- system-integrity-contract.md
- product-experience-contract.md
- testing-strategy.md
- slice-promotion-governance.md
- documentation/slices/REPORTS-1.md
- documentation/slices/ADMIN-MAINTENANCE-1.md

## Required Previous Slices
- ADMIN-MAINTENANCE-1

## Allowed Files
- documentation/slices/REPORT-EXPORTS-1.md
- documentation/implementation-roadmap.md
- documentation/architecture-contracts.md
- documentation/key-inventory-capability-map.md
- Directory.Packages.props
- src/KeyInventory.Application/**
- src/KeyInventory.Infrastructure/**
- src/KeyInventory.Web/**
- tests/KeyInventory.ArchitectureTests/**
- tests/**

## Forbidden Files
- documentation/** except the Allowed Files governing documents listed above
- Accepted slice history content rewrites (including REPORTS-1 Acceptance Record)
- REPORTS-2 slice files or new report-family feature folders
- Custom binary XLSX/PDF engines outside ClosedXML/QuestPDF adapters
- BI / warehouse / scheduled-delivery packages
- CI pipeline files
- Docker Compose files
- Testcontainers configuration
- SQLite provider packages or configuration
- EF Core InMemory provider packages or configuration

## Authority Owner
Application owns report queries and authoritative filtered DTO result sets; Infrastructure owns ClosedXML XLSX and QuestPDF PDF technical generation adapters that consume Application-provided export tables only; Web owns download presentation. No export owns business report authority independently.

## Dependency Decision
- XLSX: ClosedXML (central package version managed in Directory.Packages.props).
- PDF: QuestPDF with Community license configuration (central package version managed in Directory.Packages.props).
- CSV: existing Application `ReportCsvFormatter` (no new CSV library).
- Do not introduce EPPlus, PdfSharp, or a second library for either XLSX or PDF in this slice.

## Architectural Risks
- Letting exporters query SQL Server and duplicate REPORTS-1 authority.
- Diverging CSV/XLSX/PDF/screen result sets or filters.
- Building a generalized reporting framework beyond these seven reports.
- Introducing REPORTS-2 or BI platform scope.
- Putting DbContext access in Web or exporters.
- Shipping invalid zero-row workbooks/PDFs or incorrect MIME/filenames.

## Acceptance Criteria
- All seven existing REPORTS-1 tabular reports support CSV, XLSX, and PDF downloads.
- Screen, CSV, XLSX, and PDF use the same authoritative filtered Application result set.
- Export formatters do not query persistence.
- CSV behavior remains equivalent to pre-slice REPORTS-1 CSV for the same inputs.
- XLSX is a valid .xlsx workbook with title, headers, and matching rows (including zero-row header-only workbooks).
- PDF is a valid readable PDF with title, headings, readable rows, multi-page support with repeated headings where practical, and valid zero-row documents.
- Filters apply identically across representations.
- Web shows CSV | Excel | PDF actions with correct MIME types, extensions, and filenames.
- Web does not access DbContext.
- No REPORTS-2, reporting database, or BI platform is introduced.
- Existing issue/receive/lookup/reporting query behavior remains intact.
- Build PASS with zero warnings and zero errors.
- Tests PASS.
- Repository hygiene PASS.
- Human acceptance checkpoint is reached only after Implementation Complete evidence is recorded.

## Required Tests
- Tests covering seven reports × CSV, seven × XLSX, and seven × PDF (shared parameterized coverage acceptable when each report path is exercised).
- Screen/export result parity and filter parity tests.
- Valid XLSX structure tests (ZIP/OpenXML signature or ClosedXML open).
- Valid PDF structure tests (`%PDF` header / readable generation).
- MIME type, extension, and filename tests.
- Zero-row export tests for CSV/XLSX/PDF.
- CSV escaping regression.
- Architecture tests that formatters/ports do not take DbContext and Web report pages do not bypass Application.
- Regression that existing REPORTS-1 query behavior remains valid.

## Closure Contract
- Transversal Gate PASS
- Architecture consistency PASS
- Capability consistency PASS
- Product experience consistency PASS
- System integrity consistency PASS
- Product scope consistency PASS
- Testing strategy consistency PASS
- Build PASS
- Tests PASS
- Repository hygiene PASS
- Documentation updated only if required
- No REPORTS-2 or reporting database
- No exporter persistence queries
- CSV/XLSX/PDF/screen parity preserved
- Human acceptance checkpoint STOP after Implementation Complete

## Expected Build Result
PASS, 0 warnings, 0 errors.

## Expected Test Result
PASS.

## Human Acceptance Checkpoint
After REPORT-EXPORTS-1 reaches Implementation Complete with closure evidence, STOP for architectural governance ACCEPT before any later slice work.

## Preparation Record
- Decision: Prepare Next Slice, Planned to Approved.
- Date: 2026-08-09.
- Evidence: ADMIN-MAINTENANCE-1 is Accepted; human product governance explicitly requires CSV, XLSX, and PDF for all existing KeyInventory tabular reports; architecture-contracts and capability-map synchronized for export boundary without rewriting Accepted REPORTS-1 history; DentalInventory ClosedXML+QuestPDF pattern inspected read-only at C:\projects\Inv and reused structurally at a smaller scale for these seven reports; slice specifies Application-owned results, formatter non-query rule, dependency decision, UI, tests, and human acceptance checkpoint; implementation continues in the same continuous structural execution.
- Deciding authority role: Human Architectural Governance.

## Implementation Complete Record
- Decision: Implementation Complete.
- Date: 2026-08-09.
- Evidence: Shared Application export table + CSV/XLSX/PDF format paths for all seven REPORTS-1 reports; ClosedXML XLSX and QuestPDF PDF Infrastructure adapters consume Application-built tables only and do not query SQL Server; Web exposes CSV | Excel | PDF on each report with filter-preserving downloads, correct MIME/extensions/filenames; zero-row exports remain valid; REPORTS-1 CSV/query behavior preserved; architecture and workflow tests cover seven×CSV/XLSX/PDF, parity, MIME, zero-row, escaping, and no Web/formatter persistence bypass; build PASS 0 warnings 0 errors; tests PASS 153/153.
- Deciding authority role: Human Architectural Governance.

## Acceptance Record
- Decision: ACCEPT.
- Date: 2026-08-09.
- Evidence: REPORT-EXPORTS-1 was Implementation Complete; CSV, XLSX, and PDF downloads for all seven existing REPORTS-1 tabular reports from the same Application-owned filtered result set as the screen, with ClosedXML and QuestPDF adapters that do not query SQL Server, preserved CSV behavior, valid zero-row exports, filter-preserving Web CSV | Excel | PDF actions with correct MIME/extensions/filenames, and no REPORTS-2, reporting database, BI platform, or generalized export framework remained within approved scope; architectural review complete; build PASS; tests PASS.
- Deciding authority role: Human Architectural Governance.

## Next Allowed Slice
STOP
