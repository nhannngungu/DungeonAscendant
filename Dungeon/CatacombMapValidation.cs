using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using DungeonAscendant.Core;

namespace DungeonAscendant.Dungeon;

public static class CatacombMapValidation
{
    public static void ValidateOrThrow()
    {
        DungeonMap map=new CatacombMapGenerator().Generate();
        Need(map.IsAncientCatacombs&&map.CatacombZones.Count==10,"Map 2 needs ten Catacomb zones.");
        Need(map.WorldBounds.Width==CatacombMapGenerator.WorldWidth&&map.StartRoom!=null&&map.BossRoom!=null&&map.ExitRoom==map.BossRoom,"Map 2 world/start/boss/exit contract failed.");
        Need(map.EncounterZones.Count==10&&map.SpawnProfiles.Count==10&&map.SpawnSockets.Count>=24,"Map 2 encounter ecology is incomplete.");
        Need(map.CurseZones.Count==6&&map.SafeShrines.Count==3&&map.Tombs.Count==8,"Curse, shrine, or tomb metadata is incomplete.");
        ValidateGround(map);ValidatePhysicalTraversal(map);ValidateRoster(map);ValidateSpawns(map);ValidateCurseAndTombs(map);ValidatePhases();
        DungeonAscendant.Core.CatacombEnemyPolishValidation.ValidateOrThrow();
        DungeonAscendant.Core.AncientScholarTombValidation.ValidateOrThrow();
        ValidateProgression();
        for(int seed=0;seed<100;seed++){DungeonMap other=new CatacombMapGenerator().Generate();Need(other.WorldBounds==map.WorldBounds&&other.Platforms.Count==map.Platforms.Count,"Authored Catacomb macro changed unexpectedly.");}
    }
    private static void ValidateGround(DungeonMap map){int? prior=null;for(int x=map.WorldBounds.Left+4;x<map.WorldBounds.Right-4;x+=12){int top=int.MaxValue;foreach(var p in map.Platforms)if((p.Kind is PlatformKind.Ground or PlatformKind.Transition)&&x>=p.Bounds.Left&&x<p.Bounds.Right)top=Math.Min(top,p.Bounds.Top);Need(top!=int.MaxValue,$"Catacomb main route gap near {x}.");if(prior.HasValue)Need(Math.Abs(top-prior.Value)<=32,$"Catacomb main route step near {x} is too high.");prior=top;}Need(map.RouteFeatures.Count>=8&&Exists(map.RouteFeatures,"soul-chapel-branch"),"Soul Chapel branch/shortcuts/secrets are incomplete.");}
    private static void ValidatePhysicalTraversal(DungeonMap map)
    {
        float theoreticalSameHeight=Player.Player.DefaultMovementSpeed*(2f*MathF.Abs(Player.Player.JumpVelocity)/Player.Player.Gravity)-Player.Player.BodyHurtboxWidth;
        Need(theoreticalSameHeight>140f&&theoreticalSameHeight<160f,"Player jump envelope changed; review Catacomb traversal.");
        int missingStart=-1;int blockedStandingStart=-1;int priorTop=int.MaxValue;
        int halfPlayerWidth=(int)MathF.Ceiling(Player.Player.BodyHurtboxWidth/2f);
        for(int x=map.WorldBounds.Left+halfPlayerWidth;x<=map.WorldBounds.Right-halfPlayerWidth;x+=4)
        {
            int top=FindMandatoryTop(map,x);
            if(top==int.MaxValue){if(missingStart<0)missingStart=x;continue;}
            if(missingStart>=0){int gap=x-missingStart;Need(gap<=ConservativeGap(priorTop,top),$"Mandatory gap too wide near X={missingStart}: {gap}px.");missingStart=-1;}
            if(priorTop!=int.MaxValue&&top<priorTop){float rise=priorTop-top;Need(rise<=ConservativeJumpHeight(),$"Mandatory upward step too high near X={x}: {rise}px.");}
            bool hasClearance=FindStandableTop(map,x)!=int.MaxValue;
            if(!hasClearance){if(blockedStandingStart<0)blockedStandingStart=x;}
            else if(blockedStandingStart>=0){Need(x-blockedStandingStart<=Player.Player.BodyHurtboxWidth+4f,$"Player collider has no usable standing clearance from X={blockedStandingStart} to X={x}.");blockedStandingStart=-1;}
            priorTop=top;
        }
        Need(missingStart<0,"Mandatory route ends in an unsupported gap.");
        Need(blockedStandingStart<0,"Mandatory route ends without player standing clearance.");

        DungeonRoom gallery=map.Rooms[2];
        ValidateJump(map,new Rectangle(gallery.Bounds.Left+200,gallery.GroundY-48,120,16),new Rectangle(gallery.Bounds.Left+360,gallery.GroundY-92,230,16),"Archer Gallery lower stair");
        ValidateJump(map,new Rectangle(gallery.Bounds.Left+360,gallery.GroundY-92,230,16),new Rectangle(gallery.Bounds.Left+650,gallery.GroundY-104,140,16),"Archer Gallery collapsed slab");
        ValidateJump(map,new Rectangle(gallery.Bounds.Left+650,gallery.GroundY-104,140,16),new Rectangle(gallery.Bounds.Left+820,gallery.GroundY-118,240,16),"Archer Gallery upper landing");
        DungeonRoom chapel=map.Rooms[7];
        Rectangle[] soul={new(chapel.Bounds.Left+180,chapel.GroundY-70,190,16),new(chapel.Bounds.Left+390,chapel.GroundY-136,210,16),new(chapel.Bounds.Left+620,chapel.GroundY-190,330,16),new(chapel.Bounds.Left+970,chapel.GroundY-120,170,16)};
        for(int i=0;i<soul.Length-1;i++)ValidateJump(map,soul[i],soul[i+1],$"Soul Chapel branch {i+1}");
        for(int i=soul.Length-1;i>0;i--)ValidateJump(map,soul[i],soul[i-1],$"Soul Chapel return {i}");
    }
    private static int FindMandatoryTop(DungeonMap map,int x){int top=int.MaxValue;foreach(var p in map.Platforms)if((p.Kind is PlatformKind.Ground or PlatformKind.Transition)&&x>=p.Bounds.Left&&x<p.Bounds.Right)top=Math.Min(top,p.Bounds.Top);return top;}
    private static int FindStandableTop(DungeonMap map,int x)
    {
        Vector2 playerSize=new(Player.Player.BodyHurtboxWidth,Player.Player.BodyHurtboxHeight);
        int standable=int.MaxValue;
        foreach(var p in map.Platforms)
        {
            if(x<p.Bounds.Left||x>=p.Bounds.Right)continue;
            Vector2 standing=new(x,p.Bounds.Top-Player.Player.BodyHurtboxHeight/2f);
            if(SideScrollingCollision.IsPositionFree(standing,playerSize,map))standable=Math.Min(standable,p.Bounds.Top);
        }
        return standable;
    }
    private static float ConservativeJumpHeight()=>Player.Player.JumpVelocity*Player.Player.JumpVelocity/(2f*Player.Player.Gravity)*.72f;
    private static float ConservativeGap(float fromTop,float toTop){float delta=toTop-fromTop;float discriminant=Player.Player.JumpVelocity*Player.Player.JumpVelocity+2f*Player.Player.Gravity*delta;if(discriminant<0)return-1f;float time=(-Player.Player.JumpVelocity+MathF.Sqrt(discriminant))/Player.Player.Gravity;return MathF.Max(0f,Player.Player.DefaultMovementSpeed*time*.70f-Player.Player.BodyHurtboxWidth);}
    private static void ValidateJump(DungeonMap map,Rectangle from,Rectangle to,string label){float gap=to.Left>=from.Right?to.Left-from.Right:from.Left>=to.Right?from.Left-to.Right:0f;float allowed=ConservativeGap(from.Top,to.Top);Need(gap<=allowed,$"{label}: gap too wide ({gap}px > {allowed:0}px).");Need(to.Width>=Player.Player.BodyHurtboxWidth*2f,$"{label}: landing area too narrow.");Need(CanTraverseJump(map,from,to),$"{label}: real Player A/D + Space simulation could not land safely.");}
    private static bool CanTraverseJump(DungeonMap map,Rectangle from,Rectangle to)
    {
        bool right=to.Center.X>from.Center.X;
        float halfWidth=Player.Player.BodyHurtboxWidth/2f;
        float startX=right?from.Right-halfWidth-4f:from.Left+halfWidth+4f;
        var player=new Player.Player(new Vector2(startX,from.Top-Player.Player.BodyHurtboxHeight/2f));
        var frame=new GameTime(TimeSpan.Zero,TimeSpan.FromSeconds(1f/60f));
        player.UpdateSideScrollingMovement(frame,new KeyboardState(),false,map);
        if(!player.IsGrounded)return false;
        for(int i=0;i<150;i++)
        {
            bool keepMoving=right?player.Position.X<to.Center.X:player.Position.X>to.Center.X;
            KeyboardState movement=keepMoving?new KeyboardState(right?Keys.D:Keys.A):new KeyboardState();
            player.UpdateSideScrollingMovement(frame,movement,i==0,map);
            float feet=player.Position.Y+Player.Player.BodyHurtboxHeight/2f;
            if(i>0&&player.IsGrounded&&player.Position.X>=to.Left+halfWidth&&player.Position.X<=to.Right-halfWidth&&MathF.Abs(feet-to.Top)<=2f)return true;
        }
        return false;
    }
    private static void ValidateRoster(DungeonMap map){var types=new HashSet<EnemyType>();foreach(var z in map.EncounterZones)foreach(var t in z.SuggestedEnemyTypes)types.Add(t);EnemyType[] need={EnemyType.Skeleton,EnemyType.SkeletonArcher,EnemyType.RottenCorpse,EnemyType.Wraith,EnemyType.UndeadGuard,EnemyType.GraveBat,EnemyType.CursedKnight,EnemyType.SoulCollector,EnemyType.DeathKnight,EnemyType.FallenKnight};foreach(var t in need)Need(types.Contains(t),$"Map 2 omits {t}.");}
    private static void ValidateSpawns(DungeonMap map){foreach(var zone in map.EncounterZones){var manager=new EnemyManager(42);manager.Reset(map,2,1,RegionType.WildForest,suppressProceduralSpawns:true);var director=new WildForestEncounterDirector();director.Reset(map);Vector2 player=new(zone.ActivationBounds.Left+3,zone.ActivationBounds.Center.Y);for(int f=0;f<24;f++)director.Update(Frame(),player,map,manager,2,1);Need(director.IsTriggered(zone.ZoneId),$"{zone.ZoneId} has no safe architecture socket.");Need(manager.Enemies.Count>0&&manager.Enemies.Count<=zone.MaxConcurrentEnemies,$"{zone.ZoneId} violates its cap.");foreach(var e in manager.Enemies){Need(SideScrollingCollision.IsPositionFree(e.Position,e.Size,map),$"{e.Type} spawned inside Catacomb collision.");if(!e.IsFlying)Need(SideScrollingCollision.IsSupported(e.Position,e.Size,map),$"{e.Type} spawned unsupported.");}}}
    private static void ValidateCurseAndTombs(DungeonMap map){var player=new Player.Player(map.CurseZones[0].Bounds.Center.ToVector2());var curse=new CurseSystem();for(int i=0;i<200;i++)curse.Update(Frame(),player,map);Need(curse.Value>30&&curse.StaminaRegenerationMultiplier<1f,"Curse exposure threshold failed.");float before=curse.Value;player.MoveTo(map.SafeShrines[0].Position);for(int i=0;i<80;i++)curse.Update(Frame(),player,map);Need(curse.Value<before,"Safe Shrine did not reduce Curse.");var tombs=new TombInteractionManager();tombs.Reset(map);Need(tombs.States.Count==8,"Uncertain Tomb states were not initialized.");}
    private static void ValidatePhases(){var boss=new CatacombEnemy(EnemyType.FallenKnight,Vector2.Zero,1,1,9,true);Need(boss.CombatPhase==1,"Fallen Knight phase one missing.");boss.ReceiveDamage(boss.MaxHealth*36/100);Need(boss.CombatPhase==2,"Fallen Knight phase two threshold failed.");boss.ReceiveDamage(boss.MaxHealth*36/100);Need(boss.CombatPhase==3,"Fallen Knight phase three threshold failed.");}
    private static void ValidateProgression()
    {
        var session=new GameSession(new Rectangle(0,0,1280,720),1701);
        session.Update(Frame(),new KeyboardState(Keys.Enter),new MouseState());
        SettleEntry(session);
        session.Boss.ReceiveDamage(int.MaxValue);
        session.Update(Frame(),new KeyboardState(),new MouseState());
        Need(session.BossDefeated,"Map 1 boss defeat hook failed.");
        session.Player.MoveTo(session.ExitPosition);
        session.Update(Frame(),new KeyboardState(Keys.E),new MouseState());
        Need(session.DungeonDepth==2&&session.CurrentDungeon.IsAncientCatacombs,"Map 1 exit did not load Map 2.");
        SettleEntry(session);
        EncounterZone final=session.CurrentDungeon.EncounterZones[^1];
        session.Player.MoveTo(new Vector2(final.ActivationBounds.Left+4,session.CurrentDungeon.BossRoom.GroundY-28));
        for(int i=0;i<24;i++)session.Update(Frame(),new KeyboardState(),new MouseState());
        Enemy fallen=null;
        foreach(Enemy e in session.Enemies.Enemies)if(e.Type==EnemyType.FallenKnight)fallen=e;
        Need(fallen!=null,"Fallen Knight did not awaken in the Fallen Hall.");
        fallen.ReceiveDamage(int.MaxValue);
        session.Update(Frame(),new KeyboardState(),new MouseState());
        Need(session.Map02Cleared&&session.BossDefeated,"Map 2 completion hook failed.");
    }
    private static void SettleEntry(GameSession session){for(int i=0;i<40&&session.IsMapEntryFallActive;i++)session.Update(Frame(),new KeyboardState(),new MouseState());Need(!session.IsMapEntryFallActive&&session.Player.IsGrounded,"Map entry fall did not settle.");}
    private static bool Exists(IReadOnlyList<WildForestRouteFeature> f,string id){foreach(var x in f)if(x.FeatureId==id)return true;return false;}
    private static GameTime Frame()=>new(TimeSpan.Zero,TimeSpan.FromSeconds(.05));
    private static void Need(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
}
