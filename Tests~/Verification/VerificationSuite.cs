using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Achieve.MaskedValues;

public static unsafe class MaskedValuesVerificationSuite
{
    private static long sink;
    private static int assertions;
    private const int ReadArrayLength = 1024;
    private static readonly int[] plainReads = new int[ReadArrayLength];
    private static readonly MaskedValue<int>[] maskedReads = new MaskedValue<int>[ReadArrayLength];
    private static readonly GuardedValue<int>[] guardedReads = new GuardedValue<int>[ReadArrayLength];

    public static string Run()
    {
        assertions = 0;
        CheckPrimitives();
        CheckStructsAndCopies();
        CheckStrings();
        CheckExplicitCodec();
        CheckMasking();
        CheckGuardedValues();
        CheckGuardedTampering();
        CheckAllocations();
        for (int i = 0; i < ReadArrayLength; i++)
        {
            plainReads[i] = i;
            maskedReads[i] = i;
            guardedReads[i] = i;
        }

        var report = new StringBuilder();
        report.AppendLine("# Masked Values verification");
        report.AppendLine();
        report.AppendLine("Passed " + assertions + " assertions. No reflection or automatic serialization is used by the package.");
        report.AppendLine("Measured managed allocation after warmup: masked/guarded primitive and struct read-write, guarded refresh and failed TryGetValue, string CopyTo/ValueEquals, and codec CopyTo: 0 bytes.");
        report.AppendLine();
        report.AppendLine("Release, single thread, median of 7 samples, 1,000,000 iterations per sample. These are this machine's measurements, not a zero-overhead guarantee.");
        report.AppendLine();
        report.AppendLine("| Operation | Plain ns/op | Masked ns/op | Guarded ns/op | Guarded - Masked ns/op | Guarded / Masked |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|");
        Benchmark(report, "int read", PlainRead, MaskedRead, GuardedRead);
        Benchmark(report, "int array read (1024 values)", PlainArrayRead, MaskedArrayRead, GuardedArrayRead);
        Benchmark(report, "int write + read", PlainInt, MaskedInt, GuardedInt);
        Benchmark(report, "float write + read", PlainFloat, MaskedFloat, GuardedFloat);
        Benchmark(report, "24-byte struct write + read", PlainStruct, MaskedStruct, GuardedStruct);
        report.AppendLine();
        report.AppendLine("Guarded int RefreshMask: " + Measure(GuardedRefresh).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " ns/op; refresh is explicit, not charged to every read.");
        report.AppendLine((IntPtr.Size * 8) + "-bit wrapper sizes: int plain " + sizeof(int) + " B, masked " + sizeof(MaskedValue<int>) + " B, guarded " + sizeof(GuardedValue<int>) + " B.");
        report.AppendLine();
        report.AppendLine("Read/write costs and wrapper size increase with the struct size. String Value and reference-containing codecs can allocate. Unity Editor/IL2CPP and target devices must be measured separately.");
        return report.ToString();
    }

    private static void CheckPrimitives()
    {
        Bits(true, new MaskedValue<bool>(true).Value);
        Bits(false, new MaskedValue<bool>(false).Value);
        Bits(byte.MaxValue, new MaskedValue<byte>(byte.MaxValue).Value);
        Bits(sbyte.MinValue, new MaskedValue<sbyte>(sbyte.MinValue).Value);
        Bits(short.MinValue, new MaskedValue<short>(short.MinValue).Value);
        Bits(ushort.MaxValue, new MaskedValue<ushort>(ushort.MaxValue).Value);
        Bits(int.MinValue, new MaskedValue<int>(int.MinValue).Value);
        Bits(uint.MaxValue, new MaskedValue<uint>(uint.MaxValue).Value);
        Bits(long.MinValue, new MaskedValue<long>(long.MinValue).Value);
        Bits(ulong.MaxValue, new MaskedValue<ulong>(ulong.MaxValue).Value);
        Bits('\ud800', new MaskedValue<char>('\ud800').Value);
        Bits(decimal.MaxValue, new MaskedValue<decimal>(decimal.MaxValue).Value);
        Bits(decimal.MinValue, new MaskedValue<decimal>(decimal.MinValue).Value);
        Bits(double.NaN, new MaskedValue<double>(double.NaN).Value);
        Bits(double.PositiveInfinity, new MaskedValue<double>(double.PositiveInfinity).Value);
        Bits(-0.0d, new MaskedValue<double>(-0.0d).Value);
        Bits(float.NaN, new MaskedValue<float>(float.NaN).Value);
        Bits(-0.0f, new MaskedValue<float>(-0.0f).Value);
        Bits(TestEnum.Second, new MaskedValue<TestEnum>(TestEnum.Second).Value);
        Bits(new IntPtr(12345), new MaskedValue<IntPtr>(new IntPtr(12345)).Value);
        Bits(0, default(MaskedValue<int>).Value);
        Require(!default(MaskedValue<int>).IsInitialized, "default wrapper state");

        MaskedValue<int> integer = 0;
        MaskedValue<bool> boolean = false;
        MaskedValue<float> floating = 0f;
        uint random = 0xAD38F91Du;
        for (int i = 0; i < 10000; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            integer.Value = unchecked((int)random);
            Bits(unchecked((int)random), integer.Value);
            boolean.Value = (random & 1) == 1;
            Bits((random & 1) == 1, boolean.Value);
            float input = *(float*)&random;
            floating.Value = input;
            Bits(input, floating.Value);
        }
        integer.Value = 100;
        integer += 7;
        Require(integer.Value == 107, "implicit arithmetic assignment");
        Require(integer.ToString() == "<masked value>", "redacted logging");
    }

