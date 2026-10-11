using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.Items;
using DungeonAscendant.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace DungeonAscendant.Audio;

public sealed class AudioManager : IDisposable
{
    private const float MaximumAudibleDistance = 920f;
    private const float DuplicateCooldownSeconds = .075f;
    private const float CrossfadeSeconds = .8f;
    private readonly Dictionary<string, SoundEffect> _sounds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _lastPlayedAt = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _lastVariation = new(StringComparer.Ordinal);
    private readonly List<string> _missingAssets = new();
    private readonly Random _random = new();
    private SoundEffectInstance _music;
    private SoundEffectInstance _previousMusic;
    private SoundEffectInstance _ambience;
    private MusicState _musicState;
    private AmbienceState _ambienceState;
    private float _musicFade = 1f;
    private float _previousFade;
    private float _musicMix = 1f;
    private float _previousMusicMix = 1f;
    private float _clock;

    public IReadOnlyList<string> MissingAssets => _missingAssets;

    private AudioManager()
    {
    }

    public static AudioManager CreateDisabled() => new();

    public AudioManager(ContentManager content)
    {
        foreach (string asset in AudioProfiles.EnumerateAssets())
        {
            try { _sounds[asset] = content.Load<SoundEffect>(asset); }
            catch (Exception) { _missingAssets.Add(asset); }
        }
    }

    public void Update(GameTime gameTime)
    {
        float elapsed = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .1f);
        _clock += elapsed;
        float step = elapsed / CrossfadeSeconds;
        _musicFade = MathF.Min(1f, _musicFade + step);
        _previousFade = MathF.Max(0f, _previousFade - step);
        SetInstanceVolume(_music, Loc.MasterVolume * Loc.MusicVolume * _musicMix * _musicFade);
        SetInstanceVolume(_previousMusic, Loc.MasterVolume * Loc.MusicVolume * _previousMusicMix * _previousFade);
        SetInstanceVolume(_ambience, Loc.MasterVolume * Loc.MusicVolume * .327f);
        if (_previousMusic != null && _previousFade <= 0f)
        {
            _previousMusic.Stop();
            _previousMusic.Dispose();
            _previousMusic = null;
        }
    }

    public void SetMusic(MusicState state)
    {
        if (state == _musicState) return;
        _musicState = state;
        _previousMusic?.Stop();
        _previousMusic?.Dispose();
        _previousMusic = _music;
        _previousMusicMix = _musicMix;
        _previousFade = _previousMusic == null ? 0f : 1f;
        _music = CreateLoop(AudioProfiles.GetMusic(state));
        _musicMix = state is MusicState.Map1Boss or MusicState.Map2Boss
            ? .95f
            : state is MusicState.Map1Combat or MusicState.Map2Combat
                ? .78f
                : .60f;
        _musicFade = 0f;
    }

    public void SetAmbience(AmbienceState state)
    {
        if (state == _ambienceState) return;
        _ambienceState = state;
        _ambience?.Stop();
        _ambience?.Dispose();
        _ambience = CreateLoop(AudioProfiles.GetAmbience(state));
    }

    public void PlayPlayer(PlayerSoundEvent e, WeaponFamily family) => Play($"player:{family}:{e}", AudioProfiles.GetPlayer(family).GetClips(e), 1f, 0f);
    public void PlayUi(UiSoundEvent e) => Play($"ui:{e}", AudioProfiles.GetUi(e), 1f, 0f);
    public void PlayWorld(WorldSoundEvent e, Vector2 source, Vector2 listener) => PlaySpatial($"world:{e}", AudioProfiles.GetWorld(e), source, listener);
    public void PlayBoss(BossSoundEvent e, Vector2 source, Vector2 listener) => PlaySpatial($"boss:{e}", AudioProfiles.GetBoss(e), source, listener);
    public void PlayEnemy(EnemyType type, EnemySoundEvent e, Vector2 source, Vector2 listener)
    {
        EnemySoundProfile profile = AudioProfiles.GetEnemy(type);
        PlaySpatial($"enemy:{profile.Id}:{e}", profile.GetClips(e), source, listener);
    }
    public void PlayImpact(ImpactMaterial material, Vector2 source, Vector2 listener) => PlaySpatial($"impact:{material}", AudioProfiles.GetImpact(material), source, listener);

    private void PlaySpatial(string group, IReadOnlyList<string> clips, Vector2 source, Vector2 listener)
    {
        float distance = Vector2.Distance(source, listener);
        if (distance >= MaximumAudibleDistance) return;
        float normalized = 1f - distance / MaximumAudibleDistance;
        Play(group, clips, normalized * normalized, Math.Clamp((source.X-listener.X)/MaximumAudibleDistance, -.72f, .72f));
    }

    private void Play(string group, IReadOnlyList<string> candidates, float spatialVolume, float pan)
    {
        if (Loc.MasterVolume <= 0f || Loc.SfxVolume <= 0f || spatialVolume <= 0f || candidates.Count == 0) return;
        if (_lastPlayedAt.TryGetValue(group, out float last) && _clock-last < DuplicateCooldownSeconds) return;
        var loaded = new List<(int Index, SoundEffect Sound)>();
        for (int i=0;i<candidates.Count;i++) if (_sounds.TryGetValue(candidates[i], out SoundEffect sound)) loaded.Add((i,sound));
        if (loaded.Count == 0) return;
        int choice = _random.Next(loaded.Count);
        if (loaded.Count > 1 && _lastVariation.TryGetValue(group, out int previous))
            for (int attempt=0;attempt<3 && loaded[choice].Index==previous;attempt++) choice=_random.Next(loaded.Count);
        (int variation, SoundEffect selected)=loaded[choice];
        float volume=Math.Clamp(Loc.MasterVolume*Loc.SfxVolume*spatialVolume*(.95f+(float)_random.NextDouble()*.05f),0f,1f);
        try
        {
            if (selected.Play(volume,-.025f+(float)_random.NextDouble()*.05f,pan))
            { _lastPlayedAt[group]=_clock; _lastVariation[group]=variation; }
        }
        catch (InvalidOperationException) { }
    }

    private SoundEffectInstance CreateLoop(string asset)
    {
        if (asset == null || !_sounds.TryGetValue(asset, out SoundEffect sound)) return null;
        try { SoundEffectInstance instance=sound.CreateInstance(); instance.IsLooped=true; instance.Volume=0f; instance.Play(); return instance; }
        catch (InvalidOperationException) { return null; }
    }

    private static void SetInstanceVolume(SoundEffectInstance instance, float volume)
    { if (instance != null) try { instance.Volume=Math.Clamp(volume,0f,1f); } catch (InvalidOperationException) { } }

    public void Dispose()
    {
        _music?.Dispose(); _previousMusic?.Dispose(); _ambience?.Dispose();
    }
}
