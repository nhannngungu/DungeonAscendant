using System;
using System.Collections.Generic;
using DungeonAscendant.Enemies;
using DungeonAscendant.World;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.Dungeon;

public sealed class CatacombMapGenerator
{
    public const int ZoneCount = 10;
    public const int WorldWidth = 12976;
    private const int Margin = 80;
    private const int Transition = 64;

    public DungeonMap Generate()
    {
        int[] widths={1000,1150,1250,1100,1200,1150,1250,1200,1400,1700};
        int[] ground={676,684,676,688,680,676,680,680,680,680};
        string[] names={"TOMB ENTRANCE","OSSUARY CORRIDORS","ARCHER GALLERIES","ROT PITS","WRAITH HALLS","GUARD BARRACKS","CURSED KNIGHT MAUSOLEUM","SOUL CHAPEL","DEATH KNIGHT WAR TOMB","THE FALLEN HALL"};
        CatacombZoneKind[] kinds=(CatacombZoneKind[])Enum.GetValues(typeof(CatacombZoneKind));
        float[] curse={.05f,.12f,.10f,.38f,.48f,.18f,.62f,.82f,.58f,.35f};
        var rooms=new List<DungeonRoom>(ZoneCount); var corridors=new List<Rectangle>(9);
        var zones=new List<CatacombZoneDefinition>(ZoneCount); int x=Margin;
        for(int i=0;i<ZoneCount;i++)
        {
            RoomType type=i==0?RoomType.Start:i==7?RoomType.Treasure:i==9?RoomType.Boss:RoomType.Enemy;
            var room=new DungeonRoom(i,new Rectangle(x,Margin,widths[i],DungeonGenerator.RoomHeight),type){GroundY=ground[i]};
            AddGeometry(room,kinds[i]); rooms.Add(room);
            zones.Add(new CatacombZoneDefinition($"catacomb-{i+1:00}",names[i],kinds[i],room.Bounds,ground[i],curse[i]));
            if(i>0){var prev=rooms[i-1];prev.ConnectTo(room.Id);room.ConnectTo(prev.Id);var c=new Rectangle(prev.Bounds.Right,Margin,Transition,DungeonGenerator.RoomHeight);corridors.Add(c);AddTransition(prev,c,prev.GroundY,room.GroundY);}
            x+=widths[i]+(i<9?Transition:0);
        }
        var encounters=new List<EncounterZone>(); var sockets=new List<SpawnSocket>();
        AddEncounters(zones,encounters,sockets);
        var profiles=CreateProfiles(zones); var features=CreateFeatures(zones);
        var curseZones=CreateCurseZones(zones); var shrines=CreateShrines(zones); var tombs=CreateTombs(zones);
        DungeonRoom arena=rooms[9];
        return new DungeonMap(rooms,corridors,new Rectangle(Margin,Margin,WorldWidth,DungeonGenerator.RoomHeight),
            isAncientCatacombs:true, encounterZones:encounters, spawnSockets:sockets,
            routeFeatures:features, spawnProfiles:profiles, catacombZones:zones,
            curseZones:curseZones,safeShrines:shrines,tombs:tombs,
            exitRoomOverride:arena,authoredExitPosition:new Vector2(arena.Bounds.Right-120,arena.GroundY-28));
    }

