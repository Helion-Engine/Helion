using Helion.World.Entities;

namespace Helion.World;

internal struct MoveLinkData
{
    public Entity Entity;
    public double Height;
    public bool CheckHeight;
    public bool Success;

    public void Init(Entity entity, double height, bool checkHeight)
    {
        Entity = entity;
        Height = height;
        CheckHeight = checkHeight;
        Success = true;
    }
}
