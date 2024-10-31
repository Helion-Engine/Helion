using Helion.Graphics;
using Helion.Util.Extensions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Helion.Resources.Definitions.Locks;

public class LockDefinitions
{
    public readonly List<LockDef> LockDefs = [];

    public LockDefinitions()
    {
        LockDefs.Add(new LockDef()
        {
            DoorMessage = "$PD_REDC",
            ObjectMessage = "$PD_REDO",
            KeyNumber = 1,
            MapColor = Color.Red,
            KeyDefinitionNames = ["RedCard"]
        });

        LockDefs.Add(new LockDef()
        {
            DoorMessage = "$PD_BLUEC",
            ObjectMessage = "$PD_BLUEO",
            KeyNumber = 2,
            MapColor = Color.Blue,
            KeyDefinitionNames = ["BlueCard"]
        });

        LockDefs.Add(new LockDef()
        {
            DoorMessage = "$PD_YELLOWC",
            ObjectMessage = "$PD_YELLOWO",
            KeyNumber = 3,
            MapColor = Color.Yellow,
            KeyDefinitionNames = ["YellowCard"]
        });

        LockDefs.Add(new LockDef()
        {
            DoorMessage = "$PD_REDS",
            ObjectMessage = "$PD_REDO",
            KeyNumber = 4,
            MapColor = Color.Red,
            KeyDefinitionNames = ["RedSkull"]
        });

        LockDefs.Add(new LockDef()
        {
            DoorMessage = "$PD_BLUES",
            ObjectMessage = "$PD_BLUEO",
            KeyNumber = 5,
            MapColor = Color.Blue,
            KeyDefinitionNames = ["BlueSkull"]
        });

        LockDefs.Add(new LockDef()
        {
            DoorMessage = "$PD_YELLOWS",
            ObjectMessage = "$PD_REDO",
            KeyNumber = 6,
            MapColor = Color.Yellow,
            KeyDefinitionNames = ["YellowSkull"]
        });

        LockDefs.Add(new LockDef()
        {
            DoorMessage = "$PD_ALL6",
            ObjectMessage = "$PD_ALL6",
            KeyNumber = 101,
            MapColor = Color.Purple,
            KeyDefinitionNames = ["RedCard", "RedSkull", "BlueCard", "BlueSkull", "YellowCard", "YellowSkull"]
        });

        var anyRed = new LockDef()
        {
            DoorMessage = "$PD_REDK",
            ObjectMessage = "$PD_REDO",
            KeyNumber = 129,
            MapColor = Color.Red
        };
        anyRed.AnyKeyDefinitionNames.Add(["RedCard", "RedSkull"]);
        LockDefs.Add(anyRed);

        var anyBlue = new LockDef()
        {
            DoorMessage = "$PD_BLUEK",
            ObjectMessage = "$PD_BLUEO",
            KeyNumber = 130,
            MapColor = Color.Blue
        };
        anyBlue.AnyKeyDefinitionNames.Add(["BlueCard", "BlueSkull"]);
        LockDefs.Add(anyBlue);

        var anyYellow = new LockDef()
        {
            DoorMessage = "$PD_YELLOWK",
            ObjectMessage = "$PD_YELLOWO",
            KeyNumber = 131,
            MapColor = Color.Yellow,
        };
        anyYellow.AnyKeyDefinitionNames.Add(["YellowCard", "YellowSkull"]);
        LockDefs.Add(anyYellow);

        var any = new LockDef()
        {
            DoorMessage = "$PD_ANY",
            ObjectMessage = "$PD_ANY",
            KeyNumber = 100,
            MapColor = Color.LightBlue,
        };
        any.AnyKeyDefinitionNames.Add(["RedCard", "RedSkull", "BlueCard", "BlueSkull", "YellowCard", "YellowSkull"]);
        LockDefs.Add(any);

        var allThreeColors = new LockDef()
        {
            DoorMessage = "$PD_ALL3",
            ObjectMessage = "$PD_ALL3",
            KeyNumber = 229,
            MapColor = Color.Purple,
        };
        allThreeColors.AnyKeyDefinitionNames.Add(["RedCard", "RedSkull"]);
        allThreeColors.AnyKeyDefinitionNames.Add(["BlueCard", "BlueSkull"]);
        allThreeColors.AnyKeyDefinitionNames.Add(["YellowCard", "YellowSkull"]);
        LockDefs.Add(allThreeColors);
    }

    public LockDef? GetLockDef(int keyNumber) => LockDefs.FirstOrDefault(x => x.KeyNumber == keyNumber);

    public bool TryGetLockDef(string definitionName, [NotNullWhen(true)] out LockDef? lockDef)
    {
        for (int i = 0; i <  LockDefs.Count; i++)
        {
            var def = LockDefs[i];
            for (int j = 0; j < def.KeyDefinitionNames.Count; j++)
            {
                var name = def.KeyDefinitionNames[j];
                if (definitionName.EqualsIgnoreCase(name))
                {
                    lockDef = def;
                    return true;
                }
            }
        }

        lockDef = null;
        return false;
    }
}
