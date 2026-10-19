# Changelog

All notable changes to this project are documented here.

## [1.4.3] - 2026-10-19

- Replaced the custom plugin artwork with the original Pobuca icon.
- Added public NuGet packaging and automated GitHub build/release workflows.

## [1.4.2] - 2026-10-18

- Replaced enable/disable plus and minus icons with compact toggle-switch icons.

## [1.4.1] - 2026-10-18

- Applied the Pobuca palette to the table and column section headers.

## [1.4.0] - 2026-10-17

- Added date-bounded field audit-activity drill-downs.
- Added guarded, asynchronous audit-history cleanup by table.
- Added typed confirmation, an optional cutoff date, and background-job reporting for destructive cleanup.

## [1.3.1] - 2026-10-17

- Added the read-only per-table audit-storage dashboard using `GetAuditStorageDetails`.
- Added polling support for `Pending`, `InProgress`, and `Running` calculation states.

## [1.3.0] - 2026-10-17

- Added batched apply and publish operations with progress reporting.
- Added post-publish verification of effective audit settings.
- Added modern virtual-field detection and sortable grids.

## [1.2.0] - 2026-10-16

- Added lazy field metadata loading and responsive grids.
- Added bulk table and column actions and the compact pending-changes review.

## [1.0.0] - 2026-10-16

- Initial Dataverse Audit Manager implementation for XrmToolBox.
