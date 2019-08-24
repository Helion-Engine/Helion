using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Util;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer
{
    public abstract class GameLayer : IDisposable, IComparable<GameLayer>
    {
        protected readonly Config Config;
        private readonly List<GameLayer> m_layers = new List<GameLayer>();

        protected abstract CIString Name { get; }
        protected abstract double Priority { get; }

        protected GameLayer(Config config)
        {
            Config = config;
        }

        ~GameLayer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public GameLayer? GetGameLayer(Type type)
        {
            return m_layers.FirstOrDefault(x => x.GetType() == type);
        }
        
        public bool RemoveAllTypes(Type type)
        {
            List<GameLayer> layersToRemove = m_layers.Where(layer => layer.GetType() == type).ToList();
            
            // Though we extracted the list so we could invoke .Any(), it must
            // also be noted that we can't call this as part of the query or it
            // may (will?) mutate while handling iteration, which is bad.
            layersToRemove.ForEach(layer =>
            {
                layer.Dispose();
                m_layers.Remove(layer);
            });

            return layersToRemove.Any();
        }
        
        public void Add(GameLayer layer)
        {
            RemoveAllTypes(layer.GetType());
            
            m_layers.Add(layer);
            m_layers.Sort();
        } 
        
        public virtual void HandleInput(ConsumableInput consumableInput)
        {
            m_layers.ForEach(layer => layer.HandleInput(consumableInput));
        }

        public virtual void RunLogic()
        {
            m_layers.ForEach(layer => layer.RunLogic());
        }

        public virtual void Render(RenderCommands renderCommands)
        {
            m_layers.ForEach(layer => layer.Render(renderCommands));
        }

        public int CompareTo(GameLayer other) => Priority.CompareTo(other.Priority);

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        private void PerformDispose()
        {
            m_layers.ForEach(layer => layer.Dispose());
            m_layers.Clear();
        }
    }
}