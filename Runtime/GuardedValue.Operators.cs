using System.Runtime.CompilerServices;

namespace Achieve.MaskedValues
{
    public partial struct GuardedValue<T> where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator +(GuardedValue<T> left, GuardedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Add(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator +(GuardedValue<T> left, T right)
        {
            left.Value = ValueArithmetic<T>.Add(left.Value, right);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator +(T left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Add(left, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator -(GuardedValue<T> left, GuardedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Subtract(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator -(GuardedValue<T> left, T right)
        {
            left.Value = ValueArithmetic<T>.Subtract(left.Value, right);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator -(T left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Subtract(left, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator *(GuardedValue<T> left, GuardedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Multiply(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator *(GuardedValue<T> left, T right)
        {
            left.Value = ValueArithmetic<T>.Multiply(left.Value, right);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator *(T left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Multiply(left, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator /(GuardedValue<T> left, GuardedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Divide(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator /(GuardedValue<T> left, T right)
        {
            left.Value = ValueArithmetic<T>.Divide(left.Value, right);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator /(T left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Divide(left, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator %(GuardedValue<T> left, GuardedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Remainder(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator %(GuardedValue<T> left, T right)
        {
            left.Value = ValueArithmetic<T>.Remainder(left.Value, right);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator %(T left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Remainder(left, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator ++(GuardedValue<T> value)
        {
            value.Value = ValueArithmetic<T>.Add(value.Value, ValueArithmetic<T>.One());
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator --(GuardedValue<T> value)
        {
            value.Value = ValueArithmetic<T>.Subtract(value.Value, ValueArithmetic<T>.One());
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator +(GuardedValue<T> left, MaskedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Add(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator +(MaskedValue<T> left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Add(left.Value, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator -(GuardedValue<T> left, MaskedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Subtract(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator -(MaskedValue<T> left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Subtract(left.Value, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator *(GuardedValue<T> left, MaskedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Multiply(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator *(MaskedValue<T> left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Multiply(left.Value, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator /(GuardedValue<T> left, MaskedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Divide(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator /(MaskedValue<T> left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Divide(left.Value, right.Value);
            return right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator %(GuardedValue<T> left, MaskedValue<T> right)
        {
            left.Value = ValueArithmetic<T>.Remainder(left.Value, right.Value);
            return left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GuardedValue<T> operator %(MaskedValue<T> left, GuardedValue<T> right)
        {
            right.Value = ValueArithmetic<T>.Remainder(left.Value, right.Value);
            return right;
        }

        public static GuardedValue<T> operator +(GuardedValue<T> value) { _ = value.Value; return value; }
        public static GuardedValue<T> operator -(GuardedValue<T> value) { value.Value = ValueArithmetic<T>.Subtract(default, value.Value); return value; }
    }
}
