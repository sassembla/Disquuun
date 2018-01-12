``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]     : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT


```
|                Method |     Mean |    Error |   StdDev |
|---------------------- |---------:|---------:|---------:|
|           Take_10byte | 75.66 us | 1.503 us | 2.633 us |
|  Take_10byte_pipeline | 78.90 us | 1.547 us | 2.453 us |
| Take_10byte_pipeline2 | 89.49 us | 1.756 us | 2.836 us |
