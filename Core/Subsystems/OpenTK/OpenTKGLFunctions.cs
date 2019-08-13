using System;
using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Util.Geometry;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Subsystems.OpenTK
{
    public class OpenTKGLFunctions : IGLFunctions
    {
        /// <summary>
        /// Holds a reference to the last registered callback. Required due to
        /// how the GC works or else we trigger SystemAccessViolations.
        /// </summary>
        /// <remarks>
        /// See: https://stackoverflow.com/questions/16544511/prevent-delegate-from-being-garbage-collected
        /// See: https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c
        /// </remarks>
        private Action<DebugLevel, string>? m_lastCallbackReference;
        
        /// <summary>
        /// Same as <see cref="m_lastCallbackReference"/>.
        /// </summary>
        private DebugProc? m_lastCallbackProcReference;

        public void ActiveTexture(TextureUnitType textureUnit)
        {
            GL.ActiveTexture((TextureUnit)textureUnit);
        }

        public void AttachShader(int programId, int shaderId)
        {
            GL.AttachShader(programId, shaderId);
        }

        public void BindAttribLocation(int programId, int attrIndex, string attrName)
        {
            GL.BindAttribLocation(programId, attrIndex, attrName);
        }

        public void BindBuffer(BufferType type, int bufferId)
        {
            GL.BindBuffer((BufferTarget)type, bufferId);
        }

        public void BindBufferBase(BufferRangeType type, int bindIndex, int bufferId)
        {
            GL.BindBufferBase((BufferRangeTarget)type, bindIndex, bufferId);
        }

        public void BindTexture(TextureTargetType type, int textureId)
        {
            GL.BindTexture((TextureTarget)type, textureId);
        }

        public void BindVertexArray(int vaoId)
        {
            GL.BindVertexArray(vaoId);
        }

        public void BlendFunc(BlendingFactorType sourceFactor, BlendingFactorType destFactor)
        {
            GL.BlendFunc((BlendingFactor)sourceFactor, (BlendingFactor)destFactor);
        }

        public void BufferData<T>(BufferType bufferType, int totalBytes, T[] data, BufferUsageType usageType) where T : struct
        {
            GL.BufferData((BufferTarget)bufferType, totalBytes, data, (BufferUsageHint)usageType);
        }

        public void BufferStorage<T>(BufferType bufferType, int totalBytes, T[] data, BufferStorageFlagType flags) where T : struct
        {
            GL.BufferStorage((BufferTarget)bufferType, totalBytes, data, (BufferStorageFlags)flags);
        }

        public void BufferSubData<T>(BufferType type, int byteOffset, int numBytes, T[] values) where T : struct
        {
            IntPtr offset = new IntPtr(byteOffset);
            GL.BufferSubData((BufferTarget)type, offset, numBytes, values);
        }

        public void Clear(ClearType type)
        {
            GL.Clear((ClearBufferMask)type);
        }
        
        public void ClearColor(float r, float g, float b, float a)
        {
            GL.ClearColor(r, g, b, a);
        }

        public void CompileShader(int shaderId)
        {
            GL.CompileShader(shaderId);
        }

        public int CreateProgram()
        {
            return GL.CreateProgram();
        }

        public int CreateShader(ShaderComponentType type)
        {
            return GL.CreateShader((ShaderType)type);
        }

        public void CullFace(CullFaceType type)
        {
            GL.CullFace((CullFaceMode)type);
        }

        public void DebugMessageCallback(Action<DebugLevel, string> callback)
        {
            // If we don't do this, the GC will collect it (since the lambda
            // below won't) and then we end up with a SystemAccessViolation.
            // See the docs of this variable for more information.
            m_lastCallbackReference = callback;
            m_lastCallbackProcReference = (source, type, id, severity, length, message, userParam) =>
            {
                switch (severity)
                {
                case DebugSeverity.DebugSeverityHigh:
                    callback(DebugLevel.High, Marshal.PtrToStringAnsi(message, length));
                    break;
                case DebugSeverity.DebugSeverityMedium:
                    callback(DebugLevel.Medium, Marshal.PtrToStringAnsi(message, length));
                    break;
                case DebugSeverity.DebugSeverityLow:
                    callback(DebugLevel.Low, Marshal.PtrToStringAnsi(message, length));
                    break;
                }
            };
            
            GL.DebugMessageCallback(m_lastCallbackProcReference, IntPtr.Zero);
        }

        public void DeleteBuffer(int bufferId)
        {
            GL.DeleteBuffer(bufferId);
        }

        public void DeleteProgram(int programId)
        {
            GL.DeleteProgram(programId);
        }

        public void DeleteShader(int shaderId)
        {
            GL.DeleteShader(shaderId);
        }

        public void DeleteTexture(int textureId)
        {
            GL.DeleteTexture(textureId);
        }

        public void DeleteVertexArray(int vaoId)
        {
            GL.DeleteVertexArray(vaoId);
        }

        public void DetachShader(int programId, int shaderId)
        {
            GL.DetachShader(programId, shaderId);
        }

        public void DrawArrays(PrimitiveDrawType type, int startIndex, int count)
        {
            GL.DrawArrays((PrimitiveType)type, startIndex, count);
        }

        public void Enable(EnableType type)
        {
            GL.Enable((EnableCap)type);
        }

        public void EnableVertexAttribArray(int index)
        {
            GL.EnableVertexAttribArray(index);
        }

        public void FrontFace(FrontFaceType type)
        {
            GL.FrontFace((FrontFaceDirection)type);
        }

        public int GenBuffer()
        {
            return GL.GenBuffer();
        }

        public void GenerateMipmap(MipmapTargetType type)
        {
            GL.GenerateMipmap((GenerateMipmapTarget)type);
        }

        public int GenTexture()
        {
            return GL.GenTexture();
        }

        public int GenVertexArray()
        {
            return GL.GenVertexArray();
        }

        public string GetActiveUniform(int programId, int uniformIndex, out int size, out int typeEnum)
        {
            string result = GL.GetActiveUniform(programId, uniformIndex, out size, out ActiveUniformType type);
            typeEnum = (int)type;
            return result;
        }

        public ErrorType GetError()
        {
            ErrorCode errorCode = GL.GetError();
            return errorCode == ErrorCode.NoError ? ErrorType.None : (ErrorType)errorCode;
        }
        
        public int GetInteger(GetIntegerType type)
        {
            return GL.GetInteger((GetPName)type);
        }

        public void GetProgram(int programId, GetProgramParameterType type, out int value)
        {
            GL.GetProgram(programId, (GetProgramParameterName)type, out value);
        }

        public string GetProgramInfoLog(int programId)
        {
            return GL.GetProgramInfoLog(programId);
        }

        public void GetShader(int shaderId, ShaderParameterType type, out int value)
        {
            GL.GetShader(shaderId, (ShaderParameter)type, out value);
        }

        public string GetShaderInfoLog(int shaderId)
        {
            return GL.GetShaderInfoLog(shaderId);
        }

        public string GetString(GetStringType type)
        {
            return GL.GetString((StringName)type);
        }

        public string GetString(GetStringType type, int index)
        {
            return GL.GetString((StringNameIndexed)type, index);
        }

        public long GetTextureHandleARB(int textureId)
        {
            return GL.Arb.GetTextureHandle(textureId);
        }

        public int GetUniformLocation(int programId, string name)
        {
            return GL.GetUniformLocation(programId, name);
        }

        public void LinkProgram(int programId)
        {
            GL.LinkProgram(programId);
        }

        public void MakeTextureHandleNonResident(long handle)
        {
            GL.Arb.MakeTextureHandleNonResident(handle);
        }

        public void MakeTextureHandleResidentARB(long handle)
        {
            GL.Arb.MakeTextureHandleResident(handle);
        }

        public void ObjectLabel(ObjectLabelType type, int objectId, string name)
        {
            GL.ObjectLabel((ObjectLabelIdentifier)type, objectId, name.Length, name);
        }

        public void PolygonMode(PolygonFaceType faceType, PolygonModeType fillType)
        {
            GL.PolygonMode((MaterialFace)faceType, (PolygonMode)fillType);
        }

        public void ShaderSource(int shaderId, string sourceText)
        {
            GL.ShaderSource(shaderId, sourceText);
        }

        public void TexParameter(TextureTargetType targetType, TextureParameterNameType paramType, int value)
        {
            GL.TexParameter((TextureTarget)targetType, (TextureParameterName)paramType, value);
        }

        public void TexStorage2D(TexStorageTargetType targetType, int mipmapLevels, TexStorageInternalType internalType, Dimension dimension)
        {
            GL.TexStorage2D((TextureTarget2d)targetType, mipmapLevels, (SizedInternalFormat)internalType, dimension.Width, dimension.Height);
        }

        public void TexSubImage2D(TextureTargetType targetType, int mipmapLevels, Vec2I position, Dimension dimension, PixelFormatType formatType, PixelDataType pixelType, IntPtr data)
        {
            GL.TexSubImage2D((TextureTarget)targetType, mipmapLevels, position.X, position.Y, 
                dimension.Width, dimension.Height, (PixelFormat)formatType, (PixelType)pixelType, data);
        }

        public void TexImage2D(TextureTargetType textureType, int level, PixelInternalFormatType internalType, Dimension dimension, PixelFormatType formatType, PixelDataType dataType, IntPtr data)
        {
            GL.TexImage2D((TextureTarget)textureType, level, (PixelInternalFormat)internalType, dimension.Width, 
                dimension.Height, 0, (PixelFormat)formatType, (PixelType)dataType, data);
        }

        public void Uniform1(int location, int value)
        {
            GL.Uniform1(location, value);
        }

        public void Uniform1(int location, float value)
        {
            GL.Uniform1(location, value);
        }

        public void UniformMatrix4(int location, int count, bool transpose, mat4 matrix)
        {
            float[] data = matrix.Values1D;
            GL.UniformMatrix4(location, count, transpose, data);
        }

        public void UseProgram(int programId)
        {
            GL.UseProgram(programId);
        }

        public void VertexAttribIPointer(int index, int size, VertexAttributeIntegralPointerType type, int stride, int offset)
        {
            GL.VertexAttribIPointer(index, size, (VertexAttribIntegerType)type, stride, new IntPtr(offset));
        }

        public void VertexAttribPointer(int index, int size, VertexAttributePointerType type, bool normalized, int stride, int offset)
        {
            GL.VertexAttribPointer(index, size, (VertexAttribPointerType)type, normalized, stride, new IntPtr(offset));
        }

        public void Viewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
        }
    }
}