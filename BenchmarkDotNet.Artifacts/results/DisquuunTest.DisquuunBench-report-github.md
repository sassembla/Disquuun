``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]   : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  ShortRun : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Job=ShortRun  LaunchCount=1  TargetCount=3  
WarmupCount=3  

```
|                          Method |      Mean |      Error |    StdDev |
|-------------------------------- |----------:|-----------:|----------:|
|         Take_10byte_2sock_async |  4.031 us |  1.6177 us | 0.0914 us |
|          Take_10byte_2sock_sync |  1.424 us |  0.1356 us | 0.0077 us |
|        Take_10byte_10sock_async |  4.341 us |  0.9520 us | 0.0538 us |
|        Take_10byte_30sock_async |  5.793 us |  9.2631 us | 0.5234 us |
|   Take_10byte_2sock_sync_10item | 16.174 us | 16.9311 us | 0.9566 us |
| Take_10byte_10sock_async_10item | 20.976 us |  2.6745 us | 0.1511 us |
| Take_10byte_30sock_async_10item | 22.492 us | 37.3068 us | 2.1079 us |
|      Take_10byte_2sock_pipeline |  5.263 us |  7.7800 us | 0.4396 us |
|     Take_10byte_10sock_pipeline |  4.996 us | 10.7955 us | 0.6100 us |
|     Take_10byte_30sock_pipeline |  4.636 us |  0.5175 us | 0.0292 us |
|   Take_10byte_2sock_pipeline_10 | 21.390 us | 10.6405 us | 0.6012 us |
|  Take_10byte_10sock_pipeline_10 | 20.962 us |  1.2112 us | 0.0684 us |
|  Take_10byte_30sock_pipeline_10 | 22.087 us | 22.3302 us | 1.2617 us |
