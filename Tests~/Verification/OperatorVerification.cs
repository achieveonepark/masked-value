using System;
using System.Reflection;
using Achieve.MaskedValues;

internal static class OperatorVerification
{
    private static int sink;
    internal static void Run()
    {
        Check0();
        Check1();
        Check2();
        Check3();
        Check4();
        Check5();
        Check6();
        Check7();
        Check8();
        Check9();
        Check10();
        Check11();
        MaskedValue<int> masked = 12; GuardedValue<int> guarded = 3;
        Require((masked + guarded).Value == 15 && (guarded + masked).Value == 15, "Mixed wrapper addition");
        Require((masked - guarded).Value == 9 && (guarded - masked).Value == -9, "Mixed wrapper subtraction");
        Require((masked * guarded).Value == 36 && (masked / guarded).Value == 4 && (masked % guarded).Value == 0, "Mixed wrapper arithmetic");
        Require(masked > guarded && masked != guarded && guarded <= 3 && masked >= 12, "Implicit comparisons");
        Require((masked & 7) == 4 && (masked | 1) == 13 && (masked ^ 4) == 8 && (masked << 1) == 24 && (masked >> 1) == 6, "Integral bitwise operations");
        MaskedString a = "A", b = "B"; a += b;
        Require(a.Value == "AB" && ("C" + b).Value == "CB" && (b + "D").Value == "BD", "Masked string concatenation decodes values");
        GuardedValue<int> zero = 0;
        Throws<DivideByZeroException>(() => { var unused = guarded / zero; }, "Integer division by zero");
        GuardedValue<float> floating = 1f;
        Require(float.IsPositiveInfinity((floating / 0f).Value), "Floating division preserves IEEE behavior");
        GuardedValue<byte> small = 255; small++; Require(small.Value == 0, "Narrow integral overflow wraps");
        GuardedValue<decimal> money = decimal.MaxValue;
        Throws<OverflowException>(() => { var unused = money + 1m; }, "Decimal overflow still throws");
        object corrupt = new GuardedValue<int>(100);
        typeof(GuardedValue<int>).GetField("storage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(corrupt, 123456);
        GuardedValue<int> bad = (GuardedValue<int>)corrupt;
        Throws<InvalidOperationException>(() => { var unused = bad + 1; }, "Operator must reject corrupt left operand");
        Throws<InvalidOperationException>(() => { var unused = 1 + bad; }, "Operator must reject corrupt right operand");
        Throws<InvalidOperationException>(() => { bad++; }, "Increment must reject corruption");
        var unsupported = new MaskedValue<NonNumeric>(new NonNumeric { field = 1 });
        Throws<NotSupportedException>(() => { var unused = unsupported + unsupported; }, "Unsupported custom arithmetic is explicit");
        Work(1000);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Work(10000);
        Require(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Warm numeric operators allocate zero managed bytes");
        Console.WriteLine("Operator verification passed: all 12 numeric types, arithmetic/compound/unary/increment, mixed wrappers, string concatenation, corruption and zero warm allocations.");
    }
    private static void Check0()
    {
        MaskedValue<byte> m = (byte)12, n = (byte)3;
        GuardedValue<byte> g = (byte)12, h = (byte)3;
        Require((m + n).Value == (byte)15 && (g + h).Value == (byte)15, "byte +");
        Require((m - n).Value == (byte)9 && (g - h).Value == (byte)9, "byte -");
        Require((m * n).Value == (byte)36 && (g * h).Value == (byte)36, "byte *");
        Require((m / n).Value == (byte)4 && (g / h).Value == (byte)4, "byte /");
        Require((m % n).Value == (byte)0 && (g % h).Value == (byte)0, "byte %");
        m += (byte)3; m -= (byte)3; m *= (byte)3; m /= (byte)3;
        g += (byte)3; g -= (byte)3; g *= (byte)3; g /= (byte)3;
        Require(m.Value == (byte)12 && g.Value == (byte)12, "byte compound operators");
        Require(((byte)15 - n).Value == (byte)12 && ((byte)12 / h).Value == (byte)4, "byte scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (byte)12 && oldG.Value == (byte)12 && m.Value == (byte)13 && g.Value == (byte)13, "byte postfix copies");
        --m; --g; Require(m.Value == (byte)12 && g.Value == (byte)12, "byte prefix decrement");
        Require((+m).Value == (byte)12 && (+g).Value == (byte)12, "byte unary plus");
    }
    private static void Check1()
    {
        MaskedValue<sbyte> m = (sbyte)12, n = (sbyte)3;
        GuardedValue<sbyte> g = (sbyte)12, h = (sbyte)3;
        Require((m + n).Value == (sbyte)15 && (g + h).Value == (sbyte)15, "sbyte +");
        Require((m - n).Value == (sbyte)9 && (g - h).Value == (sbyte)9, "sbyte -");
        Require((m * n).Value == (sbyte)36 && (g * h).Value == (sbyte)36, "sbyte *");
        Require((m / n).Value == (sbyte)4 && (g / h).Value == (sbyte)4, "sbyte /");
        Require((m % n).Value == (sbyte)0 && (g % h).Value == (sbyte)0, "sbyte %");
        m += (sbyte)3; m -= (sbyte)3; m *= (sbyte)3; m /= (sbyte)3;
        g += (sbyte)3; g -= (sbyte)3; g *= (sbyte)3; g /= (sbyte)3;
        Require(m.Value == (sbyte)12 && g.Value == (sbyte)12, "sbyte compound operators");
        Require(((sbyte)15 - n).Value == (sbyte)12 && ((sbyte)12 / h).Value == (sbyte)4, "sbyte scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (sbyte)12 && oldG.Value == (sbyte)12 && m.Value == (sbyte)13 && g.Value == (sbyte)13, "sbyte postfix copies");
        --m; --g; Require(m.Value == (sbyte)12 && g.Value == (sbyte)12, "sbyte prefix decrement");
        Require((+m).Value == (sbyte)12 && (+g).Value == (sbyte)12, "sbyte unary plus");
    }
    private static void Check2()
    {
        MaskedValue<short> m = (short)12, n = (short)3;
        GuardedValue<short> g = (short)12, h = (short)3;
        Require((m + n).Value == (short)15 && (g + h).Value == (short)15, "short +");
        Require((m - n).Value == (short)9 && (g - h).Value == (short)9, "short -");
        Require((m * n).Value == (short)36 && (g * h).Value == (short)36, "short *");
        Require((m / n).Value == (short)4 && (g / h).Value == (short)4, "short /");
        Require((m % n).Value == (short)0 && (g % h).Value == (short)0, "short %");
        m += (short)3; m -= (short)3; m *= (short)3; m /= (short)3;
        g += (short)3; g -= (short)3; g *= (short)3; g /= (short)3;
        Require(m.Value == (short)12 && g.Value == (short)12, "short compound operators");
        Require(((short)15 - n).Value == (short)12 && ((short)12 / h).Value == (short)4, "short scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (short)12 && oldG.Value == (short)12 && m.Value == (short)13 && g.Value == (short)13, "short postfix copies");
        --m; --g; Require(m.Value == (short)12 && g.Value == (short)12, "short prefix decrement");
        Require((+m).Value == (short)12 && (+g).Value == (short)12, "short unary plus");
    }
    private static void Check3()
    {
        MaskedValue<ushort> m = (ushort)12, n = (ushort)3;
        GuardedValue<ushort> g = (ushort)12, h = (ushort)3;
        Require((m + n).Value == (ushort)15 && (g + h).Value == (ushort)15, "ushort +");
        Require((m - n).Value == (ushort)9 && (g - h).Value == (ushort)9, "ushort -");
        Require((m * n).Value == (ushort)36 && (g * h).Value == (ushort)36, "ushort *");
        Require((m / n).Value == (ushort)4 && (g / h).Value == (ushort)4, "ushort /");
        Require((m % n).Value == (ushort)0 && (g % h).Value == (ushort)0, "ushort %");
        m += (ushort)3; m -= (ushort)3; m *= (ushort)3; m /= (ushort)3;
        g += (ushort)3; g -= (ushort)3; g *= (ushort)3; g /= (ushort)3;
        Require(m.Value == (ushort)12 && g.Value == (ushort)12, "ushort compound operators");
        Require(((ushort)15 - n).Value == (ushort)12 && ((ushort)12 / h).Value == (ushort)4, "ushort scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (ushort)12 && oldG.Value == (ushort)12 && m.Value == (ushort)13 && g.Value == (ushort)13, "ushort postfix copies");
        --m; --g; Require(m.Value == (ushort)12 && g.Value == (ushort)12, "ushort prefix decrement");
        Require((+m).Value == (ushort)12 && (+g).Value == (ushort)12, "ushort unary plus");
    }
    private static void Check4()
    {
        MaskedValue<char> m = (char)12, n = (char)3;
        GuardedValue<char> g = (char)12, h = (char)3;
        Require((m + n).Value == (char)15 && (g + h).Value == (char)15, "char +");
        Require((m - n).Value == (char)9 && (g - h).Value == (char)9, "char -");
        Require((m * n).Value == (char)36 && (g * h).Value == (char)36, "char *");
        Require((m / n).Value == (char)4 && (g / h).Value == (char)4, "char /");
        Require((m % n).Value == (char)0 && (g % h).Value == (char)0, "char %");
        m += (char)3; m -= (char)3; m *= (char)3; m /= (char)3;
        g += (char)3; g -= (char)3; g *= (char)3; g /= (char)3;
        Require(m.Value == (char)12 && g.Value == (char)12, "char compound operators");
        Require(((char)15 - n).Value == (char)12 && ((char)12 / h).Value == (char)4, "char scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (char)12 && oldG.Value == (char)12 && m.Value == (char)13 && g.Value == (char)13, "char postfix copies");
        --m; --g; Require(m.Value == (char)12 && g.Value == (char)12, "char prefix decrement");
        Require((+m).Value == (char)12 && (+g).Value == (char)12, "char unary plus");
    }
    private static void Check5()
    {
        MaskedValue<int> m = (int)12, n = (int)3;
        GuardedValue<int> g = (int)12, h = (int)3;
        Require((m + n).Value == (int)15 && (g + h).Value == (int)15, "int +");
        Require((m - n).Value == (int)9 && (g - h).Value == (int)9, "int -");
        Require((m * n).Value == (int)36 && (g * h).Value == (int)36, "int *");
        Require((m / n).Value == (int)4 && (g / h).Value == (int)4, "int /");
        Require((m % n).Value == (int)0 && (g % h).Value == (int)0, "int %");
        m += (int)3; m -= (int)3; m *= (int)3; m /= (int)3;
        g += (int)3; g -= (int)3; g *= (int)3; g /= (int)3;
        Require(m.Value == (int)12 && g.Value == (int)12, "int compound operators");
        Require(((int)15 - n).Value == (int)12 && ((int)12 / h).Value == (int)4, "int scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (int)12 && oldG.Value == (int)12 && m.Value == (int)13 && g.Value == (int)13, "int postfix copies");
        --m; --g; Require(m.Value == (int)12 && g.Value == (int)12, "int prefix decrement");
        Require((+m).Value == (int)12 && (+g).Value == (int)12, "int unary plus");
    }
    private static void Check6()
    {
        MaskedValue<uint> m = (uint)12, n = (uint)3;
        GuardedValue<uint> g = (uint)12, h = (uint)3;
        Require((m + n).Value == (uint)15 && (g + h).Value == (uint)15, "uint +");
        Require((m - n).Value == (uint)9 && (g - h).Value == (uint)9, "uint -");
        Require((m * n).Value == (uint)36 && (g * h).Value == (uint)36, "uint *");
        Require((m / n).Value == (uint)4 && (g / h).Value == (uint)4, "uint /");
        Require((m % n).Value == (uint)0 && (g % h).Value == (uint)0, "uint %");
        m += (uint)3; m -= (uint)3; m *= (uint)3; m /= (uint)3;
        g += (uint)3; g -= (uint)3; g *= (uint)3; g /= (uint)3;
        Require(m.Value == (uint)12 && g.Value == (uint)12, "uint compound operators");
        Require(((uint)15 - n).Value == (uint)12 && ((uint)12 / h).Value == (uint)4, "uint scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (uint)12 && oldG.Value == (uint)12 && m.Value == (uint)13 && g.Value == (uint)13, "uint postfix copies");
        --m; --g; Require(m.Value == (uint)12 && g.Value == (uint)12, "uint prefix decrement");
        Require((+m).Value == (uint)12 && (+g).Value == (uint)12, "uint unary plus");
    }
    private static void Check7()
    {
        MaskedValue<long> m = (long)12, n = (long)3;
        GuardedValue<long> g = (long)12, h = (long)3;
        Require((m + n).Value == (long)15 && (g + h).Value == (long)15, "long +");
        Require((m - n).Value == (long)9 && (g - h).Value == (long)9, "long -");
        Require((m * n).Value == (long)36 && (g * h).Value == (long)36, "long *");
        Require((m / n).Value == (long)4 && (g / h).Value == (long)4, "long /");
        Require((m % n).Value == (long)0 && (g % h).Value == (long)0, "long %");
        m += (long)3; m -= (long)3; m *= (long)3; m /= (long)3;
        g += (long)3; g -= (long)3; g *= (long)3; g /= (long)3;
        Require(m.Value == (long)12 && g.Value == (long)12, "long compound operators");
        Require(((long)15 - n).Value == (long)12 && ((long)12 / h).Value == (long)4, "long scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (long)12 && oldG.Value == (long)12 && m.Value == (long)13 && g.Value == (long)13, "long postfix copies");
        --m; --g; Require(m.Value == (long)12 && g.Value == (long)12, "long prefix decrement");
        Require((+m).Value == (long)12 && (+g).Value == (long)12, "long unary plus");
    }
    private static void Check8()
    {
        MaskedValue<ulong> m = (ulong)12, n = (ulong)3;
        GuardedValue<ulong> g = (ulong)12, h = (ulong)3;
        Require((m + n).Value == (ulong)15 && (g + h).Value == (ulong)15, "ulong +");
        Require((m - n).Value == (ulong)9 && (g - h).Value == (ulong)9, "ulong -");
        Require((m * n).Value == (ulong)36 && (g * h).Value == (ulong)36, "ulong *");
        Require((m / n).Value == (ulong)4 && (g / h).Value == (ulong)4, "ulong /");
        Require((m % n).Value == (ulong)0 && (g % h).Value == (ulong)0, "ulong %");
        m += (ulong)3; m -= (ulong)3; m *= (ulong)3; m /= (ulong)3;
        g += (ulong)3; g -= (ulong)3; g *= (ulong)3; g /= (ulong)3;
        Require(m.Value == (ulong)12 && g.Value == (ulong)12, "ulong compound operators");
        Require(((ulong)15 - n).Value == (ulong)12 && ((ulong)12 / h).Value == (ulong)4, "ulong scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (ulong)12 && oldG.Value == (ulong)12 && m.Value == (ulong)13 && g.Value == (ulong)13, "ulong postfix copies");
        --m; --g; Require(m.Value == (ulong)12 && g.Value == (ulong)12, "ulong prefix decrement");
        Require((+m).Value == (ulong)12 && (+g).Value == (ulong)12, "ulong unary plus");
    }
    private static void Check9()
    {
        MaskedValue<float> m = (float)12, n = (float)3;
        GuardedValue<float> g = (float)12, h = (float)3;
        Require((m + n).Value == (float)15 && (g + h).Value == (float)15, "float +");
        Require((m - n).Value == (float)9 && (g - h).Value == (float)9, "float -");
        Require((m * n).Value == (float)36 && (g * h).Value == (float)36, "float *");
        Require((m / n).Value == (float)4 && (g / h).Value == (float)4, "float /");
        Require((m % n).Value == (float)0 && (g % h).Value == (float)0, "float %");
        m += (float)3; m -= (float)3; m *= (float)3; m /= (float)3;
        g += (float)3; g -= (float)3; g *= (float)3; g /= (float)3;
        Require(m.Value == (float)12 && g.Value == (float)12, "float compound operators");
        Require(((float)15 - n).Value == (float)12 && ((float)12 / h).Value == (float)4, "float scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (float)12 && oldG.Value == (float)12 && m.Value == (float)13 && g.Value == (float)13, "float postfix copies");
        --m; --g; Require(m.Value == (float)12 && g.Value == (float)12, "float prefix decrement");
        Require((+m).Value == (float)12 && (+g).Value == (float)12, "float unary plus");
    }
    private static void Check10()
    {
        MaskedValue<double> m = (double)12, n = (double)3;
        GuardedValue<double> g = (double)12, h = (double)3;
        Require((m + n).Value == (double)15 && (g + h).Value == (double)15, "double +");
        Require((m - n).Value == (double)9 && (g - h).Value == (double)9, "double -");
        Require((m * n).Value == (double)36 && (g * h).Value == (double)36, "double *");
        Require((m / n).Value == (double)4 && (g / h).Value == (double)4, "double /");
        Require((m % n).Value == (double)0 && (g % h).Value == (double)0, "double %");
        m += (double)3; m -= (double)3; m *= (double)3; m /= (double)3;
        g += (double)3; g -= (double)3; g *= (double)3; g /= (double)3;
        Require(m.Value == (double)12 && g.Value == (double)12, "double compound operators");
        Require(((double)15 - n).Value == (double)12 && ((double)12 / h).Value == (double)4, "double scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (double)12 && oldG.Value == (double)12 && m.Value == (double)13 && g.Value == (double)13, "double postfix copies");
        --m; --g; Require(m.Value == (double)12 && g.Value == (double)12, "double prefix decrement");
        Require((+m).Value == (double)12 && (+g).Value == (double)12, "double unary plus");
    }
    private static void Check11()
    {
        MaskedValue<decimal> m = (decimal)12, n = (decimal)3;
        GuardedValue<decimal> g = (decimal)12, h = (decimal)3;
        Require((m + n).Value == (decimal)15 && (g + h).Value == (decimal)15, "decimal +");
        Require((m - n).Value == (decimal)9 && (g - h).Value == (decimal)9, "decimal -");
        Require((m * n).Value == (decimal)36 && (g * h).Value == (decimal)36, "decimal *");
        Require((m / n).Value == (decimal)4 && (g / h).Value == (decimal)4, "decimal /");
        Require((m % n).Value == (decimal)0 && (g % h).Value == (decimal)0, "decimal %");
        m += (decimal)3; m -= (decimal)3; m *= (decimal)3; m /= (decimal)3;
        g += (decimal)3; g -= (decimal)3; g *= (decimal)3; g /= (decimal)3;
        Require(m.Value == (decimal)12 && g.Value == (decimal)12, "decimal compound operators");
        Require(((decimal)15 - n).Value == (decimal)12 && ((decimal)12 / h).Value == (decimal)4, "decimal scalar left");
        var oldM = m++; var oldG = g++;
        Require(oldM.Value == (decimal)12 && oldG.Value == (decimal)12 && m.Value == (decimal)13 && g.Value == (decimal)13, "decimal postfix copies");
        --m; --g; Require(m.Value == (decimal)12 && g.Value == (decimal)12, "decimal prefix decrement");
        Require((+m).Value == (decimal)12 && (+g).Value == (decimal)12, "decimal unary plus");
    }
    private static void Work(int count)
    {
        GuardedValue<int> g = 100; MaskedValue<int> m = 3; MaskedValue<float> f = 1f; GuardedValue<decimal> d = 1m;
        for (int i = 0; i < count; i++) { g += m; g -= m; g *= 2; g /= 2; g++; --g; f += 2f; f -= 2f; d *= 2m; d /= 2m; }
        sink = g.Value + (int)f.Value + (int)d.Value;
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception("Operator verification: " + message); }
    private static void Throws<T>(Action action, string message) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected exception: " + message); }
    private struct NonNumeric { public int field; }
}
