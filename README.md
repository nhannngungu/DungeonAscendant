# DungeonAscendant

DungeonAscendant is a code-first 2D medieval fantasy progression action RPG built with C# and MonoGame.

## Overview

Explore a procedurally generated dungeon, fight room-based groups of Goblins, earn experience, and grow stronger. The current prototype combines the original combat and progression loop with connected rooms, corridors, collision, and a following camera, all without external art assets.

## Current Version: v0.5.0 Dungeon & Progression

The v0.5.0 milestone replaces the fixed arena with a playable procedural dungeon and adds World Tiers that raise enemy pressure and loot quality as a run descends.

## Current Features

- Frame-rate-independent four-direction movement and facing
- Directional, multi-target melee combat with knockback
- Player health, damage feedback, and temporary invulnerability
- Multiple independently acting Goblins with attack cooldowns
- Normal, Fast, and Brute Goblin variants
- Elite Goblins with improved stats and rewards
- Procedural generation of 7-10 non-overlapping rooms with connected corridors
- Distinct Start, Normal, Enemy, Treasure, Boss, and Exit rooms
- One-shot Treasure Chest with improved 1-2 item rewards
- Goblin Warlord encounter with activation, enrage, guaranteed loot, and Exit unlock
- Multi-floor dungeon completion with persistent Player progression and linear depth scaling
- Five World Tiers with deeper enemy, variant, Elite, and loot scaling
- Dungeon-wall collision and a world-space following camera
- Room-based enemy placement and local enemy activation
- Procedural Weapon and Armor drops with five rarity tiers
- Manual nearby loot pickup and a 12-slot run inventory
- Equipment swapping with derived damage and maximum-health bonuses
- Code-rendered inventory, equipment, comparison, and loot visuals
- Experience, leveling, stat growth, and kill tracking
- Player-level enemy scaling
- Start, Playing, Paused, and Game Over states
- In-session restart with a newly generated dungeon
- Code-rendered dungeon, characters, effects, overlays, and HUD

## Controls

| Control | Action |
|---|---|
| Enter | Start from the start screen |
| W/A/S/D or Arrow Keys | Move and change facing direction |
| Space | Directional melee attack |
| E | Open a nearby chest, use an unlocked Exit, or pick up nearby loot |
| I | Open or close inventory |
| W/S or Up/Down | Change inventory selection |
| Enter | Equip the selected inventory item |
| Escape | Pause or resume during gameplay |
| R | Restart after Game Over |

## Progression Loop

Fight Goblins, earn experience and equipment drops, open the Treasure Chest, defeat the Goblin Warlord, and use the unlocked Exit to descend. Weapons add melee damage, Armor adds maximum health, and level progression continues to improve the underlying base stats. Completing a floor preserves Player progression while increasing Dungeon Depth and generating a new dungeon; Game Over restart still resets the entire run.

## Dungeon and World Tier Progression

- Every floor contains exactly one Start, Treasure, Boss, and Exit room.
- The Boss room directly precedes the Exit in the generated room tree.
- World Tier starts at 1 and advances every two completed floors: depths 1-2 use Tier 1, 3-4 use Tier 2, 5-6 use Tier 3, 7-8 use Tier 4, and depth 9 onward uses Tier 5.
- Effective enemy and Boss level is `PlayerLevel + floor((DungeonDepth - 1) / 2) + WorldTier - 1`.
- Enemy and Boss health multiplier is `100% + 20% * (WorldTier - 1)`.
- Enemy and Boss damage multiplier is `100% + 12% * (WorldTier - 1)`.
- Goblin variant weights by Tier (Normal/Fast/Brute) are `60/25/15`, `52/28/20`, `44/31/25`, `36/34/30`, and `30/35/35`.
- The farthest Enemy room always contains one Elite. Each other Enemy room has an additional Elite chance of 0%, 10%, 18%, 26%, or 34% by Tier.
- The Goblin Warlord has six times equivalent normal-Goblin health and 1.8 times equivalent normal-Goblin damage before World Tier multipliers.
- Defeating the Boss unlocks the Exit; interacting with it generates the next floor and increments Dungeon Depth.
- Player level, experience, inventory, equipment, Dungeon Depth, and World Tier persist between floors. Game Over restart resets the complete run to depth 1 and Tier 1.

## Item Progression

- Normal Goblins have a 35% equipment drop chance; Elite Goblins always drop equipment.
- Item level is `max(SourceLevel, PlayerLevel) + floor((DungeonDepth - 1) / 3) + floor((WorldTier - 1) / 2) + SourceBonus`.
- Source bonuses are 0 for Normal enemies, 0-1 for Elites, 1-2 for Treasure Chests, and 2-3 for Bosses.
- Normal rarity weights by Tier (Common/Uncommon/Rare/Epic/Legendary) are `55/25/13/6/1`, `49/27/15/8/1`, `43/28/18/9/2`, `37/29/20/11/3`, and `31/30/22/13/4`.
- Elite rarity weights by Tier are `25/30/25/15/5`, `21/29/27/17/6`, `17/28/29/19/7`, `13/27/31/21/8`, and `9/26/33/23/9`.
- Treasure Chest rarity weights by Tier are `15/35/30/15/5`, `12/32/32/18/6`, `9/29/34/21/7`, `6/26/36/24/8`, and `3/23/38/27/9`.
- Boss rarity weights by Tier are `0/0/55/35/10`, `0/0/50/38/12`, `0/0/45/41/14`, `0/0/40/44/16`, and `0/0/35/47/18`.
- Weapon base bonus is `4 + ItemLevel * 2`.
- Armor base health bonus is `10 + ItemLevel * 5`.
- Rarity multipliers are 100%, 125%, 150%, 190%, and 250% respectively.

## Enemy Types

- **Normal Goblin** — Balanced health, damage, and movement speed.
- **Fast Goblin** — Lower health and damage, but substantially faster movement.
- **Brute Goblin** — Higher health and damage, with slower movement.
- **Elite Goblin** — A modifier that increases health, damage, size, visual prominence, and experience rewards for any Goblin variant.

## Technology

- C#
- .NET 9
- MonoGame DesktopGL
- MonoGame Content Builder tooling

## Code-Only Graphics Approach

All gameplay visuals are assembled at runtime from solid-color primitives. Characters, weapons, hit effects, dungeon geometry, HUD, and state overlays do not depend on sprite sheets, external images, or font assets. This keeps the prototype lightweight and supports the long-term portability goal.

## Version Roadmap

- **v0.1.0 - Core Gameplay** — Repeatable arena combat, enemy variants, progression, game states, and restart flow.
- **v0.5.0 - Dungeon & Progression** — Dungeon structure and deeper long-term progression systems.
- **v1.0.0 - Playable Web Release** — A complete release designed to be played directly in a web browser.

## How to Run

Install the .NET 9 SDK, then run from the repository root:

```sh
dotnet restore
dotnet run
```

The project uses the MonoGame DesktopGL package restored through NuGet.

## Future Web Release Goal

The long-term goal is for the final version of DungeonAscendant to be playable directly in a web browser. Gameplay code avoids Windows-only dependencies and keeps platform-specific responsibilities isolated to support that future target.
