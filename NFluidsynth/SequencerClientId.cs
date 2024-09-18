using System;

namespace NFluidsynth
{
    public struct SequencerClientId : IEquatable<SequencerClientId>
    {
        public readonly short Value;

        /// <summary>
        ///     Used by <see cref="Sequencer.RemoveEvents"/>.
        /// </summary>
        public static readonly SequencerClientId Wildcard = new SequencerClientId(-1);

        public SequencerClientId(short value)
        {
            Value = value;
        }

        public bool Equals(SequencerClientId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SequencerClientId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(SequencerClientId left, SequencerClientId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SequencerClientId left, SequencerClientId right)
        {
            return !left.Equals(right);
        }
    }
}