using System;
using System.Collections.Generic;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Enemies;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public sealed class RottenCorpseBurst
{
    public const float WarningSeconds = .90f;
    public const float AftermathSeconds = .32f;

    public Vector2 Position { get; }
    public float TimeUntilBurst { get; set; } = WarningSeconds;
    public float AftermathTimeRemaining { get; set; } = AftermathSeconds;
    public bool HasBurst { get; set; }
    public bool IsComplete => HasBurst && AftermathTimeRemaining <= 0f;
    public float WarningProgress => Math.Clamp(
        1f - TimeUntilBurst / WarningSeconds, 0f, 1f);
    public float AftermathProgress => Math.Clamp(
        1f - AftermathTimeRemaining / AftermathSeconds, 0f, 1f);

    public RottenCorpseBurst(Vector2 position)
    {
        Position = position;
    }
}

public sealed class CurseSystem
{
    public const float Maximum=100f, PassiveDecayPerSecond=4f, ShrineDecayPerSecond=28f, PeriodicDamageInterval=3f;
    private float _damageClock=PeriodicDamageInterval;
    public float Value{get;private set;} public float Ratio=>Value/Maximum;
    public int Tier=>Value<=30?0:Value<=60?1:Value<=85?2:3;
    public float StaminaRegenerationMultiplier=>Tier switch{1=>.86f,2=>.72f,3=>.58f,_=>1f};
    public void Reset(){Value=0;_damageClock=PeriodicDamageInterval;}
    public void Reduce(float amount)=>Value=MathF.Max(0,Value-MathF.Max(0,amount));
    public void Add(float amount)=>Value=MathF.Min(Maximum,Value+MathF.Max(0,amount));
    public void Update(GameTime time,Player.Player player,DungeonMap map)
    {
        if(map==null||!map.IsAncientCatacombs){Reset();player.Combat.ExternalStaminaRegenMultiplier=1f;return;}
        float dt=MathF.Min((float)time.ElapsedGameTime.TotalSeconds,.05f),gain=0;
        foreach(var z in map.CurseZones)if(z.Bounds.Contains(player.Position.ToPoint()))gain=MathF.Max(gain,z.GainPerSecond);
        bool shrine=false;foreach(var s in map.SafeShrines)if(Vector2.DistanceSquared(player.Position,s.Position)<=s.Radius*s.Radius){shrine=true;break;}
        if(shrine)Reduce(ShrineDecayPerSecond*dt);else if(gain>0)Add(gain*dt);else Reduce(PassiveDecayPerSecond*dt);
        player.Combat.ExternalStaminaRegenMultiplier=StaminaRegenerationMultiplier;
        if(Tier==3){_damageClock-=dt;if(_damageClock<=0){player.ReceiveDamage(Math.Max(1,player.MaxHealth/50));_damageClock=PeriodicDamageInterval;}}else _damageClock=PeriodicDamageInterval;
    }
}

public sealed class TombInteractionManager
{
    public const float TelegraphSeconds=.85f;
    private readonly Dictionary<string,TombRuntime> _states=new();
    public IReadOnlyDictionary<string,TombRuntime> States=>_states;
    public Vector2? LootOpenedPosition{get;private set;}
    public void Reset(DungeonMap map){_states.Clear();if(map==null)return;foreach(var t in map.Tombs)_states[t.Id]=new TombRuntime(t);}
    public void Update(GameTime time,Player.Player player,DungeonMap map,EnemyManager enemies,int level,int tier)
    {
        LootOpenedPosition=null;if(map==null||!map.IsAncientCatacombs)return;float dt=MathF.Min((float)time.ElapsedGameTime.TotalSeconds,.05f);
        foreach(var r in _states.Values){if(r.State is TombState.Opened or TombState.Empty or TombState.Destroyed)continue;Rectangle trigger=r.Definition.Bounds;trigger.Inflate(90,55);if(!r.Telegraphing&&!trigger.Contains(player.Position.ToPoint()))continue;if(!r.Telegraphing){r.Telegraphing=true;r.State=TombState.Disturbed;r.Remaining=TelegraphSeconds;}r.Remaining-=dt;if(r.Remaining>0)continue;r.Telegraphing=false;if(r.Definition.Outcome==TombOutcome.Skeleton){var room=map.FindRoomContaining(r.Definition.Bounds.Center.ToVector2());var socket=new SpawnSocket(r.Definition.Id+"-spawn",r.Definition.Id,SpawnSocketRole.Coffin,new Vector2(r.Definition.Bounds.Center.X,r.Definition.Bounds.Bottom),new Rectangle(r.Definition.Bounds.X-50,r.Definition.Bounds.Y-90,r.Definition.Bounds.Width+100,140),new[]{EnemyType.Skeleton});enemies.SpawnAuthoredEnemy(EnemyType.Skeleton,socket,room,map,level,tier);r.State=TombState.Opened;}else{r.State=r.Definition.Outcome==TombOutcome.Empty?TombState.Empty:TombState.Opened;if(r.Definition.Outcome==TombOutcome.Loot)LootOpenedPosition=r.Definition.Bounds.Center.ToVector2();}}
    }
}
public sealed class TombRuntime
{
    public TombInteractionDefinition Definition{get;} public TombState State{get;set;}=TombState.Sealed; public bool Telegraphing{get;set;} public float Remaining{get;set;}
    public TombRuntime(TombInteractionDefinition definition){Definition=definition;State=definition.Outcome==TombOutcome.Corpse?TombState.Cracked:TombState.Sealed;}
}
