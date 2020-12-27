using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Players;
using System;
using System.Drawing;

namespace Helion.World.Entities.Inventories.Powerups
{
    public class PowerupBase : IPowerup
    {
        private int m_tics;
        private int m_colorTics;
        private Color? m_drawColor;
        private Player m_player;

        public PowerupBase(Player player, EntityDefinition definition, PowerupType type)
        {
            m_player = player;
            EntityDefinition = definition;
            PowerupType = type;
            SetTics();

            if (EntityDefinition.Properties.Powerup.Color != null)
            {
                m_drawColor = GetColor(EntityDefinition.Properties.Powerup.Color);
                DrawAlpha = (float)EntityDefinition.Properties.Powerup.Color.Alpha;
            }

            InitType();
        }

        private void SetTics()
        {
            if (EntityDefinition.Properties.Powerup.Duration < 0)
                m_tics = -EntityDefinition.Properties.Powerup.Duration * (int)Constants.TicksPerSecond;
            else
                m_tics = EntityDefinition.Properties.Powerup.Duration;

            if (PowerupType == PowerupType.Strength)
                m_colorTics = 60 * (int)Constants.TicksPerSecond;
            else
                m_colorTics = m_tics;
        }

        private Color? GetColor(PowerupColor color)
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

        public virtual InventoryTickStatus Tick(Player player)
        {
            if (EntityDefinition.Properties.Powerup.Strength > 0)
                return InventoryTickStatus.Continue;

            if (PowerupType == PowerupType.Strength)
                m_tics++;
            else
                m_tics--;

            m_colorTics--;

            if (m_colorTics <= 0)
                m_drawColor = null;

            if (m_tics <= 0)
            {
                HandleDestroy();
                return InventoryTickStatus.Destroy;
            }

            return InventoryTickStatus.Continue;
        }

        private void InitType()
        {
            // TODO temporary until colormap implemented
            if (PowerupType == PowerupType.Invulnerable)
            {
                m_drawColor = Color.White;
                DrawAlpha = 0.5f;
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
