# DungeonAscendant

DungeonAscendant is a code-first 2D side-scrolling medieval fantasy action roguelike / action RPG built with C# and MonoGame.

## Overview

Explore a procedurally generated chain of side-view combat rooms, fight room-based groups of Wild Forest enemies, earn experience, and grow stronger. The current prototype combines gravity, jumping, solid platforms, horizontal transitions, and a side-follow camera with timing-based sword combat, stamina defense, and enemy stagger, all without external art assets.

## Current Version: Armor Grade, Fusion, and Boss-Only Weapons

Armor now drops with an independent C1-C5 progression Grade, three identical unequipped pieces can be fused into the next Grade, and random weapon acquisition has been removed. The seven weapon families remain fully implemented as test equipment and as content for explicit final Region Boss rewards.

## Current Features

- Frame-rate-independent left/right movement, gravity, jumping, falling, and grounded state
- Data-driven per-family combos with one-input buffering and distinct finisher timing
- Heavy sword attack with higher damage, knockback, poise damage, and stamina cost
- Stamina-powered dodge, directional block, and guard break
- Enemy windup/active/recovery phases with shared poise recovery and stagger
- Left/right facing with directional, multi-target melee combat and horizontal knockback
- Player health, damage feedback, and temporary invulnerability
- Multiple independently acting Goblins with attack cooldowns
- Normal, Fast, and Brute Goblin variants
- Elite Goblins with improved stats and rewards
- Complete Wild Forest roster: seven normal enemies and two named elites
- Dire Wolf low-health retreat and re-engagement behavior
- Spider web shots, temporary web patches, and non-stacking Slow
- Ranged arrows with lifetime, Player collision, and dungeon-wall collision
- Reusable room-aware enemy, projectile, and temporary-status foundations
- Procedural generation of 7-10 ordered 1100-1700 pixel-wide side-view rooms
- Continuous safe ground, reachable raised platforms, jumpable obstacles, and horizontal transitions
- Distinct Start, Normal, Enemy, Treasure, Boss, and Exit rooms
- One-shot Treasure Chest with improved 1-2 Armor rewards
- Three-phase Ancient Treant Boss with root strikes and temporary terrain hazards
- Multi-floor dungeon completion with persistent Player progression and linear depth scaling
- Five World Tiers with deeper enemy, variant, Elite, and loot scaling
- Axis-separated floor/platform/wall/ceiling collision and a clamped side-follow camera
- Room-based enemy placement and local enemy activation
- Progression-scaled Armor drops with independent C1-C5 Grades and five rarity tiers
- Three-for-one Armor Fusion with confirmation and equipped-item safety
- Boss-only, explicitly assigned weapon reward architecture
- Manual nearby loot pickup and a 12-slot normal run inventory
- Equipment swapping with derived damage and maximum-health bonuses
- Seven distinct weapon families: Long Sword + Shield, Great Sword, Battle Axe, Spear, Dual Daggers, Bow, and Arcane Staff
- Charged-release Bow arrows and Arcane Staff bolts with dungeon-wall and enemy collision
- Light, Medium, and Heavy armor classes with distinct defense, mobility, stamina, and silhouettes
- Shield/block availability derived from the equipped weapon family
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
| A/D or Left/Right Arrow | Move and change facing direction |
| Space | Jump while grounded |
| Left Mouse or J | Light attack / continue the equipped weapon's combo |
| Right Mouse or K | Heavy attack; hold and release to charge Bow/Arcane attacks |
| Left or Right Shift | Dodge in the held movement direction, or facing direction |
| Left Ctrl (hold) | Directional block |
| F3 | Toggle combat hitbox debugging |
| F5 | Cycle milestone sample weapons for testing |
| F6 | Cycle Light / Medium / Heavy sample armor for testing |
| F7 | Restart the sequential Wild Forest enemy test when its debug flag is enabled |
| E | Open a nearby chest, use an unlocked Exit, or pick up nearby loot |
| I | Open or close inventory |
| Q/E (Inventory open) | Previous/next Inventory tab |
| W/S or Up/Down | Move inventory selection one row up/down |
| A/D or Left/Right | Move inventory selection one column left/right |
| Enter | Equip the selected inventory item |
| F (Inventory open) | Prepare fusion of three eligible matching Armor pieces |
| Enter (fusion prompt) | Confirm the prepared Armor fusion |
| Escape (Inventory open) | Cancel fusion confirmation, or close Inventory |
| Escape | Pause or resume during gameplay |
| R | Restart after Game Over |

