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
        if ((status & SectorMoveStatus.Blocked) != 0)
            MoveLinkedSectorsByAmount(moveSpecial, m_resetSectorLinks, -moveAmount, speed, null);

        m_resetSectorLinks.Clear();

        moveSpecial.Sector = sector;
        moveSpecial.SectorPlane = sectorPlane;
        moveSpecial.MoveData.SectorMoveType = face;
        moveSpecial.MoveData.Flags = flags;

        return status;
    }

    private SectorMoveStatus MoveLinkedSectorsByAmount(SectorMoveSpecial moveSpecial, DynamicArray<SectorLink> sectorLinks, double moveAmount, double speed,
        DynamicArray<SectorLink>? processedLinks)
    {
        var status = SectorMoveStatus.Success;
        var firstMove = new MoveLinkPlane(SectorPlaneFace.Ceiling, SectorLinkFlags.Ceiling, SectorLinkFlags.CeilingMirror);
        var secondMove = new MoveLinkPlane(SectorPlaneFace.Floor, SectorLinkFlags.Floor, SectorLinkFlags.FloorMirror);

        if (moveAmount < 0)
            (firstMove, secondMove) = (secondMove, firstMove);

        for (int i = 0; i < sectorLinks.Length; i++)
        {
            ref var link = ref sectorLinks.Data[i];
            moveSpecial.Sector = link.Sector;

            if ((link.Flags & firstMove.Flag) != 0)
            {
                var firstMoveAmount = (link.Flags & firstMove.MirrorFlag) == 0 ? moveAmount : -moveAmount;
                moveSpecial.MoveSpeed = firstMoveAmount;

                var testStatus = MoveLinkedPlane(firstMove.Face, moveSpecial, speed, firstMoveAmount, link);
                if ((testStatus & SectorMoveStatus.Blocked) != 0)
                {
                    status = SectorMoveStatus.Blocked;
                    break;
                }
            }

            if ((link.Flags & secondMove.Flag) != 0)
            {
                var secondMoveAmount = (link.Flags & secondMove.MirrorFlag) == 0 ? moveAmount : -moveAmount;
                moveSpecial.MoveSpeed = secondMoveAmount;

                var testStatus = MoveLinkedPlane(secondMove.Face, moveSpecial, speed, secondMoveAmount, link);
                if ((testStatus & SectorMoveStatus.Blocked) != 0)
                {
                    status = SectorMoveStatus.Blocked;
                    break;
                }
            }

            processedLinks?.Add(link);
        }

        return status;
    }

    private SectorMoveStatus MoveLinkedPlane(SectorPlaneFace face, SectorMoveSpecial moveSpecial, double speed, double moveAmount, in SectorLink link)
    {
        moveSpecial.SectorPlane = link.Sector.GetSectorPlane(face);
        moveSpecial.MoveData.SectorMoveType = face;
        var linkDestZ = moveSpecial.SectorPlane.Z + moveAmount;

        if (moveSpecial.IsInitialMove)
            m_world.InvokeSectorMoveStart(moveSpecial.SectorPlane);

        var status = MoveSectorZ(speed, linkDestZ, moveSpecial, moveSpecial.Sector, checkSector3D: true, checkSectorLinks: false);
        if ((status & SectorMoveStatus.Blocked) != 0)
            return status;

        moveSpecial.SetSectorDataChange();
        m_world.InvokeSectorMove(moveSpecial.SectorPlane);
        return status;
    }
}
