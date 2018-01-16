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
|         Take_10byte_2sock_async |  4.656 us |  6.2569 us | 0.3535 us |
|          Take_10byte_2sock_sync |  1.730 us |  0.9745 us | 0.0551 us |
|        Take_10byte_10sock_async |  4.337 us |  0.4410 us | 0.0249 us |
|        Take_10byte_30sock_async |  4.262 us |  0.7247 us | 0.0409 us |
|   Take_10byte_2sock_sync_10item | 17.134 us |  8.7683 us | 0.4954 us |
| Take_10byte_10sock_async_10item | 21.383 us | 16.5207 us | 0.9335 us |
| Take_10byte_30sock_async_10item | 22.547 us |  3.6320 us | 0.2052 us |
|      Take_10byte_2sock_pipeline |  6.301 us |  1.8376 us | 0.1038 us |
|     Take_10byte_10sock_pipeline |  5.949 us |  2.8413 us | 0.1605 us |
|     Take_10byte_30sock_pipeline |  6.673 us |  4.2911 us | 0.2425 us |
|   Take_10byte_2sock_pipeline_10 | 29.744 us | 20.4818 us | 1.1573 us |
|  Take_10byte_10sock_pipeline_10 | 25.569 us | 10.5866 us | 0.5982 us |
|  Take_10byte_30sock_pipeline_10 | 23.797 us |  3.4032 us | 0.1923 us |
|        Take_10byte_2sock_loop_2 | 35.643 us | 21.8902 us | 1.2368 us |
|       Take_10byte_10sock_loop_2 | 35.144 us | 16.8500 us | 0.9521 us |
|       Take_10byte_30sock_loop_2 | 35.683 us | 12.4644 us | 0.7043 us |
