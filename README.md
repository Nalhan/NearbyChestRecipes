# NearbyChestRecipes

Discovers recipes, building pieces, and materials from items in nearby chests without having to manually take them into your inventory.

Designed to pair well with mods like AzuCraftyBoxes or ValheimPlus crafting from containers.

## How it works
- Periodically checks containers within range (default `20m`, every `2s`) and unlocks any unknown items.
- Also triggers immediately whenever you interact with or open a chest.
- Respects ward permissions and private chests by default.
- Client-only — no server installation needed.

## Installation
Drop `NearbyChestRecipes.dll` into your `BepInEx/plugins/` folder, or install via your mod manager.

## Configuration
Generates at `BepInEx/config/com.pergola.nearbychestrecipes.cfg`:

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Toggle mod on/off |
| `Range` | `20` | Detection radius in meters |
| `Interval` | `2` | Seconds between proximity scans |
| `RequireAccess` | `true` | Skip chests locked by wards or private access |
| `LogDiscoveries` | `false` | Log newly unlocked items to BepInEx console |
