# NearbyChestRecipes

A lightweight Valheim BepInEx mod that automatically unlocks recipes, building pieces, and material discoveries from items stored in nearby chests and containers.

## Features
- **Proximity Discovery:** Automatically scans containers within a configurable range (default `20m`, matching AzuCraftyBoxes) every `2.0s`.
- **Instant Discovery on Open:** When you open or interact with any container, discoveries are immediately processed.
- **Access Control:** Respects private chests and ward protections so unauthorized containers aren't scanned.
- **Safe & Optimized:** Tracks containers via lightweight Harmony hooks instead of heavy scene queries; safely ignores loading, teleporting, or dead states.
- **Full Vanilla Integration:** Calls Valheim's internal `player.AddKnownItem(item)`, meaning standard unlock banners (*"Discovered: ..."*, *"Unlocked recipe: ..."*) and build hammer pieces trigger normally.

## Configuration
The config file is automatically generated at `BepInEx/config/com.pergola.nearbychestrecipes.cfg` after first run.

```ini
[1 - General]
## Enable or disable discovering recipes/materials from nearby containers.
# Setting type: Boolean
# Default value: true
Enabled = true

[2 - Proximity]
## Proximity radius in meters to scan for containers (matches AzuCraftyBoxes default of 20m).
# Setting type: Single
# Default value: 20
Range = 20

## Interval in seconds between proximity scans.
# Setting type: Single
# Default value: 2
Interval = 2

[3 - Access]
## If true, respects ward protection and private chests (must have permission to open).
# Setting type: Boolean
# Default value: true
RequireAccess = true

[4 - Debug]
## If true, outputs discovered materials and recipes to the BepInEx console log.
# Setting type: Boolean
# Default value: false
LogDiscoveries = false
```

## Installation
- **Client-only:** Copy `NearbyChestRecipes.dll` into your Valheim client's `BepInEx/plugins/` folder (or install via Thunderstore / r2modman).
- **Dedicated Server:** **Not required.** Discovery and recipes are saved in the player's local character profile, so dedicated servers do not need this mod installed.
