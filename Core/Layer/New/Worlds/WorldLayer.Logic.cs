namespace Helion.Layer.New.Worlds
{
    public partial class WorldLayer
    {
        private bool AnyLayerObscuring => Parent.ConsoleLayer != null ||
                                          Parent.MenuLayer != null ||
                                          Parent.TitlepicLayer != null ||
                                          Parent.IntermissionLayer != null;
        public void RunLogic()
        {
            if (World == null) 
                return;
            
            // If something is on top of our world (such as a menu, or a
            // console) then we should pause it. Likewise, if we're at the
            // top layer, then we should make sure we're not paused (like
            // if the user just removed the menu or console).
            if (AnyLayerObscuring)
            {
                if (World.Paused)
                    World.Resume();
            }
            else if (!World.Paused)
            {
                World.Pause();
            }
        }
    }
}
