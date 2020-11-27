using Helion.Input;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineControlConfig
    {
        public readonly ConfigValue<InputKey> MoveForward = new ConfigValue<InputKey>(InputKey.W);
        public readonly ConfigValue<InputKey> MoveLeft = new ConfigValue<InputKey>(InputKey.A);
        public readonly ConfigValue<InputKey> MoveBackward = new ConfigValue<InputKey>(InputKey.S);
        public readonly ConfigValue<InputKey> MoveRight = new ConfigValue<InputKey>(InputKey.D);
        public readonly ConfigValue<InputKey> Use = new ConfigValue<InputKey>(InputKey.E);
        public readonly ConfigValue<InputKey> Jump = new ConfigValue<InputKey>(InputKey.Space);
        public readonly ConfigValue<InputKey> Crouch = new ConfigValue<InputKey>(InputKey.C);
        public readonly ConfigValue<InputKey> Console = new ConfigValue<InputKey>(InputKey.Backtick);
        public readonly ConfigValue<InputKey> Attack = new ConfigValue<InputKey>(InputKey.MouseLeft);
        public readonly ConfigValue<InputKey> NextWeapon = new ConfigValue<InputKey>(InputKey.Up);
        public readonly ConfigValue<InputKey> PreviousWeapon = new ConfigValue<InputKey>(InputKey.Down);
        public readonly ConfigValue<InputKey> WeaponSlot1 = new ConfigValue<InputKey>(InputKey.One);
        public readonly ConfigValue<InputKey> WeaponSlot2 = new ConfigValue<InputKey>(InputKey.Two);
        public readonly ConfigValue<InputKey> WeaponSlot3 = new ConfigValue<InputKey>(InputKey.Three);
        public readonly ConfigValue<InputKey> WeaponSlot4 = new ConfigValue<InputKey>(InputKey.Four);
        public readonly ConfigValue<InputKey> WeaponSlot5 = new ConfigValue<InputKey>(InputKey.Five);
        public readonly ConfigValue<InputKey> WeaponSlot6 = new ConfigValue<InputKey>(InputKey.Six);
        public readonly ConfigValue<InputKey> WeaponSlot7 = new ConfigValue<InputKey>(InputKey.Seven);
    }
}