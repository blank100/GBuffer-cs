# Buffer 基准测试结果（BenchmarkDotNet 0.15.8）

> 由仓库内 `GBuffer/Buffer.Benchmark` 项目的原始基准用例直接跑出（不是重新实现的基准），
> 使用真机 BenchmarkDotNet 引擎，含 JIT 预热、自动 InvocationCount 调优、15 次迭代取统计量。

## 运行环境

| 项目 | 值 |
|---|---|
| OS | Debian GNU/Linux 12 (bookworm)，x86-64 |
| CPU | Intel Xeon Processor 2.60GHz，1 physical / 2 logical core（沙箱 vCPU） |
| SDK | .NET SDK 8.0.427（另装有 6.0.428，用于 net6.0 TFM） |
| Runtime | .NET 8.0.33 (8.0.3326.52219), X64 RyuJIT x86-64-v4 |
| BenchmarkDotNet | 0.15.8 |
| 作业配置 | `BenchmarkConfig`：Warmup=10，Iteration=15，MinIterationTime=100ms，`DOTNET_TieredCompilation=0`、`DOTNET_TieredPGO=0` |
| 总耗时 | 41 分 47 秒，544 个 benchmark case，18 个基准类（优化后全量复跑） |
| 分组 | `[GroupBenchmarksBy(ByCategory)]` + `[CategoriesColumn]`，每个类别内以 `**粗体**` 行（`BinaryWriter`/手写实现）为 Baseline，Ratio = 相对基线 |

### 说明 / 局限

- 沙箱 CPU 是共享的虚拟 Xeon（2 vCPU）、无法设置高优先级（日志里的 `Failed to set up high priority (Permission denied)` 属于正常提示），
  所以**绝对纳秒值只适合横向对比，跨机器请只看 Ratio 列**；同一台机器上重复跑，Ratio 一致性明显好于绝对值。
- `N` 是每个 benchmark 的参数（单次循环里的元素个数），单位里的数值已按 `OperationsPerInvoke` 折算为 per-op。
- `Ratio < 1` 表示比基线更快、分配更少；`?` 表示基线在该参数点差值太小或缺失，无法计算。
- 每个类的原始输出（含 HTML/CSV/Markdown 与构建日志）在
  `GBuffer-cs/GBuffer/Buffer.Benchmark/BenchmarkDotNet.Artifacts/results/`。

## 快速结论

同一台机器、同一组基准用例内，`Ratio` = 被测实现 / 基线（`BinaryWriter`/`BinaryReader` 或手写 span 实现），**越小越快**。
以下数字取自本次运行（沙箱 2 vCPU 共享 Xeon，绝对纳秒仅作参考，看 Ratio）。

> **本轮是“应用优化后”的全量复跑**（544 个 case，41 分 47 秒，18 个基准类）。
> 写路径新增了 `Buffer<byte>` / `Writer<byte>` 的具体类型重载，详见 `OPTIMIZATION.md`。
> 与优化前的对比见下表“优化前”列。

### 定长类型：`Buffer<byte>` vs `BinaryWriter/BinaryReader`（N=1024）

| 类型 | 写：Buffer（优化前 → 优化后） | 写：BinaryWriter | 读：Buffer / BinaryReader |
|---|---|---|---|
| Int8 / UInt8 | 0.25x → **0.38x**（968 → 1,459 ns） | 3,841 ns | 0.44x（1,519 vs 3,423 ns） |
| Boolean | 0.85x → **0.42x**（3,308 → 1,577 ns） | 3,792 ns | 0.44x（1,389 vs 3,190 ns） |
| Int16 | 0.47x → **0.17x**（3,122 → 1,493 ns） | 9,016 ns | 0.21x（1,459 vs 6,835 ns） |
| Int32 | 0.43x → **0.17x**（2,989 → 1,414 ns） | 8,313 ns | 0.31x（2,076 vs 6,756 ns） |
| Int64 | 0.34x → **0.18x**（2,419 → 1,478 ns） | 8,299 ns | 0.24x（1,699 vs 7,156 ns） |
| Float | 0.44x → **0.20x**（2,961 → 1,548 ns） | 7,938 ns | 0.18x（1,314 vs 7,180 ns） |
| Double | 0.44x → **0.23x**（2,961 → 1,767 ns） | 7,549 ns | 0.22x（1,431 vs 6,450 ns） |
| UInt16 | — → **0.17x**（1,526 ns） | 9,007 ns | 0.20x（1,400 vs 7,179 ns） |
| UInt32 | — → **0.17x**（1,358 ns） | 7,902 ns | 0.26x（1,695 vs 6,441 ns） |
| UInt64 | — → **0.22x**（1,820 ns） | 8,170 ns | 0.23x（1,666 vs 7,324 ns） |

- 结论：定长写现在比 `BinaryWriter` 快 **4.3–6.0 倍**（优化前是 2.3–4 倍）；读侧优势更大（`Reader<T>` 直接 `Unsafe.ReadUnaligned`，没有 stream 状态机）。
- 与同仓库 `ByteArray` 对照实现比：**所有定长写现在都不慢于 `ByteArray`**，多数快 2.3–6.8 倍
  （`Int32` 1,414 vs 3,648；`Int64` 1,478 vs 6,104；`Double` 1,767 vs 12,002；`UInt32` 1,358 vs 3,639；`UInt64` 1,820 vs 6,044）。
  优化前 `Int16` 写是明显洼地（`Buffer` 3,122 vs `ByteArray` 1,415），现已反超（1,493 vs 1,634）。
- ✅ `WriteBoolean` 洼地已一并修复：`Buffer_WriteBoolean` 3,308 → **1,577 ns**（2.10x），
  与 `ByteArray`（1,251 ns）的差距从 2.64x 收到 1.26x。原因是 `WriteBoolean<T>` 内部走 `self.Write(byte)`
  接口约束调用；已补 `WriteInt8`/`WriteUInt8`/`WriteBoolean` 的具体重载（6 个）。

### 变长编码（VarInt / ZigZag）

| 基准 | 结果 |
|---|---|
| VarInt32 | 写 0.78–0.80x，读 0.98–1.00x（与 `ByteArray` 的 ULEB128 持平） |
| VarInt64 | 写 0.38–0.41x，读 0.68–0.70x（全面更快） |
| 尺寸计算 `GetVarInt*Size` | 0.44–0.83x（`Buffer_GetVarUInt32Size` 0.50x、`Buffer_GetVarInt64Size` 0.44x） |

- 结论：VarInt 写侧优势明显（尤其 64 位）；32 位读侧基本持平，属于可接受范围。

### 字符串（UTF-8）

- `Buffer_WriteUtf8` 42.53 ns（1.22x）、`Buffer_ReadUtf8` 59.19 ns（1.60x）（vs `ByteArray` 的 UTF8 实现 34.94 / 37.16 ns）。
- 大端 UTF-8 写：`Buffer_WriteUtf8BE` 0.78x（24,165 vs 30,835 ns），比手写大端更快。
- ✅ 优化前记录的 `Span_ReadUtf8BE` **369x 异常值已定位并修复**：那是基准把 `int32+int64+double+utf8` 交错布局的数据
  从 offset 0 起按 utf8 解析导致的（把 int32 高半字当长度前缀，解出 16401 字节垃圾串），**库代码无缺陷**。
  修正基准后 `Span_ReadUtf8BE` 40,924 ns vs 手写完整解码 42,317 ns = **0.97x**。

### 大端 / 非对齐

- `Span_WriteInt32BE` 0.72x、`Span_WriteInt64BE` 0.98x：与 `BinaryPrimitives` 手写大端同量级或更快。
- `Span_ReadInt32BE` 1.17x、`Span_ReadDoubleBE` 1.50x：大端读仍比手写慢 17–50%（`ReverseEndianness` + 非对齐读的组合有优化空间）。
- ✅ `Buffer_WriteDoubleBE` 从 **5.98x** 降到 **2.05x**（1,347 vs 656 ns）——第 2 项问题已收敛；
  再补批量写 `WriteSpan<T>` 后，`Buffer_WriteDoubleBE_Batch` 达到 **705.3 ns（0.97x）**，
  已**快于**手写大端基线 `Manual_WriteDoubleBE`（730.5 ns）。
  逐个写的 `Buffer_WriteDoubleBE` 仍为 1,767.6 ns（2.42x），批量场景应优先用 `WriteSpan`。

### CircularBuffer vs Queue

| 场景 | 相对 Queue |
|---|---|
| Enqueue+Dequeue 主循环 | 0.84x（3,533 vs 4,216 ns） |
| TryDequeue（空/非空混合） | 0.51x（2,126 vs 3,358 ns） |
| 满容 TryEnqueue(overwrite) | 0.69x（2,915 vs 5,291 ns） |

- 结论：`CircularBuffer<T>` 三种场景全面快于 `Queue<T>`（1.2–2.0 倍）。

### 泛型 / 接口路径（`GenericWriteBenchmark`、`ReaderWriterBenchmark`）

- `Writer_WriteInt32` 0.18x、`Writer_WriteInt64` 0.21x、`Writer_WriteDouble` 0.21x：
  **`Writer<byte>` 现在与 `Buffer<byte>` 同量级**（1,544 / 1,736 / 1,775 ns），优化前 `Writer<byte>` 比 `Buffer<byte>` 还慢 1.8x。
- `Reader_Read*` 0.17–0.24x、`RefReader_Read*` 0.13–0.14x：**`RefReader`（ref struct）最快，比 `Reader<T>` 再快约 1.4 倍**。
- `Writer_WriteElement` 1.15x、`Writer_Write6Elements` 1.10x：单元素/少元素泛型追加仍略慢于 `Buffer` 的批量写，属可接受范围。

### 测试环境噪声提示

- 沙箱只有 2 个共享 vCPU，`StdDev` 普遍在 5–20%，个别 case（`N=1`、`*_WriteUtf8BE`）误差很大；
  `N=1` 的 case 由于被测量操作太短，`Ratio` 经常是 `?`，**不要用 N=1 的绝对值下结论**。
- 同一份未改动的读端代码在多次跑批中为 1,131 / 1,554 / 1,488 / 1,483 ns（`Buffer_ReadDouble`），
  说明**跨批次比较绝对值要留 ±20% 余量**，结论应建立在 Ratio 与多次复跑之上。

## 目录

