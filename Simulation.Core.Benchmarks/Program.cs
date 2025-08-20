using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using Arch.Core;
using Simulation.Core.Components;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;

namespace Simulation.Core.Benchmarks;

public static class Program
{
    private static readonly Type[] All = new[]
    {
        typeof(MovementBenchmarks),
        //typeof(TeleportBenchmarks),
        typeof(IndexRebuildBenchmarks)
    };

    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "all")
        {
            BenchmarkRunner.Run(All);
        }
        else
        {
            var benchmarks = All.Where(t => t.Name.EndsWith("Benchmarks")).ToArray();
            if (benchmarks.Length == 0)
            {
                Console.WriteLine("No benchmarks found.");
                return;
            }

            foreach (var benchmark in benchmarks)
            {
                Console.WriteLine($"Running {benchmark.Name}...");
                BenchmarkRunner.Run(benchmark);
            }
        }
    }
}