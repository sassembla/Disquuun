``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]   : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  ShortRun : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Job=ShortRun  LaunchCount=1  TargetCount=3  
WarmupCount=3  

```
|                          Method |      Mean |      Error |     StdDev |
|-------------------------------- |----------:|-----------:|-----------:|
|         Take_10byte_2sock_async | 10.266 us |  19.760 us |  1.1165 us |
|          Take_10byte_2sock_sync |  2.258 us |   2.341 us |  0.1323 us |
|        Take_10byte_10sock_async |  5.572 us |  22.626 us |  1.2784 us |
|        Take_10byte_30sock_async |  4.622 us |   7.957 us |  0.4496 us |
|   Take_10byte_2sock_sync_10item | 26.883 us | 156.795 us |  8.8592 us |
| Take_10byte_10sock_async_10item | 39.719 us |  46.928 us |  2.6515 us |
| Take_10byte_30sock_async_10item | 30.820 us |  32.019 us |  1.8091 us |
|      Take_10byte_2sock_pipeline |  8.421 us |  10.710 us |  0.6051 us |
|     Take_10byte_10sock_pipeline |  8.178 us |   8.513 us |  0.4810 us |
|     Take_10byte_30sock_pipeline |  5.042 us |   4.002 us |  0.2261 us |
|   Take_10byte_2sock_pipeline_10 | 22.033 us |   3.058 us |  0.1728 us |
|  Take_10byte_10sock_pipeline_10 | 39.859 us | 264.833 us | 14.9635 us |
|  Take_10byte_30sock_pipeline_10 | 35.860 us |  86.443 us |  4.8842 us |
|        Take_10byte_2sock_loop_2 | 54.622 us | 227.064 us | 12.8295 us |
|       Take_10byte_10sock_loop_2 | 53.244 us | 155.729 us |  8.7990 us |
|       Take_10byte_30sock_loop_2 | 58.615 us |  74.910 us |  4.2325 us |
