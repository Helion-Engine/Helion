﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Bytes;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Archives
{
    /// <summary>
    /// A wad specific archive implementation.
    /// </summary>
    public class Wad : Archive, IDisposable
    {
        private const int LumpTableEntryBytes = 16;

        public WadHeader Header;
        private readonly BinaryReader m_binaryReader;
        private readonly ByteReader m_byteReader;

        private readonly Dictionary<CIString, ResourceNamespace> m_entryToNamespace = new Dictionary<CIString, ResourceNamespace>()
        {
            ["F_START"] = ResourceNamespace.Flats,
            ["F_END"] = ResourceNamespace.Global,
            ["FF_START"] = ResourceNamespace.Flats,
            ["FF_END"] = ResourceNamespace.Global,
            ["HI_START"] = ResourceNamespace.Textures,
            ["HI_END"] = ResourceNamespace.Textures,
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
        };

        public Wad(IEntryPath path) : base(path)
        {
            m_binaryReader = new BinaryReader(File.Open(Path.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            m_byteReader = new ByteReader(m_binaryReader);
            LoadWadEntries();
        }

        public byte[] ReadData(WadEntry entry)
        {
            Precondition(entry.Parent == this, "Bad entry parent");

            m_byteReader.Position = entry.Offset;
            return m_byteReader.Bytes(entry.Size);
        }

        public void Dispose()
        {
            m_binaryReader.Close();
            m_binaryReader.Dispose();
        }

        private WadHeader ReadHeader()
        {
            m_byteReader.Position = 0;

            string headerId = Encoding.UTF8.GetString(m_byteReader.Bytes(4));
            bool isIwad = (headerId[0] == 'I');
            int numEntries = m_byteReader.Int();
            int entryTableOffset = m_byteReader.Int();

            return new WadHeader(isIwad, numEntries, entryTableOffset);
        }

        private (int offset, int size, string name) ReadDirectoryEntry()
        {
            int offset = m_byteReader.Int();
            int size = m_byteReader.Int();
            string name = m_byteReader.EightByteString().ToUpper();
            return (offset, size, name);
        }

        private void LoadWadEntries()
        {
            Header = ReadHeader();

            if (Header.DirectoryTableOffset + (Header.EntryCount * LumpTableEntryBytes) > m_byteReader.Length)
                throw new HelionException("Lump entry table runs out of data");

            ResourceNamespace currentNamespace = ResourceNamespace.Global;

            m_byteReader.Position = Header.DirectoryTableOffset;
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

                WadEntryPath entryPath = new WadEntryPath(upperName);
                Entries.Add(new WadEntry(this, offset, size, entryPath, isMarker ? ResourceNamespace.Global : currentNamespace));
            }
        }
    }
}