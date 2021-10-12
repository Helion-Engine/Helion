using Helion.Util;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using System.Collections.Generic;

namespace Helion.World.StatusBar;

public class PlayerStatusBar
{
    private const int PainFaces = 5;
    private const int StraightFaces = 3;
    private const int TurnFaces = 2;
    private const int SpecialFaces = 3;

    private const int FaceStride = StraightFaces + TurnFaces + SpecialFaces;

    private const int TurnOffset = StraightFaces;
    private const int OuchOffset = TurnOffset + TurnFaces;
    private const int EvilGrinOffset = OuchOffset + 1;
    private const int RampageOffset = EvilGrinOffset + 1;
    private const int GodFace = PainFaces * FaceStride;
    private const int DeadFace = GodFace + 1;

    private const int EvilGrinCount = (int)Constants.TicksPerSecond * 2;
    private const int StraightFaceCount = (int)Constants.TicksPerSecond / 2;
    private const int TurnCount = (int)Constants.TicksPerSecond;
    private const int RampageDelay = (int)Constants.TicksPerSecond * 2;

    private const int MuchPain = 20;

    private readonly Player m_player;
    private readonly List<string> m_faces = new List<string>();
    private int m_oldHeath;
    private int m_lastPainOffset;
    private int m_priority;
    private int m_faceIndex;
    private int m_faceCount;
    private int m_lastAttackCount = -1;
    private int m_random;

    public PlayerStatusBar(Player player)
    {
        m_player = player;

        for (int i = 0; i < PainFaces; i++)
        {
            for (int j = 0; j < StraightFaces; j++)
                m_faces.Add($"STFST{i}{j}");

            m_faces.Add($"STFTR{i}0");
            m_faces.Add($"STFTL{i}0");
            m_faces.Add($"STFOUCH{i}");
            m_faces.Add($"STFEVL{i}");
            m_faces.Add($"STFKILL{i}");
        }

        m_faces.Add("STFGOD0");
        m_faces.Add("STFDEAD0");
    }

    public void Tick()
    {
        m_random = m_player.World.Random.NextByte();
        UpdateFace();
    }

    public string GetFacePatch()
    {
        if (m_faceIndex >= 0 && m_faceIndex < m_faces.Count)
            return m_faces[m_faceIndex];

        return string.Empty;
    }

    private void UpdateFace()
    {
        if (m_priority < 10 && m_player.IsDead)
            DeathFace();

        if (m_priority < 9 && m_player.BonusCount > 0 && m_player.HasNewWeapon())
            NewWeaponFace();

        if (m_priority < 8 && m_player.DamageCount > 0 && m_player.Attacker != null && m_player.Attacker != m_player)
            EnemyDamageFace();

        if (m_priority < 7 && m_player.DamageCount > 0)
            EnvironmentDamageFace();

        if (m_priority < 6)
            AttackHoldFace();

        if (m_priority < 5 && m_player.IsInvulnerable)
            InvulnerableFace();

        if (m_faceCount == 0)
            LookingFace();

        m_faceCount--;
    }

    private void LookingFace()
    {
        m_faceIndex = GetPainOffset() + (m_random % 3);
        m_faceCount = StraightFaceCount;
        m_priority = 0;
    }

    private void InvulnerableFace()
    {
        m_priority = 4;
        m_faceIndex = GodFace;
        m_faceCount = 1;
    }

    private void AttackHoldFace()
    {
        if (m_player.TickCommand.Has(TickCommands.Attack))
        {
            if (m_lastAttackCount == -1)
            {
                m_lastAttackCount = RampageDelay;
            }
            else if (--m_lastAttackCount == 0)
            {
                m_priority = 5;
                m_faceIndex = GetPainOffset() + RampageOffset;
                m_faceCount = 1;
                m_lastAttackCount = 1;
            }
        }
        else
        {
            m_lastAttackCount = -1;
        }
    }

    private void EnvironmentDamageFace()
    {
        m_faceCount = TurnCount;

        if (m_oldHeath - m_player.Health > MuchPain)
        {
            m_priority = 7;
            m_faceIndex = GetPainOffset() + OuchOffset;
        }
        else
        {
            m_priority = 6;
            m_faceIndex = GetPainOffset() + RampageOffset;
        }
    }

    private void EnemyDamageFace()
    {
        if (m_player.Attacker == null)
            return;

        m_priority = 7;

        if (m_oldHeath - m_player.Health > MuchPain)
        {
            m_faceIndex = GetPainOffset() + OuchOffset;
            m_faceCount = TurnCount;
        }
        else
        {
            double playerAngle = MathHelper.GetPositiveAngle(m_player.AngleRadians);
            double angle = MathHelper.GetPositiveAngle(m_player.Position.Angle(m_player.Attacker.Position));
            bool right;

            if (angle > playerAngle)
            {
                angle -= playerAngle;
                right = angle > MathHelper.Pi;
            }
            else
            {
                angle = playerAngle - angle;
                right = angle <= MathHelper.Pi;
            }

            m_faceCount = TurnCount;
            m_faceIndex = GetPainOffset();

            if (angle < MathHelper.QuarterPi)
                m_faceIndex += RampageOffset;
            else if (right)
                m_faceIndex += TurnOffset;
            else
                m_faceIndex += TurnOffset + 1;
        }
    }

    private void NewWeaponFace()
    {
        m_priority = 8;
        m_faceIndex = GetPainOffset() + EvilGrinOffset;
        m_faceCount = EvilGrinCount;
    }

    private void DeathFace()
    {
        m_priority = 9;
        m_faceIndex = DeadFace;
        m_faceCount = 1;
    }

    private int GetPainOffset()
    {
        int maxHealth = 100;
        int health = MathHelper.Clamp(m_player.Health, 0, maxHealth);

        if (m_player.Health != m_oldHeath)
        {
            m_oldHeath = health;
            m_lastPainOffset = FaceStride * ((maxHealth - health) * PainFaces / (maxHealth + 1));
        }

        return m_lastPainOffset;
    }
}
