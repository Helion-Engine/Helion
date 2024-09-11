namespace ZMusicWrapper.Generated;

public unsafe partial struct ZMusicConfigurationSetting_
{
    [NativeTypeName("const char *")]
    public sbyte* name;

    public int identifier;

    [NativeTypeName("ZMusicVariableType")]
    public ZMusicVariableType_ type;

    public float defaultVal;

    [NativeTypeName("const char *")]
    public sbyte* defaultString;
}
