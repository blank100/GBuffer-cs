using System;

namespace Serialize.Test
{
    public static class RandomExtension
    {
        public static uint NextUInt(this Random self, uint minValue, uint maxValue) => minValue + (uint)((maxValue - minValue) * self.NextDouble());
    }
}