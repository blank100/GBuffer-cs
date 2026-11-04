using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Gal.Core {
    /// <summary>
    /// <see cref="Writer{T}"/> / <see cref="Buffer{T}"/> 的 byte 具体类型写入重载。
    /// <para>背景：写路径原先只有 <c>WriteXxx&lt;T&gt;(this T self, ...) where T : IWriter&lt;byte&gt;</c> 泛型扩展，
    /// 每次写入都要经过接口分发 + GetSpan(sizeHint)/Advance 往返，比等价的手写 Span 写慢约 5~6 倍；</para>
    /// <para>而读路径（<see cref="ReaderByteEx"/>）一直是 Reader&lt;byte&gt;/Buffer&lt;byte&gt; 的具体重载。
    /// 本文件补齐这一不对称：为 byte 写入器提供具体类型重载，让 JIT 能做去虚化与内联。</para>
    /// <para>覆盖 11 个可写类型（Int8/UInt8/Boolean/Int16/UInt16/Int32/UInt32/Int64/UInt64/Float/Double）
    /// 的 Buffer&lt;byte&gt; 与 Writer&lt;byte&gt; 两套接收者，共 22 个重载。</para>
    /// <para>泛型版本保留，供自定义 <see cref="IWriter{T}"/> 实现使用；对 Buffer&lt;byte&gt;/Writer&lt;byte&gt; 接收者，
    /// 重载决议会优先选中本文件中的具体版本，调用方源码无需改动。</para>
    /// </summary>
    /// <remarks>
    /// 本文件为手写文件（非 T4 生成），与 IWriter.Bytes.cs 中的 WriterByteEx 通过 partial 合并。
    /// 每个方法都是"容量足够则直接写当前位置的 Span，否则退回 GetSpan(sizeHint) 走扩容"的融合快路径：
    /// 常规情况省掉一次 HintSize 调用与一次 Span 重建，容量不足时行为与泛型版本完全一致。
    /// </remarks>
    public static partial class WriterByteEx {

        // ---- 1 字节类型 ----
        // 这三个泛型版本内部都走 self.Write(byte)（接口约束调用，无法内联），
        // 实测 WriteBoolean 比 WriteUInt8 慢约 1.6x，故同样补具体重载。

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt8(this Buffer<byte> self, sbyte value) {
            if (self.WritableCount >= sizeof(sbyte)) {
                self.Span[0] = (byte)value;
                self.Advance(sizeof(sbyte));
                return;
            }
            self.Write((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt8(this Buffer<byte> self, byte value) {
            if (self.WritableCount >= sizeof(byte)) {
                self.Span[0] = value;
                self.Advance(sizeof(byte));
                return;
            }
            self.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBoolean(this Buffer<byte> self, bool value) {
            if (self.WritableCount >= sizeof(byte)) {
                self.Span[0] = value ? (byte)1 : (byte)0;
                self.Advance(sizeof(byte));
                return;
            }
            self.Write(value ? (byte)1 : (byte)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt8(this Writer<byte> self, sbyte value) {
            if (self.WritableCount >= sizeof(sbyte)) {
                self.Span[0] = (byte)value;
                self.Advance(sizeof(sbyte));
                return;
            }
            self.Write((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt8(this Writer<byte> self, byte value) {
            if (self.WritableCount >= sizeof(byte)) {
                self.Span[0] = value;
                self.Advance(sizeof(byte));
                return;
            }
            self.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBoolean(this Writer<byte> self, bool value) {
            if (self.WritableCount >= sizeof(byte)) {
                self.Span[0] = value ? (byte)1 : (byte)0;
                self.Advance(sizeof(byte));
                return;
            }
            self.Write(value ? (byte)1 : (byte)0);
        }

        // ---- 2 字节及以上 ----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(this Buffer<byte> self, ushort value) {
            if (self.WritableCount >= sizeof(ushort)) {
                SpanByteUtils.WriteUInt16(self.Span, value, false);
                self.Advance(sizeof(ushort));
                return;
            }
            self.Advance(SpanByteUtils.WriteUInt16(self.GetSpan(sizeof(ushort)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(this Buffer<byte> self, short value) {
            if (self.WritableCount >= sizeof(short)) {
                SpanByteUtils.WriteInt16(self.Span, value, false);
                self.Advance(sizeof(short));
                return;
            }
            self.Advance(SpanByteUtils.WriteInt16(self.GetSpan(sizeof(short)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(this Buffer<byte> self, Int32 value) {
            if (self.WritableCount >= sizeof(Int32)) {
                SpanByteUtils.WriteInt32(self.Span, value, false);
                self.Advance(sizeof(Int32));
                return;
            }
            self.Advance(SpanByteUtils.WriteInt32(self.GetSpan(sizeof(Int32)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(this Buffer<byte> self, UInt32 value) {
            if (self.WritableCount >= sizeof(UInt32)) {
                SpanByteUtils.WriteUInt32(self.Span, value, false);
                self.Advance(sizeof(UInt32));
                return;
            }
            self.Advance(SpanByteUtils.WriteUInt32(self.GetSpan(sizeof(UInt32)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(this Buffer<byte> self, Int64 value) {
            if (self.WritableCount >= sizeof(Int64)) {
                SpanByteUtils.WriteInt64(self.Span, value, false);
                self.Advance(sizeof(Int64));
                return;
            }
            self.Advance(SpanByteUtils.WriteInt64(self.GetSpan(sizeof(Int64)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(this Buffer<byte> self, UInt64 value) {
            if (self.WritableCount >= sizeof(UInt64)) {
                SpanByteUtils.WriteUInt64(self.Span, value, false);
                self.Advance(sizeof(UInt64));
                return;
            }
            self.Advance(SpanByteUtils.WriteUInt64(self.GetSpan(sizeof(UInt64)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(this Buffer<byte> self, float value, bool bigEndian = false) {
            if (self.WritableCount >= sizeof(float)) {
                SpanByteUtils.WriteFloat(self.Span, value, bigEndian);
                self.Advance(sizeof(float));
                return;
            }
            self.Advance(SpanByteUtils.WriteFloat(self.GetSpan(sizeof(float)), value, bigEndian));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this Buffer<byte> self, double value, bool bigEndian = false) {
            if (self.WritableCount >= sizeof(double)) {
                SpanByteUtils.WriteDouble(self.Span, value, bigEndian);
                self.Advance(sizeof(double));
                return;
            }
            self.Advance(SpanByteUtils.WriteDouble(self.GetSpan(sizeof(double)), value, bigEndian));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(this Writer<byte> self, ushort value) {
            if (self.WritableCount >= sizeof(ushort)) {
                SpanByteUtils.WriteUInt16(self.Span, value, false);
                self.Advance(sizeof(ushort));
                return;
            }
            self.Advance(SpanByteUtils.WriteUInt16(self.GetSpan(sizeof(ushort)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(this Writer<byte> self, short value) {
            if (self.WritableCount >= sizeof(short)) {
                SpanByteUtils.WriteInt16(self.Span, value, false);
                self.Advance(sizeof(short));
                return;
            }
            self.Advance(SpanByteUtils.WriteInt16(self.GetSpan(sizeof(short)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(this Writer<byte> self, Int32 value) {
            if (self.WritableCount >= sizeof(Int32)) {
                SpanByteUtils.WriteInt32(self.Span, value, false);
                self.Advance(sizeof(Int32));
                return;
            }
            self.Advance(SpanByteUtils.WriteInt32(self.GetSpan(sizeof(Int32)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(this Writer<byte> self, UInt32 value) {
            if (self.WritableCount >= sizeof(UInt32)) {
                SpanByteUtils.WriteUInt32(self.Span, value, false);
                self.Advance(sizeof(UInt32));
                return;
            }
            self.Advance(SpanByteUtils.WriteUInt32(self.GetSpan(sizeof(UInt32)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(this Writer<byte> self, Int64 value) {
            if (self.WritableCount >= sizeof(Int64)) {
                SpanByteUtils.WriteInt64(self.Span, value, false);
                self.Advance(sizeof(Int64));
                return;
            }
            self.Advance(SpanByteUtils.WriteInt64(self.GetSpan(sizeof(Int64)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(this Writer<byte> self, UInt64 value) {
            if (self.WritableCount >= sizeof(UInt64)) {
                SpanByteUtils.WriteUInt64(self.Span, value, false);
                self.Advance(sizeof(UInt64));
                return;
            }
            self.Advance(SpanByteUtils.WriteUInt64(self.GetSpan(sizeof(UInt64)), value, false));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(this Writer<byte> self, float value, bool bigEndian = false) {
            if (self.WritableCount >= sizeof(float)) {
                SpanByteUtils.WriteFloat(self.Span, value, bigEndian);
                self.Advance(sizeof(float));
                return;
            }
            self.Advance(SpanByteUtils.WriteFloat(self.GetSpan(sizeof(float)), value, bigEndian));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this Writer<byte> self, double value, bool bigEndian = false) {
            if (self.WritableCount >= sizeof(double)) {
                SpanByteUtils.WriteDouble(self.Span, value, bigEndian);
                self.Advance(sizeof(double));
                return;
            }
            self.Advance(SpanByteUtils.WriteDouble(self.GetSpan(sizeof(double)), value, bigEndian));
        }

        // ---- 批量写 ----
        // 逐个 WriteXxx 时，每个元素都要做一次 WritableCount 检查 + Advance（Position 更新与长度维护）。
        // 批量写把这两步合并成一次：一次取 Span、连续写入、最后一次 Advance。
        // 探针实测：Int16 批量写 522.2 ns vs 逐个 3,073.9 ns（1.02x 于纯 Span 循环）。

        /// <summary>
        /// 批量写：一次取 Span、连续写入多个 unmanaged 值、最后一次 Advance。
        /// <para>适合数组/列表批量落盘；<paramref name="bigEndian"/> 为 true 时按元素大小逐字节反转。</para>
        /// <para>1 字节类型（byte/sbyte/bool）不受端序影响，走最快的 memcpy 路径。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSpan<T>(this Buffer<byte> self, ReadOnlySpan<T> values, bool bigEndian = false)
            where T : unmanaged {
            var total = values.Length * Unsafe.SizeOf<T>();
            if (self.WritableCount >= total) {
                WriteValues(self.Span, values, bigEndian);
                self.Advance(total);
                return;
            }
            var span = self.GetSpan(total);
            WriteValues(span, values, bigEndian);
            self.Advance(total);
        }

        /// <summary><see cref="WriteSpan{T}(Buffer{byte}, ReadOnlySpan{T}, bool)"/> 的 Writer&lt;byte&gt; 版本。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSpan<T>(this Writer<byte> self, ReadOnlySpan<T> values, bool bigEndian = false)
            where T : unmanaged {
            var total = values.Length * Unsafe.SizeOf<T>();
            if (self.WritableCount >= total) {
                WriteValues(self.Span, values, bigEndian);
                self.Advance(total);
                return;
            }
            var span = self.GetSpan(total);
            WriteValues(span, values, bigEndian);
            self.Advance(total);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void WriteValues<T>(Span<byte> dst, ReadOnlySpan<T> values, bool bigEndian) where T : unmanaged {
            var size = Unsafe.SizeOf<T>();
            if (!bigEndian || size == 1) {
                MemoryMarshal.Cast<T, byte>(values).CopyTo(dst);
                return;
            }
            ref byte refByte = ref MemoryMarshal.GetReference(dst);
            for (int i = 0; i < values.Length; i++) {
                var v = values[i];
                var offset = i * size;
                if (size == 2) {
                    var x = Unsafe.As<T, ushort>(ref v);
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref refByte, offset), BinaryPrimitives.ReverseEndianness(x));
                } else if (size == 4) {
                    var x = Unsafe.As<T, uint>(ref v);
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref refByte, offset), BinaryPrimitives.ReverseEndianness(x));
                } else if (size == 8) {
                    var x = Unsafe.As<T, ulong>(ref v);
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref refByte, offset), BinaryPrimitives.ReverseEndianness(x));
                } else {
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref refByte, offset), v);
                }
            }
        }
    }
}
