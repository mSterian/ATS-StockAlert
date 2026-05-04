# Changelog

All notable changes to this project will be documented in this file.

## [1.1.1] - 2026-05-04

### Changed
-possibly fixed Options Menu text display issue on Linux/Proton and improved overall window visibility and design
-added optional alert for idle builders that auto updates with the count of idle builders
-added optional alert+pause 3 seconds before season ends to check if you have a trade route you still want to start before it changes

## [1.1.0] - 2026-05-04

### Added
- Added optional queued worker assignments for building worker slots.
- You can now reserve a race for a slot even when no matching free villager is currently available.
- Queued workers only auto-fill when the slot later becomes empty and a matching free villager becomes available.
- Queued races appear as a second icon to the left of the normal worker slot icon, and can be changed or cleared from there.

### Changed
- Queued worker assignments are disabled by default.
- When multiple slots are waiting for the same race, the oldest queue is filled first.
- Queued worker assignments are session-only and do not persist across restarts.

## [1.0.9] - 2026-05-02

### Added
- Added an optional `F8` setting to auto-adjust the Purging Fire production limit in blight posts to `existing cysts + 1`.

### Changed
- The Purging Fire auto-adjust option is separate from the general consumer-based production limit automation and is off by default.

## [1.0.8] - 2026-05-01

### Added
- Integrated Builder Icon mod into my mod, because I wanted to make some changes to it. Credit to ~DGH! (off by default for those who still want to use the original mod separately)
- My version of the builder icons now shows the exclamation marker over idling builders, while the other builders show the hammer icon.

### Credits
- Builder status icon feature adapted from `BuilderIcon` source by `~DGH`.

## [1.0.7] - 2026-05-01

### Changed
- Changed the HUD pin to the bottom-right corner. It should no longer leave empty space below the HUD after the list shrinks, and instead moves the list down to stay anchored.
- A pin placement option may be added in a future version.

## [1.0.6] - 2026-05-01

### Changed
- Building shortage indicators now only consider recipes that are currently enabled in each building.

## [1.0.5] - 2026-04-30

### Added
- Added optional world-space staffing indicators for recipe buildings tied to current shortages.
- Added optional red indicators for buildings that can produce under-threshold goods but have no workers assigned.
- Added optional yellow indicators for buildings that can produce under-threshold goods and are partially staffed.

### Changed
- Unmanned recipe buildings without current shortage relevance now keep the normal white vanilla worker icon.
- Updated the README to explain the new optional building indicator behavior.

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
