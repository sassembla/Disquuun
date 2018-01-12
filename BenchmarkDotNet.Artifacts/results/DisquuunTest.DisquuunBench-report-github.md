``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]   : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  ShortRun : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Job=ShortRun  LaunchCount=1  TargetCount=3  
WarmupCount=3  

```
|                   Method |     Mean |    Error |   StdDev |
|------------------------- |---------:|---------:|---------:|
|  Take_10byte_2sock_async | 68.04 us | 28.83 us | 1.629 us |
| Take_10byte_10sock_async | 72.87 us | 22.36 us | 1.263 us |
| Take_10byte_30sock_async | 73.63 us | 29.12 us | 1.645 us |
