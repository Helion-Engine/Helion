using Generators.Generators;

namespace Generators
{
    public static class Start
    {
        public static void Main(string[] args)
        {
            PrimitiveExtensionsGenerator.Generate();
            VectorGenerator.Generate();
            SegmentGenerator.Generate();
            BoxGenerator.Generate();
        }
    }
}
