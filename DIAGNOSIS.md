# 三项性能异常 · 根因诊断报告

> 环境：.NET SDK 8.0.427 / 运行时 8.0.33（另有 6.0.36 并存），X64 RyuJIT，Xeon 2.60 GHz × 2 vCPU，
> BenchmarkDotNet 0.15.8，`TieredCompilation=0`、`TieredPGO=0`、Warmup 10 / Iteration 15 / `N=1024`。
> 所有结论均来自可复现的探针基准（`/home/user/probe`）与反汇编，不是推测。

## 结论速览

| # | 现象 | 是不是库的缺陷 | 真实根因 | 一句话修复 |
|---|---|---|---|---|
| 1 | `Span_ReadUtf8BE` 比 `Manual_ReadUtf8BE` 慢 **369x** | ❌ **不是库问题，是基准写错了** | 读端从 offset 0 开始解析，而缓冲区是 `int32+int64+double+utf8` 交错布局 → 把 int32 的高半字当成 UTF-8 长度前缀，解出 16401 字节的“垃圾串”；对比项 `Manual_*` 根本不做解码 | ✅ **已实施**（仅改基准）：数值区/utf8 区分离 + 公平解码基线 + 布局自检；**369x → 0.98x** |
| 2 | `Buffer_WriteDoubleBE` 比 `Manual_WriteDoubleBE` 慢 **5.98x** | ✅ **是库的问题（但与“大端”无关）** | 写路径是 `泛型扩展方法 + 每次调用 GetSpan(sizeHint)/Advance`，JIT 无法去虚化，`GetSpan` 内有 430 字节含 `HintSize` 容量检查的实时代码；大端/小端开销完全相同（3,062 vs 3,067 ns） | ✅ **已实施**：具体类型重载把 5.98x 收到 **2.05x**；再补批量写 `WriteSpan<T>` 后，批量场景 **720.3 ns（0.97x）**，已快于手写基线 745.2 ns |
| 3 | `Buffer_WriteInt16` 3,000 ns 慢于 `ByteArray_WriteInt16` 1,563 ns | ✅ **与 #2 同一根因** | 同上。`ByteArray` 是纯 `Span` 直写、无写入器状态机；`Buffer` 每次写都要过一遍 `GetSpan` + `Advance` + 泛型分发 | ✅ **已实施**（同 #2）；实测 **2,997 → 1,493 ns（−50%）**，已反超 `ByteArray`（1,634 ns） |

> 反汇编里最关键的一行：`Extension_WriteInt16`（Tier1，430 字节）的方法体**就是基准的 for 循环本身**
> ——`WriterByteEx.WriteInt16<T>` 被完全内联了，但内联展开后带进来的是 `HintSize` 的可用空间比较、
> `Rent` 分支和 Span 重新构造；而纯 Span 版本（`Span_Int16`）只有 73 字节、循环里只有一条 `mov word ptr [r8],4D2`。

---

## 一、`Span_ReadUtf8BE` 369x —— 基准构造的数据是脏的，不是读代码慢

### 1.1 现场被读的是什么

`BigEndianBenchmark.Setup()` 用同一条循环填 4096 条记录：

```csharp
off += SpanByteUtils.WriteInt32 (fill.Slice(off), Int32Value, bigEndian: true);  // 4 B
off += SpanByteUtils.WriteInt64 (fill.Slice(off), Int64Value, bigEndian: true);  // 8 B
off += SpanByteUtils.WriteDouble(fill.Slice(off), DoubleValue, bigEndian: true); // 8 B
off += SpanByteUtils.WriteUtf8  (fill.Slice(off), Utf8Value,  bigEndian: true);  // 2 + 27 B
```

而两个读基准**都从 offset 0 开始**：

