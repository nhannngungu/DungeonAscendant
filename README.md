# DungeonAscendant

DungeonAscendant is a code-first 2D medieval fantasy progression action RPG built with C# and MonoGame.

## Overview

Explore a procedurally generated dungeon, fight room-based groups of Wild Forest enemies, earn experience, and grow stronger. The current prototype combines the original combat and progression loop with connected rooms, corridors, collision, and a following camera, all without external art assets.

## Current Version: v0.5.0 Dungeon & Progression

The v0.5.0 milestone replaces the fixed arena with a playable procedural dungeon, adds World Tiers, and introduces the first official content region: the corrupted Wild Forest.

## Current Features

- Frame-rate-independent four-direction movement and facing
- Directional, multi-target melee combat with knockback
- Player health, damage feedback, and temporary invulnerability
- Multiple independently acting Goblins with attack cooldowns
- Normal, Fast, and Brute Goblin variants
- Elite Goblins with improved stats and rewards
- Wild Forest Goblins, Dire Wolves, Giant Spiders, and Goblin Hunters
- Dire Wolf low-health retreat and re-engagement behavior
- Spider web shots, temporary web patches, and non-stacking Slow
- Ranged arrows with lifetime, Player collision, and dungeon-wall collision
- Reusable room-aware enemy, projectile, and temporary-status foundations
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

Fight region enemies, earn experience and equipment drops, open the Treasure Chest, defeat the Goblin Warlord, and use the unlocked Exit to descend. Weapons add melee damage, Armor adds maximum health, and level progression continues to improve the underlying base stats. Completing a floor preserves Player progression while increasing Dungeon Depth and generating a new dungeon; Game Over restart still resets the entire run.

## Regions

The run tracks Region independently from World Tier. Region selects content and presentation; World Tier continues to control difficulty pressure and loot quality. The current run starts and remains in `WildForest`. The region model also reserves Ancient Catacombs, Ruined City, Demon Lands, Demon Fortress, Demon City, and Demon King's Sanctuary for future milestones without implementing their content early.

The Wild Forest uses dark earth, moss markings, corrupted growth, roots, vines, and thorn-like boundaries drawn entirely from runtime primitives. The current Goblin Warlord remains its temporary Boss.

## Wild Forest Encounters

Base archetype selection weights are:

| World Tier | Goblin | Dire Wolf | Giant Spider | Goblin Hunter |
|---:|---:|---:|---:|---:|
| 1 | 40% | 25% | 20% | 15% |
| 2 | 37% | 26% | 21% | 16% |
| 3 | 34% | 27% | 22% | 17% |
| 4 | 31% | 28% | 23% | 18% |
| 5 | 28% | 29% | 24% | 19% |

Enemy rooms receive a 4-5 point budget. Goblins and Dire Wolves cost one point; Giant Spiders and Goblin Hunters cost two. Spider and Hunter anchors receive a Goblin frontline companion when budget permits, while Wolf anchors have a 55% chance to form a two-Wolf pack. Remaining budget uses the table above. Normal rooms have a 38% chance of a smaller 1-2 point encounter. Start, Treasure, Boss, and Exit rooms do not receive regular enemies.

Wild Forest archetype values before Elite modifiers are:

| Enemy | Health | Damage | Speed | Attack | EXP |
|---|---|---|---:|---|---:|
| Goblin | `100 + 20 * (Level - 1)` | `10 + 2 * (Level - 1)` | 110 | 50 range / 1.0s | 50 |
| Dire Wolf | `80 + 16 * (Level - 1)` | `12 + 2 * (Level - 1)` | 178 | 44 range / 0.85s | 60 |
| Giant Spider | `110 + 22 * (Level - 1)` | `11 + 2 * (Level - 1)` | 96 | 48 range / 1.15s | 75 |
| Goblin Hunter | `85 + 17 * (Level - 1)` | `14 + 2 * (Level - 1)` | 112 | 300 range / 1.4s | 70 |

All health and damage values receive the existing World Tier multipliers. Elites receive 1.75x health, 1.5x damage, doubled EXP, and 1.15x size.

- Dire Wolves retreat at 28% health or lower for 1.4 seconds at 1.12x movement speed, then re-engage. The retreat occurs once per Wolf.
- Giant Spiders prefer 82-155 range. Web shots fire every 3.1 seconds at 245 world units per second and apply a 58% movement multiplier for 2.5 seconds. Web patches last 4.5 seconds and are attempted every 6.2 seconds.
- Goblin Hunters prefer 145-220 range. Arrows fire every 1.4 seconds, travel at 330 world units per second, inherit Hunter damage, and expire after 2.4 seconds.
- Slow never changes base Player speed. Reapplication keeps the strongest multiplier and refreshes only to the longer remaining duration.

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

- Normal enemies have a 35% equipment drop chance; Elite enemies always drop equipment.
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

- **Goblin** - Balanced melee baseline with Normal, Fast, and Brute variants.
- **Dire Wolf** - Fast melee pressure that briefly retreats at low health.
- **Giant Spider** - Control enemy with melee, web shots, and floor patches.
- **Goblin Hunter** - Ranged enemy that maintains distance and fires arrows.
- **Elite** - A shared modifier that increases health, damage, size, visual prominence, and EXP for any archetype.

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
