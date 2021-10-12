using System;
using Helion.Render.Common.Enums;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities;

public class GLInfo
{
    public readonly GpuVendor Vendor;
    public readonly string VendorInfo;
    public readonly string ShadingVersion;
    public readonly string Renderer;

    internal GLInfo()
    {
        Renderer = GL.GetString(StringName.Renderer);
        ShadingVersion = GL.GetString(StringName.ShadingLanguageVersion);
        VendorInfo = GL.GetString(StringName.Vendor);
        Vendor = FromVendorInfo(VendorInfo);
    }

    private static GpuVendor FromVendorInfo(string vendorInfo)
    {
        foreach (GpuVendor vendor in Enum.GetValues<GpuVendor>())
            if (vendorInfo.Contains(vendor.ToString(), StringComparison.OrdinalIgnoreCase))
                return vendor;
        return GpuVendor.Unknown;
    }
}
