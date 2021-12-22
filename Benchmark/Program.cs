// See https://aka.ms/new-console-template for more information
using Benchmark;

using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

Summary summary = BenchmarkRunner.Run<InitQuery>();

Console.WriteLine(summary);
