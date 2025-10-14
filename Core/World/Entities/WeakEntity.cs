namespace Helion.World.Entities;

// Represents a weak reference to an entity.
// Used for handling references that can be disposed.
// For example if a monster has a lost soul as a target it will eventually be completely disposed from the game.
// When the lost soul is disposed it will clear it's id and will no longer match the id set in this struct.
public readonly struct WeakEntity(Entity? entity)
{
    public static readonly WeakEntity Default = new(null);

    private readonly Entity? m_entity = entity;
    private readonly int m_id = entity == null ? 0 : entity.Id;

    public readonly bool IsNull() => m_entity == null || m_entity.Id != m_id;

    public readonly bool NotNull() => m_entity != null && m_entity.Id == m_id;

    public readonly Entity? Get()
    {
        if (m_entity != null && m_entity.Id == m_id)
            return m_entity;
        return null;
    }
}