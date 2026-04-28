using Helion.Render.OpenGL.Textures;
using Helion.Util.Loggers;
using System;
using System.IO;
using static Helion.Util.Constants;

namespace Helion.Render;

public partial class Renderer
{
    public void DumpBuffers()
    {
        WriteBufferFile(m_sectorLightsBuffer, "SectorLights.txt", LightBuffer.SectorIndexStart, LightBuffer.BufferSize);
        WriteBufferFile(m_sectorColorMapsBuffer, "SectorColorMaps.txt", LightBuffer.SectorIndexStart * 4, LightBuffer.BufferSize * 4);
        WriteBufferFile(m_sectorFogBuffer, "SectorFog.txt", LightBuffer.SectorIndexStart * 4, LightBuffer.BufferSize * 4);
        WriteBufferFile(m_mapDataBuffer, "MapData.txt", 0, 4);
        WriteBufferFile(m_lineHeightsBuffer, "LineHeights.txt", 0, 4);
    }

    private static unsafe void WriteBufferFile<T>(GLBufferTextureStorage<T>? buffer, string path, int headerLength, int componentsPerLine) where T : struct
    {
        if (buffer == null)
            return;

        try
        {
            if (File.Exists(path))
                File.Delete(path);

            using var writer = new StreamWriter(path);

            var mappedBuffer = buffer.GetMappedBufferAndBind();
            var data = mappedBuffer.MappedMemoryPtr;

            for (int i = 0; i < headerLength; i++)
            {
                if (i > 0)
                    writer.Write(',');

                writer.Write(data[i]);
            }

            if (headerLength > 0)
                writer.WriteLine("");

            var componentCount = 0;
            for (int i = headerLength; i < buffer.DataLength(); i++)
            {
                if (componentCount > 0)
                    writer.Write(',');

                writer.Write(data[i]);

                componentCount++;

                if (componentCount == componentsPerLine)
                {
                    componentCount = 0;
                    writer.WriteLine("");
                }
            }
        }
        catch (Exception ex)
        {
            HelionLog.Error(ex.Message);
        }
    }
}
