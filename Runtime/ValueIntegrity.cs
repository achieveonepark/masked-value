using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Achieve.MaskedValues
{
    // A fast, non-cryptographic integrity check. The process-local random seed
    // discourages blind tag recomputation but does not resist a process attacker.
    internal static unsafe class ValueIntegrity
    {
        private const ulong Prime = 0xD6E8FEB86659FD93UL;
        private static readonly ulong seed = CreateSeed();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValid(byte* storage, int count, ulong key, ulong tag)
        {
            if (key != 0)
                return tag == Compute(storage, count, key);

            // default(GuardedValue<T>) is valid only when all fields are zero.
            // Complete erasure/replay cannot be distinguished without external state.
            if (tag != 0)
                return false;
            for (int i = 0; i < count; i++)
                if (storage[i] != 0)
                    return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Compute(byte* storage, int count, ulong key)
        {
            unchecked
            {
                ulong hash = (key ^ seed) * Prime;
                if (count == 4)
                    hash = (hash ^ (((ulong)storage & 3) == 0 ? *(uint*)storage : ReadUnaligned(storage, 4))) * Prime;
                else if (count == 8)
                    hash = (hash ^ (((ulong)storage & 7) == 0 ? *(ulong*)storage : ReadUnaligned(storage, 8))) * Prime;
                else if (count == 2)
                    hash = (hash ^ (((ulong)storage & 1) == 0 ? *(ushort*)storage : ReadUnaligned(storage, 2))) * Prime;
                else if (count == 1)
                    hash = (hash ^ *storage) * Prime;
                else if ((count & 7) == 0)
                {
                    for (int i = 0; i < count; i += 8)
                        hash = (hash ^ (((ulong)storage & 7) == 0
                            ? *(ulong*)(storage + i) : ReadUnaligned(storage + i, 8))) * Prime;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                        hash = (hash ^ storage[i]) * Prime;
                }
                return hash ^ (hash >> 32);
            }
        }

        // Use the same word boundaries on aligned and packed instances so a
        // struct copy remains valid when its new address has different alignment.
        private static ulong ReadUnaligned(byte* source, int count)
        {
            ulong result = 0;
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < count; i++) result |= (ulong)source[i] << (i * 8);
            }
            else
            {
                for (int i = 0; i < count; i++) result = (result << 8) | source[i];
            }
            return result;
        }

        private static ulong CreateSeed()
        {
            Span<byte> bytes = stackalloc byte[8];
            RandomNumberGenerator.Fill(bytes);
            ulong result = 0;
            for (int i = 0; i < bytes.Length; i++)
                result |= (ulong)bytes[i] << (i * 8);
            return result;
        }
    }
}
