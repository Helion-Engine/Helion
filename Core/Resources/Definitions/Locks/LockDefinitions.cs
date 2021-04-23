using Helion.Util;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Helion.Resources.Definitions.Locks
{
    public class LockDefinitions
    {
        public readonly List<LockDef> LockDefs = new List<LockDef>();

        private static string KeyMessage(string name) => $"{name} key";

        public LockDefinitions()
        {
            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("red"),
                KeyNumber = 1,
                MapColor = Color.Red,
                KeyDefinitionNames = new List<string>() { "RedCard" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("blue"),
                KeyNumber = 2,
                MapColor = Color.Blue,
                KeyDefinitionNames = new List<string>() { "BlueCard" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("yellow"),
                KeyNumber = 3,
                MapColor = Color.Yellow,
                KeyDefinitionNames = new List<string>() { "YellowCard" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("red skull"),
                KeyNumber = 4,
                MapColor = Color.Red,
                KeyDefinitionNames = new List<string>() { "RedSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("blue skull"),
                KeyNumber = 5,
                MapColor = Color.Blue,
                KeyDefinitionNames = new List<string>() { "BlueSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("yellow skull"),
                KeyNumber = 6,
                MapColor = Color.Yellow,
                KeyDefinitionNames = new List<string>() { "YellowSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("red"),
                KeyNumber = 129,
                MapColor = Color.Red,
                KeyDefinitionNames = new List<string>() { "RedCard", "RedSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("blue"),
                KeyNumber = 130,
                MapColor = Color.Blue,
                KeyDefinitionNames = new List<string>() { "BlueCard", "BlueSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("yellow"),
                KeyNumber = 131,
                MapColor = Color.Yellow,
                KeyDefinitionNames = new List<string>() { "YellowCard", "YellowSkull" }
            });
        }

        public LockDef? GetLockDef(int keyNumber)
        {
            return LockDefs.FirstOrDefault(x => x.KeyNumber == keyNumber);
        }
    }
}
