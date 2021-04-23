## How It Works
When entering the chat command the named player or if no name given yourself is granted an item named as Taser. This item can normally be dropped and will function as Taser for everyone else. This is the main difference to https://umod.org/plugins/taser, where it depends on permission and ammo type rather then a chat command.
The taser has one charge. After using that charge the taser needs a specific amount of time to recharge. If a player in range is hit by the taser he immediatly gets into the wounded state and will get back up again after a specific amount of time or when another player helps him in the normal way. Unless the taser damage is changed to something > 0, the taser is unable to kill a player. 

## Chat Commands
### GiveTaser

```
/givetaser [optional:playername]
Example: /givetaser ZockiRR
```

#### Syntax - Options
 - **playername** - The player who will get the taser (default: yourself)

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
