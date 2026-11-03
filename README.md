高性能的 Buffer 库
内存安全与非对齐写入：利用 Unsafe 和 MemoryMarshal 进行非对齐内存写入，并自动处理大小端（Endianness）转换。
变长编码（VarInt）：支持 VarInt 和 ZigZag 编码，有效压缩整数存储空间。
零拷贝思想：通过 Span<byte> 直接操作目标内存，避免不必要的中间缓冲区分配。
高性能扩展：所有方法均采用 [MethodImpl(MethodImplOptions.AggressiveInlining)] 特性，强制编译器内联，以减少方法调用开销。

 依赖项
    System.Buffers.Binary
    System.Runtime.InteropServices
    System.Runtime.CompilerServices

API 概览
基础类型写入
方法名	说明	写入长度
WriteInt8(sbyte)	写入 8 位带符号整数	1 Byte
WriteBoolean(bool)	写入布尔值（true -> 1, false -> 0）	1 Byte
WriteUInt16(ushort)	写入 16 位无符号整数（小端序）	2 Bytes
WriteInt16(short)	写入 16 位带符号整数（小端序）	2 Bytes
WriteInt32(int)	写入 32 位带符号整数	4 Bytes
WriteUInt32(uint)	写入 32 位无符号整数	4 Bytes
WriteInt64(long)	写入 64 位带符号整数	8 Bytes
WriteUInt64(ulong)	写入 64 位无符号整数	8 Bytes
WriteFloat(float)	写入单精度浮点数	4 Bytes
WriteDouble(double)	写入双精度浮点数	8 Bytes

压缩类型写入 (VarInt/ZigZag)

    WriteVarUInt32 / WriteVarUInt64: 使用 Base128 变长编码写入无符号整数。
    WriteVarInt32 / WriteVarInt64: 结合 ZigZag 编码处理带符号整数，使小的负数也能占用较少的字节。

字符串写入 (UTF-8)

    WriteUtf8(string, int bytesCount): 将字符串以 UTF-8 编码写入指定字节数长度的内存区域。
    WriteUtf8(string): 自动计算长度并写入。格式为：[UInt16 长度前缀] + [UTF-8 字节流]。

聚合读写 (unmanaged struct)

    Write<TValue>(TValue value) where TValue : unmanaged: 一次写入整个 struct（GetSpan + Unsafe.WriteUnaligned + Advance），写入长度为 Unsafe.SizeOf<TValue>() Bytes。
    Read<TValue>() where TValue : unmanaged: 一次读出整个 struct（守卫检查 + Unsafe.ReadUnaligned + Advance），Reader<byte> 与 Buffer<byte> 各提供一套重载。
    约定：struct 需标记 [StructLayout(LayoutKind.Sequential, Pack = 1)]，且只含 unmanaged 字段；按 native 小端序整体写入（与库内固定长度类型一致）；变长字段（string、数组等）请另行调用对应方法写入/读取。

使用示例

// 假设已有一个 RefWriter<byte> 实例 writer
ref var writer = ...;

// 写入基础数据
writer.WriteInt32(42);
writer.WriteBoolean(true);

// 写入变长整数（节省空间）
writer.WriteVarInt32(-100);

// 写入字符串
writer.WriteUtf8("Hello World");

// 聚合写入整个 struct（需 [StructLayout(LayoutKind.Sequential, Pack = 1)]，只含 unmanaged 字段）
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct MsgHeader { public int Id; public long Timestamp; public ushort Type; }

var header = new MsgHeader { Id = 1, Timestamp = 123456789L, Type = 7 };
writer.Write(header);              // 一次写入 14 Bytes（Unsafe.SizeOf<MsgHeader>()）

// 对应读出（Reader<byte> / Buffer<byte> 各一套 Read<TValue>() 重载）
var reader = new Reader<byte>(bytes);
var h = reader.Read<MsgHeader>();  // 一次读出 14 Bytes
```

## 协议格式与限制

- 固定长度类型按小端序写入（`bigEndian: true` 可切换大端序）；VarInt 为 Base128 变长编码，带符号整数先经 ZigZag 编码
- 字符串格式：`[UInt16 长度前缀] + [UTF-8 字节流]`，UTF-8 字节数上限为 **32767**（超过抛出 `ArgumentOutOfRangeException`）
- 读取时若剩余字节不足（定长类型）或 VarInt 序列非法/截断，抛出 `System.IO.EndOfStreamException` / `FormatException`，不会越界读取
- 聚合读写 `Write<TValue>` / `Read<TValue>`：要求 `TValue` 为 unmanaged 且 struct 标记 `[StructLayout(LayoutKind.Sequential, Pack = 1)]`，按 native 小端序整体写入/读出，长度等于 `Unsafe.SizeOf<TValue>()`；只含 unmanaged 字段，变长内容（string、数组等）需另行读写；剩余不足时同样抛出 `EndOfStreamException`
- 目标框架：`netstandard2.1;net8.0`（测试与 Benchmark 为 `net8.0`）

## 视图语义说明（Buffer<T>）

`Buffer<T>`（以及 `Writer<T>`）的 `Span` / `Memory` 表示 **当前 Position 到 Capacity 结尾的可写区域（包含未写入部分）**；已写入区域请用 `WrittenSpan` / `WrittenMemory`，剩余可读字节数请用 `ReadableCount`。
> 命名建议：为避免 `Span` 被误认为"可读数据"，未来可新增 `ReadableSpan` / `ReadableMemory`（Position..Length 的只读视图）作为显式命名，现有属性保持不变。