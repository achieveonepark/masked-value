using System;

namespace Achieve.MaskedValues
{
    /// <summary>
    /// An explicit serializer for values containing references. Implementations
    /// must not retain the spans. No automatic field discovery is performed.
    /// </summary>
    public interface ISpanValueCodec<T>
    {
        int GetByteCount(in T value);
        void Encode(in T value, Span<byte> destination);
        T Decode(ReadOnlySpan<byte> source);
    }
}
