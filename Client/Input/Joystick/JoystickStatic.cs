namespace Helion.Client.Input.Joystick
{
    using Helion.Window.Input;
    using System.Collections.Generic;

    public static class JoystickStatic
    {
        public static readonly Key[] ButtonsToKeys =
        [
            Key.Button1,
            Key.Button2,
            Key.Button3,
            Key.Button4,
            Key.Button5,
            Key.Button6,
            Key.Button7,
            Key.Button8,
            Key.Button9,
            Key.Button10,
            Key.Button11,
            Key.Button12,
            Key.Button13,
            Key.Button14,
            Key.Button15,
            Key.Button16,
            Key.Button17,
            Key.Button18,
            Key.Button19,
            Key.Button20,
            Key.Button21,
            Key.Button22,
            Key.Button23,
            Key.Button24,
            Key.Button25,
            Key.Button26,
            Key.Button27,
            Key.Button28,
            Key.Button29,
            Key.Button30
        ];

        public static readonly Key[] PadToKeys =
        [
            Key.DPad1Up,
            Key.DPad1Right,
            Key.DPad1Down,
            Key.DPad1Left,
            Key.DPad2Up,
            Key.DPad2Right,
            Key.DPad2Down,
            Key.DPad2Left,
            Key.DPad3Up,
            Key.DPad3Right,
            Key.DPad3Down,
            Key.DPad3Left,
        ];

        public static readonly Key[] AxisToKeys =
        [
            Key.Axis1Plus,
            Key.Axis1Minus,
            Key.Axis2Plus,
            Key.Axis2Minus,
            Key.Axis3Plus,
            Key.Axis3Minus,
            Key.Axis4Plus,
            Key.Axis4Minus,
            Key.Axis5Plus,
            Key.Axis5Minus,
            Key.Axis6Plus,
            Key.Axis6Minus,
            Key.Axis7Plus,
            Key.Axis7Minus,
            Key.Axis8Plus,
            Key.Axis8Minus,
            Key.Axis9Plus,
            Key.Axis9Minus,
            Key.Axis10Plus,
            Key.Axis10Minus,
        ];

        public static readonly Dictionary<Key, (int axisId, bool isPositive)> KeysToAxis = new()
        {
            { Key.Axis1Plus, (0, true) },
            { Key.Axis1Minus, (0, false) },
            { Key.Axis2Plus, (1, true) },
            { Key.Axis2Minus, (1, false) },
            { Key.Axis3Plus, (2, true) },
            { Key.Axis3Minus, (2, false) },
            { Key.Axis4Plus, (3, true) },
            { Key.Axis4Minus, (3, false) },
            { Key.Axis5Plus, (4, true) },
            { Key.Axis5Minus, (4, false) },
            { Key.Axis6Plus, (5, true) },
            { Key.Axis6Minus, (5, false) },
            { Key.Axis7Plus, (6, true) },
            { Key.Axis7Minus, (6, false) },
            { Key.Axis8Plus, (7, true) },
            { Key.Axis8Minus, (7, false) },
            { Key.Axis9Plus, (8, true) },
            { Key.Axis9Minus, (8, false) },
            { Key.Axis10Plus, (9, true) },
            { Key.Axis10Minus, (9, false) },
        };
    }
}
