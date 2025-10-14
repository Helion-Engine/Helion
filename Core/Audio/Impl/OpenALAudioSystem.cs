using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helion.Audio.Impl.Components;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using NLog;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl;

public class OpenALAudioSystem : IAudioSystem
{
    private static bool PrintedALInfo;

    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private Logger m_log;

    public IMusicPlayer Music { get; }
    public event EventHandler? DeviceChanging;
    private readonly HashSet<OpenALAudioSourceManager> m_sourceManagers = new();

    private readonly CancellationTokenSource m_cancelTask = new();
    private OpenALDevice? m_alDevice;
    private OpenALContext? m_alContext;
    private string m_currentDeviceName;
    private string m_activeDeviceName;

    public double Gain { get; private set; }

    public OpenALAudioSystem(IConfig config, ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, Logger log)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_log = log;

        try
        {
            m_alDevice = new OpenALDevice(config.Audio.Device);
            m_alContext = new OpenALContext(m_alDevice);
            m_activeDeviceName = m_alDevice.OpenALDeviceName;
            m_currentDeviceName = m_activeDeviceName;
        }
        catch (Exception ex)
        {
            m_log.Warn($"Failed to initialize audio output: {ex}");
            m_alDevice = null;
            m_alContext = null;
            m_activeDeviceName = string.Empty;
            m_currentDeviceName = string.Empty;
        }
        Music = musicPlayer;

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
        return m_alDevice?.DeviceName ?? string.Empty;
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

        m_alContext?.Dispose();
        m_alDevice?.Dispose();

        bool retVal = false;

        try
        {
            m_alDevice = new OpenALDevice(deviceName);
            m_alContext = new OpenALContext(m_alDevice);
            SetVolume(Gain);
            Music.OutputChanged();

            retVal = true;
        }
        catch (Exception ex)
        {
            m_log.Warn($"Failed to initialize audio output: {ex}");
            m_alContext?.Dispose();
            m_alDevice?.Dispose();

            m_alContext = null;
            m_alDevice = null;
            Music.OutputChanged();
        }

        return retVal;
    }

    public void SetVolume(double volume)
    {
        Gain = volume;

        if (volume == 0)
        {
            // Shut off all the sources but set the main gain to 1 so the music can still come through
            SetSourceManagerGains(0);
            AL.Listener(ALListenerf.Gain, 1);
        }
        else
        {
            // Set listener gain based on sound effects volume; music is scaled as a multiple of effects volume
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
        if (m_currentDeviceName != m_activeDeviceName)
        {
            SetDevice(m_currentDeviceName);
            m_activeDeviceName = m_currentDeviceName;
        }
    }

    [Conditional("DEBUG")]
    public static void CheckForErrors(string debugInfo = "", params object[] objs)
    {
        ALError error = AL.GetError();
        if (error != ALError.NoError)
        {
            string reason = string.Format(CultureInfo.InvariantCulture, debugInfo, objs);
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
        OpenALAudioSourceManager sourceManager = new (this, m_archiveCollection, m_config);
        m_sourceManagers.Add(sourceManager);
        SetVolume(m_config.Audio.SoundVolume);
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
            if (m_currentDeviceName != currentDeviceName)
            {
                m_currentDeviceName = currentDeviceName;
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

        m_alContext?.Dispose();
        m_alDevice?.Dispose();
        Music?.Dispose();
    }
}
