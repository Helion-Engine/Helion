using Helion.Resources.Definitions.Decorate.Properties;

namespace Helion.Resources.Definitions.Decorate.Parser
{
    public partial class DecorateParser
    {
        private WeaponBob ConsumeBobStyleProperty()
        {
            string bobStyleText = ConsumeString();
            switch (bobStyleText.ToUpper())
            {
            case "ALPHA":
                return WeaponBob.Alpha;
            case "INVERSEALPHA":
                return WeaponBob.InverseAlpha;
            case "INVERSENORMAL":
                return WeaponBob.InverseNormal;
            case "INVERSESMOOTH":
                return WeaponBob.InverseSmooth;
            case "NORMAL":
                return WeaponBob.Normal;
            case "SMOOTH":
                return WeaponBob.Smooth;
            default:
                ThrowException($"Unknown weapon bob type '{bobStyleText}' on actor '{m_currentDefinition.Name}'");
                return WeaponBob.Normal;
            }
        }
        
        private void ConsumeActorPropertyOrCombo()
        {
            string property = ConsumeString();

            if (ConsumeIf('.'))
            {
                switch (property.ToUpper())
                {
                case "AMMO":
                    ConsumeAmmoProperty();
                    break;
                case "ARMOR":
                    ConsumeArmorProperty();
                    break;
                case "FAKEINVENTORY":
                    ConsumeFakeInventoryProperty();
                    break;
                case "HEALTH":
                    ConsumeHealthProperty();
                    break;
                case "HEALTHPICKUP":
                    ConsumeHealthPickupProperty();
                    break;
                case "INVENTORY":
                    ConsumeInventoryProperty();
                    break;
                case "MORPHPROJECTILE":
                    ConsumeMorphProjectileProperty();
                    break;
                case "PLAYER":
                    ConsumePlayerProperty();
                    break;
                case "POWERUP":
                    ConsumePowerupProperty();
                    break;
                case "PUZZLEITEM":
                    ConsumePuzzleItemProperty();
                    break;
                case "WEAPON":
                    ConsumeWeaponProperty();
                    break;
                case "WEAPONPIECE":
                    ConsumeWeaponPieceProperty();
                    break;
                default:
                    ThrowException($"Unknown prefix property '{property}' on actor '{m_currentDefinition.Name}'");
                    return;
                }
            }
            else
                ConsumeTopLevelPropertyOrCombo(property);
        }

        private void ConsumePuzzleItemProperty()
        {
            string nestedProperty = ConsumeIdentifier();
            switch (nestedProperty.ToUpper())
            {
            case "NUMBER":
                m_currentDefinition.Properties.PuzzleItem.Number = ConsumeInteger();
                break;
            case "FAILMESSAGE":
                m_currentDefinition.Properties.PuzzleItem.FailMessage = ConsumeString();
                break;
            default:
                Log.Warn("Unknown puzzle item property suffix '{0}' for actor {1} (in particular, PUZZLEITEM.{2})", nestedProperty, m_currentDefinition.Name);
                break;
            }
        }

