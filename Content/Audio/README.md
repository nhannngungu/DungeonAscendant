# Dungeon Ascendant audio assets

Audio is referenced through MonoGame Content asset IDs without extensions.
Add source `.wav` or `.ogg` files under these folders and register them in
`Content.mgcb` using the matching asset IDs:

- `Combat/`: player swing, hit, block, heavy, dash, bow, and staff variations.
- `Enemies/Common/`: fallback idle, windup, release, hit, stagger, death, and special variations.
- `Enemies/<profile>/`: Map 2 profile hooks defined in `Audio/SoundProfiles.cs`.
- `Impacts/`: `flesh`, `bone`, `metal`, `stone`, and `spectral` variations.
- `UI/`: reserved for future menu/UI cues.

Each event supports `_01`, `_02`, and where listed `_03` variations. Missing
assets are intentionally skipped at startup; gameplay remains silent and does
not fail.
