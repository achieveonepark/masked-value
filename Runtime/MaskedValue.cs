using System.Runtime.CompilerServices;

namespace Achieve.MaskedValues
{
    /// <summary>
    /// Stores an unmanaged value as masked bytes. After first-use initialization,
    /// construction, reading and writing allocate no managed memory.
    /// This is reversible memory obfuscation, not cryptographic encryption.
    /// </summary>
    public unsafe struct MaskedValue<T> where T : unmanaged
    {
        // Never access this storage as a T: masked bools, floats and decimals may
        // have invalid representations. Only the decoded bytes are returned as T.
        private T storage;
        private ulong key;

        public MaskedValue(T value)
        {
            storage = default;
            key = 0;
            Value = value;
        }

        public bool IsInitialized => key != 0;

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (key == 0)
                    return default;

                T result = default;
                byte* destination = (byte*)&result;
                fixed (T* sourceValue = &storage)
                {
                    ValueMask.Apply((byte*)sourceValue, destination, sizeof(T), key);
                }
                return result;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ulong nextKey = MaskKeys.Next(key);
                byte* source = (byte*)&value;
                fixed (T* destinationValue = &storage)
                {
                    ValueMask.Apply(source, (byte*)destinationValue, sizeof(T), nextKey);
                }
                key = nextKey;
            }
        }

        public static implicit operator MaskedValue<T>(T value) => new MaskedValue<T>(value);
        public static implicit operator T(MaskedValue<T> value) => value.Value;

        // Logging a wrapper should not reveal its contents accidentally.
        public override string ToString() => "<masked value>";
    }
}