    private static void CheckStructsAndCopies()
    {
        var source = new Stats { x = 1.5, y = -2.5, z = 17.25 };
        var masked = new MaskedValue<Stats>(source);
        Stats result = masked.Value;
        Require(result.x == source.x && result.y == source.y && result.z == source.z, "struct values");
        var copy = masked;
        copy.Value = new Stats { x = 9, y = 8, z = 7 };
        Require(masked.Value.x == 1.5 && copy.Value.x == 9, "struct copy independence");

        var packed = new PackedStats { enabled = true, number = 789, nested = source };
        var protectedPacked = new MaskedValue<PackedStats>(packed);
        PackedStats packedResult = protectedPacked.Value;
        Require(packedResult.enabled && packedResult.number == 789 && packedResult.nested.z == 17.25,
            "packed nested struct with bool");
        var unaligned = new PackedHolder { prefix = 1, integer = 4321, number = 3.75 };
        Require(unaligned.integer.Value == 4321 && unaligned.number.Value == 3.75,
            "unaligned wrapper in packed enclosing struct");
        var holder = new FieldHolder();
        holder.health = 37;
        holder.stats = masked;
        var array = new MaskedValue<bool>[2];
        array[0] = true;
        array[1] = false;
        Require(holder.health.Value == 37 && holder.stats.Value.y == -2.5 && array[0].Value && !array[1].Value,
            "class fields and arrays");
    }

    private static void CheckStrings()
    {
        string[] cases = { null, "", "hello", "유니티 🐈", "a\0b", "\ud800\udfff\ud800" };
        foreach (string input in cases)
        {
            var value = new MaskedString(input);
            Require(value.Value == input && value.ValueEquals(input), "string roundtrip");
            var copy = value;
            copy.Value = "changed";
            Require(value.ValueEquals(input), "string copy independence");
            Span<char> destination = stackalloc char[value.Length];
            value.CopyTo(destination);
            Require(input == null ? destination.Length == 0 : new string(destination) == input, "string span copy");
        }
        Require(default(MaskedString).Value == null, "default string");
        var text = new MaskedString("abcdef");
        bool threw = false;
        try { text.CopyTo(new char[1]); }
        catch (ArgumentException) { threw = true; }
        Require(threw, "short string buffer rejected");
        Require(!text.ValueEquals("abcdeg") && !text.ValueEquals(null), "string inequality");
    }

    private static void CheckExplicitCodec()
    {
        var input = new PlayerProfile { name = "유니티\ud800", level = 72 };
        var value = new MaskedData<PlayerProfile>(input, PlayerProfileCodec.Instance);
        var result = value.Value;
        Require(result.name == input.name && result.level == input.level, "managed struct codec");
        var copy = value;
        copy.Value = new PlayerProfile { name = null, level = 2 };
        Require(value.Value.name == input.name && copy.Value.name == null && copy.Value.level == 2,
            "managed struct copy independence");
        Span<byte> bytes = stackalloc byte[value.ByteCount];
        value.CopyTo(bytes);
        Require(PlayerProfileCodec.Instance.Decode(bytes).level == 72, "managed struct span copy");
        bool threw = false;
        try { var uninitialized = default(MaskedData<PlayerProfile>); uninitialized.Value = input; }
        catch (InvalidOperationException) { threw = true; }
        Require(threw, "codec required for assignment");

        var big = new MaskedData<PlayerProfile>(new PlayerProfile { name = new string('x', 1000), level = 9 },
            PlayerProfileCodec.Instance);
        Require(big.Value.name.Length == 1000 && big.Value.level == 9, "pooled decode path");
        var failing = new MaskedData<int>(17, new FailingCodec());
        threw = false;
        try { failing.Value = -1; }
        catch (ArgumentException) { threw = true; }
        Require(threw && failing.Value == 17, "failed encoding preserves previous value");
    }

