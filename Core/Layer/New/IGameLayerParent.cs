namespace Helion.Layer.New
{
    /// <summary>
    /// A game layer that acts like a parent. Children can remove themselves
    /// from the parent by notifying them with themselves.
    /// </summary>
    public interface IGameLayerParent : IGameLayer
    {
        void Remove(object layer);
    }
}
