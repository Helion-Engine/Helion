using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helion.Render.OpenGL.Arrays;

namespace Helion.Render.OpenGL.Attributes
{
    /// <summary>
    /// A helper class for deducing the attributes that would arise from some
    /// struct.
    /// </summary>
    public static class VertexStructAttributes
    {
        /// <summary>
        /// Gets the attributes that correlate to the struct.
        /// </summary>
        /// <returns>A list of attributes from the struct.</returns>
        public static List<VertexAttributeArrayElement> GetAttributes<TVertex>() where TVertex : struct
        {
            List<VertexAttributeArrayElement> elements = new();
            
            // TODO
            
            return elements;
        }
        
        private static bool HasNormalizedAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.GetCustomAttributes().OfType<NormalizedAttribute>().Any();
        }
    }
}