    private static void AddGeometry(DungeonRoom r,CatacombZoneKind k)
    {
        int l=r.Bounds.Left,w=r.Bounds.Width,g=r.GroundY;
        Ground(r,l,w,g);
        if(k==CatacombZoneKind.TombEntrance){Ground(r,l,260,g-24);Ground(r,l+260,250,g-16);}
        if(k==CatacombZoneKind.ArcherGalleries){Raised(r,l+200,g-48,120);Raised(r,l+360,g-92,230);Raised(r,l+650,g-104,140);Raised(r,l+820,g-118,240);}
        if(k==CatacombZoneKind.RotPits){Raised(r,l+430,g-62,220);}
        if(k==CatacombZoneKind.WraithHalls){Raised(r,l+760,g-88,240);}
        if(k==CatacombZoneKind.GuardBarracks){Raised(r,l+210,g-70,210);Raised(r,l+850,g-70,210);}
        if(k==CatacombZoneKind.SoulChapel){Raised(r,l+180,g-70,190);Raised(r,l+390,g-136,210);Raised(r,l+620,g-190,330);Raised(r,l+970,g-120,170);}
        if(k==CatacombZoneKind.CursedKnightMausoleum||k==CatacombZoneKind.DeathKnightWarTomb||k==CatacombZoneKind.FallenHall){Raised(r,l+170,g-42,170);Raised(r,l+w-340,g-42,170);}
    }
    private static void Ground(DungeonRoom r,int x,int w,int y)=>r.AddPlatform(new Platform(new Rectangle(x,y,w,r.Bounds.Bottom-y),PlatformKind.Ground,r.Id));
    private static void Raised(DungeonRoom r,int x,int y,int w)=>r.AddPlatform(new Platform(new Rectangle(x,y,w,16),PlatformKind.Raised,r.Id));
    private static void AddTransition(DungeonRoom r,Rectangle c,int a,int b){int m=c.Width/2;int y1=(int)MathF.Round(MathHelper.Lerp(a,b,.33f));int y2=(int)MathF.Round(MathHelper.Lerp(a,b,.67f));Ground(r,c.Left,m,y1);Ground(r,c.Left+m,c.Width-m,y2);}

