# DungeonAscendant

DungeonAscendant is a code-first 2D medieval fantasy progression action RPG built with C# and MonoGame.

## Overview

Explore a procedurally generated dungeon, fight room-based groups of Goblins, earn experience, and grow stronger. The current prototype combines the original combat and progression loop with connected rooms, corridors, collision, and a following camera, all without external art assets.

## Current Version: v0.5.0 Dungeon & Progression

The first v0.5.0 milestone replaces the fixed arena with a playable procedural dungeon while preserving the v0.1.0 movement, combat, progression, and run-state foundations.

## Current Features

- Frame-rate-independent four-direction movement and facing
- Directional, multi-target melee combat with knockback
- Player health, damage feedback, and temporary invulnerability
- Multiple independently acting Goblins with attack cooldowns
- Normal, Fast, and Brute Goblin variants
- Elite Goblins with improved stats and rewards
- Procedural generation of 7-10 non-overlapping rooms with connected corridors
- Distinct Start, Normal, Enemy, and Exit rooms
- Dungeon-wall collision and a world-space following camera
- Room-based enemy placement and local enemy activation
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
| Escape | Pause or resume during gameplay |
| R | Restart after Game Over |

## Progression Loop

Fight Goblins, earn experience from each kill, level up, and increase maximum health and melee damage. Dungeon enemies are scaled when the run is created, and a distant Enemy room contains an Elite challenge with increased rewards.

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
