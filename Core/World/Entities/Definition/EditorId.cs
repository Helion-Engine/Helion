namespace Helion.World.Entities.Definition
{
    public static class EditorIds
    {
        public static bool IsPlayerStart(int? id) => id < 4;
    }

    public enum EditorId
    {
        DeathmatchStart = 11,
        TeleportLanding = 14,
        PointPusher = 5001,
        PointPuller,
        MapMarker = 9040
    }
}