## Combat 2.0

The default sword uses three light attacks with progressively stronger damage, poise damage, reach, and recovery. One late light input can be buffered into the next strike; waiting more than 0.55 seconds after a strike resets the chain. Heavy attacks consume 30 stamina and have a visible 0.32-second startup.

The Player has 100 stamina. Dodge costs 24 stamina, lasts 0.34 seconds, and is invulnerable from 0.05 through 0.22 seconds. Stamina begins regenerating after a 0.8-second delay at 30 points per second. Blocking uses an invisible 80-by-90 forward defense zone, only protects the facing side, and consumes one stamina per incoming damage, with a minimum cost of 10. An exhausted guard breaks for 0.7 seconds and allows half damage through. Goblin Hunter arrows are blockable from the front and are consumed on contact with an active guard.

## Equipment Identity

Every new run starts with a Knight Long Sword and Knight Armor equipped. While `Player.DebugGiveAllTestEquipment` is `true`, the test inventory contains all seven sample weapons plus three C1 copies each of Scout, Knight, and Fortress Armor so every base fusion can be exercised immediately. Debug inventory capacity is 20; normal inventory capacity remains 12. Press `I`, select with `W/S` or the arrow keys, press `Enter` to equip, or press `F` and then `Enter` to confirm an eligible fusion. Set the flag to `false` to restore an empty starting inventory and normal loot/equip behavior. `F5` and `F6` remain available as additional weapon/armor cycle shortcuts. Debug weapons are test-only and are never part of random loot.

Inventory selection uses a four-column grid. WASD and the arrow keys move in matching directions without wrapping; vertical movement into a partial final row chooses the nearest valid slot.

The Inventory has `WEAPONS` and `EQUIPMENT` tabs. Press `Q` or `E` while Inventory is open to switch tabs. Each tab filters the same underlying inventory item references and remembers its own grid selection; Armor fusion is available only from `EQUIPMENT`.

- Long Swords use the balanced three-hit combo and are the only family that renders a shield or accepts normal block input.
- Great Swords use slower, longer, higher-damage two-handed swings with the most committed movement and a powerful overhead heavy.
- Battle Axes use medium-slow chops with shorter reach than Great Swords but the strongest knockback and poise pressure.
- Spears use two long narrow thrusts, a sweep finisher, and a long heavy lunge with the greatest melee reach.
- Dual Daggers use a low dual-wield stance, a fast five-hit advancing combo, short recovery, and the shortest melee reach.
- Bows have no melee chain: light fires an arrow and held heavy releases a faster, stronger charged arrow from the visible bow.
- Arcane Staves use a caster stance and slower, heavier arcane bolts. Their attacks are marked `MagicReady` for a future resource system but currently spend stamina.
- Scout Armor has the narrowest silhouette, fastest movement/dodge, and strongest stamina regeneration, but the least mitigation.
- Knight Armor retains the established balanced hero silhouette and baseline mobility.
- Fortress Armor adds broad plate and greaves, more health and mitigation, cheaper shield guarding, but slower movement/dodge and stamina regeneration.

Armor Grade and rarity are independent. Grade controls progression power and is displayed as C1 through C5; rarity remains the item's quality classification. Grade scaling uses centralized power multipliers of 100%, 112%, 125%, 139%, and 154%. It improves maximum-health bonuses and each Armor class's defensive/stamina strengths without removing Light, Medium, and Heavy tradeoffs.

Common melee attacks and arrows are blockable. Web projectiles and root hazards remain outside defensive handling. F3 toggles the developer combat overlay, which defaults off and can show body, defense, melee-attack, and projectile zones plus block-state labels. Every enemy has regenerating poise; low-poise enemies such as Goblins and Blood Bats stagger quickly, while Corrupted Treants, named Elites, and the Ancient Treant require heavier pressure. The boss restores its poise on stagger and gains 2.5 seconds of stagger resistance to prevent stun locking.

## Progression Loop

Fight region enemies, earn experience and Armor drops, fuse matching Armor, open the Treasure Chest, defeat the Ancient Treant, and use the unlocked Exit to descend. Weapons add melee damage, Armor adds maximum health and class-specific modifiers, and level progression continues to improve the underlying base stats. Completing a floor preserves Player progression while increasing Dungeon Depth and generating a new dungeon; Game Over restart still resets the entire run.