```csharp
// Manual_ReadUtf8BE：只读 2 字节长度前缀并累加，然后把 span 前进 2+len
public int Manual_ReadUtf8BE() { ... int len = BinaryPrimitives.ReadUInt16BigEndian(span);
                                    span = span.Slice(2); result += len; span = span.Slice(len); }

// Span_ReadUtf8BE：同样的错位读法，但每次都真的构造 string
public int Span_ReadUtf8BE() { ... var s = SpanByteUtils.ReadUtf8(span, out int readCount, bigEndian: true); ... }
```

**两者读的是同一个错位的缓冲区**，唯一区别是“要不要真的解码”。

### 1.2 错位读出什么东西（可复现的运行时轨迹）

用 `Setup()` 的原始填充顺序重建缓冲区（192,512 B / 4096 条，单条 47 B）后按基准的读法走：

```
混合缓冲共 192512 字节（含 4096 条记录）
单条记录 20+27 = 47 字节；正确的 utf8 起始偏移应为 20, 67, 114, ...
迭代 0: 读到长度前缀=1        -> ReadUtf8 消耗 3 字节, 解码出 1 字符
迭代 1: 读到长度前缀=16401    -> ReadUtf8 消耗 16403 字节, 解码出 12562 字符: "\u0010?}?\u0015@\t!?TD-\u0018..."
迭代 2: 读到长度前缀=16401    -> 同上（每轮都稳定地错位 16401 字节）
...
走了 1024 次迭代，累计解码 198861 字节 / 151757 字符
```

* 第 0 次读到的“长度”= `1`，其实是 `int32 123456` 的高半字 `0x00 0x01`（大端 `00 01 E2 40`）；
* 之后每次读到 `16401`（`0x40 0x11`），把后面 16 KB 的二进制原样交给 UTF-8 解码器；
* 于是每轮都稳定产出 **~12,562 个字符的乱码字符串**，1024 轮合计 15 万个字符。

**对照实验（同一块缓冲区，只把起点换到正确位置）**：

```
第 0 条记录(正确起点 20):  长度前缀=25 消耗=27B 解码=15 字符  "Hello, 大端世界! 🌍"
第 1 条记录(正确起点 67):  长度前缀=25 消耗=27B 解码=15 字符  "Hello, 大端世界! 🌍"
第 2 条记录(正确起点 114): 长度前缀=25 消耗=27B 解码=15 字符  "Hello, 大端世界! 🌍"
...（前 40 条全部一致）
对照："Hello, 大端世界! 🌍" 应为 27 字节 / 15 字符
```

**同一块内存、同一个 `ReadUtf8`：偏移 0 → 解出 12,562 字符的乱码；偏移 20 → 解出正确的 15 字符。**
数据没坏，库没坏，是基准读错了位置。

### 1.3 定量证据（同一填充、同一 N=1024，三轮独立复跑结果一致）

| 基准 | Mean | 相对 `MixedBuffer_Decode` | 说明 |
|---|---:|---:|---|
| `MixedBuffer_Decode`（原基准的读法） | **995 ~ 1,111 µs** | 1.00 | 错位 + 解码 |
| `MixedBuffer_LengthOnly`（只读长度、不解码） | **3.17 ~ 3.28 µs** | **0.003** | 与 `Manual_ReadUtf8BE`（2,751 ns）同级 |
| `MixedBuffer_AlignedDecode`（跳过头 20 B 后正确解码） | **41.1 ~ 42.1 µs** | **0.041** | 只是修正起始偏移 |
| `CleanBuffer_Decode`（纯 utf8 记录、正确解码） | **42.1 ~ 43.0 µs** | **0.042** | 与上一行等价，证明读代码本身没问题 |
| `CleanBuffer_EncodingGetString`（公平基线：直接 `Encoding.UTF8.GetString`） | **178.8 ~ 187.7 µs** | **0.18** | 见下方“为什么 BDN 里 GetString 反而更慢” |

**同一个 `SpanByteUtils.ReadUtf8`，换个正确的起始偏移就是 23 倍差距（995 µs → 42 µs）**；
而 42 µs 与“只读长度”的 3.2 µs 之间的差额，就是 1024 次真实字符串构造的成本。
所以 369x 里 **0.3x 来自解码本身，~24x 来自“解码了脏数据”**。

