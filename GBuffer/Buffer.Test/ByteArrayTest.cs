using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class ByteArrayTest {
		[Fact]
		public void WriteReadTest() {
			using var buffer = new Buffer<byte>(1024);

			//int8测试
			{
				buffer.WriteInt8((sbyte) '\r');
				buffer.WriteInt8(127);
				buffer.WriteUInt8(128);
				buffer.WriteUInt8(255);

				buffer.position = 0;
				Assert.Equal((sbyte) '\r', buffer.ReadInt8());
				Assert.Equal(127,          buffer.ReadInt8());
				Assert.Equal(128,          buffer.ReadUInt8());
				Assert.Equal(255,          buffer.ReadUInt8());
				Assert.Equal(1024,         buffer.capacity);
			}

			//int16测试
			{
				buffer.position = 0;
				buffer.WriteInt16(32767);
				buffer.WriteUInt16(32768);

				buffer.position = 0;
				Assert.Equal(32767, buffer.ReadInt16());
				Assert.Equal(32768, buffer.ReadUInt16());
				Assert.Equal(1024,  buffer.capacity);
			}

			//int测试
			{
				buffer.length = 0;
				Assert.Equal(0, buffer.length);
				Assert.Equal(0, buffer.position);

				buffer.WriteInt32(int.MaxValue);
				buffer.WriteUInt32(uint.MaxValue);

				buffer.position = 0;
				Assert.Equal(int.MaxValue,  buffer.ReadInt32());
				Assert.Equal(uint.MaxValue, buffer.ReadUInt32());
				Assert.Equal(1024,          buffer.capacity);
			}

			//int64测试
			{
				buffer.position = 0;
				buffer.WriteInt64(long.MaxValue);
				buffer.WriteInt64(long.MinValue);
				buffer.WriteUInt64(ulong.MaxValue);
				buffer.WriteUInt64(ulong.MinValue);

				buffer.position = 0;
				Assert.Equal(long.MaxValue,  buffer.ReadInt64());
				Assert.Equal(long.MinValue,  buffer.ReadInt64());
				Assert.Equal(ulong.MaxValue, buffer.ReadUInt64());
				Assert.Equal(ulong.MinValue, buffer.ReadUInt64());
				Assert.Equal(1024,           buffer.capacity);
			}

			//文本测试
			{
				const string text1 = "this is string";
				const string text2 = "这是文本";

				buffer.length = 0;
				buffer.WriteUtf8(text1);
				buffer.WriteUtf8(text2);

				buffer.position = 0;
				Assert.Equal(text1, buffer.ReadUtf8());
				Assert.Equal(text2, buffer.ReadUtf8());
				Assert.Equal(1024,  buffer.capacity);
			}

			//float测试
			{
				const float float1 = float.MaxValue;
				const float float2 = float.MinValue;
				const float float3 = float.Epsilon;
				const float float4 = float.NaN;
				const float float5 = float.NegativeInfinity;
				const float float6 = float.PositiveInfinity;

				buffer.position = 0;
				buffer.WriteFloat(float1);
				buffer.WriteFloat(float2);
				buffer.WriteFloat(float3);
				buffer.WriteFloat(float4);
				buffer.WriteFloat(float5);
				buffer.WriteFloat(float6);

				buffer.position = 0;
				Assert.Equal(float1, buffer.ReadFloat());
				Assert.Equal(float2, buffer.ReadFloat());
				Assert.Equal(float3, buffer.ReadFloat());
				Assert.Equal(float4, buffer.ReadFloat());
				Assert.Equal(float5, buffer.ReadFloat());
				Assert.Equal(float6, buffer.ReadFloat());
				Assert.Equal(1024,   buffer.capacity);
			}

			//double测试
			{
				const double v1 = double.MaxValue;
				const double v2 = double.MinValue;
				const double v3 = double.Epsilon;
				const double v4 = double.NaN;
				const double v5 = double.NegativeInfinity;
				const double v6 = double.PositiveInfinity;

				buffer.position = 0;
				buffer.WriteDouble(v1);
				buffer.WriteDouble(v2);
				buffer.WriteDouble(v3);
				buffer.WriteDouble(v4);
				buffer.WriteDouble(v5);
				buffer.WriteDouble(v6);

				buffer.position = 0;
				Assert.Equal(v1,   buffer.ReadDouble());
				Assert.Equal(v2,   buffer.ReadDouble());
				Assert.Equal(v3,   buffer.ReadDouble());
				Assert.Equal(v4,   buffer.ReadDouble());
				Assert.Equal(v5,   buffer.ReadDouble());
				Assert.Equal(v6,   buffer.ReadDouble());
				Assert.Equal(1024, buffer.capacity);
			}

			//综合测试
			{
				buffer.position = 0;
				for (var i = 0; i < 10; i++) {
					buffer.WriteInt8((sbyte) i);
				}
				buffer.WriteInt8(sbyte.MaxValue);
				buffer.WriteInt16(short.MaxValue);
				buffer.WriteInt32(int.MaxValue);
				buffer.WriteInt64(long.MaxValue);
				buffer.WriteFloat(float.MaxValue);
				buffer.WriteDouble(double.MaxValue);
				buffer.WriteUtf8("this is text");

				buffer.position = 10;
				buffer.Discard();
				Assert.Equal(sbyte.MaxValue,  buffer.ReadInt8());
				Assert.Equal(short.MaxValue,  buffer.ReadInt16());
				Assert.Equal(int.MaxValue,    buffer.ReadInt32());
				Assert.Equal(long.MaxValue,   buffer.ReadInt64());
				Assert.Equal(float.MaxValue,  buffer.ReadFloat());
				Assert.Equal(double.MaxValue, buffer.ReadDouble());
				Assert.Equal("this is text",  buffer.ReadUtf8());
				Assert.Equal(1024,            buffer.capacity);
			}

			//varuint32测试
			{
				var pcg = Random.Shared;
				for (var i = 0; i < 100000; i++) {
					var numbers = new ulong [10];

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						var number = pcg.NextUInt(0, uint.MaxValue);
						buffer.WriteVarUInt32(number);
						numbers[j] = number;
					}

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						Assert.Equal(numbers[j], buffer.ReadVarUInt32());
					}
				}
			}

			//varint32测试
			{
				var pcg = Random.Shared;
				for (var i = 0; i < 100000; i++) {
					var numbers = new int [10];

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						var number = pcg.Next(0, int.MaxValue);
						buffer.WriteVarInt32(number);
						numbers[j] = number;
					}

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						Assert.Equal(numbers[j], buffer.ReadVarInt32());
					}
				}
			}

			//varuint64测试
			{
				var pcg = Random.Shared;
				for (var i = 0; i < 100000; i++) {
					var numbers = new ulong [10];

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						var number1 = pcg.NextUInt(0, uint.MaxValue);
						var number2 = pcg.NextUInt(0, uint.MaxValue);

						var number = (ulong) number1 << 32 | (ulong) number2;

						buffer.WriteVarUInt64(number);
						numbers[j] = number;
					}

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						Assert.Equal(numbers[j], buffer.ReadVarUInt64());
					}
				}
			}

			//varint64测试
			{
				var pcg = Random.Shared;
				for (var i = 0; i < 100000; i++) {
					var numbers = new long [10];

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						var number1 = pcg.NextUInt(0, uint.MaxValue);
						var number2 = pcg.NextUInt(0, uint.MaxValue);

						var number = (long) number1 << 32 | (long) number2;

						buffer.WriteVarInt64(number);
						numbers[j] = number;
					}

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						Assert.Equal(numbers[j], buffer.ReadVarInt64());
					}
				}
			}
			
			//grow测试
			{
				buffer.length = 0;
				for (var i = 0; i < 1024; i++) {
					buffer.Write(0b00001);
				}
				Assert.Equal(1024, buffer.capacity);
				for (var i = 0; i < 1024; i++) {
					buffer.Write(0b00001);
				}
				Assert.Equal(2048, buffer.capacity);
			}
			
			//varuint32测试
			{
				var pcg = Random.Shared;
				for (var i = 0; i < 10; i++) {
					var numbers = new ulong [10000];

					buffer.position = 0;
					for (var j = 0; j < 10000; j++) {
						var number = pcg.NextUInt(0, uint.MaxValue);
						buffer.WriteVarUInt32(number);
						numbers[j] = number;
					}

					buffer.position = 0;
					for (var j = 0; j < 10; j++) {
						Assert.Equal(numbers[j], buffer.ReadVarUInt32());
					}
				}
			}
		}
	}
}