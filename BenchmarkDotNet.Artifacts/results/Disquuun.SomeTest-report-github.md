``` ini

BenchmarkDotNet=v0.10.11, OS=macOS 10.12.6 (16G1114) [Darwin 16.7.0]
Processor=Intel Core i7-7Y75 CPU 1.30GHz, ProcessorCount=4
.NET Core SDK=2.0.0
  [Host]     : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT


```
|          Method |      Mean |     Error |    StdDev |
|---------------- |----------:|----------:|----------:|
|     UseArrayFor |  6.443 us | 0.1243 us | 0.1823 us |
| UseArrayForEach |  5.231 us | 0.0979 us | 0.0818 us |
|      UseListFor | 12.999 us | 0.2599 us | 0.7158 us |
|  UseListForEach | 37.976 us | 0.7483 us | 0.8007 us |
