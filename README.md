# DungeonAscendant

DungeonAscendant is a code-first 2D medieval fantasy progression action RPG built with C# and MonoGame.

## Overview

Fight through a continuously replenished arena of Goblins, earn experience, grow stronger, and survive increasingly dangerous enemy variants. The current prototype focuses on a compact, repeatable combat and progression loop built without external art assets.

## Current Version: v0.1.0 Core Gameplay

Version 0.1.0 establishes the complete core gameplay foundation: movement, directional combat, enemies, progression, run states, death, and restart.

## Current Features

- Frame-rate-independent four-direction movement and facing
- Directional, multi-target melee combat with knockback
- Player health, damage feedback, and temporary invulnerability
- Multiple independently acting Goblins with attack cooldowns
- Normal, Fast, and Brute Goblin variants
- Elite Goblins with improved stats and rewards
- Randomized bounded spawning and enemy replacement
- Experience, leveling, stat growth, and kill tracking
- Player-level enemy scaling
- Start, Playing, Paused, and Game Over states
- In-session run restart
- Code-rendered arena, characters, effects, overlays, and HUD

## Controls

| Control | Action |
|---|---|
| Enter | Start from the start screen |
| W/A/S/D or Arrow Keys | Move and change facing direction |
| Space | Directional melee attack |
| Escape | Pause or resume during gameplay |
| R | Restart after Game Over |

## Progression Loop

Fight Goblins, earn experience from each kill, level up, increase maximum health and melee damage, and face newly spawned enemies scaled to the current Player level. Elite enemies periodically add a stronger challenge and grant increased experience.

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

All gameplay visuals are assembled at runtime from solid-color primitives. Characters, weapons, hit effects, the arena, HUD, and state overlays do not depend on sprite sheets, external images, or font assets. This keeps the prototype lightweight and supports the long-term portability goal.

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
