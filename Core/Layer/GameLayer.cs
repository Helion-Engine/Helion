using Helion.Input;
using Helion.Render.Commands;
using Helion.Util.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Layer
{
    public abstract class GameLayer : IDisposable, IComparable<GameLayer>
    {
        private bool disposed;
        private List<GameLayer> layers = new List<GameLayer>();

        protected abstract double GetPriority();

        protected readonly Config Config;

        public GameLayer(Config config)
        {
            Config = config;
        }
        
        public bool RemoveAllTypes(Type type)
        {
            List<GameLayer> layersToRemove = layers.Where(layer => layer.GetType() == type).ToList();
            
            // Though we extracted the list so we could invoke .Any(), it must
            // also be noted that we can't call this as part of the query or it
            // may (will?) mutate while handling iteration, which is bad.
            layersToRemove.ForEach(layer =>
            {
                layer.Dispose();
                layers.Remove(layer);
            });

            return layersToRemove.Any();
        }
        
        public void Add(GameLayer layer)
        {
            RemoveAllTypes(layer.GetType());
            
            layers.Add(layer);
            layers.Sort();
        } 
        
        public virtual void HandleInput(ConsumableInput consumableInput)
        {
            layers.ForEach(layer => layer.HandleInput(consumableInput));
        }

        public virtual void RunLogic()
        {
            layers.ForEach(layer => layer.RunLogic());
        }

        public virtual void Render(RenderCommands renderCommands)
        {
            layers.ForEach(layer => layer.Render(renderCommands));
        }

        public int CompareTo(GameLayer other) => GetPriority().CompareTo(other.GetPriority());

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                layers.ForEach(layer => layer.Dispose());
                layers.Clear();
            }
            
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}