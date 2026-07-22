# Changelog

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
