using System;
using CodegenCS;

namespace Generators
{
    public class VectorGenerator
    {
        private readonly Types type;
        private readonly int dimension;
        private readonly bool isStruct;
        private readonly string[] fields = { "X", "Y", "Z", "W" };

        public bool Is2D => dimension == 2;
        public string StructName => "Vec" + dimension + type.GetShorthand();
        public string ClassName => "Vector" + dimension + type.GetShorthand();

        private VectorGenerator(Types vectorType, int vectorDimension, bool isStructure)
        {
            type = vectorType;
            dimension = vectorDimension;
            isStruct = isStructure;
            fields = fields[..vectorDimension];
        }

        private string GetClassKeyword() => isStruct ? "struct" : "class";
        private string GetClassName() => isStruct ? StructName : ClassName; 
        
        private void PerformGeneration()
        {
            var w = new CodegenTextWriter();

            w.WriteCommentHeader();
            w.WriteNamespaceBlock("Geometry.Vectors", () => 
            {
                w.WithCBlock($"public {GetClassKeyword()} {GetClassName()}", () =>
                {
                /*
                    public static readonly Vec2D Zero = new Vec2D(0, 0);
                    public static readonly Vec2D One = new Vec2D(1, 1);

                    // Fields
                    public double X;
                    public double Y;
                    
                    // For 2D only
                    public double U => X;
                    public double V => Y;
                    
                    // Conversions
                    public double Int => new Vec2I((int)X, (int)Y);
                    public double Float => new Vec2F((float)X, (float)Y);
                    public double Fixed => new Vec2Fixed(...);
                    
                    // Permutations...
                    public Vec2D XY => new Vec2D(X, Y);
                    public Vec2D XZ => new Vec2D(X, Z);
                    public Vec3D XYZ => new Vec2D(X, Y, Z);
                    public Vec3D XYW => new Vec2D(X, Y, W);
                    
                    // For classes only
                    public Vec2D Struct => new Vec2D(X, Y);
                    
                    public Vec2D(double x, double y)
                    {
                        X = x;
                        Y = y;
                    }
                    
                    // implicit constructor from tuple values
                    
                    public static void Deconstruct(out double x, out double y)
                    {
                        x = X;
                        y = Y;
                    }
                    
                    // Also have instance methods as well
                    public static Vec2D operator -(Vec2D self) => new Vec2D(-self.X, -self.Y);
                    public static Vec2D operator +(Vec2D self, Vec2D other) => new Vec2D(self.X + other.X, self.Y + other.Y);
                    public static Vec2D operator -(Vec2D self, Vec2D other) => new Vec2D(self.X - other.X, self.Y - other.Y);
                    public static Vec2D operator *(Vec2D self, Vec2D other) => new Vec2D(self.X * other.X, self.Y * other.Y);
                    public static Vec2D operator *(Vec2D self, double value) => new Vec2D(self.X * value, self.Y * value);
                    public static Vec2D operator *(double value, Vec2D self) => new Vec2D(self.X * value, self.Y * value);
                    public static Vec2D operator /(Vec2D self, Vec2D other) => new Vec2D(self.X / other.X, self.Y / other.Y);
                    public static Vec2D operator /(Vec2D self, double value) => new Vec2D(self.X / value, self.Y / value);
                    public static Vec2D operator /(double value, Vec2D self) => new Vec2D(self.X / value, self.Y / value);
                    public static bool operator ==(Vec2D self, Vec2D other) => self.X == other.X && self.Y == other.Y;
                    public static bool operator !=(Vec2D self, Vec2D other) => !(self == other);
                    
                    // Math methods here...
                    Max() // returns Math.Max(x, y, z, w)
                    Min() // returns Math.Min(x, y, z, w)
                    
                    public Vec2D WithX(double x);
                    public Vec2D WithY(double y);
                    public Vec3D WithZ(double z);
                    public Vec2D To3D(double z);
                    public Vec2D To4D(double z, double w);
                */
                });
            });

            string path = $"Geometry/Vectors/{GetClassName()}.cs";
            Console.WriteLine($"Generating {path}");
            w.WriteToCoreProject(path);
        }

        public static void Generate()
        {
            foreach (Types type in Enum.GetValues<Types>())
            {
                foreach (int dimension in new[] { 2, 3, 4 })
                {
                    new VectorGenerator(type, dimension, true).PerformGeneration();
                    new VectorGenerator(type, dimension, false).PerformGeneration();
                }
            }
        }
    }
}
