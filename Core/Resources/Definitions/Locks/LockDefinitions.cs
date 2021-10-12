using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Helion.Resources.Definitions.Locks;

public class LockDefinitions
{
    public readonly List<LockDef> LockDefs = new List<LockDef>();

    private static string NeedKeyMessage(string name) => $"You need {name}";

    public LockDefinitions()
    {
        LockDefs.Add(new LockDef()
        {
            Message = NeedKeyMessage("a red card"),
            KeyNumber = 1,
            MapColor = Color.Red,
            KeyDefinitionNames = new List<string>() { "RedCard" }
        });

        LockDefs.Add(new LockDef()
        {
            Message = NeedKeyMessage("a blue card"),
            KeyNumber = 2,
            MapColor = Color.Blue,
            KeyDefinitionNames = new List<string>() { "BlueCard" }
        });

        LockDefs.Add(new LockDef()
        {
            Message = NeedKeyMessage("a yellow card"),
            KeyNumber = 3,
            MapColor = Color.Yellow,
            KeyDefinitionNames = new List<string>() { "YellowCard" }
        });

        LockDefs.Add(new LockDef()
        {
            Message = NeedKeyMessage("a red skull"),
            KeyNumber = 4,
            MapColor = Color.Red,
            KeyDefinitionNames = new List<string>() { "RedSkull" }
        });

        LockDefs.Add(new LockDef()
        {
            Message = NeedKeyMessage("a blue skull"),
            KeyNumber = 5,
            MapColor = Color.Blue,
            KeyDefinitionNames = new List<string>() { "BlueSkull" }
        });

        LockDefs.Add(new LockDef()
        {
            Message = NeedKeyMessage("a yellow skull"),
            KeyNumber = 6,
            MapColor = Color.Yellow,
            KeyDefinitionNames = new List<string>() { "YellowSkull" }
        });

        LockDefs.Add(new LockDef()
        {
            Message = NeedKeyMessage("all six keys"),
            KeyNumber = 101,
            MapColor = Color.Purple,
            KeyDefinitionNames = new List<string>() { "RedCard", "RedSkull", "BlueCard", "BlueSkull", "YellowCard", "YellowSkull" }
        });

        var anyRed = new LockDef()
        {
            Message = NeedKeyMessage("a red key"),
            KeyNumber = 129,
            MapColor = Color.Red
        };
        anyRed.AnyKeyDefinitionNames.Add(new List<string>() { "RedCard", "RedSkull" });
        LockDefs.Add(anyRed);

        var anyBlue = new LockDef()
        {
            Message = NeedKeyMessage("a blue key"),
            KeyNumber = 130,
            MapColor = Color.Blue
        };
        anyBlue.AnyKeyDefinitionNames.Add(new List<string>() { "BlueCard", "BlueSkull" });
        LockDefs.Add(anyBlue);

        var anyYellow = new LockDef()
        {
            Message = NeedKeyMessage("a yellow key"),
            KeyNumber = 131,
            MapColor = Color.Yellow,
        };
        anyYellow.AnyKeyDefinitionNames.Add(new List<string>() { "YellowCard", "YellowSkull" });
        LockDefs.Add(anyYellow);

        var any = new LockDef()
        {
            Message = NeedKeyMessage("any key"),
            KeyNumber = 100,
            MapColor = Color.LightBlue,
        };
        any.AnyKeyDefinitionNames.Add(new List<string>() { "RedCard", "RedSkull", "BlueCard", "BlueSkull", "YellowCard", "YellowSkull" });
        LockDefs.Add(any);

        var allThreeColors = new LockDef()
        {
            Message = NeedKeyMessage("all three key colors"),
            KeyNumber = 229,
            MapColor = Color.Purple,
        };
        allThreeColors.AnyKeyDefinitionNames.Add(new List<string>() { "RedCard", "RedSkull" });
        allThreeColors.AnyKeyDefinitionNames.Add(new List<string>() { "BlueCard", "BlueSkull" });
        allThreeColors.AnyKeyDefinitionNames.Add(new List<string>() { "YellowCard", "YellowSkull" });
        LockDefs.Add(allThreeColors);
    }

    public LockDef? GetLockDef(int keyNumber) => LockDefs.FirstOrDefault(x => x.KeyNumber == keyNumber);
}