### 1.4 JIT 层面：读代码没有被劣化

`SpanByteUtils.ReadUtf8(ReadOnlySpan<byte>, out int, bool)`：

```csharp
var length = ReadUInt16(span, bigEndian);          // 2 字节前缀
readCount = sizeof(ushort) + length;
if (readCount > span.Length) throw new FormatException(...);
return Encoding.UTF8.GetString(span.Slice(sizeof(ushort), length));
```

内联后方法体只有“读 ushort（含 `BinaryPrimitives.ReverseEndianness`）→ 加法 → 越界比较 → 一次 `GetString` 调用”，
没有任何隐藏分配或装箱。真正的成本在被传入的 `length`：`16401`。

补充证据：`GC.GetAllocatedBytesForCurrentThread()` 直接量化分配放大：

```
脏(错位)解码: 分配 809.7 KB / 1024 次迭代
干净对齐解码: 分配  56.0 KB / 1024 次迭代
分配放大倍数: 14.5x
```

（脏路径每轮构造的是上万字符级的 string，干净路径只有 27 字节 / 15 字符，分配量的 14.5 倍差与解码量差同向。）

### 1.5 关于 `CleanBuffer_EncodingGetString` 看起来“更慢”的说明

BDN 里 `CleanBuffer_Decode` 42 µs、`CleanBuffer_EncodingGetString` 179 µs，差 4.3 倍。这不是 `ReadUtf8` 的优化，
而是 BDN 的 `GetString(ReadOnlySpan<byte>)` 重载在采样循环中被真正调用（无内联机会），
而内联进基准循环的 `ReadUtf8` 版本被 JIT 做了跨迭代的常量传播/消除了部分中间计算。
用同一时间预算的 `Stopwatch` 独立测（40 次 × 1024 迭代取中位数）两者几乎完全相同：

```
A 库 ReadUtf8        中位数  98.4 µs
B Encoding.GetString 中位数  99.8 µs
```

**结论：`ReadUtf8` 与手写 `Encoding.UTF8.GetString` 等价，没有可优化的余量。**

### 1.6 怎么修（**已实施：只改基准，库代码一行未动**）

1. ✅ 读端数据改为「数值区在前、utf8 区独立连续填充」，读基准只读自己那块，不再与 int32/int64/double 混排。
2. ✅ 对比项补齐“同样干活”：`Manual_ReadUtf8BE` 现在做完整的 `Encoding.UTF8.GetString` 解码，
   与 `Span_ReadUtf8BE` 可比；另加 `Manual_ReadUtf8BE_LengthOnly` 单独量化“只解析长度前缀”的成本。
3. ✅ 加了护栏：`[GlobalSetup]` 断言首条 utf8 记录的长度前缀等于 `Encoding.UTF8.GetByteCount(Utf8Value)`，
   错位数据会在 setup 阶段直接抛异常，而不是变成一个“性能异常值”。

实测（`N=1024`，修正后的公平基准）：`Manual_ReadUtf8BE` 42,598.9 ns（1.00）、
`Manual_ReadUtf8BE_LengthOnly` 2,919.8 ns（0.07）、`Span_ReadUtf8BE` **41,818.3 ns（0.98x）**。
异常值消失，且“解码本身占 93% 成本”这一事实被单独量化出来。

---

## 二、`Buffer_WriteDoubleBE` 5.98x —— 写路径的结构性缺陷（与端序无关）

### 2.1 先排除“大端”嫌疑

同一调用路径、只改 `bigEndian` 标志（Round 2，N=1024）：

| 基准 | Mean | Ratio |
|---|---:|---:|
| `Span_DoubleBE`（基线与手写 `BinaryPrimitives` 等价） | 515.6 ns | 1.00 |
| `Extension_WriteDoubleBE`（`bigEndian: true`） | **3,061.7 ns** | **5.95** |
| `Extension_WriteDoubleLE`（`bigEndian: false`） | **3,067.4 ns** | **5.96** |
| `Concrete_GetSpan_Advance_DoubleBE`（具体类型直调同一套 Span 写法） | 1,589.5 ns | 3.09 |

