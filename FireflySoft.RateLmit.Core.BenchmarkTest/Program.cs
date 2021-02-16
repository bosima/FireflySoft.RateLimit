using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace FireflySoft.RateLmit.Core.BenchmarkTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var benchmark = BenchmarkRunner.Run<BenchmarkTest>();
            Console.Read();
        }
    }
}
