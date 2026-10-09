using System;
using System.Buffers.Binary;
using Achieve.MaskedValues;

public struct PlayerProfile
{
    public string name;
    public int level;
}

// Explicit field handling without member discovery or automatic serialization.
public sealed class PlayerProfileCodec : ISpanValueCodec<PlayerProfile>
{
    public static readonly PlayerProfileCodec Instance = new PlayerProfileCodec();

    public int GetByteCount(in PlayerProfile value)
    {
        return checked(8 + (value.name == null ? 0 : value.name.Length * 2));
    }

    public void Encode(in PlayerProfile value, Span<byte> destination)
    {
        BinaryPrimitives.WriteInt32LittleEndian(destination, value.level);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(4),
            value.name == null ? -1 : value.name.Length);
        if (value.name == null)
            return;
        for (int i = 0; i < value.name.Length; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(8 + i * 2), value.name[i]);
    }

    public PlayerProfile Decode(ReadOnlySpan<byte> source)
    {
        if (source.Length < 8)
            throw new ArgumentException("Invalid profile length.", nameof(source));
        int length = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4));
        if (length < -1 || (length == -1 && source.Length != 8) ||
            (length >= 0 && length != (source.Length - 8) / 2) || ((source.Length - 8) & 1) != 0)
            throw new ArgumentException("Invalid profile name length.", nameof(source));

        string name = null;
        if (length >= 0)
        {
            Span<char> characters = length <= 256 ? stackalloc char[length] : new char[length];
            try
            {
                for (int i = 0; i < length; i++)
                    characters[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(8 + i * 2));
                name = new string(characters);
            }
            finally
            {
                characters.Clear();
            }
        }
        return new PlayerProfile { name = name, level = BinaryPrimitives.ReadInt32LittleEndian(source) };
    }
}
