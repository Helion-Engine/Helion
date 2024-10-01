using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helion.Audio.Impl.Components;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl;

public class OpenALAudioSystem : IAudioSystem
{
    private static bool PrintedALInfo;

    public IMusicPlayer Music { get; }
    public event EventHandler? DeviceChanging;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly HashSet<OpenALAudioSourceManager> m_sourceManagers = new();
    private readonly IConfig m_config;
    private readonly CancellationTokenSource m_cancelTask = new();
    private OpenALDevice m_alDevice;
    private OpenALContext m_alContext;
    private string m_changeDeviceName = string.Empty;
    private string m_lastDeviceName;

    public double Gain { get; private set; }

    public OpenALAudioSystem(IConfig config, ArchiveCollection archiveCollection, IMusicPlayer musicPlayer)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_alDevice = new OpenALDevice(config.Audio.Device);
        m_alContext = new OpenALContext(m_alDevice);
        Music = musicPlayer;

        m_lastDeviceName = m_alDevice.OpenALDeviceName;
        Task.Factory.StartNew(DefaultDeviceChangeTask, m_cancelTask.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);

        PrintOpenALInfo();
    }

    public IEnumerable<string> GetDeviceNames()
    {
        List<string> devices = ALC.GetString(AlcGetStringList.AllDevicesSpecifier);
        devices.Insert(0, IAudioSystem.DefaultAudioDevice);
        return devices;
    }

    public string GetDeviceName()
    {
        return m_alDevice.DeviceName;
    }

    public unsafe string GetDefaultDeviceName()
    {
        // It doesn't detect a change unless you call AlcGetStringList.AllDevicesSpecifier...?
        ALC.GetStringPtr(ALDevice.Null, (AlcGetString)AlcGetStringList.AllDevicesSpecifier);
        return ALC.GetString(ALDevice.Null, AlcGetString.DefaultAllDevicesSpecifier);
    }

    public bool SetDevice(string deviceName)
    {
        DeviceChanging?.Invoke(this, EventArgs.Empty);
        Music.OutputChanging();

        m_alContext.Dispose();
        m_alDevice.Dispose();

        m_alDevice = new OpenALDevice(deviceName);
        m_alContext = new OpenALContext(m_alDevice);
        SetVolume(Gain);

        Music.OutputChanged();
        // TODO: This assumes we always successfully changed. We should probably limit this.
        return true;
    }

    public void SetVolume(double volume)
    {
        Gain = volume;

        if (volume == 0)
        {
            SetSourceManagerGains(0);
            AL.Listener(ALListenerf.Gain, 1);
        }
        else
        {
            SetSourceManagerGains(1);
            AL.Listener(ALListenerf.Gain, (float)volume);
        }
    }

    private void SetSourceManagerGains(float gain)
    {
        foreach (var sourceManager in m_sourceManagers)
        {
            sourceManager.SetGain(gain);
        }
    }

    public void ThrowIfErrorCheckFails()
    {
        CheckForErrors("Checking for errors");
    }

    public void Tick()
    {
        if (m_changeDeviceName.Length > 0)
        {
            SetDevice(m_changeDeviceName);
            m_changeDeviceName = string.Empty;
        }
    }

    [Conditional("DEBUG")]
    public static void CheckForErrors(string debugInfo = "", params object[] objs)
    {
        ALError error = AL.GetError();
        if (error != ALError.NoError)
        {
            string reason = string.Format(debugInfo, objs);
            Fail($"Unexpected OpenAL error: {error} (reason: {AL.GetErrorString(error)}) {reason}");
        }
    }

    private void PrintOpenALInfo()
    {
        if (PrintedALInfo)
            return;

        HelionLog.Info($"OpenAL v{GetString(ALGetString.Version)}");
        HelionLog.Info($"OpenAL Vendor: {GetString(ALGetString.Vendor)}");
        HelionLog.Info($"OpenAL Renderer: {GetString(ALGetString.Renderer)}");
        HelionLog.Info($"OpenAL Extensions: {GetString(ALGetString.Extensions).Count(x => x == ' ') + 1}");

        foreach (string device in GetDeviceNames())
            HelionLog.Info($"Device: {device}");

        PrintedALInfo = true;
    }

    private static string GetString(ALGetString type)
    {
        string str = AL.Get(type);
        if (str != null)
            return str;
        return string.Empty;
    }

    ~OpenALAudioSystem()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public IAudioSourceManager CreateContext()
    {
        OpenALAudioSourceManager sourceManager = new(this, m_archiveCollection, m_config);
        m_sourceManagers.Add(sourceManager);
        SetVolume(m_config.Audio.SoundVolume * m_config.Audio.Volume);
        return sourceManager;
    }

    public void Dispose()
    {
        m_cancelTask.Cancel();
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    internal void Unlink(OpenALAudioSourceManager context)
    {
        m_sourceManagers.Remove(context);
    }

    private void DefaultDeviceChangeTask()
    {
        while (true)
        {
            if (m_cancelTask.IsCancellationRequested)
                break;

            var currentDeviceName = GetDefaultDeviceName();
            if (m_lastDeviceName != currentDeviceName)
            {
                m_lastDeviceName = currentDeviceName;
                m_changeDeviceName = currentDeviceName;
            }
            Thread.Sleep(1000);
        }
    }

    private void PerformDispose()
    {
        // Since children contexts on disposing unlink themselves from us,
        // we don't want to be mutating the container while iterating over
        // it.
        m_sourceManagers.ToList().ForEach(srcManager => srcManager.Dispose());
        Invariant(m_sourceManagers.Empty(), "Disposal of AL audio context children should empty out of the context container");

        m_alContext.Dispose();
        m_alDevice.Dispose();
        Music.Dispose();
    }
}