## Regions

The run tracks Region independently from World Tier. Region selects content and presentation; World Tier continues to control difficulty pressure and loot quality. The current run starts and remains in `WildForest`. The region model also reserves Ancient Catacombs, Ruined City, Demon Lands, Demon Fortress, Demon City, and Demon King's Sanctuary for future milestones without implementing their content early.

The Wild Forest uses dark earth, moss markings, corrupted growth, roots, vines, and thorn-like boundaries drawn entirely from runtime primitives. Its Boss is the Ancient Treant.

## Wild Forest Encounters

### Sequential Enemy Test Mode

`GameSession.DebugSequentialWildForestEnemies` temporarily replaces Map 1's procedural population with one main test enemy at a time. Its order is Goblin, Goblin Hunter, Dire Wolf, Giant Spider, Blood Bat, Thorn Crawler, Corrupted Treant, Goblin Chief, Mother Spider, and Ancient Treant. Set the flag to `false` to restore the unchanged procedural room compositions, Elite rules, and normal Region Boss flow.

Test enemies spawn in the first Enemy room at role-sensitive horizontal distances: 425 pixels for standard melee enemies, 475 for Thorn Crawler, 500 for Blood Bat and Corrupted Treant, 550 for Mother Spider, and 650 for Goblin Hunter. Ground and collision checks adjust the exact position within the room. The next stage begins after 1.25 seconds. Mother Spider can use its normal summons; orphaned Spiderlings and temporary projectiles/hazards are cleared when the main enemy dies.

Sequential kills grant no EXP, loot, kill-count progress, exit unlock, checkpoint progress, or Boss reward. Ancient Treant retains its normal combat implementation but uses the test room and completes the sequence without setting normal `BossDefeated`. The HUD displays the current test enemy and `WILD FOREST TEST COMPLETE`; press `F7` to restart the sequence without conflicting with the existing F6 Armor shortcut.

### Goblin Visual Pass - Phase 1

Goblin and Goblin Hunter use `EnemyVisualProfile`, `EnemyAnimationController`, and `GoblinFamilySpriteRenderer`. Gameplay remains in the enemy classes; the presentation controller reads movement, hit, stagger, death, and attack-phase state. Poses are authored facing right and the entire layered composition is mirrored when facing left without reversing animation order. Visual sizes are 52x50 for Goblin and 56x57 for Hunter, while their existing 36x44 and 36x46 collision boxes remain unchanged.

Goblin presentation states are Idle, Move, Attack Windup (0.48s), Attack Active (0.17s), Attack Recovery (0.40s), Hurt, Stagger, and one-shot Death. Its short crude sword pose uses the live `MeleeAttack.PhaseProgress`, so the committed slash coincides with the existing active hit window. Hunter presentation states are Idle, Move, Retreat, Aim (0.26s), Shoot (0.06s), Recovery (0.18s), Hurt, Stagger, and one-shot Death. The Hunter stabilizes during its shot sequence; its bow, string, arms, and nocked arrow use the actual target direction, and the gameplay arrow originates at the rendered bow release point.

Defeated Goblins and Hunters are removed from combat and progression immediately, then retained in a presentation-only list for 0.57 seconds so their non-looping collapse can finish. No textures are created per frame. Run `dotnet run --no-build -- --validate-enemy-visuals` for headless checks covering state mapping, unchanged hitboxes/timings, vertical bow aim, arrow release alignment, arrow blocking, death cleanup, and the sequential Goblin-to-Hunter transition.

Base archetype selection weights are:

| Tier | Goblin | Wolf | Spider | Hunter | Crawler | Treant | Bat |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 32% | 22% | 14% | 10% | 10% | 5% | 7% |
| 2 | 28% | 22% | 15% | 11% | 11% | 6% | 7% |
| 3 | 24% | 21% | 16% | 12% | 12% | 7% | 8% |
| 4 | 21% | 20% | 17% | 13% | 13% | 8% | 8% |
| 5 | 18% | 19% | 18% | 14% | 14% | 9% | 8% |

Enemy rooms receive a 5-7 point budget. Goblins, Wolves, and Bats cost one point; Spiders, Hunters, and Crawlers cost two; normal Treants cost three. Normal rooms have a 38% chance of a smaller 1-3 point encounter. Start, Treasure, Boss, and Exit rooms do not receive regular enemies.

