namespace Generators
{
    public static class Start
    {
        public static void Main(string[] args)
        { 
            VectorGenerator.Generate();
            PrimitiveExtensionsGenerator.Generate();
        }
    }
}