- [BigEndianBenchmark](#bigendianbenchmark)
- [ByteBenchmark](#bytebenchmark)
- [CircularBufferBenchmark](#circularbufferbenchmark)
- [DoubleBenchmark](#doublebenchmark)
- [FloatBenchmark](#floatbenchmark)
- [GenericWriteBenchmark](#genericwritebenchmark)
- [Int16Benchmark](#int16benchmark)
- [Int32Benchmark](#int32benchmark)
- [Int64Benchmark](#int64benchmark)
- [ReaderWriterBenchmark](#readerwriterbenchmark)
- [SpanWriteBenchmark](#spanwritebenchmark)
- [UInt16Benchmark](#uint16benchmark)
- [UInt32Benchmark](#uint32benchmark)
- [UInt64Benchmark](#uint64benchmark)
- [UTF8Benchmark](#utf8benchmark)
- [VarInt32Benchmark](#varint32benchmark)
- [VarInt64Benchmark](#varint64benchmark)
- [VarIntSizeBenchmark](#varintsizebenchmark)

# GBuffer / Buffer 基准测试汇总（BenchmarkDotNet）

## BigEndianBenchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| Buffer_WriteDoubleBE | WriteDoubleBE | 1 | 2.4934 ns | 0.5078 ns | 0.4750 ns | 2.2075 ns | 8.20 | 3.11 |
| Buffer_WriteDoubleBE | WriteDoubleBE | 16 | 28.0517 ns | 1.5476 ns | 1.3719 ns | 28.0058 ns | 2.70 | 0.21 |
| Buffer_WriteDoubleBE | WriteDoubleBE | 128 | 231.9333 ns | 16.2742 ns | 14.4266 ns | 226.9893 ns | 2.70 | 0.17 |
| Buffer_WriteDoubleBE | WriteDoubleBE | 1024 | 1,767.5798 ns | 61.5480 ns | 54.5607 ns | 1,777.5620 ns | 2.42 | 0.09 |
| Buffer_WriteDoubleBE_Batch | WriteDoubleBE | 1 | 2.7805 ns | 0.2296 ns | 0.1917 ns | 2.7668 ns | 9.14 | 3.05 |
| Buffer_WriteDoubleBE_Batch | WriteDoubleBE | 16 | 11.8906 ns | 0.9865 ns | 0.7702 ns | 11.9009 ns | 1.15 | 0.10 |
| Buffer_WriteDoubleBE_Batch | WriteDoubleBE | 128 | 90.8867 ns | 5.6153 ns | 4.6891 ns | 92.0952 ns | 1.06 | 0.06 |
| Buffer_WriteDoubleBE_Batch | WriteDoubleBE | 1024 | 705.3171 ns | 18.1295 ns | 15.1389 ns | 708.5812 ns | 0.97 | 0.03 |
| Manual_WriteDoubleBE | WriteDoubleBE | 1 | 0.3267 ns | 0.0842 ns | 0.0746 ns | 0.3626 ns | 1.07 | 0.43 |
| Manual_WriteDoubleBE | WriteDoubleBE | 16 | 10.4158 ns | 0.7119 ns | 0.6311 ns | 10.6120 ns | 1.00 | 0.09 |
| Manual_WriteDoubleBE | WriteDoubleBE | 128 | 85.8468 ns | 2.1696 ns | 1.9233 ns | 86.3811 ns | 1.00 | 0.03 |
| Manual_WriteDoubleBE | WriteDoubleBE | 1024 | 730.4710 ns | 21.7646 ns | 18.1744 ns | 736.8707 ns | 1.00 | 0.03 |

## ByteBenchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadBoolean | ReadBoolean | 1 | 3.8957 ns | 0.0719 ns | 0.0637 ns | 3.8756 ns | 1.00 | 0.02 |
| BinaryReader_ReadBoolean | ReadBoolean | 16 | 51.7634 ns | 1.6587 ns | 1.5516 ns | 50.9022 ns | 1.00 | 0.04 |
| BinaryReader_ReadBoolean | ReadBoolean | 128 | 419.3950 ns | 19.4183 ns | 18.1639 ns | 409.8221 ns | 1.00 | 0.06 |
| BinaryReader_ReadBoolean | ReadBoolean | 1024 | 3,190.1160 ns | 70.6324 ns | 66.0696 ns | 3,164.3492 ns | 1.00 | 0.03 |
| BinaryReader_ReadInt8 | ReadInt8 | 1 | 4.0112 ns | 0.1087 ns | 0.1017 ns | 3.9667 ns | 1.00 | 0.03 |
| BinaryReader_ReadInt8 | ReadInt8 | 16 | 52.5332 ns | 1.6625 ns | 1.4738 ns | 52.6462 ns | 1.00 | 0.04 |
| BinaryReader_ReadInt8 | ReadInt8 | 128 | 408.6505 ns | 7.4364 ns | 6.9560 ns | 407.1061 ns | 1.00 | 0.02 |
| BinaryReader_ReadInt8 | ReadInt8 | 1024 | 3,294.3983 ns | 94.6246 ns | 83.8823 ns | 3,277.5242 ns | 1.00 | 0.03 |
| BinaryReader_ReadUInt8 | ReadUInt8 | 1 | 4.6126 ns | 0.2154 ns | 0.1910 ns | 4.6095 ns | 1.00 | 0.06 |
| BinaryReader_ReadUInt8 | ReadUInt8 | 16 | 52.3925 ns | 1.9492 ns | 1.7279 ns | 52.3392 ns | 1.00 | 0.04 |
| BinaryReader_ReadUInt8 | ReadUInt8 | 128 | 429.0111 ns | 15.6839 ns | 13.0968 ns | 427.3726 ns | 1.00 | 0.04 |
| BinaryReader_ReadUInt8 | ReadUInt8 | 1024 | 3,423.1885 ns | 118.8774 ns | 105.3817 ns | 3,452.0625 ns | 1.00 | 0.04 |
| BinaryWriter_WriteBoolean | WriteBoolean | 1 | 5.8026 ns | 0.2263 ns | 0.2116 ns | 5.7710 ns | 1.00 | 0.05 |
| BinaryWriter_WriteBoolean | WriteBoolean | 16 | 64.8655 ns | 3.0926 ns | 2.8928 ns | 63.7559 ns | 1.00 | 0.06 |
| BinaryWriter_WriteBoolean | WriteBoolean | 128 | 497.4676 ns | 10.2253 ns | 9.5648 ns | 498.6804 ns | 1.00 | 0.03 |
| BinaryWriter_WriteBoolean | WriteBoolean | 1024 | 3,792.4698 ns | 23.1336 ns | 18.0612 ns | 3,795.3348 ns | 1.00 | 0.01 |
| BinaryWriter_WriteInt8 | WriteInt8 | 1 | 5.4866 ns | 0.2482 ns | 0.2321 ns | 5.4907 ns | 1.00 | 0.06 |
| BinaryWriter_WriteInt8 | WriteInt8 | 16 | 60.2246 ns | 1.0626 ns | 0.9420 ns | 60.1472 ns | 1.00 | 0.02 |
| BinaryWriter_WriteInt8 | WriteInt8 | 128 | 486.8043 ns | 14.7352 ns | 13.0624 ns | 488.7621 ns | 1.00 | 0.04 |
| BinaryWriter_WriteInt8 | WriteInt8 | 1024 | 4,972.8931 ns | 66.1240 ns | 61.8524 ns | 4,951.6595 ns | 1.00 | 0.02 |
| BinaryWriter_WriteUInt8 | WriteUInt8 | 1 | 5.4698 ns | 0.1374 ns | 0.1218 ns | 5.4274 ns | 1.00 | 0.03 |
| BinaryWriter_WriteUInt8 | WriteUInt8 | 16 | 66.0845 ns | 3.0475 ns | 2.5448 ns | 66.1503 ns | 1.00 | 0.05 |
| BinaryWriter_WriteUInt8 | WriteUInt8 | 128 | 500.6938 ns | 16.2157 ns | 15.1681 ns | 499.1110 ns | 1.00 | 0.04 |
| BinaryWriter_WriteUInt8 | WriteUInt8 | 1024 | 3,841.4447 ns | 92.9441 ns | 82.3925 ns | 3,839.6960 ns | 1.00 | 0.03 |
| Buffer_ReadBoolean | ReadBoolean | 1 | 0.5654 ns | 0.0233 ns | 0.0195 ns | 0.5621 ns | 0.15 | 0.01 |
| Buffer_ReadBoolean | ReadBoolean | 16 | 14.7988 ns | 1.0182 ns | 0.9026 ns | 14.4813 ns | 0.29 | 0.02 |
| Buffer_ReadBoolean | ReadBoolean | 128 | 153.8953 ns | 4.0530 ns | 3.5929 ns | 154.7591 ns | 0.37 | 0.02 |
| Buffer_ReadBoolean | ReadBoolean | 1024 | 1,389.1937 ns | 103.1012 ns | 96.4409 ns | 1,357.1747 ns | 0.44 | 0.03 |
| Buffer_ReadInt8 | ReadInt8 | 1 | 0.7496 ns | 0.0713 ns | 0.0632 ns | 0.7357 ns | 0.19 | 0.02 |
| Buffer_ReadInt8 | ReadInt8 | 16 | 13.4557 ns | 0.4656 ns | 0.4355 ns | 13.2726 ns | 0.26 | 0.01 |
| Buffer_ReadInt8 | ReadInt8 | 128 | 152.9960 ns | 13.1976 ns | 12.3451 ns | 150.2105 ns | 0.37 | 0.03 |
| Buffer_ReadInt8 | ReadInt8 | 1024 | 1,741.0674 ns | 140.8064 ns | 131.7104 ns | 1,722.5461 ns | 0.53 | 0.04 |
| Buffer_ReadUInt8 | ReadUInt8 | 1 | 0.6057 ns | 0.1849 ns | 0.1729 ns | 0.5421 ns | 0.13 | 0.04 |
| Buffer_ReadUInt8 | ReadUInt8 | 16 | 13.4112 ns | 0.5166 ns | 0.4833 ns | 13.4186 ns | 0.26 | 0.01 |
| Buffer_ReadUInt8 | ReadUInt8 | 128 | 165.7123 ns | 16.9204 ns | 15.8274 ns | 162.1922 ns | 0.39 | 0.04 |
| Buffer_ReadUInt8 | ReadUInt8 | 1024 | 1,518.6945 ns | 74.5717 ns | 69.7544 ns | 1,493.8819 ns | 0.44 | 0.02 |
| Buffer_WriteBoolean | WriteBoolean | 1 | 2.0584 ns | 0.1295 ns | 0.1211 ns | 2.0641 ns | 0.36 | 0.02 |
| Buffer_WriteBoolean | WriteBoolean | 16 | 21.0458 ns | 0.9954 ns | 0.9311 ns | 20.9213 ns | 0.33 | 0.02 |
| Buffer_WriteBoolean | WriteBoolean | 128 | 172.0667 ns | 5.7373 ns | 5.3667 ns | 171.8637 ns | 0.35 | 0.01 |
| Buffer_WriteBoolean | WriteBoolean | 1024 | 1,576.9717 ns | 122.4019 ns | 102.2111 ns | 1,544.6596 ns | 0.42 | 0.03 |
| Buffer_WriteInt8 | WriteInt8 | 1 | 1.9618 ns | 0.1621 ns | 0.1516 ns | 1.9113 ns | 0.36 | 0.03 |
| Buffer_WriteInt8 | WriteInt8 | 16 | 21.4852 ns | 1.1583 ns | 1.0835 ns | 21.4572 ns | 0.36 | 0.02 |
| Buffer_WriteInt8 | WriteInt8 | 128 | 173.7293 ns | 7.0569 ns | 6.2558 ns | 173.3502 ns | 0.36 | 0.02 |
| Buffer_WriteInt8 | WriteInt8 | 1024 | 1,469.9574 ns | 163.4507 ns | 152.8919 ns | 1,444.1555 ns | 0.30 | 0.03 |
| Buffer_WriteUInt8 | WriteUInt8 | 1 | 2.0358 ns | 0.1949 ns | 0.1823 ns | 2.0443 ns | 0.37 | 0.03 |
| Buffer_WriteUInt8 | WriteUInt8 | 16 | 20.7681 ns | 0.9205 ns | 0.8160 ns | 20.4099 ns | 0.31 | 0.02 |
| Buffer_WriteUInt8 | WriteUInt8 | 128 | 171.9181 ns | 4.7951 ns | 4.4853 ns | 171.1879 ns | 0.34 | 0.01 |
| Buffer_WriteUInt8 | WriteUInt8 | 1024 | 1,458.9674 ns | 197.4147 ns | 184.6618 ns | 1,393.5410 ns | 0.38 | 0.05 |
| ByteArray_ReadBoolean | ReadBoolean | 1 | 0.6444 ns | 0.0712 ns | 0.0631 ns | 0.6182 ns | 0.17 | 0.02 |
| ByteArray_ReadBoolean | ReadBoolean | 16 | 11.4312 ns | 0.2861 ns | 0.2536 ns | 11.3598 ns | 0.22 | 0.01 |
| ByteArray_ReadBoolean | ReadBoolean | 128 | 112.2694 ns | 9.1822 ns | 8.5890 ns | 112.2042 ns | 0.27 | 0.02 |
| ByteArray_ReadBoolean | ReadBoolean | 1024 | 1,016.3633 ns | 115.0032 ns | 107.5741 ns | 1,020.7729 ns | 0.32 | 0.03 |
| ByteArray_ReadInt8 | ReadInt8 | 1 | 0.5504 ns | 0.0776 ns | 0.0726 ns | 0.5163 ns | 0.14 | 0.02 |
| ByteArray_ReadInt8 | ReadInt8 | 16 | 11.1254 ns | 0.2832 ns | 0.2649 ns | 11.1769 ns | 0.21 | 0.01 |
| ByteArray_ReadInt8 | ReadInt8 | 128 | 115.1326 ns | 13.1134 ns | 11.6247 ns | 111.8159 ns | 0.28 | 0.03 |
| ByteArray_ReadInt8 | ReadInt8 | 1024 | 1,342.9486 ns | 179.3816 ns | 167.7936 ns | 1,304.1830 ns | 0.41 | 0.05 |
| ByteArray_ReadUInt8 | ReadUInt8 | 1 | 0.1500 ns | 0.1142 ns | 0.1068 ns | 0.1413 ns | 0.03 | 0.02 |
| ByteArray_ReadUInt8 | ReadUInt8 | 16 | 11.5468 ns | 0.7669 ns | 0.7173 ns | 11.6421 ns | 0.22 | 0.01 |
| ByteArray_ReadUInt8 | ReadUInt8 | 128 | 111.0749 ns | 12.6295 ns | 11.8137 ns | 109.6052 ns | 0.26 | 0.03 |
| ByteArray_ReadUInt8 | ReadUInt8 | 1024 | 1,560.9568 ns | 118.5640 ns | 110.9048 ns | 1,528.4441 ns | 0.46 | 0.03 |
| ByteArray_WriteBoolean | WriteBoolean | 1 | 1.9259 ns | 0.1296 ns | 0.1212 ns | 1.9057 ns | 0.33 | 0.02 |
| ByteArray_WriteBoolean | WriteBoolean | 16 | 20.7548 ns | 0.5301 ns | 0.4138 ns | 20.6706 ns | 0.32 | 0.01 |
| ByteArray_WriteBoolean | WriteBoolean | 128 | 156.9127 ns | 4.7655 ns | 3.9794 ns | 155.7490 ns | 0.32 | 0.01 |
| ByteArray_WriteBoolean | WriteBoolean | 1024 | 1,250.6967 ns | 47.2481 ns | 44.1959 ns | 1,241.3349 ns | 0.33 | 0.01 |
| ByteArray_WriteInt8 | WriteInt8 | 1 | 1.8266 ns | 0.1243 ns | 0.1102 ns | 1.8069 ns | 0.33 | 0.02 |
| ByteArray_WriteInt8 | WriteInt8 | 16 | 22.4567 ns | 1.3188 ns | 1.2336 ns | 22.0092 ns | 0.37 | 0.02 |
| ByteArray_WriteInt8 | WriteInt8 | 128 | 153.3694 ns | 2.8765 ns | 2.6907 ns | 153.2348 ns | 0.32 | 0.01 |
| ByteArray_WriteInt8 | WriteInt8 | 1024 | 1,274.5877 ns | 54.7146 ns | 48.5031 ns | 1,254.7490 ns | 0.26 | 0.01 |
| ByteArray_WriteUInt8 | WriteUInt8 | 1 | 2.0534 ns | 0.0778 ns | 0.0690 ns | 2.0375 ns | 0.38 | 0.01 |
| ByteArray_WriteUInt8 | WriteUInt8 | 16 | 22.1645 ns | 1.0575 ns | 0.9892 ns | 21.9002 ns | 0.34 | 0.02 |
| ByteArray_WriteUInt8 | WriteUInt8 | 128 | 160.2207 ns | 8.2259 ns | 7.6945 ns | 160.6720 ns | 0.32 | 0.02 |
| ByteArray_WriteUInt8 | WriteUInt8 | 1024 | 1,283.1079 ns | 24.6955 ns | 21.8919 ns | 1,286.8597 ns | 0.33 | 0.01 |

## CircularBufferBenchmark

| Method | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|
| Circular_EnqueueDequeue | 1 | 183.7 ns | 4.20 ns | 3.93 ns | 183.2 ns | 0.93 | 0.07 |
| Circular_EnqueueDequeue | 16 | 237.9 ns | 11.34 ns | 10.61 ns | 236.1 ns | 0.93 | 0.07 |
| Circular_EnqueueDequeue | 128 | 708.0 ns | 13.32 ns | 12.46 ns | 708.7 ns | 1.01 | 0.03 |
| Circular_EnqueueDequeue | 1024 | 3,532.5 ns | 109.26 ns | 96.85 ns | 3,553.7 ns | 0.84 | 0.06 |
| Circular_TryDequeue | 1 | 194.1 ns | 7.03 ns | 6.23 ns | 194.2 ns | 0.99 | 0.07 |
| Circular_TryDequeue | 16 | 228.0 ns | 9.07 ns | 8.48 ns | 227.8 ns | 0.89 | 0.06 |
| Circular_TryDequeue | 128 | 457.6 ns | 23.18 ns | 21.69 ns | 456.4 ns | 0.65 | 0.03 |
| Circular_TryDequeue | 1024 | 2,126.3 ns | 108.97 ns | 101.94 ns | 2,096.9 ns | 0.51 | 0.04 |
| Circular_TryEnqueueOverwrite | 1 | 1,600.2 ns | 49.71 ns | 41.51 ns | 1,599.3 ns | 8.14 | 0.58 |
| Circular_TryEnqueueOverwrite | 16 | 1,578.5 ns | 65.70 ns | 61.45 ns | 1,563.5 ns | 6.15 | 0.42 |
| Circular_TryEnqueueOverwrite | 128 | 1,864.6 ns | 70.86 ns | 66.28 ns | 1,869.3 ns | 2.65 | 0.11 |
| Circular_TryEnqueueOverwrite | 1024 | 2,915.1 ns | 147.28 ns | 137.77 ns | 2,864.1 ns | 0.69 | 0.05 |
| Queue_EnqueueDequeue | 1 | 197.4 ns | 14.77 ns | 13.82 ns | 197.1 ns | 1.00 | 0.10 |
| Queue_EnqueueDequeue | 16 | 257.3 ns | 15.92 ns | 14.90 ns | 257.5 ns | 1.00 | 0.08 |
| Queue_EnqueueDequeue | 128 | 704.0 ns | 17.76 ns | 15.74 ns | 698.3 ns | 1.00 | 0.03 |
| Queue_EnqueueDequeue | 1024 | 4,216.1 ns | 298.30 ns | 279.03 ns | 4,239.2 ns | 1.00 | 0.09 |
| Queue_OverwriteSim | 1 | 1,985.7 ns | 134.89 ns | 126.17 ns | 1,989.0 ns | 10.11 | 0.92 |
| Queue_OverwriteSim | 16 | 1,974.5 ns | 49.94 ns | 46.72 ns | 1,967.9 ns | 7.70 | 0.47 |
| Queue_OverwriteSim | 128 | 2,421.9 ns | 129.51 ns | 121.14 ns | 2,477.3 ns | 3.44 | 0.18 |
| Queue_OverwriteSim | 1024 | 5,290.8 ns | 119.81 ns | 106.21 ns | 5,311.2 ns | 1.26 | 0.08 |
| Queue_TryDequeue | 1 | 193.4 ns | 13.97 ns | 13.07 ns | 188.1 ns | 0.98 | 0.09 |
| Queue_TryDequeue | 16 | 255.2 ns | 19.22 ns | 17.98 ns | 254.5 ns | 0.99 | 0.09 |
| Queue_TryDequeue | 128 | 598.5 ns | 10.29 ns | 9.62 ns | 596.1 ns | 0.85 | 0.02 |
| Queue_TryDequeue | 1024 | 3,358.0 ns | 90.98 ns | 80.65 ns | 3,349.6 ns | 0.80 | 0.05 |

## DoubleBenchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadDouble | Read | 1 | 6.977 ns | 0.2467 ns | 0.2187 ns | 6.884 ns | 1.00 | 0.04 |
| BinaryReader_ReadDouble | Read | 16 | 109.277 ns | 6.4505 ns | 5.7182 ns | 109.414 ns | 1.00 | 0.07 |
| BinaryReader_ReadDouble | Read | 128 | 851.265 ns | 26.1962 ns | 23.2222 ns | 847.786 ns | 1.00 | 0.04 |
| BinaryReader_ReadDouble | Read | 1024 | 6,449.759 ns | 111.8864 ns | 99.1843 ns | 6,412.517 ns | 1.00 | 0.02 |
| BinaryWriter_WriteDouble | Write | 1 | 10.077 ns | 0.7663 ns | 0.6793 ns | 10.117 ns | 1.00 | 0.09 |
| BinaryWriter_WriteDouble | Write | 16 | 145.738 ns | 9.1168 ns | 8.5278 ns | 144.381 ns | 1.00 | 0.08 |
| BinaryWriter_WriteDouble | Write | 128 | 988.542 ns | 26.7427 ns | 25.0151 ns | 987.644 ns | 1.00 | 0.03 |
| BinaryWriter_WriteDouble | Write | 1024 | 7,548.903 ns | 269.2473 ns | 251.8541 ns | 7,474.612 ns | 1.00 | 0.05 |
| Buffer_ReadDouble | Read | 1 | 1.832 ns | 0.0933 ns | 0.0827 ns | 1.812 ns | 0.26 | 0.01 |
| Buffer_ReadDouble | Read | 16 | 24.919 ns | 0.5206 ns | 0.4615 ns | 24.970 ns | 0.23 | 0.01 |
| Buffer_ReadDouble | Read | 128 | 214.648 ns | 10.2033 ns | 9.5442 ns | 214.046 ns | 0.25 | 0.01 |
| Buffer_ReadDouble | Read | 1024 | 1,431.031 ns | 41.2423 ns | 36.5602 ns | 1,426.676 ns | 0.22 | 0.01 |
| Buffer_WriteDouble | Write | 1 | 2.006 ns | 0.0750 ns | 0.0626 ns | 2.012 ns | 0.20 | 0.01 |
| Buffer_WriteDouble | Write | 16 | 26.256 ns | 0.9982 ns | 0.9337 ns | 26.195 ns | 0.18 | 0.01 |
| Buffer_WriteDouble | Write | 128 | 228.328 ns | 13.9242 ns | 13.0247 ns | 227.874 ns | 0.23 | 0.01 |
| Buffer_WriteDouble | Write | 1024 | 1,767.133 ns | 64.5105 ns | 57.1868 ns | 1,775.727 ns | 0.23 | 0.01 |
| ByteArray_ReadDouble | Read | 1 | 5.423 ns | 0.2370 ns | 0.2101 ns | 5.390 ns | 0.78 | 0.04 |
| ByteArray_ReadDouble | Read | 16 | 84.250 ns | 1.9744 ns | 1.8469 ns | 83.441 ns | 0.77 | 0.04 |
| ByteArray_ReadDouble | Read | 128 | 740.062 ns | 55.7892 ns | 52.1852 ns | 731.568 ns | 0.87 | 0.06 |
| ByteArray_ReadDouble | Read | 1024 | 5,615.185 ns | 106.1301 ns | 82.8594 ns | 5,629.018 ns | 0.87 | 0.02 |
| ByteArray_WriteDouble | Write | 1 | 11.653 ns | 0.5048 ns | 0.4215 ns | 11.595 ns | 1.16 | 0.08 |
| ByteArray_WriteDouble | Write | 16 | 201.109 ns | 9.1308 ns | 7.6246 ns | 201.186 ns | 1.38 | 0.09 |
| ByteArray_WriteDouble | Write | 128 | 1,467.987 ns | 63.1226 ns | 52.7102 ns | 1,464.329 ns | 1.49 | 0.06 |
| ByteArray_WriteDouble | Write | 1024 | 12,001.713 ns | 1,219.2827 ns | 1,140.5178 ns | 11,699.041 ns | 1.59 | 0.15 |

## FloatBenchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadFloat | Read | 1 | 7.158 ns | 0.2472 ns | 0.2191 ns | 7.147 ns | 1.00 | 0.04 |
| BinaryReader_ReadFloat | Read | 16 | 112.424 ns | 5.2050 ns | 4.8687 ns | 111.380 ns | 1.00 | 0.06 |
| BinaryReader_ReadFloat | Read | 128 | 908.108 ns | 34.6321 ns | 32.3949 ns | 904.065 ns | 1.00 | 0.05 |
| BinaryReader_ReadFloat | Read | 1024 | 7,180.362 ns | 311.7809 ns | 291.6400 ns | 7,114.294 ns | 1.00 | 0.06 |
| BinaryWriter_WriteFloat | Write | 1 | 9.188 ns | 0.2655 ns | 0.2353 ns | 9.236 ns | 1.00 | 0.04 |
| BinaryWriter_WriteFloat | Write | 16 | 127.912 ns | 4.9791 ns | 4.4138 ns | 128.483 ns | 1.00 | 0.05 |
| BinaryWriter_WriteFloat | Write | 128 | 1,025.474 ns | 45.0963 ns | 37.6575 ns | 1,036.218 ns | 1.00 | 0.05 |
| BinaryWriter_WriteFloat | Write | 1024 | 7,938.210 ns | 411.8973 ns | 365.1363 ns | 7,926.835 ns | 1.00 | 0.06 |
| Buffer_ReadFloat | Read | 1 | 2.036 ns | 0.2156 ns | 0.2017 ns | 1.965 ns | 0.28 | 0.03 |
| Buffer_ReadFloat | Read | 16 | 21.060 ns | 1.3511 ns | 1.1977 ns | 20.633 ns | 0.19 | 0.01 |
| Buffer_ReadFloat | Read | 128 | 181.724 ns | 12.8545 ns | 11.3952 ns | 176.485 ns | 0.20 | 0.01 |
| Buffer_ReadFloat | Read | 1024 | 1,313.583 ns | 62.2437 ns | 58.2228 ns | 1,305.307 ns | 0.18 | 0.01 |
| Buffer_WriteFloat | Write | 1 | 2.068 ns | 0.1861 ns | 0.1554 ns | 2.048 ns | 0.23 | 0.02 |
| Buffer_WriteFloat | Write | 16 | 24.538 ns | 1.1162 ns | 1.0441 ns | 24.252 ns | 0.19 | 0.01 |
| Buffer_WriteFloat | Write | 128 | 189.918 ns | 6.2501 ns | 5.8464 ns | 190.228 ns | 0.19 | 0.01 |
| Buffer_WriteFloat | Write | 1024 | 1,548.165 ns | 122.7015 ns | 114.7751 ns | 1,546.699 ns | 0.20 | 0.02 |
| ByteArray_ReadFloat | Read | 1 | 1.704 ns | 0.2011 ns | 0.1783 ns | 1.644 ns | 0.24 | 0.03 |
| ByteArray_ReadFloat | Read | 16 | 35.997 ns | 1.0274 ns | 0.9108 ns | 35.672 ns | 0.32 | 0.02 |
| ByteArray_ReadFloat | Read | 128 | 336.001 ns | 17.1969 ns | 16.0860 ns | 333.071 ns | 0.37 | 0.02 |
| ByteArray_ReadFloat | Read | 1024 | 2,630.524 ns | 62.7313 ns | 55.6097 ns | 2,631.544 ns | 0.37 | 0.02 |
| ByteArray_WriteFloat | Write | 1 | 3.598 ns | 0.2744 ns | 0.2432 ns | 3.591 ns | 0.39 | 0.03 |
| ByteArray_WriteFloat | Write | 16 | 67.658 ns | 3.8713 ns | 3.4318 ns | 68.143 ns | 0.53 | 0.03 |
| ByteArray_WriteFloat | Write | 128 | 463.668 ns | 27.9378 ns | 26.1330 ns | 447.789 ns | 0.45 | 0.03 |
| ByteArray_WriteFloat | Write | 1024 | 3,611.664 ns | 195.0517 ns | 182.4515 ns | 3,629.293 ns | 0.46 | 0.03 |

## GenericWriteBenchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| Buffer_ReadByte | GenericRead | 1 | 0.3092 ns | 0.1295 ns | 0.1211 ns | 0.2553 ns | 1.14 | 0.60 |
| Buffer_ReadByte | GenericRead | 16 | 12.9316 ns | 0.5623 ns | 0.5260 ns | 12.7534 ns | 1.00 | 0.06 |
| Buffer_ReadByte | GenericRead | 128 | 115.5524 ns | 8.2976 ns | 7.7616 ns | 114.7578 ns | 1.00 | 0.09 |
| Buffer_ReadByte | GenericRead | 1024 | 1,214.3893 ns | 152.3484 ns | 142.5068 ns | 1,175.2519 ns | 1.01 | 0.16 |
| Buffer_Write6Bytes | GenericWrite6 | 1 | 0.5725 ns | 0.1472 ns | 0.1377 ns | 0.5513 ns | 1.05 | 0.35 |
| Buffer_Write6Bytes | GenericWrite6 | 16 | 4.6528 ns | 0.3049 ns | 0.2546 ns | 4.5819 ns | 1.00 | 0.07 |
| Buffer_Write6Bytes | GenericWrite6 | 128 | 42.3536 ns | 1.2520 ns | 1.1098 ns | 42.5458 ns | 1.00 | 0.04 |
| Buffer_Write6Bytes | GenericWrite6 | 1024 | 315.9881 ns | 9.1374 ns | 8.1001 ns | 317.7315 ns | 1.00 | 0.04 |
| Buffer_WriteByte | GenericWrite | 1 | 2.1183 ns | 0.0930 ns | 0.0824 ns | 2.1000 ns | 1.00 | 0.05 |
| Buffer_WriteByte | GenericWrite | 16 | 23.6237 ns | 0.5787 ns | 0.5130 ns | 23.5442 ns | 1.00 | 0.03 |
| Buffer_WriteByte | GenericWrite | 128 | 178.3743 ns | 6.4702 ns | 5.7356 ns | 177.3364 ns | 1.00 | 0.04 |
| Buffer_WriteByte | GenericWrite | 1024 | 1,338.1259 ns | 56.2525 ns | 52.6187 ns | 1,335.2860 ns | 1.00 | 0.05 |
| Buffer_WriteSpan | GenericWriteSpan | 1 | 5.3296 ns | 0.6310 ns | 0.5902 ns | 5.4628 ns | 1.01 | 0.15 |
| Buffer_WriteSpan | GenericWriteSpan | 16 | 5.3769 ns | 0.1764 ns | 0.1564 ns | 5.3435 ns | 1.00 | 0.04 |
| Buffer_WriteSpan | GenericWriteSpan | 128 | 10.9603 ns | 0.6302 ns | 0.5895 ns | 10.8580 ns | 1.00 | 0.07 |
| Buffer_WriteSpan | GenericWriteSpan | 1024 | 74.8988 ns | 3.4288 ns | 3.2073 ns | 74.7084 ns | 1.00 | 0.06 |
| Reader_ReadElement | GenericRead | 1 | 5.7619 ns | 0.4268 ns | 0.3783 ns | 5.7270 ns | 21.30 | 7.42 |
| Reader_ReadElement | GenericRead | 16 | 13.7275 ns | 0.6686 ns | 0.6254 ns | 13.8339 ns | 1.06 | 0.06 |
| Reader_ReadElement | GenericRead | 128 | 101.7918 ns | 3.6095 ns | 3.3763 ns | 101.0470 ns | 0.88 | 0.06 |
| Reader_ReadElement | GenericRead | 1024 | 990.3152 ns | 64.5536 ns | 57.2251 ns | 976.3094 ns | 0.83 | 0.10 |
| Writer_Write6Elements | GenericWrite6 | 1 | 0.8215 ns | 0.1212 ns | 0.1074 ns | 0.8038 ns | 1.51 | 0.40 |
| Writer_Write6Elements | GenericWrite6 | 16 | 5.5148 ns | 0.2681 ns | 0.2377 ns | 5.4629 ns | 1.19 | 0.08 |
| Writer_Write6Elements | GenericWrite6 | 128 | 54.0800 ns | 2.3778 ns | 2.2242 ns | 54.4937 ns | 1.28 | 0.06 |
| Writer_Write6Elements | GenericWrite6 | 1024 | 358.8763 ns | 19.7130 ns | 18.4395 ns | 356.4681 ns | 1.14 | 0.06 |
| Writer_WriteElement | GenericWrite | 1 | 2.6297 ns | 0.1400 ns | 0.1241 ns | 2.6146 ns | 1.24 | 0.07 |
| Writer_WriteElement | GenericWrite | 16 | 25.0805 ns | 0.9024 ns | 0.8441 ns | 24.7383 ns | 1.06 | 0.04 |
| Writer_WriteElement | GenericWrite | 128 | 207.2849 ns | 7.8322 ns | 6.9431 ns | 206.3925 ns | 1.16 | 0.05 |
| Writer_WriteElement | GenericWrite | 1024 | 1,588.9670 ns | 54.3308 ns | 48.1629 ns | 1,603.5139 ns | 1.19 | 0.06 |
| Writer_WriteSpan | GenericWriteSpan | 1 | 4.6352 ns | 0.2343 ns | 0.2077 ns | 4.6672 ns | 0.88 | 0.10 |
| Writer_WriteSpan | GenericWriteSpan | 16 | 5.1487 ns | 0.5346 ns | 0.5001 ns | 4.9939 ns | 0.96 | 0.09 |
| Writer_WriteSpan | GenericWriteSpan | 128 | 9.7731 ns | 0.3924 ns | 0.3670 ns | 9.8813 ns | 0.89 | 0.06 |
| Writer_WriteSpan | GenericWriteSpan | 1024 | 70.6889 ns | 2.5151 ns | 2.2296 ns | 70.6142 ns | 0.95 | 0.05 |

## Int16Benchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadInt16 | Read | 1 | 7.3095 ns | 0.2479 ns | 0.2319 ns | 7.3050 ns | 1.00 | 0.04 |
| BinaryReader_ReadInt16 | Read | 16 | 112.0428 ns | 3.8230 ns | 3.5760 ns | 112.2932 ns | 1.00 | 0.04 |
| BinaryReader_ReadInt16 | Read | 128 | 900.7452 ns | 26.5928 ns | 24.8749 ns | 901.9576 ns | 1.00 | 0.04 |
| BinaryReader_ReadInt16 | Read | 1024 | 6,835.5389 ns | 246.7856 ns | 206.0772 ns | 6,817.6691 ns | 1.00 | 0.04 |
| BinaryWriter_WriteInt16 | Write | 1 | 10.7421 ns | 0.8512 ns | 0.7962 ns | 10.4428 ns | 1.00 | 0.10 |
| BinaryWriter_WriteInt16 | Write | 16 | 147.7954 ns | 5.3961 ns | 5.0476 ns | 146.4002 ns | 1.00 | 0.05 |
| BinaryWriter_WriteInt16 | Write | 128 | 1,126.8424 ns | 38.3127 ns | 35.8377 ns | 1,130.2724 ns | 1.00 | 0.04 |
| BinaryWriter_WriteInt16 | Write | 1024 | 9,016.4956 ns | 471.9102 ns | 441.4251 ns | 9,003.3366 ns | 1.00 | 0.07 |
| Buffer_ReadInt16 | Read | 1 | 2.0189 ns | 0.2263 ns | 0.2006 ns | 1.9902 ns | 0.28 | 0.03 |
| Buffer_ReadInt16 | Read | 16 | 22.1044 ns | 0.8085 ns | 0.7167 ns | 22.0978 ns | 0.20 | 0.01 |
| Buffer_ReadInt16 | Read | 128 | 185.2258 ns | 4.2504 ns | 3.7679 ns | 185.7512 ns | 0.21 | 0.01 |
| Buffer_ReadInt16 | Read | 1024 | 1,459.4735 ns | 76.4254 ns | 71.4883 ns | 1,421.5580 ns | 0.21 | 0.01 |
| Buffer_WriteInt16 | Write | 1 | 2.1953 ns | 0.1494 ns | 0.1397 ns | 2.2250 ns | 0.21 | 0.02 |
| Buffer_WriteInt16 | Write | 16 | 26.0575 ns | 1.2818 ns | 1.1990 ns | 25.6893 ns | 0.18 | 0.01 |
| Buffer_WriteInt16 | Write | 128 | 433.5807 ns | 18.0966 ns | 16.9276 ns | 431.2766 ns | 0.39 | 0.02 |
| Buffer_WriteInt16 | Write | 1024 | 1,492.7453 ns | 54.9669 ns | 51.4160 ns | 1,473.5966 ns | 0.17 | 0.01 |
| ByteArray_ReadInt16 | Read | 1 | 0.3609 ns | 0.0877 ns | 0.0777 ns | 0.3645 ns | 0.05 | 0.01 |
| ByteArray_ReadInt16 | Read | 16 | 17.5064 ns | 0.9073 ns | 0.8487 ns | 17.5882 ns | 0.16 | 0.01 |
| ByteArray_ReadInt16 | Read | 128 | 158.4545 ns | 31.0527 ns | 29.0468 ns | 147.1293 ns | 0.18 | 0.03 |
| ByteArray_ReadInt16 | Read | 1024 | 1,112.8433 ns | 43.2835 ns | 38.3697 ns | 1,101.3917 ns | 0.16 | 0.01 |
| ByteArray_WriteInt16 | Write | 1 | 2.5568 ns | 0.2052 ns | 0.1920 ns | 2.6063 ns | 0.24 | 0.02 |
| ByteArray_WriteInt16 | Write | 16 | 27.3182 ns | 0.5788 ns | 0.5414 ns | 27.2711 ns | 0.19 | 0.01 |
| ByteArray_WriteInt16 | Write | 128 | 211.1353 ns | 10.4663 ns | 9.7902 ns | 210.7663 ns | 0.19 | 0.01 |
| ByteArray_WriteInt16 | Write | 1024 | 1,634.0575 ns | 63.7683 ns | 59.6489 ns | 1,647.8998 ns | 0.18 | 0.01 |

## Int32Benchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadInt32 | Read | 1 | 7.7403 ns | 0.3390 ns | 0.3005 ns | 7.7820 ns | 1.00 | 0.05 |
| BinaryReader_ReadInt32 | Read | 16 | 114.5984 ns | 5.5002 ns | 5.1449 ns | 113.5241 ns | 1.00 | 0.06 |
| BinaryReader_ReadInt32 | Read | 128 | 880.6836 ns | 33.5689 ns | 31.4004 ns | 883.1475 ns | 1.00 | 0.05 |
| BinaryReader_ReadInt32 | Read | 1024 | 6,755.8995 ns | 288.8685 ns | 256.0744 ns | 6,663.6010 ns | 1.00 | 0.05 |
| BinaryWriter_WriteInt32 | Write | 1 | 10.3525 ns | 0.4201 ns | 0.3724 ns | 10.2657 ns | 1.00 | 0.05 |
| BinaryWriter_WriteInt32 | Write | 16 | 146.0157 ns | 8.9689 ns | 7.9507 ns | 144.7973 ns | 1.00 | 0.07 |
| BinaryWriter_WriteInt32 | Write | 128 | 1,071.0321 ns | 40.3165 ns | 37.7121 ns | 1,073.9533 ns | 1.00 | 0.05 |
| BinaryWriter_WriteInt32 | Write | 1024 | 8,313.1822 ns | 276.9140 ns | 259.0255 ns | 8,287.4640 ns | 1.00 | 0.04 |
| Buffer_ReadInt32 | Read | 1 | 2.0479 ns | 0.1752 ns | 0.1554 ns | 2.0092 ns | 0.26 | 0.02 |
| Buffer_ReadInt32 | Read | 16 | 22.2286 ns | 1.4008 ns | 1.2418 ns | 22.2572 ns | 0.19 | 0.01 |
| Buffer_ReadInt32 | Read | 128 | 203.7060 ns | 10.3907 ns | 9.7195 ns | 203.6682 ns | 0.23 | 0.01 |
| Buffer_ReadInt32 | Read | 1024 | 2,076.1112 ns | 332.4824 ns | 311.0042 ns | 1,950.1682 ns | 0.31 | 0.05 |
| Buffer_WriteInt32 | Write | 1 | 1.8038 ns | 0.1762 ns | 0.1648 ns | 1.7676 ns | 0.17 | 0.02 |
| Buffer_WriteInt32 | Write | 16 | 26.8655 ns | 0.9797 ns | 0.9164 ns | 26.9817 ns | 0.18 | 0.01 |
| Buffer_WriteInt32 | Write | 128 | 182.4764 ns | 4.1135 ns | 3.8478 ns | 182.3286 ns | 0.17 | 0.01 |
| Buffer_WriteInt32 | Write | 1024 | 1,414.1433 ns | 57.8922 ns | 51.3199 ns | 1,420.7451 ns | 0.17 | 0.01 |
| ByteArray_ReadInt32 | Read | 1 | 1.6987 ns | 0.1051 ns | 0.0932 ns | 1.6857 ns | 0.22 | 0.01 |
| ByteArray_ReadInt32 | Read | 16 | 33.7281 ns | 1.6634 ns | 1.5560 ns | 33.6372 ns | 0.29 | 0.02 |
| ByteArray_ReadInt32 | Read | 128 | 271.5978 ns | 14.7620 ns | 11.5252 ns | 272.1149 ns | 0.31 | 0.02 |
| ByteArray_ReadInt32 | Read | 1024 | 2,252.8034 ns | 99.6978 ns | 83.2522 ns | 2,293.5587 ns | 0.33 | 0.02 |
| ByteArray_WriteInt32 | Write | 1 | 3.5746 ns | 0.2056 ns | 0.1605 ns | 3.5320 ns | 0.35 | 0.02 |
| ByteArray_WriteInt32 | Write | 16 | 69.2028 ns | 3.6992 ns | 3.4602 ns | 69.2243 ns | 0.48 | 0.03 |
| ByteArray_WriteInt32 | Write | 128 | 451.0979 ns | 12.0843 ns | 11.3036 ns | 451.8735 ns | 0.42 | 0.02 |
| ByteArray_WriteInt32 | Write | 1024 | 3,647.6816 ns | 271.5016 ns | 253.9628 ns | 3,669.9471 ns | 0.44 | 0.03 |
| Bytes_ReadInt32 | Read | 1 | 0.8484 ns | 0.1094 ns | 0.1024 ns | 0.8660 ns | 0.11 | 0.01 |
| Bytes_ReadInt32 | Read | 16 | 10.2680 ns | 0.5388 ns | 0.4500 ns | 10.2103 ns | 0.09 | 0.01 |
| Bytes_ReadInt32 | Read | 128 | 83.5695 ns | 2.9085 ns | 2.2707 ns | 84.3082 ns | 0.10 | 0.00 |
| Bytes_ReadInt32 | Read | 1024 | 679.5899 ns | 44.3978 ns | 41.5297 ns | 687.1947 ns | 0.10 | 0.01 |
| Bytes_WriteInt32 | Write | 1 | 0.8631 ns | 0.0491 ns | 0.0383 ns | 0.8675 ns | 0.08 | 0.00 |
| Bytes_WriteInt32 | Write | 16 | 6.7806 ns | 0.4603 ns | 0.4081 ns | 6.7744 ns | 0.05 | 0.00 |
| Bytes_WriteInt32 | Write | 128 | 43.5393 ns | 2.1720 ns | 1.9254 ns | 43.2201 ns | 0.04 | 0.00 |
| Bytes_WriteInt32 | Write | 1024 | 378.2306 ns | 18.5344 ns | 17.3371 ns | 379.6550 ns | 0.05 | 0.00 |

## Int64Benchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadInt64 | Read | 1 | 7.892 ns | 0.3607 ns | 0.3374 ns | 7.870 ns | 1.00 | 0.06 |
| BinaryReader_ReadInt64 | Read | 16 | 114.435 ns | 5.7714 ns | 5.3985 ns | 112.838 ns | 1.00 | 0.06 |
| BinaryReader_ReadInt64 | Read | 128 | 892.212 ns | 46.2618 ns | 43.2733 ns | 881.467 ns | 1.00 | 0.07 |
| BinaryReader_ReadInt64 | Read | 1024 | 7,156.172 ns | 320.2628 ns | 299.5740 ns | 7,192.333 ns | 1.00 | 0.06 |
| BinaryWriter_WriteInt64 | Write | 1 | 10.404 ns | 0.3274 ns | 0.2902 ns | 10.428 ns | 1.00 | 0.04 |
| BinaryWriter_WriteInt64 | Write | 16 | 143.773 ns | 12.6866 ns | 11.8670 ns | 139.301 ns | 1.01 | 0.11 |
| BinaryWriter_WriteInt64 | Write | 128 | 1,055.733 ns | 47.5131 ns | 42.1191 ns | 1,051.539 ns | 1.00 | 0.05 |
| BinaryWriter_WriteInt64 | Write | 1024 | 8,298.621 ns | 497.5696 ns | 465.4269 ns | 8,221.736 ns | 1.00 | 0.08 |
| Buffer_ReadInt64 | Read | 1 | 1.798 ns | 0.2316 ns | 0.2053 ns | 1.851 ns | 0.23 | 0.03 |
| Buffer_ReadInt64 | Read | 16 | 25.972 ns | 0.8827 ns | 0.8257 ns | 26.101 ns | 0.23 | 0.01 |
| Buffer_ReadInt64 | Read | 128 | 181.418 ns | 7.5930 ns | 6.7310 ns | 181.951 ns | 0.20 | 0.01 |
| Buffer_ReadInt64 | Read | 1024 | 1,698.594 ns | 43.8956 ns | 41.0600 ns | 1,700.060 ns | 0.24 | 0.01 |
| Buffer_WriteInt64 | Write | 1 | 2.337 ns | 0.1852 ns | 0.1546 ns | 2.346 ns | 0.22 | 0.02 |
| Buffer_WriteInt64 | Write | 16 | 25.572 ns | 1.2533 ns | 1.1723 ns | 25.489 ns | 0.18 | 0.02 |
| Buffer_WriteInt64 | Write | 128 | 207.318 ns | 10.8723 ns | 9.6380 ns | 205.794 ns | 0.20 | 0.01 |
| Buffer_WriteInt64 | Write | 1024 | 1,478.060 ns | 49.0136 ns | 45.8474 ns | 1,476.688 ns | 0.18 | 0.01 |
| ByteArray_ReadInt64 | Read | 1 | 4.583 ns | 0.3216 ns | 0.2851 ns | 4.682 ns | 0.58 | 0.04 |
| ByteArray_ReadInt64 | Read | 16 | 75.322 ns | 2.9747 ns | 2.7825 ns | 74.620 ns | 0.66 | 0.04 |
| ByteArray_ReadInt64 | Read | 128 | 617.559 ns | 28.2056 ns | 25.0035 ns | 614.601 ns | 0.69 | 0.04 |
| ByteArray_ReadInt64 | Read | 1024 | 5,028.328 ns | 213.9066 ns | 189.6226 ns | 5,036.659 ns | 0.70 | 0.04 |
| ByteArray_WriteInt64 | Write | 1 | 5.627 ns | 0.3411 ns | 0.3191 ns | 5.580 ns | 0.54 | 0.03 |
| ByteArray_WriteInt64 | Write | 16 | 97.189 ns | 3.5412 ns | 3.1392 ns | 97.254 ns | 0.68 | 0.06 |
| ByteArray_WriteInt64 | Write | 128 | 774.595 ns | 26.9211 ns | 22.4804 ns | 781.227 ns | 0.73 | 0.03 |
| ByteArray_WriteInt64 | Write | 1024 | 6,104.367 ns | 165.7949 ns | 146.9729 ns | 6,095.918 ns | 0.74 | 0.04 |

## ReaderWriterBenchmark

| Method | Categories | N | Alloc Ratio | Mean | Error | StdDev | Median | Ratio | RatioSD | Allocated |
|---|---|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadDouble | ReadDouble | 1 | NA | 7.4424 ns | 0.3034 ns | 0.2838 ns | 7.4145 ns | 1.00 | 0.05 | 0 B |
| BinaryReader_ReadDouble | ReadDouble | 16 | NA | 111.7901 ns | 4.9384 ns | 4.3778 ns | 111.2171 ns | 1.00 | 0.05 | 0 B |
| BinaryReader_ReadDouble | ReadDouble | 128 | NA | 902.5572 ns | 18.8562 ns | 17.6381 ns | 900.9781 ns | 1.00 | 0.03 | 0 B |
| BinaryReader_ReadDouble | ReadDouble | 1024 | NA | 7,164.1547 ns | 342.1516 ns | 320.0489 ns | 7,168.4745 ns | 1.00 | 0.06 | 0 B |
| BinaryReader_ReadInt32 | ReadInt32 | 1 | NA | 8.4042 ns | 0.4154 ns | 0.3885 ns | 8.4103 ns | 1.00 | 0.06 | 0 B |
| BinaryReader_ReadInt32 | ReadInt32 | 16 | NA | 115.5546 ns | 7.3845 ns | 6.9075 ns | 114.4071 ns | 1.00 | 0.08 | 0 B |
| BinaryReader_ReadInt32 | ReadInt32 | 128 | NA | 866.6682 ns | 36.5231 ns | 34.1637 ns | 861.8836 ns | 1.00 | 0.05 | 0 B |
| BinaryReader_ReadInt32 | ReadInt32 | 1024 | NA | 7,047.1421 ns | 379.7430 ns | 355.2118 ns | 6,995.7215 ns | 1.00 | 0.07 | 0 B |
| BinaryReader_ReadInt64 | ReadInt64 | 1 | NA | 7.5760 ns | 0.6542 ns | 0.6119 ns | 7.5261 ns | 1.01 | 0.11 | 0 B |
| BinaryReader_ReadInt64 | ReadInt64 | 16 | NA | 107.5019 ns | 3.7099 ns | 3.4703 ns | 107.3206 ns | 1.00 | 0.04 | 0 B |
| BinaryReader_ReadInt64 | ReadInt64 | 128 | NA | 842.4601 ns | 26.2587 ns | 23.2776 ns | 843.1395 ns | 1.00 | 0.04 | 0 B |
| BinaryReader_ReadInt64 | ReadInt64 | 1024 | NA | 6,999.4649 ns | 506.3041 ns | 473.5972 ns | 6,868.5660 ns | 1.00 | 0.09 | 0 B |
| BinaryWriter_WriteDouble | WriteDouble | 1 | NA | 9.2842 ns | 0.3283 ns | 0.2910 ns | 9.3153 ns | 1.00 | 0.04 | 0 B |
| BinaryWriter_WriteDouble | WriteDouble | 16 | NA | 143.6698 ns | 6.2924 ns | 5.5780 ns | 143.0665 ns | 1.00 | 0.05 | 0 B |
| BinaryWriter_WriteDouble | WriteDouble | 128 | NA | 1,067.5163 ns | 49.2771 ns | 43.6828 ns | 1,074.4164 ns | 1.00 | 0.06 | 0 B |
| BinaryWriter_WriteDouble | WriteDouble | 1024 | NA | 8,347.1571 ns | 305.1186 ns | 270.4797 ns | 8,225.2837 ns | 1.00 | 0.04 | 0 B |
| BinaryWriter_WriteInt32 | WriteInt32 | 1 | NA | 9.7696 ns | 0.3854 ns | 0.3605 ns | 9.6293 ns | 1.00 | 0.05 | 0 B |
| BinaryWriter_WriteInt32 | WriteInt32 | 16 | NA | 142.0628 ns | 4.0440 ns | 3.7827 ns | 141.3875 ns | 1.00 | 0.04 | 0 B |
| BinaryWriter_WriteInt32 | WriteInt32 | 128 | NA | 1,103.2565 ns | 40.0310 ns | 37.4451 ns | 1,099.4667 ns | 1.00 | 0.05 | 0 B |
| BinaryWriter_WriteInt32 | WriteInt32 | 1024 | NA | 8,583.8511 ns | 362.7558 ns | 339.3220 ns | 8,477.1337 ns | 1.00 | 0.05 | 0 B |
| BinaryWriter_WriteInt64 | WriteInt64 | 1 | NA | 9.7551 ns | 0.4824 ns | 0.4513 ns | 9.6848 ns | 1.00 | 0.06 | 0 B |
| BinaryWriter_WriteInt64 | WriteInt64 | 16 | NA | 140.7996 ns | 4.4428 ns | 4.1558 ns | 142.6377 ns | 1.00 | 0.04 | 0 B |
| BinaryWriter_WriteInt64 | WriteInt64 | 128 | NA | 1,081.1776 ns | 40.3838 ns | 35.7992 ns | 1,086.8018 ns | 1.00 | 0.05 | 0 B |
| BinaryWriter_WriteInt64 | WriteInt64 | 1024 | NA | 8,277.9524 ns | 226.0816 ns | 200.4154 ns | 8,276.6158 ns | 1.00 | 0.03 | 0 B |
| Buffer_ReadDouble | ReadDouble | 1 | NA | 1.6786 ns | 0.2115 ns | 0.1766 ns | 1.7128 ns | 0.23 | 0.02 | 0 B |
| Buffer_ReadDouble | ReadDouble | 16 | NA | 26.9785 ns | 1.2101 ns | 1.0727 ns | 26.9321 ns | 0.24 | 0.01 | 0 B |
| Buffer_ReadDouble | ReadDouble | 128 | NA | 203.8670 ns | 8.3211 ns | 7.7835 ns | 201.9828 ns | 0.23 | 0.01 | 0 B |
| Buffer_ReadDouble | ReadDouble | 1024 | NA | 1,515.3062 ns | 47.9452 ns | 44.8480 ns | 1,523.2181 ns | 0.21 | 0.01 | 0 B |
| Buffer_ReadInt32 | ReadInt32 | 1 | NA | 2.1650 ns | 0.1543 ns | 0.1368 ns | 2.1647 ns | 0.26 | 0.02 | 0 B |
| Buffer_ReadInt32 | ReadInt32 | 16 | NA | 20.6745 ns | 0.6538 ns | 0.5796 ns | 20.6591 ns | 0.18 | 0.01 | 0 B |
| Buffer_ReadInt32 | ReadInt32 | 128 | NA | 174.1145 ns | 6.1542 ns | 5.7567 ns | 173.5230 ns | 0.20 | 0.01 | 0 B |
| Buffer_ReadInt32 | ReadInt32 | 1024 | NA | 1,289.5318 ns | 33.4630 ns | 31.3013 ns | 1,296.7348 ns | 0.18 | 0.01 | 0 B |
| Buffer_ReadInt64 | ReadInt64 | 1 | NA | 2.0861 ns | 0.1644 ns | 0.1538 ns | 2.0186 ns | 0.28 | 0.03 | 0 B |
| Buffer_ReadInt64 | ReadInt64 | 16 | NA | 21.6423 ns | 1.5808 ns | 1.4014 ns | 21.7814 ns | 0.20 | 0.01 | 0 B |
| Buffer_ReadInt64 | ReadInt64 | 128 | NA | 185.7800 ns | 13.4692 ns | 12.5991 ns | 185.0247 ns | 0.22 | 0.02 | 0 B |
| Buffer_ReadInt64 | ReadInt64 | 1024 | NA | 1,661.3237 ns | 81.3108 ns | 76.0582 ns | 1,652.6425 ns | 0.24 | 0.02 | 0 B |
| Buffer_WriteDouble | WriteDouble | 1 | NA | 2.0162 ns | 0.1043 ns | 0.0925 ns | 1.9987 ns | 0.22 | 0.01 | 0 B |
| Buffer_WriteDouble | WriteDouble | 16 | NA | 27.0342 ns | 1.5016 ns | 1.1723 ns | 26.9737 ns | 0.19 | 0.01 | 0 B |
| Buffer_WriteDouble | WriteDouble | 128 | NA | 248.8773 ns | 6.6070 ns | 5.5171 ns | 247.3652 ns | 0.23 | 0.01 | 0 B |
| Buffer_WriteDouble | WriteDouble | 1024 | NA | 1,676.2280 ns | 78.0619 ns | 73.0191 ns | 1,651.2071 ns | 0.20 | 0.01 | 0 B |
| Buffer_WriteInt32 | WriteInt32 | 1 | NA | 2.4028 ns | 0.2325 ns | 0.2061 ns | 2.4345 ns | 0.25 | 0.02 | 0 B |
| Buffer_WriteInt32 | WriteInt32 | 16 | NA | 24.6803 ns | 1.6762 ns | 1.4859 ns | 24.6718 ns | 0.17 | 0.01 | 0 B |
| Buffer_WriteInt32 | WriteInt32 | 128 | NA | 195.5617 ns | 17.9206 ns | 16.7630 ns | 187.8306 ns | 0.18 | 0.02 | 0 B |
| Buffer_WriteInt32 | WriteInt32 | 1024 | NA | 1,648.4714 ns | 40.8746 ns | 34.1321 ns | 1,644.3748 ns | 0.19 | 0.01 | 0 B |
| Buffer_WriteInt64 | WriteInt64 | 1 | NA | 2.4561 ns | 0.2712 ns | 0.2404 ns | 2.4120 ns | 0.25 | 0.03 | 0 B |
| Buffer_WriteInt64 | WriteInt64 | 16 | NA | 26.6985 ns | 2.1378 ns | 1.8951 ns | 25.9303 ns | 0.19 | 0.01 | 0 B |
| Buffer_WriteInt64 | WriteInt64 | 128 | NA | 220.0811 ns | 10.5932 ns | 9.9089 ns | 215.1351 ns | 0.20 | 0.01 | 0 B |
| Buffer_WriteInt64 | WriteInt64 | 1024 | NA | 1,824.0379 ns | 135.4730 ns | 120.0933 ns | 1,806.2878 ns | 0.22 | 0.01 | 0 B |
| Reader_ReadDouble | ReadDouble | 1 | NA | 1.6249 ns | 0.1568 ns | 0.1466 ns | 1.6215 ns | 0.22 | 0.02 | 0 B |
| Reader_ReadDouble | ReadDouble | 16 | NA | 26.7943 ns | 0.8531 ns | 0.7980 ns | 26.4340 ns | 0.24 | 0.01 | 0 B |
| Reader_ReadDouble | ReadDouble | 128 | NA | 220.4844 ns | 5.0010 ns | 4.6779 ns | 220.1188 ns | 0.24 | 0.01 | 0 B |
| Reader_ReadDouble | ReadDouble | 1024 | NA | 1,408.9502 ns | 73.7325 ns | 68.9694 ns | 1,414.5581 ns | 0.20 | 0.01 | 0 B |
| Reader_ReadInt32 | ReadInt32 | 1 | NA | 1.5080 ns | 0.2501 ns | 0.2340 ns | 1.4845 ns | 0.18 | 0.03 | 0 B |
| Reader_ReadInt32 | ReadInt32 | 16 | NA | 19.7402 ns | 0.5955 ns | 0.5570 ns | 19.7174 ns | 0.17 | 0.01 | 0 B |
| Reader_ReadInt32 | ReadInt32 | 128 | NA | 226.9399 ns | 7.2932 ns | 6.4652 ns | 225.4611 ns | 0.26 | 0.01 | 0 B |
| Reader_ReadInt32 | ReadInt32 | 1024 | NA | 1,209.3736 ns | 43.4414 ns | 38.5096 ns | 1,206.8611 ns | 0.17 | 0.01 | 0 B |
| Reader_ReadInt64 | ReadInt64 | 1 | NA | 1.4217 ns | 0.1603 ns | 0.1252 ns | 1.4389 ns | 0.19 | 0.02 | 0 B |
| Reader_ReadInt64 | ReadInt64 | 16 | NA | 20.7908 ns | 1.2260 ns | 1.0868 ns | 20.7133 ns | 0.19 | 0.01 | 0 B |
| Reader_ReadInt64 | ReadInt64 | 128 | NA | 240.1224 ns | 9.1227 ns | 8.5334 ns | 240.6180 ns | 0.29 | 0.01 | 0 B |
| Reader_ReadInt64 | ReadInt64 | 1024 | NA | 1,632.0386 ns | 82.6390 ns | 77.3006 ns | 1,600.6180 ns | 0.23 | 0.02 | 0 B |
| RefReader_ReadDouble | ReadDouble | 1 | NA | 0.6130 ns | 0.1297 ns | 0.1214 ns | 0.6050 ns | 0.08 | 0.02 | 0 B |
| RefReader_ReadDouble | ReadDouble | 16 | NA | 15.3451 ns | 0.5931 ns | 0.5547 ns | 15.1881 ns | 0.14 | 0.01 | 0 B |
| RefReader_ReadDouble | ReadDouble | 128 | NA | 130.9520 ns | 10.6095 ns | 9.9242 ns | 130.3549 ns | 0.15 | 0.01 | 0 B |
| RefReader_ReadDouble | ReadDouble | 1024 | NA | 978.5540 ns | 46.7923 ns | 36.5323 ns | 987.3859 ns | 0.14 | 0.01 | 0 B |
| RefReader_ReadInt32 | ReadInt32 | 1 | NA | 0.8509 ns | 0.2525 ns | 0.2362 ns | 0.8349 ns | 0.10 | 0.03 | 0 B |
| RefReader_ReadInt32 | ReadInt32 | 16 | NA | 15.2681 ns | 0.7446 ns | 0.6965 ns | 15.2246 ns | 0.13 | 0.01 | 0 B |
| RefReader_ReadInt32 | ReadInt32 | 128 | NA | 114.7938 ns | 5.5001 ns | 4.5929 ns | 115.0265 ns | 0.13 | 0.01 | 0 B |
| RefReader_ReadInt32 | ReadInt32 | 1024 | NA | 975.2650 ns | 30.7094 ns | 27.2231 ns | 970.5925 ns | 0.14 | 0.01 | 0 B |
| RefReader_ReadInt64 | ReadInt64 | 1 | NA | 0.7771 ns | 0.1068 ns | 0.0892 ns | 0.7805 ns | 0.10 | 0.01 | 0 B |
| RefReader_ReadInt64 | ReadInt64 | 16 | NA | 14.9566 ns | 0.4859 ns | 0.4307 ns | 15.1042 ns | 0.14 | 0.01 | 0 B |
| RefReader_ReadInt64 | ReadInt64 | 128 | NA | 135.7471 ns | 8.9096 ns | 8.3340 ns | 139.2862 ns | 0.16 | 0.01 | 0 B |
| RefReader_ReadInt64 | ReadInt64 | 1024 | NA | 901.2730 ns | 60.7482 ns | 56.8239 ns | 896.6972 ns | 0.13 | 0.01 | 0 B |
| Writer_WriteDouble | WriteDouble | 1 | NA | 2.0516 ns | 0.1737 ns | 0.1540 ns | 2.0278 ns | 0.22 | 0.02 | 0 B |
| Writer_WriteDouble | WriteDouble | 16 | NA | 26.0700 ns | 1.3395 ns | 1.2530 ns | 25.6625 ns | 0.18 | 0.01 | 0 B |
| Writer_WriteDouble | WriteDouble | 128 | NA | 213.8165 ns | 6.2582 ns | 5.2259 ns | 213.2844 ns | 0.20 | 0.01 | 0 B |
| Writer_WriteDouble | WriteDouble | 1024 | NA | 1,774.8751 ns | 74.0790 ns | 69.2936 ns | 1,757.0859 ns | 0.21 | 0.01 | 0 B |
| Writer_WriteInt32 | WriteInt32 | 1 | NA | 2.2387 ns | 0.2032 ns | 0.1801 ns | 2.3193 ns | 0.23 | 0.02 | 0 B |
| Writer_WriteInt32 | WriteInt32 | 16 | NA | 27.4843 ns | 1.6757 ns | 1.5674 ns | 27.7948 ns | 0.19 | 0.01 | 0 B |
| Writer_WriteInt32 | WriteInt32 | 128 | NA | 208.3351 ns | 19.2565 ns | 18.0126 ns | 204.6328 ns | 0.19 | 0.02 | 0 B |
| Writer_WriteInt32 | WriteInt32 | 1024 | NA | 1,544.3534 ns | 99.0225 ns | 92.6257 ns | 1,552.1107 ns | 0.18 | 0.01 | 0 B |
| Writer_WriteInt64 | WriteInt64 | 1 | NA | 1.9423 ns | 0.1081 ns | 0.0903 ns | 1.9478 ns | 0.20 | 0.01 | 0 B |
| Writer_WriteInt64 | WriteInt64 | 16 | NA | 27.5631 ns | 2.8620 ns | 2.6771 ns | 26.7439 ns | 0.20 | 0.02 | 0 B |
| Writer_WriteInt64 | WriteInt64 | 128 | NA | 222.9592 ns | 12.6819 ns | 11.8627 ns | 221.1653 ns | 0.21 | 0.01 | 0 B |
| Writer_WriteInt64 | WriteInt64 | 1024 | NA | 1,736.2877 ns | 78.7307 ns | 73.6448 ns | 1,720.5686 ns | 0.21 | 0.01 | 0 B |

## SpanWriteBenchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| Buffer_ReadInt32 | SpanReadInt32 | 1 | 0.9199 ns | 0.1118 ns | 0.0934 ns | 0.8913 ns | 1.01 | 0.14 |
| Buffer_ReadInt32 | SpanReadInt32 | 16 | 19.0553 ns | 1.1773 ns | 1.1012 ns | 18.5863 ns | 1.00 | 0.08 |
| Buffer_ReadInt32 | SpanReadInt32 | 128 | 172.3642 ns | 8.5260 ns | 7.9753 ns | 173.0493 ns | 1.00 | 0.06 |
| Buffer_ReadInt32 | SpanReadInt32 | 1024 | 1,211.7250 ns | 50.6119 ns | 42.2632 ns | 1,191.8167 ns | 1.00 | 0.05 |
| Buffer_ReadVarInt32 | SpanReadVarInt32 | 1 | 3.7694 ns | 0.2392 ns | 0.2238 ns | 3.7316 ns | 1.00 | 0.08 |
| Buffer_ReadVarInt32 | SpanReadVarInt32 | 16 | 63.7429 ns | 5.9704 ns | 5.5847 ns | 62.3939 ns | 1.01 | 0.12 |
| Buffer_ReadVarInt32 | SpanReadVarInt32 | 128 | 442.7827 ns | 22.7133 ns | 21.2461 ns | 447.4247 ns | 1.00 | 0.07 |
| Buffer_ReadVarInt32 | SpanReadVarInt32 | 1024 | 3,566.7833 ns | 107.6447 ns | 100.6909 ns | 3,558.3143 ns | 1.00 | 0.04 |
| Buffer_WriteFloat | SpanWriteFloat | 1 | 2.3402 ns | 0.1228 ns | 0.1148 ns | 2.3407 ns | 1.002 | 0.07 |
| Buffer_WriteFloat | SpanWriteFloat | 16 | 24.9411 ns | 1.1859 ns | 1.0513 ns | 24.9091 ns | 1.00 | 0.06 |
| Buffer_WriteFloat | SpanWriteFloat | 128 | 197.7686 ns | 10.2602 ns | 9.5974 ns | 195.4913 ns | 1.00 | 0.07 |
| Buffer_WriteFloat | SpanWriteFloat | 1024 | 1,447.7469 ns | 76.6902 ns | 64.0398 ns | 1,444.3311 ns | 1.00 | 0.06 |
| Buffer_WriteInt32 | SpanWriteInt32 | 1 | 2.3685 ns | 0.2957 ns | 0.2766 ns | 2.3328 ns | 1.01 | 0.16 |
| Buffer_WriteInt32 | SpanWriteInt32 | 16 | 24.4415 ns | 1.1728 ns | 1.0970 ns | 24.2423 ns | 1.00 | 0.06 |
| Buffer_WriteInt32 | SpanWriteInt32 | 128 | 190.6074 ns | 9.1901 ns | 8.1468 ns | 191.9297 ns | 1.00 | 0.06 |
| Buffer_WriteInt32 | SpanWriteInt32 | 1024 | 1,461.5239 ns | 50.9184 ns | 42.5191 ns | 1,471.7857 ns | 1.00 | 0.04 |
| Buffer_WriteUtf8 | SpanWriteUtf8 | 1 | 21.0172 ns | 0.6505 ns | 0.6085 ns | 20.9266 ns | 1.00 | 0.04 |
| Buffer_WriteUtf8 | SpanWriteUtf8 | 16 | 366.6055 ns | 55.5187 ns | 51.9323 ns | 335.1391 ns | 1.02 | 0.19 |
| Buffer_WriteUtf8 | SpanWriteUtf8 | 128 | 2,757.0362 ns | 132.9598 ns | 117.8654 ns | 2,728.3862 ns | 1.00 | 0.06 |
| Buffer_WriteUtf8 | SpanWriteUtf8 | 1024 | 21,600.5791 ns | 1,190.5408 ns | 1,055.3834 ns | 21,614.2275 ns | 1.00 | 0.07 |
| Buffer_WriteVarInt32 | SpanWriteVarInt32 | 1 | 3.6225 ns | 0.2470 ns | 0.2189 ns | 3.5865 ns | 1.00 | 0.08 |
| Buffer_WriteVarInt32 | SpanWriteVarInt32 | 16 | 63.1921 ns | 2.8075 ns | 2.6262 ns | 64.0870 ns | 1.00 | 0.06 |
| Buffer_WriteVarInt32 | SpanWriteVarInt32 | 128 | 488.3594 ns | 26.5256 ns | 23.5142 ns | 482.7284 ns | 1.00 | 0.06 |
| Buffer_WriteVarInt32 | SpanWriteVarInt32 | 1024 | 3,795.1234 ns | 183.7090 ns | 171.8415 ns | 3,852.7114 ns | 1.00 | 0.06 |
| Span_ReadInt32 | SpanReadInt32 | 1 | 0.4384 ns | 0.2242 ns | 0.1987 ns | 0.4255 ns | 0.48 | 0.22 |
| Span_ReadInt32 | SpanReadInt32 | 16 | 16.4958 ns | 1.2535 ns | 1.1725 ns | 16.3619 ns | 0.87 | 0.08 |
| Span_ReadInt32 | SpanReadInt32 | 128 | 88.1607 ns | 3.3337 ns | 2.7838 ns | 88.6862 ns | 0.51 | 0.03 |
| Span_ReadInt32 | SpanReadInt32 | 1024 | 1,016.5630 ns | 30.6546 ns | 28.6743 ns | 1,019.4604 ns | 0.84 | 0.04 |
| Span_ReadVarInt32 | SpanReadVarInt32 | 1 | 2.8127 ns | 0.1521 ns | 0.1188 ns | 2.8243 ns | 0.75 | 0.05 |
| Span_ReadVarInt32 | SpanReadVarInt32 | 16 | 56.6344 ns | 2.2015 ns | 2.0593 ns | 56.4371 ns | 0.89 | 0.08 |
| Span_ReadVarInt32 | SpanReadVarInt32 | 128 | 404.6684 ns | 19.3245 ns | 18.0761 ns | 400.4491 ns | 0.92 | 0.06 |
| Span_ReadVarInt32 | SpanReadVarInt32 | 1024 | 3,141.1892 ns | 114.3498 ns | 89.2768 ns | 3,138.8805 ns | 0.88 | 0.03 |
| Span_WriteFloat | SpanWriteFloat | 1 | 0.0047 ns | 0.0135 ns | 0.0127 ns | 0.0000 ns | 0.002 | 0.01 |
| Span_WriteFloat | SpanWriteFloat | 16 | 4.9854 ns | 0.2421 ns | 0.2146 ns | 4.9782 ns | 0.20 | 0.01 |
| Span_WriteFloat | SpanWriteFloat | 128 | 57.1535 ns | 2.4382 ns | 2.1614 ns | 57.3929 ns | 0.29 | 0.02 |
| Span_WriteFloat | SpanWriteFloat | 1024 | 486.2351 ns | 20.6882 ns | 19.3518 ns | 488.7604 ns | 0.34 | 0.02 |
| Span_WriteInt32 | SpanWriteInt32 | 1 | 0.0493 ns | 0.0739 ns | 0.0692 ns | 0.0000 ns | 0.02 | 0.03 |
| Span_WriteInt32 | SpanWriteInt32 | 16 | 5.0876 ns | 0.3063 ns | 0.2865 ns | 5.0063 ns | 0.21 | 0.01 |
| Span_WriteInt32 | SpanWriteInt32 | 128 | 58.4344 ns | 3.0711 ns | 2.8727 ns | 58.2591 ns | 0.31 | 0.02 |
| Span_WriteInt32 | SpanWriteInt32 | 1024 | 465.2187 ns | 13.5368 ns | 11.3038 ns | 462.4682 ns | 0.32 | 0.01 |
| Span_WriteUtf8 | SpanWriteUtf8 | 1 | 18.6311 ns | 0.6835 ns | 0.6393 ns | 18.5602 ns | 0.89 | 0.04 |
| Span_WriteUtf8 | SpanWriteUtf8 | 16 | 322.8569 ns | 12.4245 ns | 10.3750 ns | 320.2356 ns | 0.90 | 0.12 |
| Span_WriteUtf8 | SpanWriteUtf8 | 128 | 2,479.9664 ns | 150.3604 ns | 133.2906 ns | 2,435.6570 ns | 0.90 | 0.06 |
| Span_WriteUtf8 | SpanWriteUtf8 | 1024 | 19,789.8201 ns | 665.9004 ns | 519.8912 ns | 19,762.3044 ns | 0.92 | 0.05 |
| Span_WriteVarInt32 | SpanWriteVarInt32 | 1 | 0.2203 ns | 0.1058 ns | 0.0990 ns | 0.2062 ns | 0.06 | 0.03 |
| Span_WriteVarInt32 | SpanWriteVarInt32 | 16 | 12.0027 ns | 0.5323 ns | 0.4979 ns | 11.9978 ns | 0.19 | 0.01 |
| Span_WriteVarInt32 | SpanWriteVarInt32 | 128 | 109.1838 ns | 9.7024 ns | 8.6009 ns | 106.8911 ns | 0.22 | 0.02 |
| Span_WriteVarInt32 | SpanWriteVarInt32 | 1024 | 811.9993 ns | 18.5943 ns | 17.3932 ns | 814.7567 ns | 0.21 | 0.01 |

## UInt16Benchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadUInt16 | Read | 1 | 7.9791 ns | 0.4542 ns | 0.4248 ns | 8.1552 ns | 1.00 | 0.07 |
| BinaryReader_ReadUInt16 | Read | 16 | 113.7833 ns | 3.5030 ns | 3.2767 ns | 113.3963 ns | 1.00 | 0.04 |
| BinaryReader_ReadUInt16 | Read | 128 | 954.0332 ns | 42.4943 ns | 37.6701 ns | 957.7823 ns | 1.00 | 0.05 |
| BinaryReader_ReadUInt16 | Read | 1024 | 7,179.2196 ns | 335.4420 ns | 313.7727 ns | 7,056.5586 ns | 1.00 | 0.06 |
| BinaryWriter_WriteUInt16 | Write | 1 | 10.9475 ns | 0.4882 ns | 0.4328 ns | 10.9349 ns | 1.00 | 0.05 |
| BinaryWriter_WriteUInt16 | Write | 16 | 146.4104 ns | 4.9280 ns | 4.6097 ns | 146.1384 ns | 1.00 | 0.04 |
| BinaryWriter_WriteUInt16 | Write | 128 | 1,069.2850 ns | 30.6857 ns | 27.2021 ns | 1,062.8443 ns | 1.00 | 0.03 |
| BinaryWriter_WriteUInt16 | Write | 1024 | 9,006.8598 ns | 252.0895 ns | 235.8047 ns | 9,104.5885 ns | 1.00 | 0.04 |
| Buffer_ReadUInt16 | Read | 1 | 1.9136 ns | 0.2157 ns | 0.2018 ns | 1.9384 ns | 0.24 | 0.03 |
| Buffer_ReadUInt16 | Read | 16 | 21.8280 ns | 0.9680 ns | 0.8581 ns | 21.5584 ns | 0.19 | 0.01 |
| Buffer_ReadUInt16 | Read | 128 | 199.1575 ns | 7.2523 ns | 6.7838 ns | 199.8773 ns | 0.21 | 0.01 |
| Buffer_ReadUInt16 | Read | 1024 | 1,399.9898 ns | 38.7968 ns | 32.3971 ns | 1,398.2702 ns | 0.20 | 0.01 |
| Buffer_WriteUInt16 | Write | 1 | 2.2450 ns | 0.1753 ns | 0.1639 ns | 2.2003 ns | 0.21 | 0.02 |
| Buffer_WriteUInt16 | Write | 16 | 24.7683 ns | 1.0661 ns | 0.9451 ns | 24.5020 ns | 0.17 | 0.01 |
| Buffer_WriteUInt16 | Write | 128 | 193.3065 ns | 10.3455 ns | 9.6772 ns | 193.4632 ns | 0.18 | 0.01 |
| Buffer_WriteUInt16 | Write | 1024 | 1,525.7077 ns | 106.4156 ns | 99.5412 ns | 1,499.1453 ns | 0.17 | 0.01 |
| ByteArray_ReadUInt16 | Read | 1 | 0.3668 ns | 0.1173 ns | 0.1040 ns | 0.3589 ns | 0.05 | 0.01 |
| ByteArray_ReadUInt16 | Read | 16 | 18.5509 ns | 1.2867 ns | 1.2035 ns | 18.5175 ns | 0.16 | 0.01 |
| ByteArray_ReadUInt16 | Read | 128 | 136.3418 ns | 4.9532 ns | 4.6333 ns | 134.7153 ns | 0.14 | 0.01 |
| ByteArray_ReadUInt16 | Read | 1024 | 1,116.1782 ns | 34.7418 ns | 32.4975 ns | 1,107.3060 ns | 0.16 | 0.01 |
| ByteArray_WriteUInt16 | Write | 1 | 2.1509 ns | 0.1912 ns | 0.1788 ns | 2.1704 ns | 0.20 | 0.02 |
| ByteArray_WriteUInt16 | Write | 16 | 27.2968 ns | 0.8612 ns | 0.7191 ns | 27.0032 ns | 0.19 | 0.01 |
| ByteArray_WriteUInt16 | Write | 128 | 198.9618 ns | 7.8794 ns | 6.9849 ns | 195.6808 ns | 0.19 | 0.01 |
| ByteArray_WriteUInt16 | Write | 1024 | 1,616.6739 ns | 28.7634 ns | 24.0187 ns | 1,621.4180 ns | 0.18 | 0.01 |

## UInt32Benchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadUInt32 | Read | 1 | 7.633 ns | 0.4306 ns | 0.4028 ns | 7.766 ns | 1.00 | 0.07 |
| BinaryReader_ReadUInt32 | Read | 16 | 112.775 ns | 11.1564 ns | 10.4357 ns | 112.724 ns | 1.01 | 0.13 |
| BinaryReader_ReadUInt32 | Read | 128 | 864.708 ns | 31.5698 ns | 27.9858 ns | 859.908 ns | 1.00 | 0.04 |
| BinaryReader_ReadUInt32 | Read | 1024 | 6,441.400 ns | 216.5578 ns | 202.5683 ns | 6,395.271 ns | 1.00 | 0.04 |
| BinaryWriter_WriteUInt32 | Write | 1 | 9.429 ns | 0.4628 ns | 0.4103 ns | 9.254 ns | 1.00 | 0.06 |
| BinaryWriter_WriteUInt32 | Write | 16 | 126.368 ns | 3.5353 ns | 3.3069 ns | 126.580 ns | 1.00 | 0.04 |
| BinaryWriter_WriteUInt32 | Write | 128 | 964.015 ns | 25.3734 ns | 22.4929 ns | 961.963 ns | 1.00 | 0.03 |
| BinaryWriter_WriteUInt32 | Write | 1024 | 7,901.817 ns | 274.9842 ns | 257.2204 ns | 7,866.520 ns | 1.00 | 0.04 |
| Buffer_ReadUInt32 | Read | 1 | 1.410 ns | 0.0909 ns | 0.0850 ns | 1.384 ns | 0.19 | 0.01 |
| Buffer_ReadUInt32 | Read | 16 | 19.705 ns | 0.5566 ns | 0.5206 ns | 19.515 ns | 0.18 | 0.02 |
| Buffer_ReadUInt32 | Read | 128 | 218.419 ns | 17.1967 ns | 15.2444 ns | 214.075 ns | 0.25 | 0.02 |
| Buffer_ReadUInt32 | Read | 1024 | 1,694.567 ns | 98.4184 ns | 87.2454 ns | 1,692.427 ns | 0.26 | 0.02 |
| Buffer_WriteUInt32 | Write | 1 | 2.071 ns | 0.0849 ns | 0.0709 ns | 2.050 ns | 0.22 | 0.01 |
| Buffer_WriteUInt32 | Write | 16 | 23.201 ns | 0.8111 ns | 0.6773 ns | 23.260 ns | 0.18 | 0.01 |
| Buffer_WriteUInt32 | Write | 128 | 183.179 ns | 8.4749 ns | 7.9274 ns | 183.964 ns | 0.19 | 0.01 |
| Buffer_WriteUInt32 | Write | 1024 | 1,358.025 ns | 42.9571 ns | 40.1821 ns | 1,354.436 ns | 0.17 | 0.01 |
| ByteArray_ReadUInt32 | Read | 1 | 1.811 ns | 0.1749 ns | 0.1551 ns | 1.741 ns | 0.24 | 0.02 |
| ByteArray_ReadUInt32 | Read | 16 | 31.207 ns | 1.1503 ns | 0.8981 ns | 31.292 ns | 0.28 | 0.03 |
| ByteArray_ReadUInt32 | Read | 128 | 267.464 ns | 10.1157 ns | 8.9673 ns | 266.217 ns | 0.31 | 0.01 |
| ByteArray_ReadUInt32 | Read | 1024 | 2,150.948 ns | 169.3478 ns | 158.4081 ns | 2,122.437 ns | 0.33 | 0.03 |
| ByteArray_WriteUInt32 | Write | 1 | 3.852 ns | 0.5002 ns | 0.4679 ns | 3.732 ns | 0.41 | 0.05 |
| ByteArray_WriteUInt32 | Write | 16 | 61.877 ns | 3.3199 ns | 3.1054 ns | 62.180 ns | 0.49 | 0.03 |
| ByteArray_WriteUInt32 | Write | 128 | 432.126 ns | 9.8380 ns | 8.2152 ns | 432.048 ns | 0.45 | 0.01 |
| ByteArray_WriteUInt32 | Write | 1024 | 3,638.679 ns | 156.5420 ns | 146.4295 ns | 3,644.970 ns | 0.46 | 0.02 |

## UInt64Benchmark

| Method | Categories | N | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|---|
| BinaryReader_ReadUInt64 | Read | 1 | 7.508 ns | 0.3181 ns | 0.2975 ns | 7.436 ns | 1.00 | 0.05 |
| BinaryReader_ReadUInt64 | Read | 16 | 120.205 ns | 4.8695 ns | 4.5550 ns | 118.552 ns | 1.00 | 0.05 |
| BinaryReader_ReadUInt64 | Read | 128 | 940.547 ns | 50.7800 ns | 47.4996 ns | 930.896 ns | 1.00 | 0.07 |
| BinaryReader_ReadUInt64 | Read | 1024 | 7,323.933 ns | 392.6548 ns | 367.2895 ns | 7,412.315 ns | 1.00 | 0.07 |
| BinaryWriter_WriteUInt64 | Write | 1 | 9.933 ns | 0.5293 ns | 0.4693 ns | 9.788 ns | 1.00 | 0.06 |
| BinaryWriter_WriteUInt64 | Write | 16 | 152.250 ns | 3.8615 ns | 3.6121 ns | 152.492 ns | 1.00 | 0.03 |
| BinaryWriter_WriteUInt64 | Write | 128 | 1,137.658 ns | 83.5957 ns | 78.1955 ns | 1,138.321 ns | 1.00 | 0.09 |
| BinaryWriter_WriteUInt64 | Write | 1024 | 8,170.215 ns | 206.2643 ns | 192.9398 ns | 8,139.112 ns | 1.00 | 0.03 |
| Buffer_ReadUInt64 | Read | 1 | 1.820 ns | 0.2313 ns | 0.2163 ns | 1.813 ns | 0.24 | 0.03 |
| Buffer_ReadUInt64 | Read | 16 | 22.048 ns | 1.6343 ns | 1.4487 ns | 22.008 ns | 0.18 | 0.01 |
| Buffer_ReadUInt64 | Read | 128 | 227.721 ns | 24.4706 ns | 22.8898 ns | 222.992 ns | 0.24 | 0.03 |
| Buffer_ReadUInt64 | Read | 1024 | 1,666.093 ns | 87.8827 ns | 82.2056 ns | 1,650.918 ns | 0.23 | 0.02 |
| Buffer_WriteUInt64 | Write | 1 | 2.451 ns | 0.2080 ns | 0.1843 ns | 2.424 ns | 0.25 | 0.02 |
| Buffer_WriteUInt64 | Write | 16 | 28.023 ns | 3.3411 ns | 3.1252 ns | 27.424 ns | 0.18 | 0.02 |
| Buffer_WriteUInt64 | Write | 128 | 217.425 ns | 10.4753 ns | 9.2861 ns | 216.143 ns | 0.19 | 0.01 |
| Buffer_WriteUInt64 | Write | 1024 | 1,820.390 ns | 71.6402 ns | 63.5072 ns | 1,810.835 ns | 0.22 | 0.01 |
| ByteArray_ReadUInt64 | Read | 1 | 6.054 ns | 0.2873 ns | 0.2688 ns | 6.152 ns | 0.81 | 0.05 |
| ByteArray_ReadUInt64 | Read | 16 | 98.299 ns | 3.8131 ns | 3.1841 ns | 98.063 ns | 0.82 | 0.04 |
| ByteArray_ReadUInt64 | Read | 128 | 786.526 ns | 49.4982 ns | 43.8789 ns | 775.481 ns | 0.84 | 0.06 |
| ByteArray_ReadUInt64 | Read | 1024 | 5,857.927 ns | 200.5551 ns | 177.7869 ns | 5,872.330 ns | 0.80 | 0.05 |
| ByteArray_WriteUInt64 | Write | 1 | 5.405 ns | 0.2299 ns | 0.2150 ns | 5.287 ns | 0.55 | 0.03 |
| ByteArray_WriteUInt64 | Write | 16 | 111.337 ns | 9.8629 ns | 9.2257 ns | 109.555 ns | 0.73 | 0.06 |
| ByteArray_WriteUInt64 | Write | 128 | 719.565 ns | 14.1451 ns | 12.5392 ns | 716.864 ns | 0.64 | 0.04 |
| ByteArray_WriteUInt64 | Write | 1024 | 6,044.260 ns | 223.1406 ns | 208.7259 ns | 6,028.446 ns | 0.74 | 0.03 |

## UTF8Benchmark

| Method | Categories | Alloc Ratio | Mean | Error | StdDev | Ratio | RatioSD | Allocated |
|---|---|---|---|---|---|---|---|---|
| Buffer_ReadUtf8 | Read | 1.00 | 59.19 ns | 14.780 ns | 9.776 ns | 1.60 | 0.27 | 97 B |
| Buffer_WriteUtf8 | Write | NA | 42.53 ns | 1.778 ns | 1.176 ns | 1.22 | 0.06 | 0 B |
| ByteArray_ReadUTF8 | Read | 1.00 | 37.16 ns | 3.388 ns | 2.016 ns | 1.00 | 0.07 | 97 B |
| ByteArray_WriteUTF8 | Write | NA | 34.94 ns | 2.487 ns | 1.645 ns | 1.00 | 0.06 | 0 B |

## VarInt32Benchmark

| Method | Categories | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|
| Buffer_ReadVarInt32 | Read | 3.750 ns | 0.5713 ns | 0.5064 ns | 3.664 ns | 1.00 | 0.14 |
| Buffer_ReadVarUInt32 | Read | 3.691 ns | 0.2046 ns | 0.1814 ns | 3.769 ns | 0.98 | 0.08 |
| Buffer_WriteVarInt32 | Write | 5.865 ns | 0.2589 ns | 0.2422 ns | 5.866 ns | 0.78 | 0.09 |
| Buffer_WriteVarUInt32 | Write | 6.024 ns | 0.4223 ns | 0.3526 ns | 5.953 ns | 0.80 | 0.10 |
| ByteArray_ReadULEB128 | Read | 3.764 ns | 0.2589 ns | 0.2422 ns | 3.766 ns | 1.00 | 0.09 |
| ByteArray_WriteULEB128 | Write | 7.661 ns | 1.0093 ns | 0.9441 ns | 7.610 ns | 1.01 | 0.17 |

## VarInt64Benchmark

| Method | Categories | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|---|
| Buffer_ReadVarInt64 | Read | 4.793 ns | 0.2214 ns | 0.2071 ns | 4.839 ns | 0.68 | 0.04 |
| Buffer_ReadVarUInt64 | Read | 4.959 ns | 0.2749 ns | 0.2571 ns | 4.858 ns | 0.70 | 0.04 |
| Buffer_WriteVarInt64 | Write | 6.249 ns | 0.3182 ns | 0.2977 ns | 6.208 ns | 0.38 | 0.02 |
| Buffer_WriteVarUInt64 | Write | 6.748 ns | 0.2020 ns | 0.1687 ns | 6.755 ns | 0.41 | 0.02 |
| ByteArray_ReadULEB128 | Read | 7.057 ns | 0.3518 ns | 0.2747 ns | 6.995 ns | 1.00 | 0.05 |
| ByteArray_WriteULEB128 | Write | 16.677 ns | 0.7274 ns | 0.6804 ns | 16.632 ns | 1.00 | 0.06 |

## VarIntSizeBenchmark

| Method | Mean | Error | StdDev | Median | Ratio | RatioSD |
|---|---|---|---|---|---|---|
| Buffer_GetVarInt32Size | 2.297 ms | 0.0646 ms | 0.0605 ms | 2.299 ms | 0.69 | 0.03 |
| Buffer_GetVarInt64Size | 1.462 ms | 0.0873 ms | 0.0817 ms | 1.428 ms | 0.44 | 0.03 |
| Buffer_GetVarUInt32Size | 1.672 ms | 0.1181 ms | 0.1104 ms | 1.685 ms | 0.50 | 0.04 |
| Buffer_GetVarUInt64Size | 2.762 ms | 0.1013 ms | 0.0846 ms | 2.763 ms | 0.83 | 0.04 |
| Manual_GetVarInt32Size | 4.448 ms | 0.2212 ms | 0.1961 ms | 4.450 ms | 1.34 | 0.07 |
| Manual_GetVarInt64Size | 2.681 ms | 0.1222 ms | 0.1143 ms | 2.644 ms | 0.81 | 0.04 |
| Manual_GetVarUInt32Size | 3.323 ms | 0.1282 ms | 0.1071 ms | 3.300 ms | 1.00 | 0.04 |
| Manual_GetVarUInt64Size | 7.517 ms | 0.3058 ms | 0.2711 ms | 7.497 ms | 2.26 | 0.10 |