        private void ConsumeWeaponProperty()
        {
            string nestedProperty = ConsumeIdentifier();
            switch (nestedProperty.ToUpper())
            {
            case "AMMOGIVE":
                m_currentDefinition.Properties.Weapons.AmmoGive = ConsumeInteger();
                break;
            case "AMMOGIVE1":
                m_currentDefinition.Properties.Weapons.AmmoGive1 = ConsumeInteger();
                break;
            case "AMMOGIVE2":
                m_currentDefinition.Properties.Weapons.AmmoGive2 = ConsumeInteger();
                break;
            case "AMMOTYPE":
                m_currentDefinition.Properties.Weapons.AmmoType = ConsumeString();
                break;
            case "AMMOTYPE1":
                m_currentDefinition.Properties.Weapons.AmmoType1 = ConsumeString();
                break;
            case "AMMOTYPE2":
                m_currentDefinition.Properties.Weapons.AmmoType2 = ConsumeString();
                break;
            case "AMMOUSE":
                m_currentDefinition.Properties.Weapons.AmmoUse = ConsumeInteger();
                break;
            case "AMMOUSE1":
                m_currentDefinition.Properties.Weapons.AmmoUse1 = ConsumeInteger();
                break;
            case "AMMOUSE2":
                m_currentDefinition.Properties.Weapons.AmmoUse2 = ConsumeInteger();
                break;
            case "BOBRANGEX":
                m_currentDefinition.Properties.Weapons.BobRangeX = ConsumeFloat();
                break;
            case "BOBRANGEY":
                m_currentDefinition.Properties.Weapons.BobRangeY = ConsumeFloat();
                break;
            case "BOBSPEED":
                m_currentDefinition.Properties.Weapons.BobSpeed = ConsumeFloat();
                break;
            case "BOBSTYLE":
                m_currentDefinition.Properties.Weapons.BobStyle = ConsumeBobStyleProperty();
                break;
            case "DEFAULTKICKBACK":
                m_currentDefinition.Properties.Weapons.DefaultKickBack = true;
                break;
            case "KICKBACK":
                m_currentDefinition.Properties.Weapons.KickBack = ConsumeInteger();
                break;
            case "LOOKSCALE":
                m_currentDefinition.Properties.Weapons.LookScale = ConsumeFloat();
                break;
            case "MINSELECTIONAMMO1":
                m_currentDefinition.Properties.Weapons.MinSelectionAmmo1 = ConsumeInteger();
                break;
            case "MINSELECTIONAMMO2":
                m_currentDefinition.Properties.Weapons.MinSelectionAmmo2 = ConsumeInteger();
                break;
            case "READYSOUND":
                m_currentDefinition.Properties.Weapons.ReadySound = ConsumeString();
                break;
            case "SELECTIONORDER":
                m_currentDefinition.Properties.Weapons.SelectionOrder = ConsumeInteger();
                break;
            case "SISTERWEAPON":
                m_currentDefinition.Properties.Weapons.SisterWeapon = ConsumeString();
                break;
            case "SLOTNUMBER":
                m_currentDefinition.Properties.Weapons.SlotNumber = ConsumeInteger();
                break;
            case "SLOTPRIORITY":
                m_currentDefinition.Properties.Weapons.SlotPriority = ConsumeFloat();
                break;
            case "UPSOUND":
                m_currentDefinition.Properties.Weapons.UpSound = ConsumeString();
                break;
            case "YADJUST":
                m_currentDefinition.Properties.Weapons.YAdjust = ConsumeInteger();
                break;
            default:
                Log.Warn("Unknown weapon property suffix '{0}' for actor {1} (in particular, WEAPON.{2})", nestedProperty, m_currentDefinition.Name);
                break;
            }
        }

        private void ConsumeWeaponPieceProperty()
        {
            string nestedProperty = ConsumeIdentifier();
            switch (nestedProperty.ToUpper())
            {
            case "NUMBER":
                m_currentDefinition.Properties.WeaponPieces.Number = ConsumeInteger();
                break;
            case "WEAPON":
                m_currentDefinition.Properties.WeaponPieces.Weapon = ConsumeString();
                break;
            default:
                Log.Warn("Unknown weapon piece property suffix '{0}' for actor {1} (in particular, WEAPONPIECE.{2})", nestedProperty, m_currentDefinition.Name);
                break;
            }
        }

        private void ConsumeTopLevelPropertyOrCombo(string property)
        {
            switch (property.ToUpper())
            {
            // To reduce logic and be efficient, we do combos with the top
            // level properties.
            case "MONSTER":
                m_currentDefinition.Flags.Monster = true;
                break;
            case "PROJECTILE":
                m_currentDefinition.Flags.Projectile = true;
                break;
            // The rest are all properties now.
            // TODO
            default:
                ThrowException($"Unknown property '{property}' on actor '{m_currentDefinition.Name}'");
                return;
            }
        }
    }
}