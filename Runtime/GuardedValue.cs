using System;
using System.Runtime.CompilerServices;

namespace Achieve.MaskedValues
{
    /// <summary>
    /// Masks an unmanaged value and checks a lightweight integrity tag before
    /// reading, writing or remasking. This detects ordinary memory overwrites;
    /// it is not cryptographic authentication or protection against code patches.
    /// Like MaskedValue, instances require external synchronization across threads.
    /// </summary>
    public unsafe struct GuardedValue<T> where T : unmanaged
    {
        // Access masked storage only as bytes: it can be an invalid T representation.
        private T storage;
        private ulong key;
        private ulong tag;

        public GuardedValue(T value)
        {
            storage = default;
            key = 0;
            tag = 0;
            Store(value);
        }

        public bool IsInitialized => key != 0;

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!CheckIntegrity())
                    GuardedValueDiagnostics.Throw();
                return Read();
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (!TrySetValue(value))
                    GuardedValueDiagnostics.Throw();
            }
        }

        /// <summary>Checks storage and key without returning plaintext.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckIntegrity()
        {
            fixed (T* source = &storage)
            {
                if (ValueIntegrity.IsValid((byte*)source, sizeof(T), key, tag))
                    return true;
            }
            GuardedValueDiagnostics.Report(typeof(T));
            return false;
        }

        /// <summary>Returns false and default(T) on corruption; does not throw itself.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(out T value)
        {
            value = default;
            if (!CheckIntegrity())
                return false;
            value = Read();
            return true;
        }

        /// <summary>Rejects a corrupt previous state instead of legitimizing it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetValue(T value)
        {
            if (!CheckIntegrity())
                return false;
            Store(value);
            return true;
        }

        /// <summary>Changes the stored representation while preserving the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RefreshMask()
        {
            if (!TryRefreshMask())
                GuardedValueDiagnostics.Throw();
        }

        /// <summary>Checks integrity before remasking, without decoding into a T.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRefreshMask()
        {
            if (!CheckIntegrity())
                return false;

            ulong nextKey = MaskKeys.Next(key);
            fixed (T* source = &storage)
            {
                // XOR old and new masks directly; no plaintext temporary is needed.
                ValueMask.Apply((byte*)source, (byte*)source, sizeof(T), key ^ nextKey);
                tag = ValueIntegrity.Compute((byte*)source, sizeof(T), nextKey);
            }
            key = nextKey;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Read()
        {
            if (key == 0)
                return default;

            T value = default;
            byte* destination = (byte*)&value;
            fixed (T* source = &storage)
            {
                ValueMask.Apply((byte*)source, destination, sizeof(T), key);
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Store(T value)
        {
            ulong nextKey = MaskKeys.Next(key);
            byte* source = (byte*)&value;
            fixed (T* destination = &storage)
            {
                ValueMask.Apply(source, (byte*)destination, sizeof(T), nextKey);
                tag = ValueIntegrity.Compute((byte*)destination, sizeof(T), nextKey);
            }
            key = nextKey;
        }

        public static implicit operator GuardedValue<T>(T value) => new GuardedValue<T>(value);
        public static implicit operator T(GuardedValue<T> value) => value.Value;
        public override string ToString() => "<guarded value>";
    }

    /// <summary>Optional notification for each failed check. No event runs on success.</summary>
    public static class GuardedValueDiagnostics
    {
        /// <summary>
        /// Reports the value type, never plaintext. Handlers should not throw or
        /// access the same corrupted value. Try methods can propagate handler exceptions.
        /// </summary>
        public static event Action<Type> TamperingDetected;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Report(Type valueType) => TamperingDetected?.Invoke(valueType);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Throw() => throw new InvalidOperationException("Guarded value integrity check failed.");
    }
}
