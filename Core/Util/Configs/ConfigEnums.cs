using Helion.Maps.Shared;
using Helion.Render.Common.Textures;
using Helion.Resources.Definitions;
using Helion.Util.Configs.Components;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.StatusBar;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Helion.Util.Configs
{
    /// <summary>
    /// This convenience class is an unfortunate consequence of optimizing for AOT compilation--we need to use GetValues<T> 
    /// instead of GetValues(typeof(T)).  However, this is often called in places where we either aren't using generic types,
    /// or where the generic types are insufficiently constrained.
    /// </summary>
    public static class ConfigEnums
    {
        public static Dictionary<Type, Array> KnownEnumValues { get; } = new Dictionary<Type, Array>()
        {
            { typeof(RandomPitch), Enum.GetValues<RandomPitch>() },
            { typeof(Synth), Enum.GetValues<Synth>() },
            { typeof(CompLevel), Enum.GetValues<CompLevel>() },
            { typeof(TransitionType), Enum.GetValues<TransitionType>() },
            { typeof(SkillLevel), Enum.GetValues<SkillLevel>() },
            { typeof(CrosshairStyle), Enum.GetValues<CrosshairStyle>() },
            { typeof(CrossColor), Enum.GetValues<CrossColor>() },
            { typeof(StatusBarSizeType), Enum.GetValues<StatusBarSizeType>() },
            { typeof(PlayerGender), Enum.GetValues<PlayerGender>() },
            { typeof(RenderVsyncMode), Enum.GetValues<RenderVsyncMode>() },
            { typeof(FilterType), Enum.GetValues<FilterType>() },
            { typeof(RenderLightMode), Enum.GetValues<RenderLightMode>() },
            { typeof(RenderWindowState), Enum.GetValues<RenderWindowState>() },
            { typeof(WindowBorder), Enum.GetValues<WindowBorder>() },
            { typeof(RenderColorMode), Enum.GetValues<RenderColorMode>() },
            { typeof(BlitFilter), Enum.GetValues<BlitFilter>() }
        };

        public static Dictionary<Type, Dictionary<Enum, string>> KnownEnumLabels { get; } = new Dictionary<Type, Dictionary<Enum, string>>()
        {
            { typeof(RandomPitch), GetDescriptions<RandomPitch>() },
            { typeof(Synth), GetDescriptions<Synth>() },
            { typeof(CompLevel), GetDescriptions<CompLevel>() },
            { typeof(TransitionType), GetDescriptions<TransitionType>() },
            { typeof(SkillLevel), GetDescriptions<SkillLevel>() },
            { typeof(CrosshairStyle), GetDescriptions<CrosshairStyle>() },
            { typeof(CrossColor), GetDescriptions<CrossColor>() },
            { typeof(StatusBarSizeType), GetDescriptions<StatusBarSizeType>() },
            { typeof(PlayerGender), GetDescriptions<PlayerGender>() },
            { typeof(RenderVsyncMode), GetDescriptions<RenderVsyncMode>() },
            { typeof(FilterType), GetDescriptions<FilterType>() },
            { typeof(RenderLightMode), GetDescriptions<RenderLightMode>() },
            { typeof(RenderWindowState), GetDescriptions<RenderWindowState>() },
            { typeof(WindowBorder), GetDescriptions<WindowBorder>() },
            { typeof(RenderColorMode), GetDescriptions<RenderColorMode>() },
            { typeof(BlitFilter), GetDescriptions<BlitFilter>() }
        };

        private static Dictionary<Enum, string> GetDescriptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum
        {
            Dictionary<Enum, string> dict = new Dictionary<Enum, string>();

            foreach (T value in Enum.GetValues<T>())
            {
                dict[value] = GetDescriptionOrLabel(value);
            }

            return dict;
        }

        private static string GetDescriptionOrLabel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(T value) where T : struct, Enum
        {
            var fi = typeof(T).GetField(value.ToString() ?? "");
            var descAttr = fi?.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
                return descAttr.Description;
            return value.ToString() ?? string.Empty;
        }
    }
}
