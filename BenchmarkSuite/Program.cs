using BenchmarkDotNet.Running;

namespace BenchmarkSuite;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

        //EntityMoveGeneral test = new();
        //test.MoveEnemies();
    }
}
