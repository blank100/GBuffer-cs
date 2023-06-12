using System;

namespace Serialize.Test
{
    public static class Random_Extension
    {
        public static uint NextUInt(this Random self, uint minValue, uint maxValue) => minValue + (uint)((maxValue - minValue) * self.NextDouble());
    }
}