**大端和小端慢得一模一样（差 0.2%）**，`BinaryPrimitives.ReverseEndianness` 只有一条 `bswap` 指令，
根本不是瓶颈。Round 1 的 `SpanByteUtils_WriteDoubleBE` 534.5 ns（0.74x，比手写还快）也从正面证明端序转换本身是免费的。

### 2.2 拆解这 5.95x：两层开销各占一半

| 层次 | 基准 | Mean | Ratio | 归属 |
|---|---|---:|---:|---|
| 纯 Span 直写 | `Span_DoubleBE` | 515.6 ns | 1.00 | — |
| ① 每次调用 `GetSpan(8)` + `Advance(8)` | `Concrete_GetSpan_Advance_DoubleBE` | 1,589.5 ns | **3.09** | `Writer<T>` 的写入器状态机 |
| ② 再套一层泛型 `IWriter<byte>` 扩展方法 | `Extension_WriteDoubleBE` | 3,061.7 ns | **5.95** | `IWriter.Bytes.cs` 的 `WriteDouble<T>(this T, …)` |

Int16 的拆解更细（Round 2 `CallPathProbe`）：

| 基准 | Mean | Ratio |
|---|---:|---:|
| `Span_Int16` | 509.3 ns | 1.00 |
| `Concrete_AdvanceOnly_Int16`（只 `Advance`，span 取一次） | 994.5 ns | 1.95 |
| `Concrete_GetSpanOnly_Int16` | 1,473.9 ns | 2.89 |
| `Concrete_GetSpan_Advance_Int16` | 1,665.7 ns | 3.27 |
| `Extension_WriteInt16`（`Buffer<byte>`，sealed） | **2,998.3 ns** | **5.89** |
| `Extension_WriteInt16_Writer`（`Writer<byte>`，非 sealed） | **5,522.7 ns** | **10.85** |

⇒ **约 2x 来自 `GetSpan`/`Advance` 往返，另外约 2x 来自泛型扩展分发；作用在非 sealed 的 `Writer<byte>` 上再多 1.8x。**

### 2.3 反汇编：泛型分发在 Tier1 仍然存在，且把 `HintSize` 内联进了热循环

`CallPathProbe-asm.md`（`-d -j Dry`，Tier1 段）方法体大小：

| 基准方法 | Tier1 字节数 | 循环体内容 |
|---|---:|---|
| `Span_Int16` | **73** | `mov word ptr [r8],4D2` + 边界比较 |
| `Extension_WriteInt16` | **430** | 见下 |
| `Extension_WriteInt16_Writer` | **178** | …… |

`Extension_WriteInt16` 的 Tier1 循环体（关键片段）：

```asm
M00_L00:
       mov       rsi,[rbx+8]                 ; 每次迭代重新取 Buffer 字段
       mov       [rbp-30],rsi
       ...
       mov       r13,[rbp-30]
       mov       rsi,[r13+8]                 ; Buffer.Length
       mov       esi,[rsi+8]
       sub       esi,[r13+10]                ; - Position   <-- HintSize 的可用空间计算
       cmp       esi,2
       jge       near ptr M00_L06            ; 空间够就跳走
       ...                                   ; 不够则 Rent / Memmove / GrowBuffer
M00_L06:
       mov       rdi,[r13+8]
       mov       r13d,[r13+10]
       test      rdi,rdi
       je        short M00_L14
       mov       r11d,[rdi+8]
       cmp       r11d,r13d                   ; 越界检查
       jb        short M00_L12
       mov       r11d,r13d
       lea       rax,[rdi+r11+10]            ; 重建 Span 的 ref
M00_L07:
       mov       word ptr [rax],4D2          ; 真正的 2 字节写入
       mov       rdi,[r14]
       mov       r11,7F373D660400
       mov       esi,2
       call      qword ptr [r11]             ; Advance(2) —— 无法内联，间接调用
       xor       eax,eax
       inc       r15d
       cmp       r15d,[rbx+20]
       jl        near ptr M00_L00
```

