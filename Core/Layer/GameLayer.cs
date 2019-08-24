using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Util;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer
{
    /// <summary>
    /// Represents a stackable 'window', where windows that have have a higher
    /// priority will handle input/rendering/etc before ones that have a lower
    /// priority.
    /// </summary>
    /// <remarks>
    /// Layers with a lower priority will get their rendering done first.
    /// This is because we want a painters algorithm done so things render
    /// correctly. Input will be handled from highest priority (1.0) down to
    /// the lowest (0.0), since we don't want input consumed in the higher
    /// priority layers to be read by lower layers. The logic will be handled
    /// in a top down way, but this could be either direction.
    /// </remarks>
    public abstract class GameLayer : IDisposable, IComparable<GameLayer>
    {
        private readonly List<GameLayer> m_layers = new List<GameLayer>();

        /// <summary>
        /// The unique name of the layer.
        /// </summary>
        protected abstract CIString Name { get; }
        
        /// <summary>
        /// A value that indicates the priority relative to other layers (see
        /// class summary for more info). A higher property is higher on the
        /// layer stack than a lower priority one.
        /// </summary>
        protected abstract double Priority { get; }

        ~GameLayer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        /// <summary>
        /// Gets the first game layer with the type provided.
        /// </summary>
        /// <param name="type">The type to get.</param>
        /// <returns>The layer with the type.</returns>
        public GameLayer? GetGameLayer(Type type)
        {
            return m_layers.FirstOrDefault(x => x.GetType() == type);
        }

        /// <summary>
        /// Checks if any layers exist with the name provided.
        /// </summary>
        /// <param name="name">The layer name.</param>
        /// <returns>True if so, false otherwise.</returns>
        public bool AnyExistByName(CIString name) => m_layers.Any(layer => layer.Name == name);
        
        /// <summary>
        /// Removes all layers with a matching name.
        /// </summary>
        /// <param name="name">The layer name.</param>
        public void RemoveAllByName(CIString name)
        {
            List<GameLayer> layersToRemove = m_layers.Where(layer => layer.Name == name).ToList();
            RemoveLayers(layersToRemove);
        }
        
        /// <summary>
        /// Removes all types that match the type provided.
        /// </summary>
        /// <param name="type">The type to remove.</param>
        public void RemoveAllTypes(Type type)
        {
            List<GameLayer> layersToRemove = m_layers.Where(layer => layer.GetType() == type).ToList();
            RemoveLayers(layersToRemove);
        }

        /// <summary>
        /// Adds a new layer. It will automatically be ordered based on its
        /// priority.
        /// </summary>
        /// <param name="layer">The layer to add.</param>
        public void Add(GameLayer layer)
        {
            RemoveAllTypes(layer.GetType());
            
            m_layers.Add(layer);
            m_layers.Sort();
        }

        /// <summary>
        /// Is responsible for handling the input.
        /// </summary>
        /// <remarks>
        /// Overriding implementations should always call the base function.
        /// </remarks>
        /// <param name="consumableInput">The input.</param>
        public virtual void HandleInput(ConsumableInput consumableInput)
        {
            m_layers.ForEachReverse(layer => layer.HandleInput(consumableInput));
        }

        /// <summary>
        /// Runs the logic (if any) for the layer.
        /// </summary>
        /// <remarks>
        /// Overriding implementations should always call the base function.
        /// </remarks>
        public virtual void RunLogic()
        {
            m_layers.ForEachReverse(layer => layer.RunLogic());
        }

        /// <summary>
        /// Renders the layer.
        /// </summary>
        /// <remarks>
        /// Overriding implementations should always call the base function.
        /// </remarks>
        /// <param name="renderCommands">The commands to add our rendering
        /// commands to.</param>
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

        protected virtual void PerformDispose()
        {
            m_layers.ForEach(layer => layer.Dispose());
            m_layers.Clear();
        }

        private void RemoveLayers(List<GameLayer> layersToRemove)
        {
            // Though we extracted the list so we could invoke .Any(), it must
            // also be noted that we can't call this as part of the query or it
            // may (will?) mutate while handling iteration, which is bad.
            layersToRemove.ForEach(layer =>
            {
                layer.Dispose();
                m_layers.Remove(layer);
            });
        }
    }
}