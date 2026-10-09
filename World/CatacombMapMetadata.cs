using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DungeonAscendant.World;

public enum CatacombZoneKind
{
    TombEntrance, OssuaryCorridors, ArcherGalleries, RotPits,
    WraithHalls, GuardBarracks, CursedKnightMausoleum,
    SoulChapel, DeathKnightWarTomb, FallenHall
}

public enum TombState { Sealed, Cracked, Opened, Empty, Disturbed, Destroyed }
public enum TombOutcome { Empty, Skeleton, Loot, Corpse, Nothing }

public sealed class CatacombZoneDefinition
{
    public string ZoneId { get; }
    public string Name { get; }
    public CatacombZoneKind Kind { get; }
    public Rectangle Bounds { get; }
    public int GroundY { get; }
    public float CursePressure { get; }
    public CatacombZoneDefinition(string id, string name, CatacombZoneKind kind,
        Rectangle bounds, int groundY, float cursePressure)
    { ZoneId=id; Name=name; Kind=kind; Bounds=bounds; GroundY=groundY; CursePressure=Math.Clamp(cursePressure,0f,1f); }
}

public sealed class CurseZoneDefinition
{
    public string Id { get; }
    public Rectangle Bounds { get; }
    public float GainPerSecond { get; }
    public CurseZoneDefinition(string id, Rectangle bounds, float gain)
    { Id=id; Bounds=bounds; GainPerSecond=Math.Max(0f,gain); }
}

public sealed class SafeShrineDefinition
{
    public string Id { get; }
    public Vector2 Position { get; }
    public float Radius { get; }
    public SafeShrineDefinition(string id, Vector2 position, float radius=110f)
    { Id=id; Position=position; Radius=Math.Max(48f,radius); }
}

public sealed class TombInteractionDefinition
{
    public string Id { get; }
    public string ZoneId { get; }
    public Rectangle Bounds { get; }
    public TombOutcome Outcome { get; }
    public TombInteractionDefinition(string id,string zoneId,Rectangle bounds,TombOutcome outcome)
    { Id=id; ZoneId=zoneId; Bounds=bounds; Outcome=outcome; }
}
