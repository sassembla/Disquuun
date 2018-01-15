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
|         Take_10byte_2sock_async |  4.065 us |  1.7354 us | 0.0981 us |
|          Take_10byte_2sock_sync |  1.537 us |  0.9427 us | 0.0533 us |
|        Take_10byte_10sock_async |  5.889 us |  8.2307 us | 0.4651 us |
|        Take_10byte_30sock_async |  5.515 us |  2.4331 us | 0.1375 us |
|   Take_10byte_2sock_sync_10item | 17.907 us | 28.7123 us | 1.6223 us |
| Take_10byte_10sock_async_10item | 22.929 us | 53.0855 us | 2.9994 us |
| Take_10byte_30sock_async_10item | 21.841 us |  3.7723 us | 0.2131 us |
|      Take_10byte_2sock_pipeline |  4.795 us |  0.9523 us | 0.0538 us |
|     Take_10byte_10sock_pipeline |  4.656 us |  0.5813 us | 0.0328 us |
|     Take_10byte_30sock_pipeline |  4.735 us |  0.5510 us | 0.0311 us |
|   Take_10byte_2sock_pipeline_10 | 22.294 us |  6.0993 us | 0.3446 us |
|  Take_10byte_10sock_pipeline_10 | 23.485 us | 22.0893 us | 1.2481 us |
|  Take_10byte_30sock_pipeline_10 | 21.890 us |  7.6632 us | 0.4330 us |
|        Take_10byte_2sock_loop_2 | 29.881 us |  1.8279 us | 0.1033 us |
|       Take_10byte_10sock_loop_2 | 31.114 us | 15.8645 us | 0.8964 us |
|       Take_10byte_30sock_loop_2 | 33.355 us | 21.4925 us | 1.2144 us |
