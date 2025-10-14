# Akka.Persistence Benchmark Results

## Test Environment

```
BenchmarkDotNet v0.15.4, Windows 10 (10.0.19045.6332/22H2/2022Update)
AMD Ryzen 9 3900X 3.79GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.110
  [Host]  : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3
  LongRun : .NET 9.0.9 (9.0.9, 9.0.925.41916), X64 RyuJIT x86-64-v3

Job=LongRun  EvaluateOverhead=False  Concurrent=True
Server=True  InvocationCount=1  IterationCount=10
LaunchCount=3  RunStrategy=Monitoring  UnrollFactor=1
WarmupCount=3
```

---

## Azure Table Storage

### Azure Group Persist Benchmark

| Method       | BatchSize |         Mean |         Error |        StdDev |         msg/sec |
|--------------|-----------|-------------:|--------------:|--------------:|----------------:|
| **Persist**  | **1**     |       **NA** |        **NA** |        **NA** | **<not found>** |
| PersistAsync | 1         |           NA |            NA |            NA |     <not found> |
| **Persist**  | **100**   | **1.008 ms** | **0.2551 ms** | **0.3818 ms** |      **991.91** |
| PersistAsync | 100       |     1.032 ms |     0.2684 ms |     0.4018 ms |          968.57 |

**Note**: Benchmarks with issues for BatchSize=1

### Azure Persist Benchmark

| Method       | BatchSize |           Mean |         Error |        StdDev |      msg/sec |
|--------------|-----------|---------------:|--------------:|--------------:|-------------:|
| **Persist**  | **1**     | **2,718.9 μs** | **142.66 μs** | **213.53 μs** |   **367.80** |
| PersistAsync | 1         |     2,685.5 μs |     111.16 μs |     166.38 μs |       372.37 |
| **Persist**  | **100**   |   **277.2 μs** |  **25.16 μs** |  **37.66 μs** | **3,607.36** |
| PersistAsync | 100       |       283.8 μs |      40.57 μs |      60.73 μs |     3,523.35 |

### Azure Recovery Benchmark

| Method                  | Mean     | Error    | StdDev   | msg/sec   |
|------------------------ |---------:|---------:|---------:|----------:|
| RecoveryBenchmarkMethod | 30.85 μs | 2.768 μs | 4.143 μs | 32,417.54 |

---

## MongoDB

### MongoDB Group Persist Benchmark

| Method       | BatchSize |         Mean |         Error |        StdDev |        Median |       msg/sec |
|--------------|-----------|-------------:|--------------:|--------------:|--------------:|--------------:|
| **Persist**  | **1**     | **76.60 μs** | **10.226 μs** | **15.307 μs** | **73.260 μs** | **13,054.79** |
| PersistAsync | 1         |     76.47 μs |      6.315 μs |      9.451 μs |     73.763 μs |     13,077.38 |
| **Persist**  | **100**   | **12.73 μs** |  **4.833 μs** |  **7.233 μs** |  **9.040 μs** | **78,582.94** |
| PersistAsync | 100       |     11.74 μs |      4.216 μs |      6.310 μs |      8.960 μs |     85,179.51 |

### MongoDB Persist Benchmark

| Method       | BatchSize |          Mean |         Error |        StdDev |       msg/sec |
|--------------|-----------|--------------:|--------------:|--------------:|--------------:|
| **Persist**  | **1**     | **396.07 μs** | **60.553 μs** | **90.632 μs** |  **2,524.80** |
| PersistAsync | 1         |     384.12 μs |     55.282 μs |     82.743 μs |      2,603.38 |
| **Persist**  | **100**   |  **48.56 μs** |  **2.322 μs** |  **3.476 μs** | **20,591.09** |
| PersistAsync | 100       |      48.24 μs |      2.071 μs |      3.099 μs |     20,730.80 |

### MongoDB Recovery Benchmark

| Method                  | Mean     | Error    | StdDev   | msg/sec   |
|------------------------ |---------:|---------:|---------:|----------:|
| RecoveryBenchmarkMethod | 30.76 μs | 1.420 μs | 2.126 μs | 32,511.04 |

---

## PostgreSQL

### PostgreSQL Group Persist Benchmark

| Method       | BatchSize |          Mean |         Error |        StdDev |       msg/sec |
|--------------|-----------|--------------:|--------------:|--------------:|--------------:|
| **Persist**  | **1**     | **248.14 μs** | **15.827 μs** | **23.688 μs** |  **4,030.00** |
| PersistAsync | 1         |     248.91 μs |     16.294 μs |     24.388 μs |      4,017.59 |
| **Persist**  | **100**   |  **22.24 μs** |  **4.922 μs** |  **7.367 μs** | **44,969.43** |
| PersistAsync | 100       |      21.50 μs |      3.884 μs |      5.813 μs |     46,521.44 |

