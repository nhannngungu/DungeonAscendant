using System;
using DungeonAscendant.Combat;
using DungeonAscendant.Dungeon;
using DungeonAscendant.Progression;
using Microsoft.Xna.Framework;
using PlayerCharacter=DungeonAscendant.Player.Player;

namespace DungeonAscendant.Enemies;

/// <summary>Data-driven Catacomb combatant with role-specific state.</summary>
public sealed class CatacombEnemy : Enemy
{
    private float _specialCooldown;
    private float _telegraphRemaining;
    private float _spectralClock=2.2f;
    public bool IsEthereal { get; private set; }
    public int CombatPhase => Type==EnemyType.FallenKnight
        ? CurrentHealth*100>MaxHealth*65?1:CurrentHealth*100>MaxHealth*30?2:3 : 1;
    public override bool IsFlying => Type==EnemyType.GraveBat;
    public override bool CanBeTargeted => base.CanBeTargeted && !IsEthereal;
    public bool IsSpecialTelegraphing=>_telegraphRemaining>0f;
    public Rectangle SpecialAttackArea=>new((int)Position.X-(CombatPhase==3?260:150),(int)Position.Y-55,CombatPhase==3?520:300,110);

    public CatacombEnemy(EnemyType type,Vector2 position,int level,int worldTier,int roomId,bool elite)
        :base(type,position,SizeFor(type),SpeedFor(type),620f,HealthFor(type,level,worldTier),DamageFor(type,level,worldTier),RewardFor(type),level,worldTier,elite,roomId,RangeFor(type),CooldownFor(type)){}

    protected override void UpdateBehavior(GameTime gameTime,PlayerCharacter player,DungeonMap dungeon,ProjectileManager projectiles,RootHazardManager rootHazards,EnemyManager enemies)
    {
        float dt=MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds,.05f);
        _specialCooldown=MathF.Max(0f,_specialCooldown-dt);
        if(Type==EnemyType.FallenKnight&&CombatPhase>=2)
        {
            if(_telegraphRemaining>0f){_telegraphRemaining-=dt;if(_telegraphRemaining<=0f&&SpecialAttackArea.Intersects(player.Bounds))player.ReceiveMeleeAttack(new AttackContact(EffectiveAttackDamage+(CombatPhase==3?14:5),Position,true,false,SpecialAttackArea));return;}
            if(_specialCooldown<=0f){_telegraphRemaining=CombatPhase==3?.95f:.58f;_specialCooldown=CombatPhase==3?5.6f:4.2f;return;}
        }
        if(Type==EnemyType.Wraith){_spectralClock-=dt;if(_spectralClock<=0f){IsEthereal=!IsEthereal;_spectralClock=IsEthereal?1.0f:2.4f;}if(IsEthereal){MoveWithinRoom(new Vector2(MathF.Sign(player.Position.X-Position.X)*MovementSpeed*.55f*dt,MathF.Sin(_spectralClock*7f)*18f*dt),dungeon);return;}}
        if(Type==EnemyType.GraveBat){MoveWithinRoom(Vector2.Normalize(player.Position-Position)*MovementSpeed*dt,dungeon);if(Vector2.DistanceSquared(Position,player.Position)<=Attack.Range*Attack.Range)TryMeleeAttack(player);return;}
        if(Type is EnemyType.SkeletonArcher or EnemyType.SoulCollector)
        {
            float distance=MathF.Abs(player.Position.X-Position.X);
            if(distance<115f)MoveAway(gameTime,player.Position,dungeon,.8f);
            else if(distance>460f)MoveToward(gameTime,player.Position,380f,dungeon,.45f);
            if(_specialCooldown<=0f&&distance<=520f){projectiles.SpawnArrow(Position+new Vector2(0,-10),player.Position-Position,EffectiveAttackDamage,RoomId);_specialCooldown=Type==EnemyType.SoulCollector?1.55f:1.8f;}
            return;
        }
        float stop=Attack.Range*.8f;
        if(Vector2.DistanceSquared(Position,player.Position)<=Attack.Range*Attack.Range)TryMeleeAttack(player);
        else MoveToward(gameTime,player.Position,stop,dungeon,Type==EnemyType.FallenKnight&&CombatPhase==3?1.25f:1f);
    }

    private static Vector2 SizeFor(EnemyType t)=>t switch{EnemyType.GraveBat=>new(44,30),EnemyType.RottenCorpse=>new(58,68),EnemyType.UndeadGuard=>new(48,62),EnemyType.CursedKnight=>new(54,70),EnemyType.DeathKnight=>new(62,78),EnemyType.SoulCollector=>new(50,68),EnemyType.FallenKnight=>new(68,86),_=>new(40,54)};
    private static float SpeedFor(EnemyType t)=>t switch{EnemyType.GraveBat=>145f,EnemyType.Wraith=>125f,EnemyType.RottenCorpse=>55f,EnemyType.UndeadGuard=>68f,EnemyType.CursedKnight=>82f,EnemyType.DeathKnight=>76f,EnemyType.FallenKnight=>96f,EnemyType.SkeletonArcher=>48f,_=>88f};
    private static int HealthFor(EnemyType t,int l,int wt){int b=t switch{EnemyType.Skeleton=>95,EnemyType.SkeletonArcher=>80,EnemyType.GraveBat=>65,EnemyType.Wraith=>110,EnemyType.RottenCorpse=>175,EnemyType.UndeadGuard=>190,EnemyType.CursedKnight=>420,EnemyType.SoulCollector=>460,EnemyType.DeathKnight=>650,EnemyType.FallenKnight=>1500,_=>100};return WorldProgression.ApplyPercent(b+(Math.Max(1,l)-1)*20,WorldProgression.GetHealthMultiplierPercent(wt));}
    private static int DamageFor(EnemyType t,int l,int wt){int b=t switch{EnemyType.RottenCorpse=>18,EnemyType.CursedKnight=>24,EnemyType.SoulCollector=>20,EnemyType.DeathKnight=>28,EnemyType.FallenKnight=>32,_=>12};return WorldProgression.ApplyPercent(b+(Math.Max(1,l)-1)*2,WorldProgression.GetDamageMultiplierPercent(wt));}
    private static int RewardFor(EnemyType t)=>t switch{EnemyType.CursedKnight=>240,EnemyType.SoulCollector=>280,EnemyType.DeathKnight=>400,EnemyType.FallenKnight=>1000,_=>75};
    private static float RangeFor(EnemyType t)=>t switch{EnemyType.FallenKnight=>105f,EnemyType.DeathKnight=>96f,EnemyType.CursedKnight=>88f,EnemyType.UndeadGuard=>76f,EnemyType.RottenCorpse=>70f,_=>55f};
    private static float CooldownFor(EnemyType t)=>t switch{EnemyType.FallenKnight=>1.05f,EnemyType.DeathKnight=>1.2f,EnemyType.CursedKnight=>1.3f,_=>1.5f};
}
