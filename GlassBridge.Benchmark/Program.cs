using BenchmarkDotNet.Running;
using GlassBridgeBenchmark;

// 全ベンチマークを実行
BenchmarkSwitcher.FromAssembly(typeof(ImuPacketParseBenchmark).Assembly).Run(args);