Enemy rooms choose Pack Hunt, Goblin Patrol, Web Nest, Corrupted Grove, Bat Swarm, or Mixed templates. Mixed has a direct 15% chance; other templates are selected from the tier-weighted roster. The farthest Enemy room always contains one Goblin Chief or Mother Spider. Other Enemy rooms use the existing 0/10/18/26/34% additional named-Elite chance by Tier.

Wild Forest archetype values before Elite modifiers are:

| Enemy | Health | Damage | Speed | Attack | EXP |
|---|---|---|---:|---|---:|
| Goblin | `100 + 20 * (Level - 1)` | `10 + 2 * (Level - 1)` | 110 | 50 range / 1.0s | 50 |
| Dire Wolf | `80 + 16 * (Level - 1)` | `12 + 2 * (Level - 1)` | 178 | 44 range / 0.85s | 60 |
| Giant Spider | `110 + 22 * (Level - 1)` | `11 + 2 * (Level - 1)` | 96 | 48 range / 1.15s | 75 |
| Goblin Hunter | `85 + 17 * (Level - 1)` | `14 + 2 * (Level - 1)` | 112 | 300 range / 1.4s | 70 |
| Thorn Crawler | `95 + 18 * (Level - 1)` | `15 + 2 * (Level - 1)` | 116 | 46 range / 1.05s | 70 |
| Corrupted Treant | `240 + 45 * (Level - 1)` | `24 + 4 * (Level - 1)` | 58 | 65 range / 1.45s | 120 |
| Blood Bat | `45 + 9 * (Level - 1)` | `9 + 2 * (Level - 1)` | 190 | 38 range / 0.9s | 30 |
| Goblin Chief | `160 + 30 * (Level - 1)` | `16 + 3 * (Level - 1)` | 105 | 55 range / 1.0s | 160 |
| Mother Spider | `260 + 45 * (Level - 1)` | `16 + 3 * (Level - 1)` | 82 | 60 range / 1.2s | 220 |

All health and damage values receive the existing World Tier multipliers. Elites receive 1.75x health, 1.5x damage, doubled EXP, and 1.15x size.

- Dire Wolves retreat at 28% health or lower for 1.4 seconds at 1.12x movement speed, then re-engage. The retreat occurs once per Wolf.
- Giant Spiders prefer 82-155 range. Web shots fire every 3.1 seconds at 245 world units per second and apply a 58% movement multiplier for 2.5 seconds. Web patches last 4.5 seconds and are attempted every 6.2 seconds.
- Goblin Hunters prefer 145-220 range. Arrows fire every 1.4 seconds, travel at 330 world units per second, inherit Hunter damage, and expire after 2.4 seconds.
- Thorn Crawlers track underground for 0.9 seconds, warn for 0.65 seconds, emerge for 0.35 seconds, attack for 1.35 seconds, recover for 0.9 seconds, and burrow for 0.45 seconds. Underground states cannot be hit by melee.
- Corrupted Treants create a 62x62 Root Strike every 5 seconds after a 0.85-second telegraph. It deals 75% of Treant damage and applies a 72% movement multiplier.
- Blood Bats circle, dive for up to 0.8 seconds, then retreat for 0.8 seconds. Their airborne melee target is reduced; the complete target is exposed during a dive. Individual loot chance is 12.25%.
- Goblin Chiefs grant nearby Goblins and Hunters +20% damage and +18% movement for 5 seconds every 8 seconds. The strongest values refresh rather than stack.
- Mother Spiders retain web control and summon one Spiderling every 6.5 seconds, capped at three per Mother and eight globally.
- Spiderlings use `25 + 5 * (Level - 1)` health and `5 + 1 * (Level - 1)` damage, move at 165, and award no EXP or loot.
- Slow never changes base Player speed. Reapplication keeps the strongest multiplier and refreshes only to the longer remaining duration.

## Ancient Treant

- Health: `(900 + 180 * (Level - 1)) * WorldTierHealthMultiplier`.
- Damage: `(28 + 4 * (Level - 1)) * WorldTierDamageMultiplier`.
- EXP: `650 + 100 * (PlayerLevel - 1) + 150 * (Depth - 1) + 150 * (Tier - 1)`.
- Phase 1 above 65% health: speed 45, one Root Strike zone every 4.8 seconds.
- Phase 2 from 65% through above 30%: speed 58, two Root Strike zones every 3.5 seconds.
- Phase 3 at 30% or lower: speed 72, three Root Strike zones every 2.4 seconds.
- Terrain-root patterns use 1/2/4 temporary segments by phase with 8.5/6.5/4.5-second cooldowns. Telegraphs last 1 second; active terrain lasts 3.5 seconds and damages/slows without permanently changing dungeon geometry.
- Root hazards are capped at 24 and are cleared on Boss death, dungeon transition, Game Over, and restart.

