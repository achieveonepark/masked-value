using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Achieve.MaskedValues
{
    // These keys obscure values. They are not cryptographic secrets.
    internal static class MaskKeys
    {
        private static long sequence = Stopwatch.GetTimestamp() ^ Environment.TickCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Next(ulong previous)
        {
            unchecked
            {
                if (previous == 0)
                    return Create();

                ulong key = previous;
                key ^= key << 13;
                key ^= key >> 7;
                key ^= key << 17;

                // Zero denotes an uninitialized wrapper. No mask byte is zero.
                return key | 0x0101010101010101UL;
            }
        }

        private static ulong Create()
        {
            unchecked
            {
                ulong key = (ulong)Interlocked.Increment(ref sequence);
                key += 0x9E3779B97F4A7C15UL;
                key = (key ^ (key >> 30)) * 0xBF58476D1CE4E5B9UL;
                key = (key ^ (key >> 27)) * 0x94D049BB133111EBUL;
                return (key ^ (key >> 31)) | 0x0101010101010101UL;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Rotate(ulong key)
        {
            return (key >> 8) | (key << 56);
        }
    }
}
