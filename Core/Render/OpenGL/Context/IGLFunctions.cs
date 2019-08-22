using System;
using GlmSharp;
using Helion.Render.OpenGL.Context.Types;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Context
{
    /// <summary>
    /// A provider of GL functions.
    /// </summary>
    public interface IGLFunctions
    {
        void ActiveTexture(TextureUnitType textureUnit);
        void AttachShader(int programId, int shaderId);
        void BindAttribLocation(int programId, int attrIndex, string attrName);
        void BindBuffer(BufferType type, int bufferId);
        void BindBufferBase(BufferRangeType type, int bindIndex, int bufferId);
        void BindTexture(TextureTargetType type, int textureId);
        void BindVertexArray(int vaoId);
        void BlendFunc(BlendingFactorType sourceFactor, BlendingFactorType destFactor);
        void BufferData<T>(BufferType bufferType, int totalBytes, T[] data, BufferUsageType usageType) where T : struct;
        void BufferStorage<T>(BufferType bufferType, int totalBytes, T[] data, BufferStorageFlagType flags) where T : struct;
        void BufferSubData<T>(BufferType type, int byteOffset, int numBytes, T[] values) where T : struct;
        void Clear(ClearType type);
        void ClearColor(float r, float g, float b, float a);
        void ColorMask(bool red, bool green, bool blue, bool alpha);
        void CompileShader(int shaderId);
        int CreateProgram();
        int CreateShader(ShaderComponentType type);
        void CullFace(CullFaceType type);
        void DebugMessageCallback(Action<DebugLevel, string> callback);
        void DeleteBuffer(int bufferId);
        void DeleteProgram(int programId);
        void DeleteShader(int shaderId);
        void DeleteTexture(int textureId);
        void DeleteVertexArray(int vaoId);
        void DetachShader(int programId, int shaderId);
        void Disable(EnableType type);
        void DrawArrays(PrimitiveDrawType type, int startIndex, int count);
        void Enable(EnableType type);
        void EnableVertexAttribArray(int index);
        void FrontFace(FrontFaceType type);
        int GenBuffer();
        void GenerateMipmap(MipmapTargetType type);
        int GenTexture();
        int GenVertexArray();
        string GetActiveUniform(int mrogramId, int uniformIndex, out int size, out int typeEnum);
        ErrorType GetError();
        int GetInteger(GetIntegerType type);
        void GetProgram(int programId, GetProgramParameterType type, out int value);
        string GetProgramInfoLog(int programId);
        void GetShader(int shaderId, ShaderParameterType type, out int value);
        string GetShaderInfoLog(int shaderId);
        string GetString(GetStringType type);
        string GetString(GetStringType type, int index);
        long GetTextureHandleARB(int texture);
        int GetUniformLocation(int programId, string name);
        void LinkProgram(int programId);
        void MakeTextureHandleNonResident(long handle);
        void MakeTextureHandleResidentARB(long handle);
        void ObjectLabel(ObjectLabelType type, int objectId, string name);
        void PolygonMode(PolygonFaceType faceType, PolygonModeType fillType);
        void ShaderSource(int shaderId, string sourceText);
        void StencilFunc(StencilFuncType type, int value, int mask);
        void StencilMask(int mask);
        void StencilOp(StencilOpType stencilFail, StencilOpType depthFail, StencilOpType depthPass);
        void TexParameter(TextureTargetType targetType, TextureParameterNameType paramType, int value);
        void TexStorage2D(TexStorageTargetType targetType, int mipmapLevels, TexStorageInternalType internalType, Dimension dimension);
        void TexSubImage2D(TextureTargetType targetType, int mipmapLevels, Vec2I position, Dimension dimension, PixelFormatType formatType, PixelDataType pixelType, IntPtr data);
        void TexImage2D(TextureTargetType textureType, int level, PixelInternalFormatType internalType, Dimension dimension, PixelFormatType formatType, PixelDataType dataType, IntPtr data);
        void Uniform1(int location, int value);
        void Uniform1(int location, float value);
        void UniformMatrix4(int location, int count, bool transpose, mat4 matrix);
        void UseProgram(int programId);
        void VertexAttribIPointer(int index, int size, VertexAttributeIntegralPointerType type, int stride, int offset);
        void VertexAttribPointer(int index, int size, VertexAttributePointerType type, bool normalized, int stride, int offset);
        void Viewport(int x, int y, int width, int height);
    }
}