    private static void AddEncounters(IReadOnlyList<CatacombZoneDefinition> z,List<EncounterZone> e,List<SpawnSocket> s)
    {
        Add(e,s,z[0],"entrance-dead",160,680,EnemyTheme.CatacombUndead,EncounterDifficulty.Low,2,(SpawnSocketRole.Coffin,520,-1,EnemyType.Skeleton),(SpawnSocketRole.Flying,820,-170,EnemyType.GraveBat));
        Add(e,s,z[1],"ossuary-rising",140,850,EnemyTheme.CatacombUndead,EncounterDifficulty.Standard,3,(SpawnSocketRole.BonePile,390,-1,EnemyType.Skeleton),(SpawnSocketRole.BurialPit,680,-1,EnemyType.RottenCorpse),(SpawnSocketRole.Coffin,940,-1,EnemyType.Skeleton));
        Add(e,s,z[2],"archer-gallery",100,1050,EnemyTheme.CatacombGuard,EncounterDifficulty.Hard,4,(SpawnSocketRole.GuardPost,330,-1,EnemyType.UndeadGuard),(SpawnSocketRole.Ranged,470,-92,EnemyType.SkeletonArcher),(SpawnSocketRole.Ranged,930,-118,EnemyType.SkeletonArcher),(SpawnSocketRole.BonePile,1080,-1,EnemyType.Skeleton));
        Add(e,s,z[3],"rot-pit",130,820,EnemyTheme.CatacombRot,EncounterDifficulty.Hard,3,(SpawnSocketRole.BurialPit,380,-1,EnemyType.RottenCorpse),(SpawnSocketRole.BonePile,690,-1,EnemyType.Skeleton),(SpawnSocketRole.Flying,890,-180,EnemyType.GraveBat));
        Add(e,s,z[4],"wraith-hall",160,850,EnemyTheme.CatacombSpirits,EncounterDifficulty.Hard,3,(SpawnSocketRole.Spectral,470,-1,EnemyType.Wraith),(SpawnSocketRole.Flying,720,-190,EnemyType.GraveBat),(SpawnSocketRole.Spectral,920,-1,EnemyType.Wraith));
        Add(e,s,z[5],"guard-barracks",120,900,EnemyTheme.CatacombGuard,EncounterDifficulty.Hard,4,(SpawnSocketRole.GuardPost,360,-1,EnemyType.UndeadGuard),(SpawnSocketRole.Ranged,900,-70,EnemyType.SkeletonArcher),(SpawnSocketRole.GuardPost,1040,-1,EnemyType.UndeadGuard));
        Add(e,s,z[6],"cursed-knight-duel",170,900,EnemyTheme.CatacombElite,EncounterDifficulty.Elite,1,(SpawnSocketRole.Elite,690,-1,EnemyType.CursedKnight));
        Add(e,s,z[7],"soul-chapel",330,650,EnemyTheme.CatacombSpirits,EncounterDifficulty.Elite,3,(SpawnSocketRole.Spectral,720,-190,EnemyType.SoulCollector),(SpawnSocketRole.Spectral,910,-190,EnemyType.Wraith),(SpawnSocketRole.Flying,1050,-170,EnemyType.GraveBat));
        var chapel=e[^1];e[^1]=new EncounterZone(chapel.ZoneId,chapel.SectionId,new Rectangle(z[7].Bounds.Left+330,z[7].Bounds.Top+280,650,270),chapel.GroundBounds,chapel.Theme,chapel.Difficulty,new[]{EnemyType.SoulCollector,EnemyType.Wraith,EnemyType.GraveBat},3,true,false,false,true);
        Add(e,s,z[8],"death-knight-tomb",160,1050,EnemyTheme.CatacombElite,EncounterDifficulty.Elite,2,(SpawnSocketRole.Elite,760,-1,EnemyType.DeathKnight),(SpawnSocketRole.GuardPost,1060,-1,EnemyType.UndeadGuard));
        Add(e,s,z[9],"fallen-knight",120,1460,EnemyTheme.FallenKnight,EncounterDifficulty.Boss,1,(SpawnSocketRole.Boss,900,-1,EnemyType.FallenKnight));
    }
    private static void Add(List<EncounterZone> e,List<SpawnSocket> s,CatacombZoneDefinition z,string id,int ox,int width,EnemyTheme theme,EncounterDifficulty diff,int cap,params (SpawnSocketRole role,int x,int y,EnemyType type)[] data)
    {var a=new Rectangle(z.Bounds.Left+ox,z.Bounds.Top+90,width,z.Bounds.Height-160);e.Add(new EncounterZone(id,z.ZoneId,a,new Rectangle(a.Left,z.GroundY-100,a.Width,100),theme,diff,Array.ConvertAll(data,d=>d.type),cap,diff==EncounterDifficulty.Elite,diff==EncounterDifficulty.Boss,false,true));int i=0;foreach(var d in data){float y=d.y<0?z.GroundY+d.y:z.Bounds.Top+d.y;var p=new Vector2(z.Bounds.Left+d.x,y);bool air=d.role==SpawnSocketRole.Flying;s.Add(new SpawnSocket($"{id}-{++i:00}",id,d.role,p,new Rectangle((int)p.X-80,(int)p.Y-100,160,130),new[]{d.type},true));}}
    private static List<WildForestSpawnProfile> CreateProfiles(IReadOnlyList<CatacombZoneDefinition> z){var r=new List<WildForestSpawnProfile>();EnemyType[][] t={new[]{EnemyType.Skeleton,EnemyType.GraveBat},new[]{EnemyType.Skeleton,EnemyType.RottenCorpse},new[]{EnemyType.SkeletonArcher,EnemyType.Skeleton,EnemyType.UndeadGuard},new[]{EnemyType.RottenCorpse,EnemyType.Skeleton,EnemyType.GraveBat},new[]{EnemyType.Wraith,EnemyType.GraveBat},new[]{EnemyType.UndeadGuard,EnemyType.SkeletonArcher},new[]{EnemyType.CursedKnight},new[]{EnemyType.SoulCollector,EnemyType.Wraith,EnemyType.GraveBat},new[]{EnemyType.DeathKnight,EnemyType.UndeadGuard},new[]{EnemyType.FallenKnight}};for(int i=0;i<10;i++){int[] w=new int[t[i].Length];for(int j=0;j<w.Length;j++)w[j]=100/w.Length;w[0]+=100-ArraySum(w);r.Add(new WildForestSpawnProfile($"{z[i].ZoneId}-profile",z[i].ZoneId,t[i],w,i is 2 or 5?4:i==9?1:3,280f,true,Array.IndexOf(t[i],EnemyType.GraveBat)>=0,i>=6?520:260));}return r;}
    private static int ArraySum(int[] a){int n=0;foreach(int v in a)n+=v;return n;}
    private static List<WildForestRouteFeature> CreateFeatures(IReadOnlyList<CatacombZoneDefinition> z)=>new(){F("ossuary-side-crypt",z[1],WildForestRouteFeatureKind.SideBranch,520,-130,260,110),F("gallery-balcony",z[2],WildForestRouteFeatureKind.SideBranch,330,-150,760,140),F("soul-chapel-branch",z[7],WildForestRouteFeatureKind.SideBranch,170,-235,980,225),F("war-tomb-cache",z[8],WildForestRouteFeatureKind.SideBranch,1010,-120,220,110),F("chapel-reconnect",z[7],WildForestRouteFeatureKind.Shortcut,910,-130,220,120),F("barracks-pass",z[5],WildForestRouteFeatureKind.Shortcut,800,-100,280,90),F("bone-cache",z[1],WildForestRouteFeatureKind.Secret,790,-90,140,80),F("rot-hidden-tomb",z[3],WildForestRouteFeatureKind.Secret,650,-100,150,90),F("royal-reliquary",z[6],WildForestRouteFeatureKind.Secret,960,-100,150,90)};
    private static WildForestRouteFeature F(string id,CatacombZoneDefinition z,WildForestRouteFeatureKind k,int x,int y,int w,int h){var b=new Rectangle(z.Bounds.Left+x,z.GroundY+y,w,h);return new WildForestRouteFeature(id,z.ZoneId,k,b,new Vector2(b.Left,b.Bottom),new Vector2(b.Right,b.Bottom));}
    private static List<CurseZoneDefinition> CreateCurseZones(IReadOnlyList<CatacombZoneDefinition> z)=>new(){C("rot-miasma",z[3],180,650,7f),C("wraith-mist",z[4],220,700,8f),C("mausoleum-runes",z[6],310,620,10f),C("chapel-ritual",z[7],390,560,14f),C("war-tomb-curse",z[8],440,600,9f),C("fallen-seal",z[9],610,480,6f)};
    private static CurseZoneDefinition C(string id,CatacombZoneDefinition z,int x,int w,float gain)=>new(id,new Rectangle(z.Bounds.Left+x,z.GroundY-110,w,110),gain);
    private static List<SafeShrineDefinition> CreateShrines(IReadOnlyList<CatacombZoneDefinition> z)=>new(){new("entrance-shrine",new Vector2(z[0].Bounds.Left+830,z[0].GroundY)),new("wraith-shrine",new Vector2(z[4].Bounds.Right-150,z[4].GroundY)),new("war-shrine",new Vector2(z[8].Bounds.Left+170,z[8].GroundY))};
    private static List<TombInteractionDefinition> CreateTombs(IReadOnlyList<CatacombZoneDefinition> z){var r=new List<TombInteractionDefinition>();TombOutcome[] o={TombOutcome.Empty,TombOutcome.Skeleton,TombOutcome.Loot,TombOutcome.Corpse,TombOutcome.Nothing,TombOutcome.Skeleton,TombOutcome.Empty,TombOutcome.Loot};for(int i=0;i<o.Length;i++){var q=z[1+i%6];r.Add(new TombInteractionDefinition($"tomb-{i+1:00}",q.ZoneId,new Rectangle(q.Bounds.Left+150+(i%3)*260,q.GroundY-46,92,46),o[i]));}return r;}
}
