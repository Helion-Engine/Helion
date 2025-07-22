namespace Helion.World.Entities.Definition
{
    public static class EditorIds
    {
        public static bool IsPlayerStart(int? id) => id < 4;
        public static bool IsMusicChanger(int? id) => id.HasValue && id >= (int)EditorId.MusicChangerStart && id <= (int)EditorId.MusicChangerEnd;
        public static bool IsAmbientSound(int? id) => id.HasValue && id >= (int)EditorId.AmbientSoundStart && id <= (int)EditorId.AmbientSoundEnd;
    }

    public enum EditorId
    {
        DeathmatchStart = 11,
        TeleportLanding = 14,
        PointPusher = 5001,
        PointPuller,
        MapMarker = 9040,
        AmbientSoundStart = 14001,
        AmbientSoundEnd = 14065,
        MusicChangerStart = 14100,
        MusicChangerEnd = 14164
    }
}
