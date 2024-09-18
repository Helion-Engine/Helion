namespace Helion.Client.Input.Joystick
{
    using Helion.Window.Input;

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

        public static readonly (Key left, Key right, Key up, Key down)[] PadToKeys =
        [
            (Key.DPad1Left, Key.DPad1Right, Key.DPad1Up, Key.DPad1Down),
            (Key.DPad2Left, Key.DPad2Right, Key.DPad2Up, Key.DPad2Down),
            (Key.DPad3Left, Key.DPad3Right, Key.DPad3Up, Key.DPad3Down)
        ];
    }
}