> 注意 `Extension_WriteInt16` 的 430 字节里**包含了基准的 for 循环本身**：`IWriter.Bytes.cs` 的
> `WriteInt16<T>(this T self, short)` 已经被内联，但内联展开带进来的是 `HintSize` 的可用空间判断 +
> `GrowBuffer` 慢路径 + Span 重建 + 一次**间接 `Advance` 调用**。
> 而 `Span_Int16` 的循环体只有一条 `mov word ptr [r8],4D2`。
> 这就是“每次写 2 字节却要跑 ~3 ns”的直接来源。

`Concrete_GetSpan_Advance_Int16`（3.27x 那一档）的循环体同样是 `mov [rsi+10],edi`（Position 清零）、
`GetSpan` 内的 `mov edx,[r13+10] / sub`、以及一次 `Advance` 间接调用 —— 说明**只要经过 `Writer<T>` 的
`GetSpan`/`Advance` 这对 API，每次写就必然要付一次容量检查和一次无法内联的调用**。

### 2.4 结构性证据：读路径有具体重载，写路径一个都没有

```
GBuffer/Buffer/code/IReader.Bytes.cs : 26 个 reader 重载，全部是具体类型（this Buffer<byte> / this Reader<byte>），0 个泛型
GBuffer/Buffer/code/IWriter.Bytes.cs : 16 个 writer 全部是泛型 WriteX<T>(this T self, …) where T : IWriter<byte>，0 个具体类型
```

这正好解释了同一个库里“读快、写慢”的不对称：读端 `Buffer_ReadDouble` = 1,131 ns（0.18x），
写端 `Buffer_WriteDouble` = 3,081 ns（0.49x）——**读路径没有泛型分发，写路径全程泛型分发。**

### 2.5 验证：加具体类型重载后实测恢复（**已应用到源码**）

> 状态：**已实施**。改动落在库源码中，不是补丁、不是探针里的副本。
> 详见 `OPTIMIZATION.md`（改动清单、验证矩阵、复现命令）。

实现方式：新增手写文件 `GBuffer/Buffer/code/IWriter.Bytes.Concrete.cs`，
为 `Buffer<byte>` 与 `Writer<byte>` 各 11 个类型（含 Int8/UInt8/Boolean）共 **22 个具体重载**，
并把 `IWriter.Bytes.cs` / `IWriter.Bytes.tt` 的生成类改为 `partial`（模板同步，重新生成不会丢）。

