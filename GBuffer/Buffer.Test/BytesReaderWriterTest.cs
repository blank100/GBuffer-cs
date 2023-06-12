using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class BytesReaderWriterTest {
		[Fact]
		private unsafe void RandomInt16Test() {
			{
				var bytes = new byte[4];
				fixed (byte* bytePtr = bytes) {
					for (var i = 0; i < 1000; i++) {
						var ptr1 = bytePtr;
						var ptr2 = bytePtr;

						var n1 = (ushort) Random.Shared.Next(short.MinValue, short.MaxValue);
						var n2 = (short) Random.Shared.Next(short.MinValue,  short.MaxValue);
						BytesWriter.WriteInt16(ref ptr1, n1);
						BytesWriter.WriteInt16(ref ptr1, n2);

						Assert.Equal(n1, BytesReader.ReadUInt16(ref ptr2));
						Assert.Equal(n2, BytesReader.ReadInt16(ref ptr2));
					}
				}
			}
		}

		[Fact]
		private unsafe void Int8Test() {
			var bytes = new byte[4];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteInt8(ref ptr1, (sbyte) '\r');
				BytesWriter.WriteInt8(ref ptr1, 127);
				BytesWriter.WriteUInt8(ref ptr1, 128);
				BytesWriter.WriteUInt8(ref ptr1, 255);

				Assert.Equal((sbyte) '\r', BytesReader.ReadInt8(ref ptr2));
				Assert.Equal(127,          BytesReader.ReadInt8(ref ptr2));
				Assert.Equal(128,          BytesReader.ReadUInt8(ref ptr2));
				Assert.Equal(255,          BytesReader.ReadUInt8(ref ptr2));
			}
		}

		[Fact]
		private unsafe void Int16Test() {
			var bytes = new byte[8];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteInt16(ref ptr1, 32767);
				BytesWriter.WriteInt16(ref ptr1, 32768);
				BytesWriter.WriteInt16(ref ptr1, short.MinValue);
				BytesWriter.WriteInt16(ref ptr1, -100);

				Assert.Equal(32767,          BytesReader.ReadInt16(ref ptr2));
				Assert.Equal(32768,          BytesReader.ReadUInt16(ref ptr2));
				Assert.Equal(short.MinValue, BytesReader.ReadInt16(ref ptr2));
				Assert.Equal(-100,           BytesReader.ReadInt16(ref ptr2));
			}
		}

		[Fact]
		private unsafe void Int32Test() {
			var bytes = new byte[16];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteInt32(ref ptr1, int.MinValue);
				BytesWriter.WriteInt32(ref ptr1, int.MaxValue);
				BytesWriter.WriteUInt32(ref ptr1, uint.MinValue);
				BytesWriter.WriteUInt32(ref ptr1, uint.MaxValue);

				Assert.Equal(int.MinValue,  BytesReader.ReadInt32(ref ptr2));
				Assert.Equal(int.MaxValue,  BytesReader.ReadInt32(ref ptr2));
				Assert.Equal(uint.MinValue, BytesReader.ReadUInt32(ref ptr2));
				Assert.Equal(uint.MaxValue, BytesReader.ReadUInt32(ref ptr2));
			}
		}

		[Fact]
		private unsafe void Int64Test() {
			var bytes = new byte[32];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteInt64(ref ptr1, long.MinValue);
				BytesWriter.WriteInt64(ref ptr1, long.MaxValue);
				BytesWriter.WriteUInt64(ref ptr1, ulong.MinValue);
				BytesWriter.WriteUInt64(ref ptr1, ulong.MaxValue);

				Assert.Equal(long.MinValue,  BytesReader.ReadInt64(ref ptr2));
				Assert.Equal(long.MaxValue,  BytesReader.ReadInt64(ref ptr2));
				Assert.Equal(ulong.MinValue, BytesReader.ReadUInt64(ref ptr2));
				Assert.Equal(ulong.MaxValue, BytesReader.ReadUInt64(ref ptr2));
			}
		}

		[Fact]
		private unsafe void FloatTest() {
			var bytes = new byte[24];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteFloat(ref ptr1, float.MaxValue);
				BytesWriter.WriteFloat(ref ptr1, float.MinValue);
				BytesWriter.WriteFloat(ref ptr1, float.Epsilon);
				BytesWriter.WriteFloat(ref ptr1, float.NaN);
				BytesWriter.WriteFloat(ref ptr1, float.NegativeInfinity);
				BytesWriter.WriteFloat(ref ptr1, float.PositiveInfinity);

				Assert.Equal(float.MaxValue,         BytesReader.ReadFloat(ref ptr2));
				Assert.Equal(float.MinValue,         BytesReader.ReadFloat(ref ptr2));
				Assert.Equal(float.Epsilon,          BytesReader.ReadFloat(ref ptr2));
				Assert.Equal(float.NaN,              BytesReader.ReadFloat(ref ptr2));
				Assert.Equal(float.NegativeInfinity, BytesReader.ReadFloat(ref ptr2));
				Assert.Equal(float.PositiveInfinity, BytesReader.ReadFloat(ref ptr2));
			}
		}
		
		[Fact]
		private unsafe void DoubleTest() {
			var bytes = new byte[48];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteDouble(ref ptr1, double.MaxValue);
				BytesWriter.WriteDouble(ref ptr1, double.MinValue);
				BytesWriter.WriteDouble(ref ptr1, double.Epsilon);
				BytesWriter.WriteDouble(ref ptr1, double.NaN);
				BytesWriter.WriteDouble(ref ptr1, double.NegativeInfinity);
				BytesWriter.WriteDouble(ref ptr1, double.PositiveInfinity);

				Assert.Equal(double.MaxValue,         BytesReader.ReadDouble(ref ptr2));
				Assert.Equal(double.MinValue,         BytesReader.ReadDouble(ref ptr2));
				Assert.Equal(double.Epsilon,          BytesReader.ReadDouble(ref ptr2));
				Assert.Equal(double.NaN,              BytesReader.ReadDouble(ref ptr2));
				Assert.Equal(double.NegativeInfinity, BytesReader.ReadDouble(ref ptr2));
				Assert.Equal(double.PositiveInfinity, BytesReader.ReadDouble(ref ptr2));
			}
		}

		[Fact]
		private unsafe void StringTest() {
			var bytes = new byte[1024];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteUtf8(ref ptr1, "这是文本1!@$@##$^%^#&fsdffdsaf46541313");
				BytesWriter.WriteUtf8(ref ptr1, "这是文本2");
				BytesWriter.WriteUtf8(ref ptr1, null);
				BytesWriter.WriteUtf8(ref ptr1, string.Empty);
				BytesWriter.WriteUtf8(ref ptr1, "float.NegativeInfinity");
				BytesWriter.WriteUtf8(ref ptr1, "");

				Assert.Equal("这是文本1!@$@##$^%^#&fsdffdsaf46541313", BytesReader.ReadUtf8(ref ptr2));
				Assert.Equal("这是文本2",                              BytesReader.ReadUtf8(ref ptr2));
				Assert.Equal(string.Empty,                         BytesReader.ReadUtf8(ref ptr2));
				Assert.Equal(string.Empty,                         BytesReader.ReadUtf8(ref ptr2));
				Assert.Equal("float.NegativeInfinity",             BytesReader.ReadUtf8(ref ptr2));
				Assert.Equal("",                                   BytesReader.ReadUtf8(ref ptr2));
			}
		}

		[Fact]
		private unsafe void SyntheticTest() {
			var bytes = new byte[1024];
			fixed (byte* bytePtr = bytes) {
				var ptr1 = bytePtr;
				var ptr2 = bytePtr;

				BytesWriter.WriteInt8(ref ptr1, sbyte.MaxValue);
				BytesWriter.WriteInt16(ref ptr1, short.MaxValue);
				BytesWriter.WriteInt32(ref ptr1, int.MaxValue);
				BytesWriter.WriteInt64(ref ptr1, long.MaxValue);
				BytesWriter.WriteFloat(ref ptr1, float.MaxValue);
				BytesWriter.WriteDouble(ref ptr1, double.MaxValue);
				BytesWriter.WriteUtf8(ref ptr1, "this is text");

				Assert.Equal(sbyte.MaxValue,  BytesReader.ReadInt8(ref ptr2));
				Assert.Equal(short.MaxValue,  BytesReader.ReadInt16(ref ptr2));
				Assert.Equal(int.MaxValue,    BytesReader.ReadInt32(ref ptr2));
				Assert.Equal(long.MaxValue,   BytesReader.ReadInt64(ref ptr2));
				Assert.Equal(float.MaxValue,  BytesReader.ReadFloat(ref ptr2));
				Assert.Equal(double.MaxValue, BytesReader.ReadDouble(ref ptr2));
				Assert.Equal("this is text",  BytesReader.ReadUtf8(ref ptr2));
			}
		}

		[Fact]
		private unsafe void VarUInt32Test() {
			var pcg = Random.Shared;

			var bytes = new byte[1024];
			fixed (byte* bytePtr = bytes) {
				var numbers = new uint [10];
				for (var i = 0; i < 100000; i++) {
					var ptr1 = bytePtr;
					var ptr2 = bytePtr;
					for (var j = 0; j < 10; j++) BytesWriter.WriteVarUInt32(ref ptr1, numbers[j] = pcg.NextUInt(0, uint.MaxValue));
					for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], BytesReader.ReadVarUInt32(ref ptr2));
				}
			}
		}

		[Fact]
		private unsafe void VarInt32Test() {
			var pcg = Random.Shared;

			var bytes = new byte[1024];
			fixed (byte* bytePtr = bytes) {
				var numbers = new int [10];
				for (var i = 0; i < 100000; i++) {
					var ptr1 = bytePtr;
					var ptr2 = bytePtr;
					for (var j = 0; j < 10; j++) BytesWriter.WriteVarInt32(ref ptr1, numbers[j] = pcg.Next(int.MinValue, int.MaxValue));
					for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], BytesReader.ReadVarInt32(ref ptr2));
				}
			}
		}

		[Fact]
		private unsafe void VarUInt64Test() {
			var pcg = Random.Shared;

			var bytes = new byte[1024];
			fixed (byte* bytePtr = bytes) {
				var numbers = new ulong [10];
				for (var i = 0; i < 100000; i++) {
					var ptr1 = bytePtr;
					var ptr2 = bytePtr;
					for (var j = 0; j < 10; j++) {
						var n1 = pcg.NextUInt(0, uint.MaxValue);
						var n2 = pcg.NextUInt(0, uint.MaxValue);
						var n  = (ulong) n1 << 32 | n2;
						BytesWriter.WriteVarUInt64(ref ptr1, numbers[j] = n);
					}
					for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], BytesReader.ReadVarUInt64(ref ptr2));
				}
			}
		}

		[Fact]
		private unsafe void VarInt64Test() {
			var pcg = Random.Shared;

			var bytes = new byte[1024];
			fixed (byte* bytePtr = bytes) {
				var numbers = new long [10];
				for (var i = 0; i < 100000; i++) {
					var ptr1 = bytePtr;
					var ptr2 = bytePtr;
					for (var j = 0; j < 10; j++) {
						var n1 = pcg.NextUInt(0, uint.MaxValue);
						var n2 = pcg.NextUInt(0, uint.MaxValue);
						var n  = (long) n1 << 32 | n2;
						BytesWriter.WriteVarInt64(ref ptr1, numbers[j] = n);
					}
					for (var j = 0; j < 10; j++) Assert.Equal(numbers[j], BytesReader.ReadVarInt64(ref ptr2));
				}
			}
		}
	}
}