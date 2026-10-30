# Rust Lebanon x10

**EU modded Rust server · 100 slots · weekly wipe every Thursday 20:00 CET (21:00 Lebanon time)**

```
client.connect 2.59.169.7:28015
```

In game, open the **Modded** tab and search for **Lebanon**, or press **F1** and paste the line above.

## What's on the server

- x10 gather, crafting capped at 3 seconds, fast smelting and recycling
- Barrels and crates hold useful loot with 3x scrap, and stacks are x20
- Free kits with `/kit` (starter kit every 20 minutes)
- Teleport: `/sethome`, `/home`, `/tpr` (accept with `/tpa`), `/outpost`, `/bandit`
- `/backpack`, `/remove`, `/grade hqm` auto-upgrade, furnace splitter, quick sort
- No NPCs or animals, so it's pure PvP
- A storm rolls in every hour
- Monument lights stay on and puzzles need no fuses
- Active admin

## Wipe schedule

| | |
|---|---|
| Map wipe | Every Thursday, 20:00 CET / 21:00 Lebanon time |
| Blueprints | Kept between map wipes |
| Facepunch forced wipe | First Thursday of the month (same time) |

The server name always shows the last wipe date, for example `Wiped 30/10`.

## Rules

1. No cheating, scripting or exploiting.
2. No racism, hate speech or harassment.
3. Respect the admin's decisions. Report problems in game.

## Plugins in this repo

These are the gameplay plugins written for this server (Oxide / uMod, C#). They can be used on any Rust server: drop the `.cs` file into `oxide/plugins`.

| Plugin | What it does |
|---|---|
| [FastCrafting](plugins/FastCrafting.cs) | Caps crafting time at 3 seconds for every item |
| [FastSmelt](plugins/FastSmelt.cs) | Shortens ore cook times using Rust's own furnace logic, so it adds no load |
| [FastRecycler](plugins/FastRecycler.cs) | Recyclers process an item every second instead of every 5 |
| [LootBoost](plugins/LootBoost.cs) | Removes junk from barrels and crates, triples scrap and adds useful loot |
| [StackSizes](plugins/StackSizes.cs) | Resources stack to 5000, weapons to 1, everything else that stacks x20 |
| [AutoGrade](plugins/AutoGrade.cs) | `/grade wood\|stone\|metal\|hqm\|off`: new building blocks are placed at that grade, paid from the player's inventory |
| [MonumentPower](plugins/MonumentPower.cs) | Keeps monument lights on and fuse boxes filled |
| [HourlyStorm](plugins/HourlyStorm.cs) | Starts a storm every hour and clears it after 10 minutes |
| [NoNPC](plugins/NoNPC.cs) | Removes hostile NPCs and wild animals; keeps safe-zone guards, shopkeepers, turrets and horses |
| [TunnelDwellerCap](plugins/TunnelDwellerCap.cs) | Caps tunnel dweller NPCs to save server CPU |
| [MemoryRestart](plugins/MemoryRestart.cs) | Daily restart, and a restart when memory use gets too high, with a chat countdown |
| [Welcome](plugins/Welcome.cs) | Welcome popup and chat message on join, kit reminders, online count. Set `OwnerId` to your SteamID64 to skip the popup for yourself |

The server also runs these uMod plugins by other authors (not included here): Kits, Gather Manager, NTeleportation, Remover Tool, Backpacks, Furnace Splitter, Quick Sort.
