namespace ZMusicWrapper.Generated;

public unsafe partial struct ZMusicCustomReader_
{
    public void* handle;

    [NativeTypeName("char *(*)(struct ZMusicCustomReader_ *, char *, int)")]
    public delegate* unmanaged[Cdecl]<ZMusicCustomReader_*, sbyte*, int, sbyte*> gets;

    [NativeTypeName("long (*)(struct ZMusicCustomReader_ *, void *, int32_t)")]
    public delegate* unmanaged[Cdecl]<ZMusicCustomReader_*, void*, int, int> read;

    [NativeTypeName("long (*)(struct ZMusicCustomReader_ *, long, int)")]
    public delegate* unmanaged[Cdecl]<ZMusicCustomReader_*, int, int, int> seek;

    [NativeTypeName("long (*)(struct ZMusicCustomReader_ *)")]
    public delegate* unmanaged[Cdecl]<ZMusicCustomReader_*, int> tell;

    [NativeTypeName("void (*)(struct ZMusicCustomReader_ *)")]
    public delegate* unmanaged[Cdecl]<ZMusicCustomReader_*, void> close;
}