    private static void CheckMasking()
    {
        var value = new MaskedValue<int>(0x12345678);
        byte[] first = Capture(value);
        value.Value = 0x12345678;
        byte[] second = Capture(value);
        bool different = false;
        for (int i = 0; i < first.Length; i++)
            different |= first[i] != second[i];
        Require(different, "reassignment remasks identical value");

        var small = new MaskedValue<bool>(true);
        Require(sizeof(MaskedValue<int>) <= 16, "small wrapper size");
        Require(sizeof(MaskedValue<Stats>) <= sizeof(Stats) + 8, "struct wrapper size");
        // The compiler accepts these wrappers as unmanaged: no hidden object/array references.
        Capture(small);
        Capture(new MaskedValue<decimal>(123.45m));
    }

    private static void CheckGuardedValues()
    {
        GuardedBits(true);
        GuardedBits(false);
        GuardedBits(byte.MaxValue);
        GuardedBits(sbyte.MinValue);
        GuardedBits(short.MinValue);
        GuardedBits(ushort.MaxValue);
        GuardedBits(int.MinValue);
        GuardedBits(uint.MaxValue);
        GuardedBits(long.MinValue);
        GuardedBits(ulong.MaxValue);
        GuardedBits('\ud800');
        GuardedBits(decimal.MaxValue);
        GuardedBits(decimal.MinValue);
        GuardedBits(double.NaN);
        GuardedBits(double.PositiveInfinity);
        GuardedBits(-0.0d);
        GuardedBits(float.NaN);
        GuardedBits(-0.0f);
        GuardedBits(TestEnum.Second);
        GuardedBits(new IntPtr(12345));

        var value = default(GuardedValue<int>);
        Require(!value.IsInitialized && value.CheckIntegrity(), "default guarded wrapper");
        Bits(0, value.Value);
        value.RefreshMask();
        Require(value.IsInitialized && value.Value == 0, "refresh initializes default guarded wrapper");
        value.Value = 100;
        value += 7;
        Require(value.Value == 107 && value.ToString() == "<guarded value>", "guarded arithmetic and logging");

        byte[] before = Capture(value);
        value.RefreshMask();
        Require(value.Value == 107 && Different(before, Capture(value)), "guarded refresh preserves value and changes representation");
        var copy = value;
        copy.Value = 5;
        Require(value.Value == 107 && copy.Value == 5, "guarded copy independence");

        var stats = new Stats { x = 1.5, y = -2.5, z = 17.25 };
        var guardedStats = new GuardedValue<Stats>(stats);
        guardedStats.RefreshMask();
        Stats result = guardedStats.Value;
        Require(result.x == stats.x && result.y == stats.y && result.z == stats.z, "guarded large struct");
        var packed = new GuardedValue<PackedStats>(new PackedStats { enabled = true, number = 789, nested = stats });
        packed.RefreshMask();
        Require(packed.Value.enabled && packed.Value.number == 789 && packed.Value.nested.z == 17.25,
            "guarded odd-sized packed struct");

        var unaligned = new GuardedPackedHolder { prefix = 1, integer = 4321, number = 3.75, stats = guardedStats };
        unaligned.integer.RefreshMask();
        unaligned.number.RefreshMask();
        unaligned.stats.RefreshMask();
        GuardedValue<int> alignedCopy = unaligned.integer;
        GuardedValue<double> alignedDouble = unaligned.number;
        GuardedValue<Stats> alignedStats = unaligned.stats;
        Require(alignedCopy.Value == 4321 && alignedDouble.Value == 3.75 && alignedStats.Value.z == 17.25,
            "guarded packed to aligned copies");
        unaligned.integer = new GuardedValue<int>(71);
        Require(unaligned.integer.Value == 71 && unaligned.number.Value == 3.75, "aligned to packed guarded copies");

        GuardedValue<int> integer = 0;
        GuardedValue<float> floating = 0f;
        uint random = 0xAD38F91Du;
        for (int i = 0; i < 10000; i++)
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            integer.Value = unchecked((int)random);
            Bits(unchecked((int)random), integer.Value);
            float input = *(float*)&random;
            floating.Value = input;
            Bits(input, floating.Value);
        }
        Require(sizeof(GuardedValue<int>) <= 24 && sizeof(GuardedValue<Stats>) <= sizeof(Stats) + 16,
            "guarded wrapper sizes");
    }

    private static void GuardedBits<T>(T input) where T : unmanaged
    {
        var value = new GuardedValue<T>(input);
        Bits(input, value.Value);
        value.RefreshMask();
        Bits(input, value.Value);
        Require(value.CheckIntegrity(), "guarded primitive integrity");
    }

    private static void CheckGuardedTampering()
    {
        var healthy = new GuardedValue<int>(100);
        for (int i = 0; i < 32; i++)
        {
            int stored = (int)GetGuardedField(healthy, "storage");
            var corrupt = SetGuardedField(healthy, "storage", stored ^ (1 << i));
            Require(!corrupt.TryGetValue(out int output) && output == 0, "every storage bit is checked");
        }
        foreach (string field in new[] { "key", "tag" })
        {
            ulong original = (ulong)GetGuardedField(healthy, field);
            for (int i = 0; i < 64; i++)
            {
                var corrupt = SetGuardedField(healthy, field, original ^ (1UL << i));
                Require(!corrupt.CheckIntegrity(), "every " + field + " bit is checked");
            }
        }
        var resetKey = SetGuardedField(healthy, "key", 0UL);
        Require(!resetKey.TryGetValue(out _), "zeroing key cannot bypass integrity");
        var defaultCorrupt = SetGuardedField(default(GuardedValue<int>), "storage", 1);
        Require(!defaultCorrupt.CheckIntegrity(), "default state storage must be zero");
        var invalidBool = new GuardedValue<bool>(true);
        CorruptFirstByte(ref invalidBool);
        Require(!invalidBool.TryGetValue(out bool boolOutput) && !boolOutput, "invalid bool bytes rejected before decoding");
        var invalidDecimal = new GuardedValue<decimal>(1m);
        CorruptFirstByte(ref invalidDecimal);
        Require(!invalidDecimal.TryGetValue(out _), "invalid decimal storage rejected");
        var large = new GuardedValue<Stats>(new Stats { x = 1, y = 2, z = 3 });
        for (int i = 0; i < sizeof(Stats); i++)
        {
            var corrupt = large;
            byte* source = (byte*)&corrupt;
            source[i] ^= 0x80;
            Require(!corrupt.CheckIntegrity(), "all bytes of large storage checked");
        }

        var corruptValue = healthy;
        CorruptFirstByte(ref corruptValue);
        byte[] snapshot = Capture(corruptValue);
        int notifications = 0;
        Action<Type> handler = type => { Require(type == typeof(int), "tampering event value type"); notifications++; };
        GuardedValueDiagnostics.TamperingDetected += handler;
        try
        {
            Require(!corruptValue.TryGetValue(out _), "corrupt TryGetValue rejected");
            Require(!corruptValue.TrySetValue(7), "corrupt TrySetValue rejected");
            Require(!corruptValue.TryRefreshMask(), "corrupt TryRefreshMask rejected");
            bool threw = false;
            try { sink = corruptValue.Value; }
            catch (InvalidOperationException) { threw = true; }
            Require(threw, "corrupt getter throws");
            threw = false;
            try { corruptValue.Value = 9; }
            catch (InvalidOperationException) { threw = true; }
            Require(threw, "corrupt setter throws instead of repairing corruption");
            threw = false;
            try { corruptValue.RefreshMask(); }
            catch (InvalidOperationException) { threw = true; }
            Require(threw, "corrupt remask throws instead of repairing corruption");
            Require(notifications == 6, "one tampering event per failed operation");
            Require(!Different(snapshot, Capture(corruptValue)), "failed operations preserve corrupt bytes");
            Require(healthy.Value == 100 && notifications == 6, "successful access does not raise event");
        }
        finally { GuardedValueDiagnostics.TamperingDetected -= handler; }

        // Documented boundary: a full valid snapshot or all-zero reset is valid.
        var oldSnapshot = healthy;
        healthy.Value = 90;
        healthy = oldSnapshot;
        Require(healthy.Value == 100, "complete snapshot replay requires external protection");
        healthy = default;
        Require(healthy.Value == 0, "complete erasure is indistinguishable from default");
    }

    private static object GetGuardedField<T>(GuardedValue<T> value, string name) where T : unmanaged =>
        typeof(GuardedValue<T>).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(value);

    private static GuardedValue<T> SetGuardedField<T>(GuardedValue<T> value, string name, object replacement) where T : unmanaged
    {
        // Reflection is only a test fixture for deliberate memory corruption.
        object boxed = value;
        typeof(GuardedValue<T>).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(boxed, replacement);
        return (GuardedValue<T>)boxed;
    }

    private static void CorruptFirstByte<T>(ref T value) where T : unmanaged
    {
        fixed (T* location = &value) *(byte*)location ^= 0x80;
    }

    private static bool Different(byte[] first, byte[] second)
    {
        for (int i = 0; i < first.Length; i++)
            if (first[i] != second[i]) return true;
        return false;
    }

    private static void CheckAllocations()
    {
        MaskedInt(20000);
        MaskedFloat(20000);
        MaskedStruct(20000);
        long before = GC.GetAllocatedBytesForCurrentThread();
        MaskedInt(100000);
        MaskedFloat(100000);
        MaskedStruct(100000);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, "primitive/struct managed allocations: " + allocated);

        GuardedInt(20000);
        GuardedFloat(20000);
        GuardedStruct(20000);
        GuardedRefresh(20000);
        before = GC.GetAllocatedBytesForCurrentThread();
        GuardedInt(100000);
        GuardedFloat(100000);
        GuardedStruct(100000);
        GuardedRefresh(100000);
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, "guarded read/write/refresh managed allocations: " + allocated);

        var corrupt = new GuardedValue<int>(17);
        CorruptFirstByte(ref corrupt);
        corrupt.TryGetValue(out _);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
            if (corrupt.TryGetValue(out _)) throw new Exception("Corruption was accepted.");
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, "failed guarded TryGetValue managed allocations: " + allocated);

        var text = new MaskedString("zero allocation");
        Span<char> chars = stackalloc char[text.Length];
        text.CopyTo(chars);
        text.ValueEquals("zero allocation");
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
        {
            text.CopyTo(chars);
            if (!text.ValueEquals("zero allocation")) throw new Exception("string changed");
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, "string copy managed allocations: " + allocated);

        var data = new MaskedData<PlayerProfile>(new PlayerProfile { name = "test", level = 1 }, PlayerProfileCodec.Instance);
        Span<byte> bytes = stackalloc byte[data.ByteCount];
        data.CopyTo(bytes);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) data.CopyTo(bytes);
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0, "codec copy managed allocations: " + allocated);
    }

    private static void Benchmark(StringBuilder report, string name, Action<int> plain, Action<int> masked, Action<int> guarded)
    {
        double plainNs = Measure(plain);
        double maskedNs = Measure(masked);
        double guardedNs = Measure(guarded);
        report.AppendLine("| " + name + " | " + plainNs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
            " | " + maskedNs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " | " +
            guardedNs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " | " +
            (guardedNs - maskedNs).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " | " +
            (guardedNs / maskedNs).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "x |");
    }

    private static double Measure(Action<int> action)
    {
        const int count = 1000000;
        action(50000);
        var samples = new double[7];
        for (int i = 0; i < samples.Length; i++)
        {
            long started = Stopwatch.GetTimestamp();
            action(count);
            samples[i] = (Stopwatch.GetTimestamp() - started) * (1000000000.0 / Stopwatch.Frequency) / count;
        }
        Array.Sort(samples);
        return samples[3];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PlainRead(int count)
    {
        var holder = new PlainHolder { integer = 101 };
        long sum = 0;
        for (int i = 0; i < count; i++) sum += holder.integer;
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MaskedRead(int count)
    {
        var holder = new FieldHolder { health = 101 };
        long sum = 0;
        for (int i = 0; i < count; i++) sum += holder.health.Value;
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PlainInt(int count)
    {
        int value = 0;
        long sum = 0;
        for (int i = 0; i < count; i++) { value = i; sum += value; }
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MaskedInt(int count)
    {
        MaskedValue<int> value = 0;
        long sum = 0;
        for (int i = 0; i < count; i++) { value.Value = i; sum += value.Value; }
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PlainFloat(int count)
    {
        float value = 0;
        float sum = 0;
        for (int i = 0; i < count; i++) { value = i * 0.25f; sum += value; }
        sink = (long)sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MaskedFloat(int count)
    {
        MaskedValue<float> value = 0f;
        float sum = 0;
        for (int i = 0; i < count; i++) { value.Value = i * 0.25f; sum += value.Value; }
        sink = (long)sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PlainStruct(int count)
    {
        var value = new Stats();
        double sum = 0;
        for (int i = 0; i < count; i++) { value = new Stats { x = i, y = i + 1, z = i + 2 }; sum += value.x; }
        sink = (long)sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MaskedStruct(int count)
    {
        var value = new MaskedValue<Stats>();
        double sum = 0;
        for (int i = 0; i < count; i++) { value.Value = new Stats { x = i, y = i + 1, z = i + 2 }; sum += value.Value.x; }
        sink = (long)sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GuardedRead(int count)
    {
        var holder = new GuardedHolder { health = 101 };
        long sum = 0;
        for (int i = 0; i < count; i++) sum += holder.health.Value;
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PlainArrayRead(int count)
    {
        long sum = 0;
        for (int i = 0; i < count; i++) sum += plainReads[i & (ReadArrayLength - 1)];
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MaskedArrayRead(int count)
    {
        long sum = 0;
        for (int i = 0; i < count; i++) sum += maskedReads[i & (ReadArrayLength - 1)].Value;
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GuardedArrayRead(int count)
    {
        long sum = 0;
        for (int i = 0; i < count; i++) sum += guardedReads[i & (ReadArrayLength - 1)].Value;
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GuardedInt(int count)
    {
        GuardedValue<int> value = 0;
        long sum = 0;
        for (int i = 0; i < count; i++) { value.Value = i; sum += value.Value; }
        sink = sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GuardedFloat(int count)
    {
        GuardedValue<float> value = 0f;
        float sum = 0;
        for (int i = 0; i < count; i++) { value.Value = i * 0.25f; sum += value.Value; }
        sink = (long)sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GuardedStruct(int count)
    {
        var value = new GuardedValue<Stats>();
        double sum = 0;
        for (int i = 0; i < count; i++) { value.Value = new Stats { x = i, y = i + 1, z = i + 2 }; sum += value.Value.x; }
        sink = (long)sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GuardedRefresh(int count)
    {
        GuardedValue<int> value = 100;
        for (int i = 0; i < count; i++) value.RefreshMask();
        sink = value.Value;
    }

    private static void Bits<T>(T expected, T actual) where T : unmanaged
    {
        byte* left = (byte*)&expected;
        byte* right = (byte*)&actual;
        for (int i = 0; i < sizeof(T); i++)
            Require(left[i] == right[i], "bit-preserving primitive roundtrip");
    }

    private static byte[] Capture<T>(T value) where T : unmanaged
    {
        var result = new byte[sizeof(T)];
        byte* source = (byte*)&value;
        for (int i = 0; i < result.Length; i++) result[i] = source[i];
        return result;
    }

    private static void Require(bool condition, string name)
    {
        assertions++;
        if (!condition) throw new Exception("Verification failed: " + name);
    }

    private enum TestEnum : long { First = 0, Second = long.MaxValue }
    private struct Stats { public double x, y, z; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    private struct PackedStats { public bool enabled; public int number; public Stats nested; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    private struct PackedHolder { public byte prefix; public MaskedValue<int> integer; public MaskedValue<double> number; }
    private sealed class FieldHolder { public MaskedValue<int> health; public MaskedValue<Stats> stats; }
    private sealed class PlainHolder { public int integer; }
    private sealed class GuardedHolder { public GuardedValue<int> health; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    private struct GuardedPackedHolder { public byte prefix; public GuardedValue<int> integer; public GuardedValue<double> number; public GuardedValue<Stats> stats; }

    private sealed class FailingCodec : ISpanValueCodec<int>
    {
        public int GetByteCount(in int value) => 4;
        public void Encode(in int value, Span<byte> destination)
        {
            if (value == -1) throw new ArgumentException("Intentional codec failure.");
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(destination, value);
        }
        public int Decode(ReadOnlySpan<byte> source) => System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(source);
    }
}
