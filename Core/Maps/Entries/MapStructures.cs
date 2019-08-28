namespace Helion.Maps.MapStructures
{
    /// <summary>
    /// A list of vanilla specific constants.
    /// </summary>
    public static class Constants
    {
        public static readonly ushort NoSidedef = 0xFFFF;
        public static readonly ushort NodeIsSubsectorMask = 0x8000;
        public static readonly ushort BlockmapListStart = 0x0000;
        public static readonly ushort BlockmapListEnd = 0xFFFF;
        public static readonly float AngleToFullRangeFactor = 65536.0f / 360.0f;
    }

    public struct Vertex
    {
        public static readonly int Bytes = 4;

        public short X;
        public short Y;

        public Vertex(short x, short y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Sector
    {
        public static readonly int Bytes = 26;

        public short FloorHeight;
        public short CeilingHeight;
        public string FloorTexture;
        public string CeilingTexture;
        public short LightLevel;
        public short Special;
        public short Tag;

        public Sector(short floorHeight, short ceilingHeight, string floorTexture,
            string ceilingTexture, short lightLevel, short special, short tag)
        {
            FloorHeight = floorHeight;
            CeilingHeight = ceilingHeight;
            FloorTexture = floorTexture;
            CeilingTexture = ceilingTexture;
            LightLevel = lightLevel;
            Special = special;
            Tag = tag;
        }
    }

    public struct Sidedef
    {
        public static readonly int Bytes = 30;

        public short OffsetX;
        public short OffsetY;
        public string UpperTexture;
        public string LowerTexture;
        public string MiddleTexture;
        public ushort SectorIndex;

        public Sidedef(short offsetX, short offsetY, string upperTexture, string lowerTexture,
            string middleTexture, ushort sectorIndex)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            UpperTexture = upperTexture;
            LowerTexture = lowerTexture;
            MiddleTexture = middleTexture;
            SectorIndex = sectorIndex;
        }
    }

    public struct LinedefDoom
    {
        public static readonly int Bytes = 14;

        public ushort StartVertexId;
        public ushort EndVertexId;
        public ushort Flags;
        public ushort LineType;
        public ushort SectorTag;
        public ushort RightSidedef;
        public ushort LeftSidedef;

        public bool OneSided => LeftSidedef == Constants.NoSidedef;

        public LinedefDoom(ushort startVertexId, ushort endVertexId, ushort flags,
            ushort lineType, ushort sectorTag, ushort rightSidedef, ushort leftSidedef)
        {
            StartVertexId = startVertexId;
            EndVertexId = endVertexId;
            Flags = flags;
            LineType = lineType;
            SectorTag = sectorTag;
            RightSidedef = rightSidedef;
            LeftSidedef = leftSidedef;
        }
    }

    public struct LinedefHexen
    {
        public static readonly int Bytes = 16;

        public ushort StartVertexId;
        public ushort EndVertexId;
        public ushort Flags;
        public byte ActionSpecial;
        public byte[] Args;
        public ushort RightSidedef;
        public ushort LeftSidedef;

        public bool OneSided => LeftSidedef == Constants.NoSidedef;

        public LinedefHexen(ushort startVertexId, ushort endVertexId, ushort flags, byte actionSpecial,
            byte[] args, ushort rightSidedef, ushort leftSidedef)
        {
            StartVertexId = startVertexId;
            EndVertexId = endVertexId;
            Flags = flags;
            ActionSpecial = actionSpecial;
            Args = args;
            RightSidedef = rightSidedef;
            LeftSidedef = leftSidedef;
        }
    }

    public struct Segment
    {
        public static readonly int Bytes = 12;

        public ushort StartVertexId;
        public ushort EndVertexId;
        public ushort Angle; // Bit angle (0 - 65536, 0 = east).
        public ushort LinedefId;
        public ushort Direction; // 0 = front [same direction as linedef], 1 = back
        public short Offset;

        public Segment(ushort startVertexId, ushort endVertexId, ushort angle, ushort linedefId,
            ushort direction, short offset)
        {
            StartVertexId = startVertexId;
            EndVertexId = endVertexId;
            Angle = angle;
            LinedefId = linedefId;
            Direction = direction;
            Offset = offset;
        }
    }

    public struct Subsector
    {
        public static readonly int Bytes = 4;

        public ushort SegmentCount;
        public ushort FirstSegmentId;

        public Subsector(ushort segmentCount, ushort firstSegmentId)
        {
            SegmentCount = segmentCount;
            FirstSegmentId = firstSegmentId;
        }
    }

    public struct NodeBoundingBox
    {
        public short Top;
        public short Bottom;
        public short Left;
        public short Right;

        public NodeBoundingBox(short top, short bottom, short left, short right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }
    }

    public struct Node
    {
        public static readonly int Bytes = 28;

        public short PartitionX;
        public short PartitionY;
        public short DeltaX;
        public short DeltaY;
        public NodeBoundingBox RightBoundingBox;
        public NodeBoundingBox LeftBoundingBox;
        public short RightChild;
        public short LeftChild;

        public Node(short partitionX, short partitionY, short deltaX, short deltaY,
            NodeBoundingBox rightBoundingBox, NodeBoundingBox leftBoundingBox,
            short rightChild, short leftChild)
        {
            PartitionX = partitionX;
            PartitionY = partitionY;
            DeltaX = deltaX;
            DeltaY = deltaY;
            RightBoundingBox = rightBoundingBox;
            LeftBoundingBox = leftBoundingBox;
            RightChild = rightChild;
            LeftChild = leftChild;
        }
    }

    public struct ThingDoom
    {
        public static readonly int Bytes = 10;

        public short X;
        public short Y;
        public ushort Angle; // This is in degrees, not from 0 - 65535 as we want.
        public ushort Type;
        public ushort SpawnFlags;

        public ThingDoom(short x, short y, ushort angle, ushort type, ushort spawnFlags)
        {
            X = x;
            Y = y;
            Angle = angle;
            Type = type;
            SpawnFlags = spawnFlags;
        }

        public ushort FromDegreesToFullRange() => (ushort)(Angle * Constants.AngleToFullRangeFactor);
    }

    public struct ThingHexen
    {
        public static readonly int Bytes = 20;

        public ushort ThingId;
        public short X;
        public short Y;
        public short Z;
        public ushort Angle; // Bit angle (0 - 65536, 0 = east).
        public ushort Type;
        public ushort SpawnFlags;
        public byte ActionSpecial;
        public byte Args;

        public ThingHexen(ushort thingId, short x, short y, short z, ushort angle, ushort type,
            ushort spawnFlags, byte actionSpecial, byte args)
        {
            ThingId = thingId;
            X = x;
            Y = y;
            Z = z;
            Angle = angle;
            Type = type;
            SpawnFlags = spawnFlags;
            ActionSpecial = actionSpecial;
            Args = args;
        }

        public ushort FromDegreesToFullRange() => (ushort)(Angle * Constants.AngleToFullRangeFactor);
    }
}
