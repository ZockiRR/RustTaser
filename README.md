# RustTaser
Plugin for Rust servers that allows players with permissions to spawn a taser.

## Chat Commands
### Give Taser

```
/givetaser [optional:playername]
Example: /givetaser ZockiRR
```

#### Syntax - Options
 - **playername** - The player which will get the taser (default: yourself)

## Permissions
 - taser.givetaser

## Configuration
```
{
  // Time in seconds which the taser needs to charge for the next use
  "TaserCooldown": 5.0,
  // Distance in which the taser is effective (downs a player)
  "TaserDistance": 8.0,
  // The duration in seconds which the player is wounded when hit, before he stands up again
  "TaserShockDuration": 20.0,
  // The electrical damage which the taser deals on hit (before armor reduction)
  "TaserDamage": 0.0,
  // These are the used assets from the game and should not be changed unless not working anymore
  "ItemNailgun": "pistol.nailgun",
  "PrefabScream": "assets/bundled/prefabs/fx/player/gutshot_scream.prefab",
  "PrefabShock": "assets/prefabs/locks/keypad/effects/lock.code.shock.prefab"
}
```
