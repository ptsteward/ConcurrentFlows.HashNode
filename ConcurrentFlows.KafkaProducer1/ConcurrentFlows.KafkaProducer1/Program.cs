using BenchmarkDotNet.Running;
using ConcurrentFlows.KafkaProducer1;

Console.WriteLine("Starting Producer Benchmarks");

BenchmarkRunner.Run<ProducerBenchmarks>();
