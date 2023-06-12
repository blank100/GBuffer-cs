using System;

using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class SpanByteReaderWriterTest {
		[Fact]
		private void RandomInt16Test() {
			Span<byte> span = stackalloc byte[4];
			for (var i = 0; i < 1000; i++) {
				var                writer = span;
				ReadOnlySpan<byte> reader = span;
				var                n1     = (ushort) Random.Shared.Next(short.MinValue, short.MaxValue);
				var                n2     = (short) Random.Shared.Next(short.MinValue,  short.MaxValue);
				writer.WriteInt16(n1);
				writer.WriteInt16(n2);

				Assert.Equal(n1, reader.ReadUInt16());
				Assert.Equal(n2, reader.ReadInt16());
			}
		}

		[Fact]
		private unsafe void Int8Test() {
			Span<byte> span = stackalloc byte[4];

			var                writer = span;
			ReadOnlySpan<byte> reader = span;
			writer.WriteInt8((sbyte) '\r');
			writer.WriteInt8(127);
			writer.WriteUInt8(128);
			writer.WriteUInt8(255);

			Assert.Equal((sbyte) '\r', reader.ReadInt8());
			Assert.Equal(127,          reader.ReadInt8());
			Assert.Equal(128,          reader.ReadUInt8());
			Assert.Equal(255,          reader.ReadUInt8());
		}

		[Fact]
		private unsafe void Int16Test() {
			Span<byte> span = stackalloc byte[8];

			var                writer = span;
			ReadOnlySpan<byte> reader = span;

			writer.WriteInt16(32767);
			writer.WriteInt16(32768);
			writer.WriteInt16(short.MinValue);
			writer.WriteInt16(-100);

			Assert.Equal(32767,          reader.ReadInt16());
			Assert.Equal(32768,          reader.ReadUInt16());
			Assert.Equal(short.MinValue, reader.ReadInt16());
			Assert.Equal(-100,           reader.ReadInt16());
		}

		[Fact]
		private unsafe void Int32Test() {
			Span<byte>         span   = stackalloc byte[16];
			var                writer = span;
			ReadOnlySpan<byte> reader = span;

			writer.WriteInt32(int.MinValue);
			writer.WriteInt32(int.MaxValue);
			writer.WriteUInt32(uint.MinValue);
			writer.WriteUInt32(uint.MaxValue);

			Assert.Equal(int.MinValue,  reader.ReadInt32());
			Assert.Equal(int.MaxValue,  reader.ReadInt32());
			Assert.Equal(uint.MinValue, reader.ReadUInt32());
			Assert.Equal(uint.MaxValue, reader.ReadUInt32());
		}

		[Fact]
		private unsafe void Int64Test() {
			Span<byte>         span   = stackalloc byte[32];
			var                writer = span;
			ReadOnlySpan<byte> reader = span;

			writer.WriteInt64(long.MinValue);
			writer.WriteInt64(long.MaxValue);
			writer.WriteUInt64(ulong.MinValue);
			writer.WriteUInt64(ulong.MaxValue);

			Assert.Equal(long.MinValue,  reader.ReadInt64());
			Assert.Equal(long.MaxValue,  reader.ReadInt64());
			Assert.Equal(ulong.MinValue, reader.ReadUInt64());
			Assert.Equal(ulong.MaxValue, reader.ReadUInt64());
		}

		[Fact]
		private unsafe void FloatTest() {
			Span<byte>         span   = stackalloc byte[24];
			var                writer = span;
			ReadOnlySpan<byte> reader = span;

			writer.WriteFloat(float.MaxValue);
			writer.WriteFloat(float.MinValue);
			writer.WriteFloat(float.Epsilon);
			writer.WriteFloat(float.NaN);
			writer.WriteFloat(float.NegativeInfinity);
			writer.WriteFloat(float.PositiveInfinity);

			Assert.Equal(float.MaxValue,         reader.ReadFloat());
			Assert.Equal(float.MinValue,         reader.ReadFloat());
			Assert.Equal(float.Epsilon,          reader.ReadFloat());
			Assert.Equal(float.NaN,              reader.ReadFloat());
			Assert.Equal(float.NegativeInfinity, reader.ReadFloat());
			Assert.Equal(float.PositiveInfinity, reader.ReadFloat());
		}

		[Fact]
		private unsafe void DoubleTest() {
			Span<byte>         span   = stackalloc byte[48];
			var                writer = span;
			ReadOnlySpan<byte> reader = span;

			writer.WriteDouble(double.MaxValue);
			writer.WriteDouble(double.MinValue);
			writer.WriteDouble(double.Epsilon);
			writer.WriteDouble(double.NaN);
			writer.WriteDouble(double.NegativeInfinity);
			writer.WriteDouble(double.PositiveInfinity);

			Assert.Equal(double.MaxValue,         reader.ReadDouble());
			Assert.Equal(double.MinValue,         reader.ReadDouble());
			Assert.Equal(double.Epsilon,          reader.ReadDouble());
			Assert.Equal(double.NaN,              reader.ReadDouble());
			Assert.Equal(double.NegativeInfinity, reader.ReadDouble());
			Assert.Equal(double.PositiveInfinity, reader.ReadDouble());
		}

		[Fact]
		private unsafe void StringTest() {
			Span<byte>         span   = stackalloc byte[1024];
			var                writer = span;
			ReadOnlySpan<byte> reader = span;

			writer.WriteUtf8("这是文本1!@$@##$^%^#&fsdffdsaf46541313");
			writer.WriteUtf8("这是文本2");
			writer.WriteUtf8(null);
			writer.WriteUtf8(string.Empty);
			writer.WriteUtf8("float.NegativeInfinity");
			writer.WriteUtf8("");

			Assert.Equal("这是文本1!@$@##$^%^#&fsdffdsaf46541313", reader.ReadUtf8());
			Assert.Equal("这是文本2",                              reader.ReadUtf8());
			Assert.Equal(string.Empty,                         reader.ReadUtf8());
			Assert.Equal(string.Empty,                         reader.ReadUtf8());
			Assert.Equal("float.NegativeInfinity",             reader.ReadUtf8());
			Assert.Equal("",                                   reader.ReadUtf8());
		}

		[Fact]
		private unsafe void SyntheticTest() {
			Span<byte>         span   = stackalloc byte[1024];
			var                writer = span;
			ReadOnlySpan<byte> reader = span;

			writer.WriteInt8(sbyte.MaxValue);
			writer.WriteInt16(short.MaxValue);
			writer.WriteInt32(int.MaxValue);
			writer.WriteInt64(long.MaxValue);
			writer.WriteFloat(float.MaxValue);
			writer.WriteDouble(double.MaxValue);
			writer.WriteUtf8("this is text");

			Assert.Equal(sbyte.MaxValue,  reader.ReadInt8());
			Assert.Equal(short.MaxValue,  reader.ReadInt16());
			Assert.Equal(int.MaxValue,    reader.ReadInt32());
			Assert.Equal(long.MaxValue,   reader.ReadInt64());
			Assert.Equal(float.MaxValue,  reader.ReadFloat());
			Assert.Equal(double.MaxValue, reader.ReadDouble());
			Assert.Equal("this is text",  reader.ReadUtf8());
		}

		[Fact]
		private unsafe void VarUInt32Test() {
			var pcg = Random.Shared;

			Span<byte> span    = stackalloc byte[1024];
			var        numbers = new uint [10];
			for (var i = 0; i < 100000; i++) {
				var                writer = span;
				ReadOnlySpan<byte> reader = span;
				for (var j = 0; j < 10; j++) writer.WriteVarUInt32(numbers[j] = pcg.NextUInt(0, uint.MaxValue));
				for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], reader.ReadVarUInt32());
			}
		}

		[Fact]
		private unsafe void VarInt32Test() {
			var pcg = Random.Shared;

			Span<byte> span    = stackalloc byte[1024];
			var        numbers = new int [10];
			for (var i = 0; i < 100000; i++) {
				var                writer = span;
				ReadOnlySpan<byte> reader = span;
				for (var j = 0; j < 10; j++) writer.WriteVarInt32(numbers[j] = pcg.Next(int.MinValue, int.MaxValue));
				for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], reader.ReadVarInt32());
			}
		}

		[Fact]
		private unsafe void VarUInt64Test() {
			var pcg = Random.Shared;

			Span<byte> span    = stackalloc byte[1024];
			var        numbers = new ulong [10];
			for (var i = 0; i < 100000; i++) {
				var                writer = span;
				ReadOnlySpan<byte> reader = span;
				for (var j = 0; j < 10; j++) {
					var n1 = pcg.NextUInt(0, uint.MaxValue);
					var n2 = pcg.NextUInt(0, uint.MaxValue);
					var n  = (ulong) n1 << 32 | n2;
					writer.WriteVarUInt64(numbers[j] = n);
				}
				for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], reader.ReadVarUInt64());
			}
		}

		[Fact]
		private unsafe void VarInt64Test() {
			var pcg = Random.Shared;

			Span<byte> span    = stackalloc byte[1024];
			var        numbers = new long [10];
			for (var i = 0; i < 100000; i++) {
				var                writer = span;
				ReadOnlySpan<byte> reader = span;
				for (var j = 0; j < 10; j++) {
					var n1 = pcg.NextUInt(0, uint.MaxValue);
					var n2 = pcg.NextUInt(0, uint.MaxValue);
					var n  = (long) n1 << 32 | n2;
					writer.WriteVarInt64(numbers[j] = n);
				}
				for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], reader.ReadVarInt64());
			}
		}
	}
}