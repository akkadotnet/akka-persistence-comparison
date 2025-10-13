using System;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace Akka.Persistence.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Akka.Persistence Benchmarks");
        Console.WriteLine("----------------------------");
        Console.WriteLine(
            "IMPORTANT: Make sure Docker is running.");
        Console.WriteLine();

        BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run();
    }
}