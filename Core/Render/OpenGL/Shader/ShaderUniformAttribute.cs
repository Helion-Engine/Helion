using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Shader
{
    public class ShaderUniformAttribute : Attribute
    {
    }
    
    [ShaderUniformAttribute]
    public abstract class UniformElement<T> where T : struct
    {
        internal const int NoLocation = -1;
        
        internal int Location = NoLocation;

        public abstract void Set(T value);
    }
    
    public class UniformInt : UniformElement<int>
    {
        public override void Set(int value)
        {
            Precondition(Location != NoLocation, "Uniform int value did not have the location set");
            
            GL.Uniform1(Location, value);
        }
    }
    
    public class UniformTexture : UniformInt
    {
        public override void Set(int value)
        {
            Fail("A uniform texture should be calling BindAndSet(), not Set()");
        }
        
        public void BindAndSet(TextureUnit unit)
        {
            // TODO: Should assert the implementation has enough texture units.
            Precondition(Location != NoLocation, "Uniform int value did not have the location set");
            
            GL.ActiveTexture(unit);
            base.Set((int)unit - (int)TextureUnit.Texture0);
        }
    }

    public class UniformFloat : UniformElement<float>
    {
        public override void Set(float value)
        {
            Precondition(Location != NoLocation, "Uniform float value did not have the location set");
            
            GL.Uniform1(Location, value);
        }
    }
    
    public class UniformVec3 : UniformElement<Vector3>
    {
        public void Set(System.Numerics.Vector3 vector)
        {
            Set(new Vector3(vector.X, vector.Y, vector.Z));
        }
        
        public override void Set(Vector3 value)
        {
            Precondition(Location != NoLocation, "Uniform vec3 value did not have the location set");
            
            GL.Uniform3(Location, value);
        }
    }
    
    public class UniformVec4 : UniformElement<Vector4>
    {
        public void Set(System.Numerics.Vector4 vector)
        {
            Set(new Vector4(vector.X, vector.Y, vector.Z, vector.W));
        }
        
        public override void Set(Vector4 value)
        {
            Precondition(Location != NoLocation, "Uniform vec4 value did not have the location set");
            
            GL.Uniform4(Location, value);
        }
    }
    
    public class UniformMatrix4 : UniformElement<Matrix4>
    {
        public override void Set(Matrix4 value)
        {
            Precondition(Location != NoLocation, "Uniform matrix4 value did not have the location set");
            
            GL.UniformMatrix4(Location, false, ref value);
        }
    }
}