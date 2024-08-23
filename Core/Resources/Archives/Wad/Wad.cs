using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Bytes;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Archives;

/// <summary>
/// A wad specific archive implementation.
/// </summary>
public class Wad : Archive
{
    private const int LumpTableEntryBytes = 16;

    public WadHeader Header;
    private readonly ByteReader m_byteReader;
    private readonly IIndexGenerator m_indexGenerator;

    private readonly Dictionary<string, ResourceNamespace> m_entryToNamespace = new(StringComparer.OrdinalIgnoreCase)
    {
        ["F_START"] = ResourceNamespace.Flats,
        ["F_END"] = ResourceNamespace.Global,
        ["FF_START"] = ResourceNamespace.Flats,
        ["FF_END"] = ResourceNamespace.Global,
        ["HI_START"] = ResourceNamespace.Textures,
        ["HI_END"] = ResourceNamespace.Global,
        ["P_START"] = ResourceNamespace.Textures,
        ["P_END"] = ResourceNamespace.Global,
        ["PP_START"] = ResourceNamespace.Textures,
        ["PP_END"] = ResourceNamespace.Global,
        ["S_START"] = ResourceNamespace.Sprites,
        ["S_END"] = ResourceNamespace.Global,
        ["SS_START"] = ResourceNamespace.Sprites,
        ["SS_END"] = ResourceNamespace.Global,
        ["T_START"] = ResourceNamespace.Textures,
        ["T_END"] = ResourceNamespace.Global,
        ["TX_START"] = ResourceNamespace.Textures,
        ["TX_END"] = ResourceNamespace.Global,
        ["C_START"] = ResourceNamespace.Colormaps,
        ["C_END"] = ResourceNamespace.Global,
        ["CC_START"] = ResourceNamespace.Colormaps,
        ["CC_END"] = ResourceNamespace.Global,
    };

    public Wad(IEntryPath path, IIndexGenerator indexGenerator) : base(path)
    {
        m_byteReader = new ByteReader(new BinaryReader(File.Open(Path.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)));
        m_indexGenerator = indexGenerator;
        LoadWadEntries();
    }

    public byte[] ReadData(WadEntry entry)
    {
        Precondition(entry.Parent == this, "Bad entry parent");

        m_byteReader.Offset(entry.Offset);
        return m_byteReader.ReadBytes(entry.Size);
    }

    public override void Dispose()
    {
        m_byteReader.Close();
        m_byteReader.Dispose();
        GC.SuppressFinalize(this);
    }

    private WadHeader ReadHeader()
    {
        m_byteReader.Offset(0);

        string headerId = Encoding.UTF8.GetString(m_byteReader.ReadBytes(4));
        bool isIwad = (headerId[0] == 'I');
        int numEntries = m_byteReader.ReadInt32();
        int entryTableOffset = m_byteReader.ReadInt32();

        return new WadHeader(isIwad, numEntries, entryTableOffset);
    }

    private (int offset, int size, string name) ReadDirectoryEntry()
    {
        int offset = m_byteReader.ReadInt32();
        int size = m_byteReader.ReadInt32();
        string name = m_byteReader.ReadEightByteString();
        return (offset, size, name);
    }

    private void LoadWadEntries()
    {
        Header = ReadHeader();

        if (Header.DirectoryTableOffset + (Header.EntryCount * LumpTableEntryBytes) > m_byteReader.Length)
            throw new HelionException("Lump entry table runs out of data");

        ResourceNamespace currentNamespace = ResourceNamespace.Global;

        m_byteReader.Offset(Header.DirectoryTableOffset);
        for (int i = 0; i < Header.EntryCount; i++)
        {
            (int offset, int size, string upperName) = ReadDirectoryEntry();

            // It appears that some markers have an offset of zero, so
            // it is an acceptable offset (we can't check < 12).
            if (offset < 0)
                throw new HelionException("Lump entry data location underflows");
            if (offset + size > m_byteReader.Length)
                throw new HelionException("Lump entry data location overflows");

            bool isMarker = false;
            if (m_entryToNamespace.TryGetValue(upperName, out ResourceNamespace resourceNamespace))
            {
                isMarker = true;
                currentNamespace = resourceNamespace;
            }

            int index = m_indexGenerator.GetIndex(this);
            WadEntryPath entryPath = new(upperName);
            Entries.Add(new WadEntry(this, offset, size, entryPath, isMarker ? ResourceNamespace.Global : currentNamespace, index));
        }
    }
}
