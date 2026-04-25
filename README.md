# ATS-StockAlert

A BepInEx mod for **Against the Storm** that monitors your settlement's goods stock and displays alerts when any configured good falls below a specified threshold.

## Features

- 🎯 **Set custom thresholds** for any in-game good (Wood, Food, Herbs, etc.)
- 🚨 **Real-time alerts** - A warning window displays goods that fall below their thresholds
- 💾 **Persistent configuration** - Your thresholds are saved automatically
- 🎮 **Easy UI** - Toggle settings with F8, manage all goods in one window
- 🔧 **Robust reflection-based** integration that works across game updates

## Installation

1. Download the latest release from [Thunderstore](https://thunderstore.io/c/against-the-storm/p/mSterian/StockAlert/) (coming soon)
2. Extract the ZIP into your BepInEx/plugins folder
3. Launch the game and press **F8** to open the Stock Alert settings

## Usage

### Quick Start
1. Launch a settlement in Against the Storm
2. Press **F8** to open the Stock Alert settings panel
3. Find any good you want to monitor
4. Enter a threshold number (e.g., 10 means alert when stock drops below 10)
5. Click **Set** to save
6. When that good falls below your threshold, you'll see a red warning box in the top-right corner

### Setting Thresholds
- Type a **positive number** to enable monitoring (e.g., 5, 20, 100)
- Type **0** to disable monitoring for that good
- Click **Set** to apply
- Your settings persist across game sessions

### Toggle Key
Default toggle key is **F8**. You can change it in:
- `BepInEx/config/com.stockalert.ats.cfg` → `ToggleSettingsKey`

## Configuration

All configuration is stored in: `BepInEx/config/com.stockalert.ats.cfg`

- **ToggleSettingsKey**: The key to open/close settings (default: F8)
- **Thresholds**: Your saved threshold data (auto-managed via UI)

## Requirements

- Against the Storm
- BepInEx 5.4+
- Thunderstore Mod Manager (recommended)

## Known Issues

None at this time. If you encounter any issues, please report them on GitHub.

## Future Enhancements

- [ ] Sound alerts
- [ ] Custom colors for different alert types
- [ ] Threshold profiles for different settlement types
- [ ] Integration with other popular mods

## Development

This mod is developed using:
- **C# 11**
- **.NET Framework 4.7.2**
- **BepInEx** for mod loading
- **Reflection** for robust game integration

### Building

See `StockAlert.csproj` for build configuration. Ensure your paths match your installation:
- `StormPath`: Path to Against the Storm installation
- `BepInExPath`: Path to BepInEx folder

## License

MIT License - Feel free to use and modify!

## Credits

- Built for **Against the Storm** by Eremite Games
- Uses **BepInEx** for modding
