using Helion.Util.Container;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;

namespace Helion.World.Physics;

public sealed partial class PhysicsManager
{
    readonly record struct MoveLinkPlane(SectorPlaneFace Face, SectorLinkFlags Flag, SectorLinkFlags MirrorFlag);

    private readonly DynamicArray<SectorLink> m_resetSectorLinks = [];

    private SectorMoveStatus MoveLinkedSectors(SectorMoveSpecial moveSpecial, DynamicArray<SectorLink> sectorLinks, double destZ)
    {
        var sector = moveSpecial.Sector;
        var sectorPlane = moveSpecial.SectorPlane;
        var face = moveSpecial.MoveData.SectorMoveType;
        var speed = moveSpecial.MoveSpeed;
        var flags = moveSpecial.MoveData.Flags;
        var moveAmount = destZ - sectorPlane.Z;

        moveSpecial.MoveData.Flags |= SectorMoveFlags.EntityBlockMovement;

        var status = MoveLinkedSectorsByAmount(moveSpecial, sectorLinks, moveAmount, speed, m_resetSectorLinks);
        if ((status & SectorMoveStatus.Blocked) != 0 && m_resetSectorLinks.Length > 0)
            MoveLinkedSectorsByAmount(moveSpecial, m_resetSectorLinks, -moveAmount, speed, null, resetInterpolation: true);

        m_resetSectorLinks.Clear();

        moveSpecial.Sector = sector;
        moveSpecial.SectorPlane = sectorPlane;
        moveSpecial.MoveData.SectorMoveType = face;
        moveSpecial.MoveData.Flags = flags;

        return status;
    }

    private SectorMoveStatus MoveLinkedSectorsByAmount(SectorMoveSpecial moveSpecial, DynamicArray<SectorLink> sectorLinks, double moveAmount, double speed,
        DynamicArray<SectorLink>? processedLinks, bool resetInterpolation = false)
    {
        var status = SectorMoveStatus.Success;
        var ceilingMove = new MoveLinkPlane(SectorPlaneFace.Ceiling, SectorLinkFlags.Ceiling, SectorLinkFlags.CeilingMirror);
        var floorMove = new MoveLinkPlane(SectorPlaneFace.Floor, SectorLinkFlags.Floor, SectorLinkFlags.FloorMirror);

        for (int i = 0; i < sectorLinks.Length; i++)
        {
            var firstMove = ceilingMove;
            var secondMove = floorMove;

            ref var link = ref sectorLinks.Data[i];
            moveSpecial.Sector = link.Sector;

            switch(link.Flags)
            {
                case SectorLinkFlags.FloorAndCeiling:
                    if (moveAmount < 0)
                        (firstMove, secondMove) = (secondMove, firstMove);
                    break;
                case SectorLinkFlags.FloorAndCeilingMirror:
                    if (moveAmount > 0)
                        (firstMove, secondMove) = (secondMove, firstMove);
                    break;
                case SectorLinkFlags.FloorNormalAndCeilingMirror:
                case SectorLinkFlags.CeilingNormalAndFloorMirror:
                    (firstMove, secondMove) = (secondMove, firstMove);
                    break;
            }

            if ((link.Flags & firstMove.Flag) != 0)
            {
                status = MoveLinkedPlane(firstMove, moveSpecial, speed, moveAmount, link).Merge();
                if ((status & SectorMoveStatus.Blocked) != 0)
                    break;
            }

            if ((link.Flags & secondMove.Flag) != 0)
            {
                status = MoveLinkedPlane(secondMove, moveSpecial, speed, moveAmount, link).Merge();
                if ((status & SectorMoveStatus.Blocked) != 0)
                    break;
            }

            if (resetInterpolation)
                moveSpecial.ResetInterpolation();
            processedLinks?.Add(link);
        }

        return status;
    }

    private SectorMoveStatus MoveLinkedPlane(MoveLinkPlane moveLinkPlane, SectorMoveSpecial moveSpecial, double speed, double moveAmount, in SectorLink link)
    {
        var saveMoveSpeed = moveSpecial.MoveSpeed;
        moveAmount = (link.Flags & moveLinkPlane.MirrorFlag) == 0 ? moveAmount : -moveAmount;
        moveSpecial.MoveSpeed = moveAmount;

        moveSpecial.SectorPlane = link.Sector.GetSectorPlane(moveLinkPlane.Face);
        moveSpecial.MoveData.SectorMoveType = moveLinkPlane.Face;
        var linkDestZ = moveSpecial.SectorPlane.Z + moveAmount;

        if (moveSpecial.IsInitialMove)
            m_world.InvokeSectorMoveStart(moveSpecial.SectorPlane);

        var status = MoveSectorZ(speed, linkDestZ, moveSpecial, moveSpecial.Sector, checkSector3D: true, checkSectorLinks: false);
        if ((status & SectorMoveStatus.Blocked) == 0)
        {
            moveSpecial.SetSectorDataChange();
            m_world.InvokeSectorMove(moveSpecial.SectorPlane);
        }

        moveSpecial.MoveSpeed = saveMoveSpeed;
        return status;
    }
}
