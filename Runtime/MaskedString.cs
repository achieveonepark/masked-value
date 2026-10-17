using System;

namespace Achieve.MaskedValues
{
    /// <summary>
    /// Masks UTF-16 code units. Assignment allocates storage; Value allocates the
    /// resulting string. CopyTo reuses caller-owned memory without allocation.
    /// </summary>
    [Serializable]
    public partial struct MaskedString
    {
        [NonSerialized] private ushort[] storage;
#if UNITY_6000_0_OR_NEWER
        [UnityEngine.SerializeField] private string initialValue;
#endif
        [NonSerialized] private ulong key;

        public MaskedString(string value)
        {
#if UNITY_6000_0_OR_NEWER
#if UNITY_EDITOR
            initialValue = value;
#else
            initialValue = default;
#endif
#endif
            storage = null;
            key = 0;
            Value = value;
        }

        public bool IsInitialized => key != 0;
        public bool IsNull => storage == null;
        public int Length => storage == null ? 0 : storage.Length;

        public string Value
        {
            get
            {
                if (storage == null)
                    return null;
                if (storage.Length == 0)
                    return string.Empty;
                return string.Create(storage.Length, this,
                    (destination, state) => state.CopyTo(destination));
            }
            set
            {
                ulong nextKey = MaskKeys.Next(key);
                ushort[] nextStorage = value == null ? null : new ushort[value.Length];
                if (nextStorage != null)
                {
                    ulong mask = nextKey;
                    for (int i = 0; i < value.Length; i++)
                    {
                        nextStorage[i] = (ushort)(value[i] ^ (ushort)mask);
                        mask = (mask >> 16) | (mask << 48);
                    }
                }
                storage = nextStorage;
                key = nextKey;
            }
        }

        public void CopyTo(Span<char> destination)
        {
            if (destination.Length < Length)
                throw new ArgumentException("The destination is too short.", nameof(destination));
            if (storage == null)
                return;

            ulong mask = key;
            for (int i = 0; i < storage.Length; i++)
            {
                destination[i] = (char)(storage[i] ^ (ushort)mask);
                mask = (mask >> 16) | (mask << 48);
            }
        }

        public bool ValueEquals(string value)
        {
            if (storage == null)
                return value == null;
            if (value == null || value.Length != storage.Length)
                return false;

            ulong mask = key;
            int difference = 0;
            for (int i = 0; i < storage.Length; i++)
            {
                difference |= (storage[i] ^ (ushort)mask) ^ value[i];
                mask = (mask >> 16) | (mask << 48);
            }
            return difference == 0;
        }

        public static implicit operator MaskedString(string value) => new MaskedString(value);
        public static implicit operator string(MaskedString value) => value.Value;
        public static MaskedString operator +(MaskedString left, string right)
        {
            left.Value = string.Concat(left.Value, right);
            return left;
        }
        public static MaskedString operator +(string left, MaskedString right)
        {
            right.Value = string.Concat(left, right.Value);
            return right;
        }
        public static MaskedString operator +(MaskedString left, MaskedString right) => left + right.Value;
        public override string ToString() => "<masked string>";
    }
}
