# Changelog

## [0.1.4] - 2026-08-31

- Registered the package workflow and a bounded, sanitized local-state card with Deucarian Control Center.
- Removed normal `Tools/Deucarian` menu exposure while preserving the standalone open API.
- Updated the shared Editor dependency to 1.2.0.

## [0.1.3] - 2026-08-26

### Changed

- Derived the editor workflow footer from installed package metadata instead
  of a hardcoded package version.
- Updated the exact Editor dependency to 1.1.0.

## [0.1.2] - 2026-07-23

### Fixed

- Added optional Input System cursor-position capture and restoration so Editor and desktop releases return the pointer to its pre-capture location instead of leaving it centered.

## [0.1.1] - 2026-07-23

### Fixed

- Restored non-sticky WebGL cursor-lock synchronization so releasing pointer capture does not leave Unity's cursor state locked at the canvas center.

## [0.1.0] - 2026-07-22

### Added

- Cross-platform pointer capture controller with WebGL and desktop/editor bridges.
- Project, component, and runtime capture policies.
- Observable capture states, lifecycle reasons, diagnostics, and neutral-input rearming.
- Deucarian-themed management window with integrated configuration, validation, and fixes.
- EditMode tests, documentation, and a basic sample.
