using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Helion.Input;
using Helion.Render.OpenGL.Legacy.Commands;
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
    public abstract class GameLayer : IDisposable
    {
        public bool Disposed { get; protected set; }
        protected GameLayer? Parent;
        protected readonly List<GameLayer> Layers = new();

        /// <summary>
        /// A value that indicates the priority relative to other layers (see
        /// class summary for more info). A higher property is higher on the
        /// layer stack than a lower priority one.
        /// </summary>
        protected abstract double Priority { get; }

        public int Count => Layers.Count;
        public bool Empty => Layers.Empty();

        ~GameLayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        /// <summary>
        /// Gets the first game layer with the type provided.
        /// </summary>
        /// <returns>The layer with the type.</returns>
        public GameLayer? Get<T>() where T : GameLayer
        {
            foreach (GameLayer value in Layers)
                if (value.GetType() == typeof(T))
                    return value;
            return null;
        }

        /// <summary>
        /// Checks if any layers exist with the type provided.
        /// </summary>
        /// <returns>True if so, false otherwise.</returns>
        public bool Contains<T>() => Layers.Any(layer =>
        {
            Type type = layer.GetType();
            return type == typeof(T) || type.IsSubclassOf(typeof(T));
        });

        /// <summary>
        /// Checks if either menu types.
        /// </summary>
        /// <typeparam name="T1">The first menu type.</typeparam>
        /// <typeparam name="T2">The second menu type.</typeparam>
        /// <returns>If it contains either T1 or T2.</returns>
        public bool ContainsEither<T1, T2>() => Contains<T1>() || Contains<T2>();

        /// <summary>
        /// Removes by type.
        /// </summary>
        /// <typeparam name="T">The type to remove.</typeparam>
        public void Remove<T>() where T : GameLayer
        {
            Remove(typeof(T));
        }

        /// <summary>
        /// Removes all types that match the type provided.
        /// </summary>
        /// <param name="type">The type to remove.</param>
        public void Remove(Type type)
        {
            foreach (GameLayer gameLayer in Layers.Where(layer => ShouldBeRemoved(layer, type)))
                gameLayer.Dispose();
            
            static bool ShouldBeRemoved(GameLayer layer, Type type)
            {
                return layer.GetType().IsSubclassOf(type) || layer.GetType() == type || layer.Disposed;
            }
        }
        
        public void RemoveAllBut<T>() where T : GameLayer
        {
            foreach (GameLayer layer in Layers.Where(layer => ShouldBeRemovedAll(layer, typeof(T))))
                Remove(layer.GetType());
            
            static bool ShouldBeRemovedAll(GameLayer layer, Type type)
            {
                return (!layer.GetType().IsSubclassOf(type) && layer.GetType() != type) || layer.Disposed;
            }
        }

        /// <summary>
        /// Adds a new layer. It will automatically be ordered based on its
        /// priority.
        /// </summary>
        /// <param name="layer">The layer to add.</param>
        public virtual void Add(GameLayer layer)
        {
            Remove(layer.GetType());

            Layers.Add(layer);
        }

        /// <summary>
        /// Tries to get the layer type provided.
        /// </summary>
        /// <param name="layer">The layer to be populated with if it is found.
        /// </param>
        /// <typeparam name="T">The type of layer.</typeparam>
        /// <returns>True on success, false otherwise.</returns>
        public bool TryGetLayer<T>([NotNullWhen(true)] out T? layer) where T : GameLayer
        {
            GameLayer? gameLayer = Get<T>();
            if (gameLayer != null)
            {
                layer = (T)gameLayer;
                return true;
            }

            layer = default;
            return false;
        }

        /// <summary>
        /// Is responsible for handling the input.
        /// </summary>
        /// <remarks>
        /// Overriding implementations should always call the base function.
        /// </remarks>
        /// <param name="input">The input.</param>
        public virtual void HandleInput(InputEvent input)
        {
            Layers.ForEachReverse(layer => layer.HandleInput(input));
            OrderLayers();
        }

        /// <summary>
        /// Runs the logic (if any) for the layer.
        /// </summary>
        /// <remarks>
        /// Overriding implementations should always call the base function.
        /// </remarks>
        public virtual void RunLogic()
        {
            Layers.ForEachReverse(layer => layer.RunLogic());
            OrderLayers();
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
            OrderLayers();
            foreach (GameLayer layer in Layers)
                layer.Render(renderCommands);
        }

        /// <summary>
        /// Checks if the provided layer is at the top (has the highest priority
        /// out of any layer).
        /// </summary>
        /// <param name="layer">The layer to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsTopLayer(GameLayer layer) => !Layers.Empty() && ReferenceEquals(Layers.Last(), layer);

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        public void PruneDisposed()
        {
            var keysToRemove = Layers.Where(layer => layer.Disposed).ToList();
            foreach (GameLayer layer in keysToRemove)
                Layers.Remove(layer);
        }

        protected virtual void PerformDispose()
        {
            if (Disposed)
                return;
            
            Layers.ForEach(layer => layer.Dispose());
            Layers.Clear();

            Disposed = true;
        }

        protected void OrderLayers()
        {
            Layers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
    }
}