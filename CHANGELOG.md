# Changelog

All notable changes to this project will be documented in this file.

## [1.0.5] - 2026-04-30

### Added
- Added world-space staffing indicators for recipe buildings tied to current shortages.
- Added red indicators for buildings that can produce under-threshold goods but have no workers assigned.
- Added yellow indicators for buildings that can produce under-threshold goods and are partially staffed.

### Changed
- Unmanned recipe buildings without current shortage relevance now keep the normal white vanilla worker icon.
- Updated the README to explain the new building indicator behavior.

## [1.0.4] - 2026-04-30

### Added
- Added an `F8` option to automatically adjust global production limits from villager consumption counts.
- Added an editable auto-adjust multiplier field from `1.0` to `9.0`.

### Changed
- Auto-adjusted limits now round up to the next whole number.
- Auto-adjust only affects goods for recipes currently available in the settlement.
- Updated the README to document the new automation behavior and that it is off by default.

## [1.0.3] - 2026-04-29

### Changed
- Simplified the `F8` settings window to only show HUD controls.
- Updated the README to reflect the current `F8` behavior.
- Refreshed the Thunderstore package with the new screenshot-based icon.

## [1.0.2] - 2026-04-29

### Added
- Added `Show HUD` toggle in the `F8` settings window.
- Added `Movable HUD` toggle in the `F8` settings window.
- Added saved HUD position persistence for dragged HUD placement.

### Changed
- Stock thresholds now mirror the game's global production limits from the Recipes menu.
- HUD alerts now sort by newest triggered shortage first.
- HUD now switches to two columns when more than 15 goods are listed.
- HUD box now auto-sizes more tightly to its visible content.
- HUD now hides automatically outside active missions.

## [1.0.1] - 2026-04-28

### Changed
- Updated Thunderstore metadata to depend on ATS API 3.7.0.

## [1.0.0] - 2026-04-25

### Added
- Initial release of Stock Alert mod
- UI window with F8 toggle key
- Per-good threshold configuration
- Real-time alert window showing goods below threshold
- Persistent threshold storage via BepInEx config
- Robust reflection-based game integration
- Red alert styling for critical goods
- Comprehensive logging for debugging
- Draggable UI windows