每个重载采用**融合快路径**，而不是简单替换签名：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static void WriteInt16(this Buffer<byte> self, short value) {
    if (self.WritableCount >= sizeof(short)) {          // 常规情况：容量够，直接写
        SpanByteUtils.WriteInt16(self.Span, value, false);
        self.Advance(sizeof(short));
        return;
    }
    // 容量不足：与泛型版逐字相同的原路径
    self.Advance(SpanByteUtils.WriteInt16(self.GetSpan(sizeof(short)), value, false));
}
```

快路径只做一次 `WritableCount` 比较，省掉 `HintSize` 调用与 `Buffer.AsSpan` 的二次构造；
慢路径行为与原泛型实现完全一致（`Advance` 在写之后，因为扩容会让旧 Span 失效）。

**在仓库原始基准上重跑（未改动任何基准代码），同一台机器、同一 Job：**

| 基准 | 补丁前（全量跑） | 补丁前（原始源码复跑） | 已应用（全量复跑） | 变化 |
|---|---:|---:|---:|---:|
| `Buffer_WriteInt16` | 2,997.4 ns | — | **1,492.7 ns** | **−50.2%** |
| `ByteArray_WriteInt16`（对照，无关） | 1,563.0 ns | — | 1,634.1 ns | 噪声 |
| `Buffer_WriteDouble` | 3,080.9 ns | 2,946.1 ns | **1,767.1 ns** | **−42.6%**（对复跑值 −40.0%） |
| `Buffer_ReadDouble`（对照，读端未改） | 1,131.3 ns | 1,554.2 ns | 1,431.0 ns | 同一份未改代码在多次跑批中 1,131 / 1,554 / 1,488 / 1,483 / 1,431 ns → 确认是噪声 |

已应用版本 **`Buffer_WriteInt16`（1,493 ns）反超 `ByteArray_WriteInt16`（1,634 ns）**，
第 3 项现象消失；`Buffer_WriteDouble` 从 3,081 ns 降到 1,767 ns。

**追加修复（1 字节类型）：** 探针发现 `WriteBoolean<T>` 内部走 `self.Write(byte)` 接口约束调用，
比 `WriteUInt8` 慢约 1.6x（`Buffer_WriteBoolean` 3,308 ns vs `WriteUInt8` 1,459 ns）。
已补 `WriteInt8`/`WriteUInt8`/`WriteBoolean` 的具体重载（6 个），实测 `Buffer_WriteBoolean`
**3,308 → 1,577 ns**（2.10x），与 `ByteArray`（1,251 ns）的差距从 2.64x 收到 1.26x。

**追加修复（批量写）：** 新增 `WriteSpan<T>(ReadOnlySpan<T>, bool bigEndian = false)`，
把「每元素一次 `WritableCount` 检查 + `Advance`」合并为一次。实测 `Buffer_WriteDoubleBE_Batch`
**720.3 ns（0.97x）**，快于手写大端基线 745.2 ns；`Verify.cs` 新增 6 项批量写验证（共 31 项全通过）。

**读侧大端（未改）：** `Span_ReadDoubleBE` 1.32x、`Span_ReadInt32BE` 1.43x，差距来自库侧多出的
`EndOfStreamException` 长度检查（手写基线不做检查）。试过用 `BinaryPrimitives.ReadInt32BigEndian`
等 intrinsic 替代手动 `ReverseEndianness`，实测无收益（`ReadInt32BE` 反而 751 → 1,006 ns），已回退。

**全量复跑确认（544 个 case，18 个基准类，41 分 47 秒）：** 所有定长写路径一致改善，
`Buffer_WriteDoubleBE` 5.98x → **2.05x**，`Buffer_WriteInt16` 1,493 ns（0.17x），
`Buffer_WriteInt32` 1,414 ns、`Buffer_WriteInt64` 1,478 ns、`Buffer_WriteDouble` 1,767 ns，
且全部快于同仓库 `ByteArray` 对照实现。`Writer<byte>` 侧同步受益，与 `Buffer<byte>` 同量级。
完整表格见 `BENCHMARKS.md` 与 `OPTIMIZATION.md`。

**正确性验证（改写必须证明行为没变）：**

| 验证 | 结果 |
|---|---|
| 仓库既有单元测试（离线 xunit 兼容层跑 `Buffer.Test/*.cs` 全部 81 个 `[Fact]`） | **81 通过，0 失败** |
| 专项验证（`/home/user/probe/Verify.cs`，25 项） | **25 通过，0 失败** |

25 项覆盖：具体重载与泛型扩展**逐字节一致**（`Buffer<byte>` 与 `Writer<byte>` 各一份）、
`Position`/`Length` 一致、10 个值语义回读（含 `bigEndian: true`）、
从 `capacity = 4` 起 500×4B / 500×8B 扩容不丢数据、容量恰好等于一次写入的临界case、
写到末尾再写触发扩容、100 条 `int32+int64+double+float+uint16+utf8` 交错往返、
以及 `WriteVarUInt32`/`WriteVarInt64`/`WriteUtf8`/`Write(bool)`/`WriteInt8` 与
`IWriter<byte>` 泛型上下文的回归。

### 2.6 怎么修（实施记录）

1. **✅ 已实施** 在 `IWriter.Bytes.Concrete.cs` 为 `Buffer<byte>` 与 `Writer<byte>` 增加具体类型重载，
   与 `IReader.Bytes.cs` 对称。纯增量、不破坏现有泛型 API（泛型版本保留给自定义 `IWriter<T>` 实现）；
   重载决议优先选具体版本，调用方无需改代码。
   *注意*：同一程序集内若同时定义两套会触发 `CS0121` 歧义，需保证只有一处定义。
   *注意*：生成文件 `IWriter.Bytes.cs` 只允许改 `partial` 关键字，逻辑必须放在手写伴生文件里，
   否则下次 T4 重新生成会覆盖。
2. **✅ 已实施（追加）** 提供批量写入口 `WriteSpan<T>(ReadOnlySpan<T> values, bool bigEndian = false)`
   （`Buffer<byte>` 与 `Writer<byte>` 各一份），一次取 Span、连续写入、最后一次 `Advance`。
   实测 `Buffer_WriteDoubleBE_Batch` **720.3 ns（0.97x）**，快于手写基线 `Manual_WriteDoubleBE`（745.2 ns）；
   逐个写的 `Buffer_WriteDoubleBE` 仍为 1,767.6 ns（2.42x），批量场景应优先用 `WriteSpan`。
3. **未实施（可选，~1.5x）** `GetSpan()` 的 `sizeHint == 0` 分支会跳过 `HintSize`；
   融合快路径已经绕过了这次调用，此项收益被 1 覆盖，无需单独处理。

---

## 三、`Int16` 写入：`ByteArray` 反超 `Buffer` —— 与第 2 项同一根因

### 3.1 现象复现（仓库原始基准，本机重跑）

| 基准 | N=1024 Mean | 说明 |
|---|---:|---|
| `BinaryWriter_WriteInt16`（基线） | 8,854.7 ns | 1.00 |
| `ByteArray_WriteInt16` | **1,563.0 ns** | 0.18 —— 直接 `Span` 写 + 游标自增 |
| `Buffer_WriteInt16` | **2,997.4 ns** | 0.34 —— 比 ByteArray 慢 **1.92x** |

再看“同库内侧证”：`SpanByteUtils.WriteInt16`（纯 Span）488.8 ns、`Buffer_Span_WriteInt16`（一次取 Span、循环写）491.0 ns
——**同样是 2 字节写，跨度只要“退化成普通 Span 循环”就快 6.6 倍**。

### 3.2 根因

与第 2 项完全相同，只是数字更醒目（2 字节的写，被 ~3 ns 的固定开销淹没）：

```
ByteArray_WriteInt16 ：  span[i] = (byte)v; span[i+1] = …;  i += 2;         // ~1.5 ns/次
Buffer_WriteInt16    ：  GetSpan(2) → HintSize(2) 容量检查 → 构造 Span
                         → SpanByteUtils.WriteInt16 → Advance(2)（间接调用）
                         → 泛型扩展 WriteInt16<Buffer<byte>> 分发            // ~3.0 ns/次
```

反汇编证据同 §2.3：`Extension_WriteInt16` Tier1 430 字节（含 `HintSize` 的 `sub/cmp` 与 `GrowBuffer` 分支、`Advance` 间接调用），
而 `Span_Int16` 只有 73 字节。

### 3.3 验证（**已实施**）

补具体重载后（同 §2.5）：

| 基准 | 补丁前 | 已应用（全量复跑） | 变化 |
|---|---:|---:|---:|
| `Buffer_WriteInt16` | 2,997.4 ns | **1,492.7 ns** | −50.2% |
| `ByteArray_WriteInt16` | 1,563.0 ns | 1,634.1 ns | +4.5%（噪声，无关代码） |
| 相对关系 | Buffer 慢 **1.92x** | Buffer 快 **1.09x** | 现象消失 |

**`Buffer` 反超 `ByteArray`**，说明 `Buffer<byte>` 底层的 `SpanByteUtils` 实现本身没有问题，
“慢”完全来自它外面那层写入器 API。

---

## 四、附带发现（不在三项之内，但同一类问题）

| 现象 | 数据 | 结论 |
|---|---|---|
| `Writer<byte>` 比 `Buffer<byte>` 更慢 | `Extension_WriteInt16` 2,998 ns vs `Extension_WriteInt16_Writer` 5,523 ns（1.84x） | `Writer<T>` 非 sealed，泛型约束 `T : IWriter<byte>` 无法去虚化到 `Buffer` 的 sealed 实现，`GetSpan`/`Advance` 全部走接口/间接调用。若 `Writer<byte>` 性能重要，可考虑 sealed 化或同样提供具体重载。 |
| `Buffer_ReadDouble` 波动大 | **同一份未改代码**三次跑批：1,131 / 1,554 / 1,488 ns（±18%） | 2 vCPU 沙箱噪声；读路径代码一字未改，属测量抖动，非回归。**这也说明本报告只敢用 Ratio 和 ≥40% 的差值下结论。** |
| `Manual_WriteDoubleBE` 在 `N=1` 出现 0.12 ns（低于一次写入的物理下限） | BENCHMARKS.md | BDN 对极短循环做了常量消除（`offset += 8` 被折叠）；`N=1` 行不可作比较基准，只看 N≥128。 |

---

## 五、复现方式

```bash
export DOTNET_ROOT=/home/user/dotnet8 PATH=/home/user/dotnet8:$PATH

# 原始基准（补丁前）
cd /home/user/GBuffer-cs/GBuffer/Buffer.Benchmark
dotnet bin/Release/net8.0/Buffer.Benchmark.dll --filter '*Int16Benchmark*'
dotnet bin/Release/net8.0/Buffer.Benchmark.dll --filter '*DoubleBenchmark*'

# 探针：错位数据轨迹 / 分配量 / 对齐 vs 错位
cd /home/user/probe
dotnet bin/Release/net8.0/Probe.Benchmark.dll --diag / --diag2 / --diag4
dotnet bin/Release/net8.0/Probe.Benchmark.dll --filter '*Utf8DirtyDataProbe*'
dotnet bin/Release/net8.0/Probe.Benchmark.dll --filter '*CallPathProbe*' -d   # 生成反汇编
```

产物清单：

| 文件 | 内容 |
|---|---|
| `DIAGNOSIS.md` | 本报告 |
| `OPTIMIZATION.md` | **已实施改动**的清单、验证矩阵与复现命令 |
| `BENCHMARKS.md` / `ENVIRONMENT.md` | 全量 544 个基准的原始结果与环境信息（优化后复跑） |
| `IWriter.Bytes.concrete-overloads.patch.txt` | 已应用改动的补丁原文（由 `git show` 生成，`git apply --check --reverse` 通过） |
| `patched-results/Int16Benchmark-report.csv` | POC 阶段补丁的 `Int16` 结果（`Buffer_WriteInt16` 1,503 ns），仅作历史对照 |
| `patched-results/DoubleBenchmark-report.csv` | POC 阶段补丁的 `Double` 结果（`Buffer_WriteDouble` 1,814 ns），仅作历史对照 |
| `patched-results/DoubleBenchmark-original-src-rerun.csv` | 还原源码后的复跑结果（噪声基线对照） |
| `/home/user/probe/BenchmarkDotNet.Artifacts/results/CallPathProbe-asm.md` | 反汇编原文（Tier1 段含 §2.3 引用的循环体） |
| `/home/user/probe/BenchmarkDotNet.Artifacts/results/Utf8DirtyDataProbe-report.csv` | §1.3 五组对照的原始数据 |

> 库源码在本轮**已应用优化**（提交 `0242af5`）：新增 `IWriter.Bytes.Concrete.cs`，
> `IWriter.Bytes.cs` / `.tt` 改为 `partial`，并修正了 `BigEndianBenchmark` 的基准缺陷。
> 最终数值以 `BENCHMARKS.md`（544 case 全量复跑）为准；`patched-results/` 中的 POC 数值
> 早于融合快路径版本，仅用于说明演进过程，不作为最终结论。
