using OpenTK.Audio.OpenAL;
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Helion.Audio.Impl;

public class OpenALSoftResamplerType
{
    private const ALGetInteger NumResamplersSoft = (ALGetInteger)0x1210;
    private const ALGetInteger DefaultResamplerSoft = (ALGetInteger)0x1211;
    private const ALGetString ResamplerNameSoft = (ALGetString)0x1213;

    private static List<(string Name, int Value)> ResamplerOptions;
    private static Dictionary<string, int> NameToIndexMap;
    public static List<string> MenuOptions;

    private static string? DefaultName;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr ALGetStringiSOFTSignature(ALGetString a, int b);
    static OpenALSoftResamplerType()
    {
        ResamplerOptions = new();
        NameToIndexMap = new();
        if (AL.IsExtensionPresent("AL_SOFT_source_resampler"))
        {
            var addr = AL.GetProcAddress("alGetStringiSOFT");

            var alGetStringiSOFT = Marshal.GetDelegateForFunctionPointer<ALGetStringiSOFTSignature>(addr);
            var numResamplers = AL.Get(NumResamplersSoft);

            var defaultResamplerNum = AL.Get(DefaultResamplerSoft);
            DefaultName = Marshal.PtrToStringAnsi(alGetStringiSOFT(ResamplerNameSoft, defaultResamplerNum));

            for (var i = 0; i < numResamplers; i++)
            {
                var resamplerName = Marshal.PtrToStringAnsi(alGetStringiSOFT(ResamplerNameSoft, i));
                if (resamplerName != null) {
                    ResamplerOptions.Add((resamplerName, i));
                    NameToIndexMap[resamplerName] = ResamplerOptions.Count - 1;
                }
            }
        }
        MenuOptions = (new[] {"Default"}).Concat(ResamplerOptions.ConvertAll(x => x.Name)).ToList();
    }

    private int m_optionsIndex;

    private OpenALSoftResamplerType(int index)
    {
        m_optionsIndex = index;
    }

    public static OpenALSoftResamplerType? FromName(string name)
    {
        if (name == "Default")
        {
            if (DefaultName == null)
            {
                return null;
            }
            name = DefaultName;
        }
        if (NameToIndexMap.TryGetValue(name, out int index))
        {
            return new(index);
        }
        return null;
    }

    public string Name => ResamplerOptions[m_optionsIndex].Name;
    public int Value => ResamplerOptions[m_optionsIndex].Value;
}
