``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]   : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  ShortRun : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Job=ShortRun  LaunchCount=1  TargetCount=3  
WarmupCount=3  

```
|                      Method |     Mean |     Error |    StdDev |
|---------------------------- |---------:|----------:|----------:|
|     Take_10byte_2sock_async | 78.41 us |  44.05 us |  2.489 us |
|      Take_10byte_2sock_sync | 57.63 us |  28.17 us |  1.592 us |
|    Take_10byte_10sock_async | 76.53 us |  33.12 us |  1.871 us |
|    Take_10byte_30sock_async | 83.94 us |  28.41 us |  1.605 us |
|  Take_10byte_2sock_pipeline | 85.94 us |  97.83 us |  5.527 us |
| Take_10byte_10sock_pipeline | 84.15 us | 233.62 us | 13.200 us |
| Take_10byte_30sock_pipeline | 84.44 us |  18.17 us |  1.027 us |
