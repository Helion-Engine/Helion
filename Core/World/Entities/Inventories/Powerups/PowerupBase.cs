using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Players;
using System.Drawing;

namespace Helion.World.Entities.Inventories.Powerups
{
    public class PowerupBase : IPowerup
    {
        private const int EffectTicks = 60 * (int)Constants.TicksPerSecond;

        private readonly Player m_player;
        private int m_tics;
        private int m_effectTics;
        private float m_subAlpha;
        private Color? m_drawColor;

        public PowerupBase(Player player, EntityDefinition definition, PowerupType type)
        {
            m_player = player;
            EntityDefinition = definition;
            PowerupType = type;

            if (EntityDefinition.Properties.Powerup.Color != null)
            {
                m_drawColor = GetColor(EntityDefinition.Properties.Powerup.Color);
                DrawAlpha = (float)EntityDefinition.Properties.Powerup.Color.Alpha;
            }

            SetTics();
            InitType();
        }

        private void SetTics()
        {
            if (EntityDefinition.Properties.Powerup.Duration < 0)
                m_tics = -EntityDefinition.Properties.Powerup.Duration * (int)Constants.TicksPerSecond;
            else
                m_tics = EntityDefinition.Properties.Powerup.Duration;

            if (PowerupType == PowerupType.Strength)
            {
                m_effectTics = EffectTicks;
                m_subAlpha = DrawAlpha / EffectTicks;
            }
            else
            {
                m_effectTics = m_tics;
            }
        }

        private static Color? GetColor(PowerupColor color)
        {
            if (color.Color.Length < 8)
                return null;

            if (!int.TryParse(color.Color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out int r))
                return null;
            if (!int.TryParse(color.Color.Substring(3, 2), System.Globalization.NumberStyles.HexNumber, null, out int g))
                return null;
            if (!int.TryParse(color.Color.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out int b))
                return null;

            return Color.FromArgb(0, r, g, b);
        }

        public EntityDefinition EntityDefinition { get; private set; }

        public PowerupType PowerupType { get; private set; }

        public Color? DrawColor => m_drawColor;

        public float DrawAlpha { get; private set; }

        public bool DrawPowerupEffect { get; private set; } = true;

        public virtual InventoryTickStatus Tick(Player player)
        {
            if (PowerupType == PowerupType.Strength)
                m_tics++;
            else
                m_tics--;

            m_effectTics--;

            if (m_effectTics > 0)
                CheckDrawPowerupEffect();
            else
                m_drawColor = null;

            if (m_tics <= 0)
            {
                HandleDestroy();
                return InventoryTickStatus.Destroy;
            }

            return InventoryTickStatus.Continue;
        }

        private void CheckDrawPowerupEffect()
        {
            if (PowerupType == PowerupType.Strength && m_drawColor.HasValue)
            {
                DrawAlpha -= m_subAlpha;
                return;
            }

            DrawPowerupEffect = m_effectTics > 128 || (m_effectTics & 8) > 0;
        }

        private void InitType()
        {
            // TODO temporary until colormap implemented
            if (PowerupType == PowerupType.Invulnerable)
            {
                m_drawColor = Color.White;
            }
            else if (PowerupType == PowerupType.Invisibility)
            {
                m_player.Flags.Shadow = true;
            }
        }

        private void HandleDestroy()
        {
            if (PowerupType == PowerupType.Invisibility)
                m_player.Flags.Shadow = false;
        }

        public void Reset()
        {
            SetTics();
        }
    }
}
