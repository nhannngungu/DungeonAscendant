# Map 7 final integration handoff

Implementation and headless validation complete. Manual graphics/audio/gameplay testing remains with the user. No game window was launched, no Computer Use was used, and no changes were staged, committed or pushed.

## Ending and progression

- Only genuine Vaelthar defeat unlocks the final endpoint; generic legacy `BossDefeated` cannot unlock it.
- Approach the endpoint and press E for a 7.2-second epilogue: restrained dim/letterbox, a short throne view, a Player view, and three EN/VI text lines ending in confirmation of the Demon King's defeat.
- No external assets or cutscene engine. Player is not repositioned. Ordinary combat/input pauses during this explicitly requested ending, then control returns in Map 7 without increasing depth or world tier.
- `Map07SeraphDefeated`, `Map07VaeltharDefeated`, `Map07Cleared`, `Map07EndpointPrepared`, and `Map07EndingSeen` persist in the existing additive JSON save model. Unlocked is derived from genuine defeat/prepared state.
- Bosses 70/71 do not respawn after saved defeat. Seen endings do not replay on E or reload. Closing before completion leaves Seen false and permits another endpoint interaction after reload.
- F10's explicit boss/encounter reset clears the corresponding completion state; resetting Vaelthar also clears the ending state.

## Integration fixes

- All ten Map 7 enemies tick their disengagement/cleanup even when remote; enemy-only bounds apply to every Map 7 encounter, not only elites.
- Map 7 combat has no player arena clamp, kill gate, invisible encounter blocker, or enemy-driven Player displacement. Removed Sentinel/Acolyte/Scribe push and Seraph pull; retained their attacks and Seraph's brief slow.
- Interruption/disengagement/death clear local marks, grasp, echoes and bursts. Vowshard/Script projectiles track their owner and remove only that owner's projectiles/impacts.
- Regular Map 7 deaths retain their authored presentation until it expires.
- One allocation-free HUD target selection gives eligible Vaelthar priority over Seraph; dead/returning/remote bosses are excluded. Phase marker thresholds no longer allocate an array per frame.
- Ending entry clears transient combat, owner effects, stale death events and introduction presentation without resetting discoveries; ending audio selects silence and no combat/world events are observed during it. Map 7 has semantic ambience hooks but no dedicated ambience/music loops yet, so no new audio assets were added.

## Files touched by this final pass

These are this pass's files, not the entire pre-existing uncommitted Map 7 foundation/enemy worktree:

- `Audio/AudioManager.cs`
- `Audio/CombatAudioDirector.cs`
- `Combat/Projectile.cs`
- `Combat/ProjectileManager.cs`
- `Core/GameSession.cs`
- `Core/GameState.cs`
- `Core/SanctumEndingFlow.cs` (new)
- `Dungeon/DemonKingSanctumIntegrationValidation.cs` (new)
- `Dungeon/ThroneboundSeraphValidation.cs`
- `Dungeon/VaeltharValidation.cs`
- `Enemies/Enemy.cs`
- `Enemies/EnemyManager.cs`
- `Enemies/DemonKingSanctumEnemy.cs`
- `Enemies/DemonKingSanctumPenitentEnemy.cs`
- `Enemies/DemonKingSanctumReliquaryEnemy.cs`
- `Enemies/ThroneboundSeraphEnemy.cs`
- `Graphics/GameRenderer.cs`
- `Localization/EnglishStrings.cs`
- `Localization/VietnameseStrings.cs`
- `Persistence/SaveSlotData.cs`
- `Program.cs`
- `World/EnemyIntroductionManager.cs`
- `World/WildForestEncounterDirector.cs`
- `MAP7_FINAL_INTEGRATION.md` (new)

## Validation and builds

All validations use CLI validation flags that bypass Game1/graphics/game launch, with isolated save/settings fixtures under ignored `bin/ValidationTemp` (TMP/TEMP and DUNGEON_ASCENDANT_USER_DATA redirected there).

Passed:

- `--validate-map7-final`: full Map 7 suite plus final integration; includes 62–71 factory/profile/EN/VI/socket/renderer checks, finite combat/phase/ultimate/reversal/marker tests, JKL immunity, exact-once spawn/death, leashes and cleanup. A real GameSession traverses the entire map with all ten enemies alive and zero kills. Includes unique boss UI selection, genuine boss completion, locked/unlocked endpoint, interrupted/seen ending reload, no repeated ending, camera bounds and returned movement control.
- `--validate-audio`
- `--validate-localization`
- `--validate-save-slots`
- `--validate-demon-city`
- `--validate-wilderness`
- `--validate-entry-spawn`
- `--validate-developer-panel`
- `git diff --check`

Builds:

- `dotnet build .\DungeonAscendant.csproj --no-restore`: PASS, zero warnings/errors.
- `dotnet build .\DungeonAscendant.csproj` (exact DesktopGL): PASS, zero warnings/errors; restore already up to date.
- `dotnet build .\DungeonAscendant.WindowsDX.csproj` (one exact attempt): ENVIRONMENT BLOCKED, MSB4184. Windows SDK discovery `GetPlatformSDKDisplayName(Windows, 7.0)` cannot access the registry. No project workaround or repeated attempt was made.

## User manual-test checklist

1. Enter from Map 6 and via F10. Confirm all 62–71 visuals/audio/text resolve; no repeated introductions.
2. Traverse Map 7 without killing enemies, including past Seraph and Vaelthar. Check no encounter wall, Player clamp, forced push/pull or kill requirement; enemies return home.
3. Leave active glyph/grasp/echo/projectile encounters and kill their owners. Check no frozen hazards; check surviving enemies retain their own projectiles.
4. Approach Seraph then Vaelthar with Seraph alive. Confirm one relevant boss bar, final-boss priority, and bars disappear on death/disengagement.
5. Defeat Seraph, save/reload, revisit: no respawn. Confirm intentional F10 reset permits a new test.
6. Fight Vaelthar through 70%/35% transitions, reversal, ordered lanes, Domain and End of Kings. Confirm readable safe gaps and JKL control immunity; inspect death/VFX cleanup.
7. Before defeat E at the endpoint does nothing. After genuine defeat endpoint unlocks; saving/reloading never respawns Vaelthar and preserves the endpoint.
8. Press E at the unlocked endpoint in EN and VI. Inspect the short camera/text/dim presentation, no HUD/title overlap, final confirmation, no unwanted audio loop, and returned movement control after roughly 7.2 seconds.
9. Hold/tap E during and after ending; it must not repeat or advance to another map. Close mid-ending and reload: ending remains available. Complete and reload: seen persists, E does not replay, bosses remain absent.
10. Inspect text fit at supported window sizes and confirm normal pause/menu controls after control returns. WindowsDX must be built/tested on a host with Windows SDK registry access.

## Commit preparation

Current branch: `main`. Workspace already contained uncommitted Map 7 foundation/enemies before this pass; these were preserved. Index remains empty. Review the combined Map 7 changes and finish the manual checklist before committing; no commit or push was performed here.
