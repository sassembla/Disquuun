``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]   : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  ShortRun : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Job=ShortRun  LaunchCount=1  TargetCount=3  
WarmupCount=3  

```
|                      Method |      Mean |      Error |     StdDev |
|---------------------------- |----------:|-----------:|-----------:|
|     Take_10byte_2sock_async |  81.15 us |   9.596 us |  0.5422 us |
|      Take_10byte_2sock_sync |  59.28 us |   5.387 us |  0.3044 us |
|    Take_10byte_10sock_async |  99.69 us | 223.154 us | 12.6086 us |
|    Take_10byte_30sock_async |  78.28 us |  36.978 us |  2.0893 us |
|  Take_10byte_2sock_pipeline | 152.23 us |  40.314 us |  2.2778 us |
| Take_10byte_10sock_pipeline | 156.67 us |  80.243 us |  4.5339 us |
| Take_10byte_30sock_pipeline | 184.29 us | 272.888 us | 15.4187 us |
