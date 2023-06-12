using System;

using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class RefWriterReaderTest {
		[Fact]
		public void UInt32Test() {
			{
				(uint min, uint max)[] list = {
					new(0, byte.MaxValue),
					new(short.MaxValue  - 100, short.MaxValue      + 100),
					new(ushort.MaxValue - 100, ushort.MaxValue     + 100),
					new(int.MaxValue    - 100, (uint) int.MaxValue + 100),
					new(uint.MaxValue   - 100, uint.MaxValue),
				};

				Span<byte> buffer = stackalloc byte[256];

				//Uint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteUInt32(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadUInt32());
						}
					}
				}

				//VarUint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteVarUInt32(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadVarUInt32());
						}
					}
				}
			}
		}

		[Fact]
		public void UInt64Test() {
			{
				(ulong min, ulong max)[] list = {
					new(0, byte.MaxValue),
					new(short.MaxValue  - 100, short.MaxValue        + 100),
					new(ushort.MaxValue - 100, ushort.MaxValue       + 100),
					new(int.MaxValue    - 100, (uint) int.MaxValue   + 100),
					new(uint.MaxValue   - 100, (ulong) uint.MaxValue + 100),
					new(long.MaxValue   - 100, (ulong) long.MaxValue + 100),
					new(ulong.MaxValue  - 100, ulong.MaxValue),
				};

				Span<byte> buffer = stackalloc byte[256];

				//Uint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteUInt64(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadUInt64());
						}
					}
				}

				//VarUint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteVarUInt64(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadVarUInt64());
						}
					}
				}
			}
		}
		
		[Fact]
		public void Int32Test() {
			{
				(int min, int max)[] list = {
					new(0, byte.MaxValue),
					new(short.MaxValue  - 100, short.MaxValue      + 100),
					new(ushort.MaxValue - 100, ushort.MaxValue     + 100),
					new(int.MaxValue    - 100, int.MaxValue),
				};

				Span<byte> buffer = stackalloc byte[256];

				//Uint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteInt32(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadInt32());
						}
					}
				}

				//VarUint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteVarInt32(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadVarInt32());
						}
					}
				}
			}
		}
		
		[Fact]
		public void Int64Test() {
			{
				(long min, long max)[] list = {
					new(0, byte.MaxValue),
					new(short.MaxValue  - 100, short.MaxValue       + 100),
					new(ushort.MaxValue - 100, ushort.MaxValue      + 100),
					new(int.MaxValue    - 100, (uint) int.MaxValue  + 100),
					new(uint.MaxValue   - 100, (long) uint.MaxValue + 100),
					new(long.MaxValue   - 100, long.MaxValue),
				};

				Span<byte> buffer = stackalloc byte[256];

				//Uint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteInt64(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadInt64());
						}
					}
				}

				//VarUint32测试
				foreach (var (min, max) in list) {
					for (var i = min; i < max; i += 10) {
						RefWriter<byte> writer = new(buffer);
						for (var j = i; j < i + 10; j++) {
							writer.WriteVarInt64(j);
						}
						RefReader<byte> reader = new(buffer);
						for (var j = i; j < i + 10; j++) {
							Assert.Equal(j, reader.ReadVarInt64());
						}
					}
				}
			}
		}
	}
}