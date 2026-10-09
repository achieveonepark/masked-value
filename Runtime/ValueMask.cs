using System.Runtime.CompilerServices;

namespace Achieve.MaskedValues
{
    internal static unsafe class ValueMask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Apply(byte* source, byte* destination, int count, ulong key)
        {
            // Use native-width XOR only when both addresses are aligned. Packed
            // enclosing structs remain valid on targets requiring aligned loads.
            if (count == 4 && (((ulong)source | (ulong)destination) & 3) == 0)
            {
                *(uint*)destination = *(uint*)source ^ (uint)key;
                return;
            }
            if (count == 8 && (((ulong)source | (ulong)destination) & 7) == 0)
            {
                *(ulong*)destination = *(ulong*)source ^ key;
                return;
            }
            if (count == 2 && (((ulong)source | (ulong)destination) & 1) == 0)
            {
                *(ushort*)destination = (ushort)(*(ushort*)source ^ (ushort)key);
                return;
            }
            if (count == 1)
            {
                *destination = (byte)(*source ^ (byte)key);
                return;
            }
            if ((count & 7) == 0 && (((ulong)source | (ulong)destination) & 7) == 0)
            {
                for (int i = 0; i < count; i += 8)
                    *(ulong*)(destination + i) = *(ulong*)(source + i) ^ key;
                return;
            }

            ulong mask = key;
            for (int i = 0; i < count; i++)
            {
                destination[i] = (byte)(source[i] ^ (byte)mask);
                mask = MaskKeys.Rotate(mask);
            }
        }
    }
}
