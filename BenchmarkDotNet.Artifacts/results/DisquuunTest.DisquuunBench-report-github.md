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
|         Take_10byte_2sock_async |  4.040 us |  0.7995 us | 0.0452 us |
|          Take_10byte_2sock_sync |  1.549 us |  0.2411 us | 0.0136 us |
|        Take_10byte_10sock_async |  4.290 us |  0.9721 us | 0.0549 us |
|        Take_10byte_30sock_async |  4.220 us |  1.3237 us | 0.0748 us |
|   Take_10byte_2sock_sync_10item | 16.081 us | 16.7491 us | 0.9464 us |
| Take_10byte_10sock_async_10item | 20.856 us | 18.8980 us | 1.0678 us |
| Take_10byte_30sock_async_10item | 18.623 us |  6.1906 us | 0.3498 us |
|      Take_10byte_2sock_pipeline |  5.197 us | 11.6224 us | 0.6567 us |
|     Take_10byte_10sock_pipeline |  5.598 us |  3.1213 us | 0.1764 us |
|     Take_10byte_30sock_pipeline |  5.065 us |  0.7191 us | 0.0406 us |
|   Take_10byte_2sock_pipeline_10 | 22.567 us | 12.7912 us | 0.7227 us |
|  Take_10byte_10sock_pipeline_10 | 25.333 us |  6.0615 us | 0.3425 us |
|  Take_10byte_30sock_pipeline_10 | 23.269 us | 11.4749 us | 0.6484 us |
|        Take_10byte_2sock_loop_2 | 32.199 us | 16.3175 us | 0.9220 us |
|       Take_10byte_10sock_loop_2 | 35.051 us |  4.2557 us | 0.2405 us |
|       Take_10byte_30sock_loop_2 | 34.979 us |  2.1891 us | 0.1237 us |
