# ATS-StockAlert

`ATS-StockAlert` is a BepInEx mod for **Against the Storm** that warns you when your stock of a good drops below its in-game global production limit.

## What it does

Displays goods that have a production limit set in the Recipes menu, and shows their current/max stock in a compact, movable, hideable HUD in the bottom-right corner.

It can also optionally auto-adjust production limits for all goods you currently have a recipe for, based on the number of people consuming them and an adjustable multiplier.

It can also optionally highlight recipe buildings in the world when they can produce goods that are currently under threshold:
- red when the building can help, but has no workers assigned
- yellow when the building can help, has at least one worker, but is not fully staffed
- normal white vanilla icon when the building is unmanned but none of its recipes are currently under threshold

Newly triggered shortages go at the top of the list.

Options for movable, hideable, building indicators, and production limit multiplier are available in the `F8` settings window.

If a good has no production limit, it is ignored by the mod.

## Usage

1. Set a global production limit for a good in the Recipes menu.
2. When your current stock drops below that limit, the good appears in the Stock Alert HUD.
3. Press `F8` to edit the hide and movable HUD settings.
4. Optional: enable auto-adjust in the `F8` window to make the mod set eligible global production limits for you.
5. Optional: enable building shortage indicators in the `F8` window to spot where extra staffing would help with current shortages.

## Auto-adjust option

- off by default
- when enabled, the mod checks how many alive villagers can consume a good and sets that good's global production limit to `consumers x multiplier`
- the multiplier is editable in the `F8` window from `1.0` to `9.0`
- values round up to the next whole number
- only goods for recipes you currently have available in the settlement are adjusted
- disabling the option stops further automatic changes, but does not restore previous manual limits

## Requirements

- Against the Storm
- BepInEx 5
- ATS API 3.7.0 or newer

## Build

Update `StormPath` in [StockAlert.csproj](E:/ATS_mod/StockAlert.csproj) to match your game install, then build the project.
