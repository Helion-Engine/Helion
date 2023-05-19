using Helion.Geometry.Boxes;

namespace Helion.Client;

struct MonsterClosetInfo
{
    public int Id;
    public int MonsterCount;
    public Box2D Box;
    public string Sectors;

    public MonsterClosetInfo(int id, int monsterCount, Box2D box, string sectors)
    {
        Id = id;
        MonsterCount = monsterCount;
        Box = box;
        Sectors = sectors;
    }
}
