``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]   : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  ShortRun : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Job=ShortRun  LaunchCount=1  TargetCount=3  
WarmupCount=3  

```
|                          Method |      Mean |       Error |    StdDev |
|-------------------------------- |----------:|------------:|----------:|
|         Take_10byte_2sock_async |  4.327 us |   1.1002 us | 0.0622 us |
|          Take_10byte_2sock_sync |  1.537 us |   0.4971 us | 0.0281 us |
|        Take_10byte_10sock_async |  4.316 us |   1.6885 us | 0.0954 us |
|        Take_10byte_30sock_async |  4.160 us |   1.6149 us | 0.0912 us |
|   Take_10byte_2sock_sync_10item | 16.371 us |   4.2624 us | 0.2408 us |
| Take_10byte_10sock_async_10item | 18.582 us |   7.3032 us | 0.4126 us |
| Take_10byte_30sock_async_10item | 21.301 us |  11.1108 us | 0.6278 us |
|      Take_10byte_2sock_pipeline |  5.439 us |   1.1429 us | 0.0646 us |
|     Take_10byte_10sock_pipeline |  5.383 us |   1.4103 us | 0.0797 us |
|     Take_10byte_30sock_pipeline |  5.420 us |   1.7135 us | 0.0968 us |
|   Take_10byte_2sock_pipeline_10 | 24.977 us |   6.2469 us | 0.3530 us |
|  Take_10byte_10sock_pipeline_10 | 24.304 us |   8.0790 us | 0.4565 us |
|  Take_10byte_30sock_pipeline_10 | 24.934 us |   5.6941 us | 0.3217 us |
|        Take_10byte_2sock_loop_2 | 35.461 us |  10.6080 us | 0.5994 us |
|       Take_10byte_10sock_loop_2 | 43.681 us | 112.1365 us | 6.3359 us |
|       Take_10byte_30sock_loop_2 | 45.030 us |  89.6113 us | 5.0632 us |
