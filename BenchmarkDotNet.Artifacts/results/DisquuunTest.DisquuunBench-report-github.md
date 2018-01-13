``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]   : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  ShortRun : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Job=ShortRun  LaunchCount=1  TargetCount=3  
WarmupCount=3  

```
|                  Method |     Mean |     Error |    StdDev |
|------------------------ |---------:|----------:|----------:|
| Take_10byte_2sock_async | 67.97 us | 12.661 us | 0.7154 us |
|  Take_10byte_2sock_sync | 52.78 us |  7.275 us | 0.4111 us |
