using Helion.Input;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("The bindings from some action to a key.")]
    public class ConfigControls
    {
        [ConfigInfo("The key for moving forward")]
        public readonly ConfigValueEnum<InputKey> Forward = new(InputKey.W);

        [ConfigInfo("The key for moving left")]
        public readonly ConfigValueEnum<InputKey> Left = new(InputKey.A);

        [ConfigInfo("The key for moving backward")]
        public readonly ConfigValueEnum<InputKey> Backward = new(InputKey.S);

        [ConfigInfo("The key for moving right")]
        public readonly ConfigValueEnum<InputKey> Right = new(InputKey.D);

        [ConfigInfo("The key for use")]
        public readonly ConfigValueEnum<InputKey> Use = new(InputKey.E);

        [ConfigInfo("The key for jump")]
        public readonly ConfigValueEnum<InputKey> Jump = new(InputKey.Space);

        [ConfigInfo("The key for crouch")]
        public readonly ConfigValueEnum<InputKey> Crouch = new(InputKey.C);

        [ConfigInfo("The key for opening/closing the console")]
        public readonly ConfigValueEnum<InputKey> Console = new(InputKey.Backtick);

        [ConfigInfo("The key for attacking")]
        public readonly ConfigValueEnum<InputKey> Attack = new(InputKey.MouseLeft);

        [ConfigInfo("The key for switching to the next weapon")]
        public readonly ConfigValueEnum<InputKey> NextWeapon = new(InputKey.Up);

        [ConfigInfo("The key for switching to the previous weapon")]
        public readonly ConfigValueEnum<InputKey> PreviousWeapon = new(InputKey.Down);

        [ConfigInfo("The key for switching to weapon slot 1")]
        public readonly ConfigValueEnum<InputKey> WeaponSlot1 = new(InputKey.One);

        [ConfigInfo("The key for switching to weapon slot 2")]
        public readonly ConfigValueEnum<InputKey> WeaponSlot2 = new(InputKey.Two);

        [ConfigInfo("The key for switching to weapon slot 3")]
        public readonly ConfigValueEnum<InputKey> WeaponSlot3 = new(InputKey.Three);

        [ConfigInfo("The key for switching to weapon slot 4")]
        public readonly ConfigValueEnum<InputKey> WeaponSlot4 = new(InputKey.Four);

        [ConfigInfo("The key for switching to weapon slot 5")]
        public readonly ConfigValueEnum<InputKey> WeaponSlot5 = new(InputKey.Five);

        [ConfigInfo("The key for switching to weapon slot 6")]
        public readonly ConfigValueEnum<InputKey> WeaponSlot6 = new(InputKey.Six);

        [ConfigInfo("The key for switching to weapon slot 7")]
        public readonly ConfigValueEnum<InputKey> WeaponSlot7 = new(InputKey.Seven);

        [ConfigInfo("The key for getting a screenshot")]
        public readonly ConfigValueEnum<InputKey> Screenshot = new(InputKey.PrintScreen);
    }
}
