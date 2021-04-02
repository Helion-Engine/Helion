using Helion.Geometry.Vectors;

namespace Helion.Geometry.SegmentsNew
{
    public class Segment2D<T> where T : Vector2D
    {
        public T Start { get; }
        public T End { get; }

        public Segment2D(T start, T end)
        {
            Start = start;
            End = end;
        }
        
        public Vec2D Delta => End - Start;
        
        public static Seg2D operator +(Segment2D<T> self, Vec2D other) => new(self.Start + other, self.End + other);
        public static Seg2D operator +(Segment2D<T> self, T other) => new(self.Start + other, self.End + other);
        public static Seg2D operator -(Segment2D<T> self, Vec2D other) => new(self.Start - other, self.End - other);
        public static Seg2D operator -(Segment2D<T> self, T other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Segment2D<T> self, Segment2D<T> other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Segment2D<T> self, Segment2D<T> other) => !(self == other);
    }
}
