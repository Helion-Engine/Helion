using Helion.Input;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("The bindings from some action to a key.")]
    public class ConfigControls
    {
        [ConfigInfo("The key for moving forward")]
        public readonly ConfigValueEnum<Key> Forward = new(Key.W);

        [ConfigInfo("The key for moving left")]
        public readonly ConfigValueEnum<Key> Left = new(Key.A);

        [ConfigInfo("The key for moving backward")]
        public readonly ConfigValueEnum<Key> Backward = new(Key.S);

        [ConfigInfo("The key for moving right")]
        public readonly ConfigValueEnum<Key> Right = new(Key.D);

        [ConfigInfo("The key for use")]
        public readonly ConfigValueEnum<Key> Use = new(Key.E);

        [ConfigInfo("The key for jump")]
        public readonly ConfigValueEnum<Key> Jump = new(Key.Space);

        [ConfigInfo("The key for crouch")]
        public readonly ConfigValueEnum<Key> Crouch = new(Key.C);

        [ConfigInfo("The key for opening/closing the console")]
        public readonly ConfigValueEnum<Key> Console = new(Key.Backtick);

        [ConfigInfo("The key for attacking")]
        public readonly ConfigValueEnum<Key> Attack = new(Key.MouseLeft);

        [ConfigInfo("The key for switching to the next weapon")]
        public readonly ConfigValueEnum<Key> NextWeapon = new(Key.Up);

        [ConfigInfo("The key for switching to the previous weapon")]
        public readonly ConfigValueEnum<Key> PreviousWeapon = new(Key.Down);

        [ConfigInfo("The key for switching to weapon slot 1")]
        public readonly ConfigValueEnum<Key> WeaponSlot1 = new(Key.One);

        [ConfigInfo("The key for switching to weapon slot 2")]
        public readonly ConfigValueEnum<Key> WeaponSlot2 = new(Key.Two);

        [ConfigInfo("The key for switching to weapon slot 3")]
        public readonly ConfigValueEnum<Key> WeaponSlot3 = new(Key.Three);

        [ConfigInfo("The key for switching to weapon slot 4")]
        public readonly ConfigValueEnum<Key> WeaponSlot4 = new(Key.Four);

        [ConfigInfo("The key for switching to weapon slot 5")]
        public readonly ConfigValueEnum<Key> WeaponSlot5 = new(Key.Five);

        [ConfigInfo("The key for switching to weapon slot 6")]
        public readonly ConfigValueEnum<Key> WeaponSlot6 = new(Key.Six);

        [ConfigInfo("The key for switching to weapon slot 7")]
        public readonly ConfigValueEnum<Key> WeaponSlot7 = new(Key.Seven);

        [ConfigInfo("The key for getting a screenshot")]
        public readonly ConfigValueEnum<Key> Screenshot = new(Key.PrintScreen);

        [ConfigInfo("The key for increasing hud size")]
        public readonly ConfigValueEnum<Key> HudIncrease = new(Key.Equals);

        [ConfigInfo("The key for decreasing hud size")]
        public readonly ConfigValueEnum<Key> HudDecrease = new(Key.Minus);

        [ConfigInfo("The key for increasing automap size")]
        public readonly ConfigValueEnum<Key> AutoMapIncrease = new(Key.Equals);

        [ConfigInfo("The key for decreasing automap size")]
        public readonly ConfigValueEnum<Key> AutoMapDecrease = new(Key.Minus);

        public readonly ConfigValueEnum<Key> AutoMapUp= new(Key.Up);
        public readonly ConfigValueEnum<Key> AutoMapDown = new(Key.Down);
        public readonly ConfigValueEnum<Key> AutoMapLeft = new(Key.Left);
        public readonly ConfigValueEnum<Key> AutoMapRight = new(Key.Right);

        [ConfigInfo("The key for saving the game")]
        public readonly ConfigValueEnum<Key> Save = new(Key.F2);

        [ConfigInfo("The key for loading the game")]
        public readonly ConfigValueEnum<Key> Load = new(Key.F3);
        
        [ConfigInfo("The key for toggling the automap")]
        public readonly ConfigValueEnum<Key> Automap = new(Key.Tab);
    }
}
