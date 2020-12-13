using Helion.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helion.Resources.Definitions.Decorate.Locks
{
    public class LockDefinitions
    {
        private List<LockDef> LockDefs = new List<LockDef>();

        private static string KeyMessage(string name) => $"{name} key";

        public LockDefinitions()
        {
            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("red"),
                KeyNumber = 1,
                KeyDefinitionNames = new List<CIString>() { "RedCard" }
            }); ;

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("blue"),
                KeyNumber = 2,
                KeyDefinitionNames = new List<CIString>() { "BlueCard" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("yellow"),
                KeyNumber = 3,
                KeyDefinitionNames = new List<CIString>() { "YellowCard" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("red skull"),
                KeyNumber = 4,
                KeyDefinitionNames = new List<CIString>() { "RedSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("blue skull"),
                KeyNumber = 5,
                KeyDefinitionNames = new List<CIString>() { "BlueSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("yellow skull"),
                KeyNumber = 6,
                KeyDefinitionNames = new List<CIString>() { "YellowSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("red"),
                KeyNumber = 129,
                KeyDefinitionNames = new List<CIString>() { "RedCard", "RedSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("blue"),
                KeyNumber = 130,
                KeyDefinitionNames = new List<CIString>() { "BlueCard", "BlueSkull" }
            });

            LockDefs.Add(new LockDef()
            {
                Message = KeyMessage("yellow"),
                KeyNumber = 131,
                KeyDefinitionNames = new List<CIString>() { "YellowCard", "YellowSkull" }
            });
        }

        public LockDef? GetLockDef(int keyNumber)
        {
            return LockDefs.FirstOrDefault(x => x.KeyNumber == keyNumber);
        }
    }
}
