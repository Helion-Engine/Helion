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
            { typeof(RenderColorMode), Enum.GetValues<RenderColorMode>() }
        };
    }
}
