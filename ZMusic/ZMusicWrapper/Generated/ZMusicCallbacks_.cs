namespace ZMusicWrapper.Generated;

public unsafe partial struct ZMusicCallbacks_
{
    [NativeTypeName("void (*)(int, const char *)")]
    public delegate* unmanaged[Cdecl]<int, sbyte*, void> MessageFunc;

    [NativeTypeName("const char *(*)(const char *, int)")]
    public delegate* unmanaged[Cdecl]<sbyte*, int, sbyte*> PathForSoundfont;

    [NativeTypeName("void *(*)(const char *, int)")]
    public delegate* unmanaged[Cdecl]<sbyte*, int, void*> OpenSoundFont;

    [NativeTypeName("ZMusicCustomReader *(*)(void *, const char *)")]
    public delegate* unmanaged[Cdecl]<void*, sbyte*, ZMusicCustomReader_*> SF_OpenFile;

    [NativeTypeName("void (*)(void *, const char *)")]
    public delegate* unmanaged[Cdecl]<void*, sbyte*, void> SF_AddToSearchPath;

    [NativeTypeName("void (*)(void *)")]
    public delegate* unmanaged[Cdecl]<void*, void> SF_Close;

    [NativeTypeName("const char *(*)(const char *)")]
    public delegate* unmanaged[Cdecl]<sbyte*, sbyte*> NicePath;
}