### PostgreSQL Persist Benchmark

| Method       | BatchSize |            Mean |         Error |       StdDev |       msg/sec |
|--------------|-----------|----------------:|--------------:|-------------:|--------------:|
| **Persist**  | **1**     | **1,127.35 μs** | **50.388 μs** | **75.42 μs** |    **887.04** |
| PersistAsync | 1         |     1,108.13 μs |     43.096 μs |     64.50 μs |        902.42 |
| **Persist**  | **100**   |    **46.10 μs** |  **7.517 μs** | **11.25 μs** | **21,693.92** |
| PersistAsync | 100       |        54.10 μs |      8.196 μs |     12.27 μs |     18,485.15 |

### PostgreSQL Recovery Benchmark

| Method                  | Mean     | Error    | StdDev   | msg/sec   |
|------------------------ |---------:|---------:|---------:|----------:|
| RecoveryBenchmarkMethod | 15.81 μs | 1.586 μs | 2.373 μs | 63,246.99 |

---

## Redis

### Redis Group Persist Benchmark

| Method       | BatchSize |          Mean |         Error |        StdDev |        msg/sec |
|--------------|-----------|--------------:|--------------:|--------------:|---------------:|
| **Persist**  | **1**     | **68.245 μs** | **4.1685 μs** | **6.2393 μs** |  **14,653.11** |
| PersistAsync | 1         |     66.768 μs |     4.1736 μs |     6.2469 μs |      14,977.15 |
| **Persist**  | **100**   |  **6.114 μs** | **0.8959 μs** | **1.3410 μs** | **163,571.66** |
| PersistAsync | 100       |      5.844 μs |     0.5898 μs |     0.8828 μs |     171,128.92 |

### Redis Persist Benchmark

| Method       | BatchSize |          Mean |         Error |        StdDev |       msg/sec |
|--------------|-----------|--------------:|--------------:|--------------:|--------------:|
| **Persist**  | **1**     | **546.84 μs** | **22.830 μs** | **34.170 μs** |  **1,828.68** |
| PersistAsync | 1         |     565.79 μs |     33.090 μs |     49.527 μs |      1,767.45 |
| **Persist**  | **100**   |  **17.32 μs** |  **1.677 μs** |  **2.511 μs** | **57,751.15** |
| PersistAsync | 100       |      16.84 μs |      2.027 μs |      3.034 μs |     59,394.70 |

### Redis Recovery Benchmark

| Method                  | Mean     | Error    | StdDev   | msg/sec   |
|------------------------ |---------:|---------:|---------:|----------:|
| RecoveryBenchmarkMethod | 10.36 μs | 0.564 μs | 0.845 μs | 96,570.90 |

---

## SQL Server

### SQL Server Group Persist Benchmark

| Method       | BatchSize |          Mean |         Error |        StdDev |       msg/sec |
|--------------|-----------|--------------:|--------------:|--------------:|--------------:|
| **Persist**  | **1**     | **977.86 μs** | **41.547 μs** | **62.186 μs** |  **1,022.64** |
| PersistAsync | 1         |     977.57 μs |     41.637 μs |     62.320 μs |      1,022.94 |
| **Persist**  | **100**   |  **73.32 μs** |  **5.269 μs** |  **7.886 μs** | **13,638.03** |
| PersistAsync | 100       |      72.80 μs |      4.537 μs |      6.790 μs |     13,736.47 |

### SQL Server Persist Benchmark

| Method       | BatchSize |           Mean |         Error |        StdDev |      msg/sec |
|--------------|-----------|---------------:|--------------:|--------------:|-------------:|
| **Persist**  | **1**     | **5,016.0 μs** | **186.50 μs** | **279.15 μs** |   **199.36** |
| PersistAsync | 1         |     5,093.4 μs |     302.33 μs |     452.52 μs |       196.33 |
| **Persist**  | **100**   |   **141.3 μs** |  **17.89 μs** |  **26.78 μs** | **7,079.37** |
| PersistAsync | 100       |       139.9 μs |      18.32 μs |      27.42 μs |     7,150.51 |

### SQL Server Recovery Benchmark

| Method                  | Mean     | Error    | StdDev   | msg/sec   |
|------------------------ |---------:|---------:|---------:|----------:|
| RecoveryBenchmarkMethod | 39.89 μs | 8.668 μs | 12.97 μs | 25,069.10 |
