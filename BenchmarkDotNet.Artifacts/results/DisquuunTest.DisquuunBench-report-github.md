``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]     : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT


```
|                   Method |     Mean |     Error |    StdDev |
|------------------------- |---------:|----------:|----------:|
|  Take_10byte_2sock_async | 74.68 us | 1.6555 us | 1.4676 us |
| Take_10byte_10sock_async | 75.03 us | 1.4786 us | 1.8699 us |
| Take_10byte_30sock_async | 77.88 us | 0.8031 us | 0.7119 us |