## Dungeon and World Tier Progression

- Every floor contains exactly one Start, Treasure, Boss, and Exit room.
- The Boss room directly precedes the Exit in the generated room tree.
- World Tier starts at 1 and advances every two completed floors: depths 1-2 use Tier 1, 3-4 use Tier 2, 5-6 use Tier 3, 7-8 use Tier 4, and depth 9 onward uses Tier 5.
- Effective enemy and Boss level is `PlayerLevel + floor((DungeonDepth - 1) / 2) + WorldTier - 1`.
- Enemy and Boss health multiplier is `100% + 20% * (WorldTier - 1)`.
- Enemy and Boss damage multiplier is `100% + 12% * (WorldTier - 1)`.
- Goblin variant weights by Tier (Normal/Fast/Brute) are `60/25/15`, `52/28/20`, `44/31/25`, `36/34/30`, and `30/35/35`.
- The farthest Enemy room always contains one Elite. Each other Enemy room has an additional Elite chance of 0%, 10%, 18%, 26%, or 34% by Tier.
- Rooms are 1100-1700 pixels wide and 720 pixels high. Horizontal transition spaces are 144 pixels wide.
- Every room and transition has continuous ground. Optional ledges rise no more than 108 pixels and are never required for progression.
- Defeating the Boss unlocks the Exit; interacting with it generates the next floor and increments Dungeon Depth.
- Player level, experience, inventory, equipment, Dungeon Depth, and World Tier persist between floors. Game Over restart resets the complete run to depth 1 and Tier 1.

## Armor and Reward Progression

- Normal enemies have a 35% Armor drop chance; Elite enemies always drop Armor.
- Generic Treasure Chests create 1-2 Armor rewards and never roll weapons.
- Armor definitions are selected from the current Scout, Knight, and Fortress pool.
- Grade pressure combines Dungeon Depth, World Tier, and Region order into five centralized progression stages. Normal, Elite, and Treasure sources have separate Grade weights; Elites and Treasure are biased upward.
- Early normal drops are 92% C1 and 8% C2. C5 appears only in the two latest pressure stages and remains uncommon.
- Fusion requires three inventory pieces with the same Armor definition, Grade, and rarity. C5 cannot fuse. Equipped Armor never counts as material.
- Normal rarity weights by Tier (Common/Uncommon/Rare/Epic/Legendary) are `55/25/13/6/1`, `49/27/15/8/1`, `43/28/18/9/2`, `37/29/20/11/3`, and `31/30/22/13/4`.
- Elite rarity weights by Tier are `25/30/25/15/5`, `21/29/27/17/6`, `17/28/29/19/7`, `13/27/31/21/8`, and `9/26/33/23/9`.
- Treasure Chest rarity weights by Tier are `15/35/30/15/5`, `12/32/32/18/6`, `9/29/34/21/7`, `6/26/36/24/8`, and `3/23/38/27/9`.
- Weapons are never produced by normal, Elite, Treasure, or generic item generation. A final Region Boss can award one explicitly assigned `BossWeaponReward`; the reward is protected by the one-shot Boss defeat path. Wild Forest currently has no designed unique weapon assigned, so none was invented for this milestone.

## Enemy Types

- **Goblin** - Balanced melee baseline with Normal, Fast, and Brute variants.
- **Dire Wolf** - Fast melee pressure that briefly retreats at low health.
- **Giant Spider** - Control enemy with melee, web shots, and floor patches.
- **Goblin Hunter** - Ranged enemy that maintains distance and fires arrows.
- **Thorn Crawler** - Untargetable underground ambusher with a warned emergence.
- **Corrupted Treant** - Slow heavy enemy with delayed Root Strikes.
- **Blood Bat** - Low-health flying swarm enemy with dive windows.
- **Goblin Chief** - Named support Elite with a Goblin-family War Cry.
- **Mother Spider** - Named summoner Elite with capped Spiderlings.
- **Ancient Treant** - Three-phase Wild Forest Boss with root terrain control.

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
