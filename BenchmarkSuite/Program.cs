using BenchmarkDotNet.Running;

namespace BenchmarkSuite;

internal sealed class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
