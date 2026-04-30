# ATS-StockAlert

`ATS-StockAlert` is a BepInEx mod for **Against the Storm** that warns you when your stock of a good drops below its in-game global production limit.

## What it does

- watches goods that have a production limit set in the Recipes menu
- uses that same production limit as the alert threshold
- can optionally auto-adjust eligible global production limits from villager consumption counts
- shows low-stock goods in a compact HUD list in the bottom-right corner
- puts newly triggered shortages at the top of the list
- lets you press `F8` to toggle hide and move ability for the HUD

If a good has no production limit, it is ignored by the mod.

## Usage

1. Set a global production limit for a good in the Recipes menu.
2. When your current stock drops below that limit, the good appears in the Stock Alert HUD.
3. Press `F8` to edit the hide and movable HUD settings.
4. Optional: enable auto-adjust in the `F8` window to make the mod set eligible global production limits for you.

## Auto-adjust option

- off by default
- when enabled, the mod checks how many alive villagers can consume a good and sets that good's global production limit to `consumers x multiplier`
- the multiplier is editable in the `F8` window from `1.0` to `9.0`
- values round up to the next whole number
- only goods with live recipes in your current settlement are adjusted
- disabling the option stops further automatic changes, but does not restore previous manual limits

## Requirements

- Against the Storm
- BepInEx 5
- ATS API 3.7.0 or newer

## Build

Update `StormPath` in [StockAlert.csproj](E:/ATS_mod/StockAlert.csproj) to match your game install, then build the project.
