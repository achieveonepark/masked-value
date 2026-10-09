using System;
using System.Buffers;

namespace Achieve.MaskedValues
{
    /// <summary>
    /// Reference-containing structs/classes use an explicit span codec.
    /// Assignment allocates masked storage. Decoding may allocate in the codec.
    /// </summary>
    public struct MaskedData<T>
    {
        private const int StackBufferLimit = 512;
        private byte[] storage;
        private ulong key;
        private ISpanValueCodec<T> codec;

        public MaskedData(T value, ISpanValueCodec<T> codec)
        {
            if (codec == null)
                throw new ArgumentNullException(nameof(codec));
            storage = null;
            key = 0;
            this.codec = codec;
            Value = value;
        }

        public bool IsInitialized => key != 0;
        public int ByteCount => storage == null ? 0 : storage.Length;

        public T Value
        {
            get
            {
                if (key == 0)
                    return default;

                byte[] rented = null;
                Span<byte> plaintext = storage.Length <= StackBufferLimit
                    ? stackalloc byte[storage.Length]
                    : (rented = ArrayPool<byte>.Shared.Rent(storage.Length)).AsSpan(0, storage.Length);
                try
                {
                    CopyTo(plaintext);
                    return codec.Decode(plaintext);
                }
                finally
                {
                    plaintext.Clear();
                    if (rented != null)
                        ArrayPool<byte>.Shared.Return(rented, clearArray: true);
                }
            }
            set
            {
                if (codec == null)
                    throw new InvalidOperationException("Create MaskedData with a codec before assigning a value.");

                int count = codec.GetByteCount(in value);
                if (count < 0)
                    throw new InvalidOperationException("The codec returned a negative byte count.");
                byte[] nextStorage = new byte[count];
                ulong nextKey = MaskKeys.Next(key);
                try
                {
                    codec.Encode(in value, nextStorage);
                    ulong mask = nextKey;
                    for (int i = 0; i < nextStorage.Length; i++)
                    {
                        nextStorage[i] ^= (byte)mask;
                        mask = MaskKeys.Rotate(mask);
                    }
                }
                catch
                {
                    Array.Clear(nextStorage, 0, nextStorage.Length);
                    throw;
                }
                storage = nextStorage;
                key = nextKey;
            }
        }

        public void CopyTo(Span<byte> destination)
        {
            if (destination.Length < ByteCount)
                throw new ArgumentException("The destination is too short.", nameof(destination));
            if (storage == null)
                return;

            ulong mask = key;
            for (int i = 0; i < storage.Length; i++)
            {
                destination[i] = (byte)(storage[i] ^ (byte)mask);
                mask = MaskKeys.Rotate(mask);
            }
        }

        public override string ToString() => "<masked data>";
    }
}
