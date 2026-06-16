using BenchmarkDotNet.Running;

namespace BenchmarkSuite;

internal class Program
{
    static void Main()
    {
        var summary = BenchmarkRunner.Run<MoveEnemy>();
        //var test = new MoveEnemy();
        //test.MoveEnemies();
    }
}
