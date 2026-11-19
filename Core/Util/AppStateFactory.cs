using Helion.Util.SerializationContexts;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace Helion.Util;

public interface IAppStateFactory
{
    IAppState Create();
    JsonTypeInfo JsonInfo { get; }
    bool Global { get; }
    string FileName { get; }
}

public class EnvironmentAppStateFactory : IAppStateFactory
{
    public IAppState Create() => new EnvironmentAppState();
    public JsonTypeInfo JsonInfo => EnvironmentAppStateSerializationContext.Default.EnvironmentAppState;
    public bool Global => true;
    public string FileName => "environment.json";
}

public class InstallAppStateFactory : IAppStateFactory
{
    public IAppState Create() => new InstallAppState();
    public JsonTypeInfo JsonInfo => InstallAppStateSerializationContext.Default.InstallAppState;
    public bool Global => false;
    public string FileName => "install.json";
}
