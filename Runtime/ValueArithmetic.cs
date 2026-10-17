using System;
using System.Runtime.CompilerServices;

namespace Achieve.MaskedValues
{
    // Closed generic branches avoid dynamic, reflection, delegates and boxing.
    // Integral overflow wraps; division and decimal arithmetic keep C# exceptions.
    internal static unsafe class ValueArithmetic<T> where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Add(T left, T right)
        {
            if (typeof(T) == typeof(byte)) { byte result = unchecked((byte)(*(byte*)&left + *(byte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(sbyte)) { sbyte result = unchecked((sbyte)(*(sbyte*)&left + *(sbyte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(short)) { short result = unchecked((short)(*(short*)&left + *(short*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ushort)) { ushort result = unchecked((ushort)(*(ushort*)&left + *(ushort*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(char)) { char result = unchecked((char)(*(char*)&left + *(char*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(int)) { int result = unchecked((int)(*(int*)&left + *(int*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(uint)) { uint result = unchecked((uint)(*(uint*)&left + *(uint*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(long)) { long result = unchecked((long)(*(long*)&left + *(long*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ulong)) { ulong result = unchecked((ulong)(*(ulong*)&left + *(ulong*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(float)) { float result = unchecked((float)(*(float*)&left + *(float*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(double)) { double result = unchecked((double)(*(double*)&left + *(double*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(decimal)) { decimal result = unchecked((decimal)(*(decimal*)&left + *(decimal*)&right)); return *(T*)&result; }
#if UNITY_6000_0_OR_NEWER
            if (typeof(T) == typeof(UnityEngine.Vector2)) { UnityEngine.Vector2 result = *(UnityEngine.Vector2*)&left + *(UnityEngine.Vector2*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector3)) { UnityEngine.Vector3 result = *(UnityEngine.Vector3*)&left + *(UnityEngine.Vector3*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector4)) { UnityEngine.Vector4 result = *(UnityEngine.Vector4*)&left + *(UnityEngine.Vector4*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector2Int)) { UnityEngine.Vector2Int result = *(UnityEngine.Vector2Int*)&left + *(UnityEngine.Vector2Int*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector3Int)) { UnityEngine.Vector3Int result = *(UnityEngine.Vector3Int*)&left + *(UnityEngine.Vector3Int*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Color)) { UnityEngine.Color result = *(UnityEngine.Color*)&left + *(UnityEngine.Color*)&right; return *(T*)&result; }
#endif
            throw Unsupported("+");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Subtract(T left, T right)
        {
            if (typeof(T) == typeof(byte)) { byte result = unchecked((byte)(*(byte*)&left - *(byte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(sbyte)) { sbyte result = unchecked((sbyte)(*(sbyte*)&left - *(sbyte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(short)) { short result = unchecked((short)(*(short*)&left - *(short*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ushort)) { ushort result = unchecked((ushort)(*(ushort*)&left - *(ushort*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(char)) { char result = unchecked((char)(*(char*)&left - *(char*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(int)) { int result = unchecked((int)(*(int*)&left - *(int*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(uint)) { uint result = unchecked((uint)(*(uint*)&left - *(uint*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(long)) { long result = unchecked((long)(*(long*)&left - *(long*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ulong)) { ulong result = unchecked((ulong)(*(ulong*)&left - *(ulong*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(float)) { float result = unchecked((float)(*(float*)&left - *(float*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(double)) { double result = unchecked((double)(*(double*)&left - *(double*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(decimal)) { decimal result = unchecked((decimal)(*(decimal*)&left - *(decimal*)&right)); return *(T*)&result; }
#if UNITY_6000_0_OR_NEWER
            if (typeof(T) == typeof(UnityEngine.Vector2)) { UnityEngine.Vector2 result = *(UnityEngine.Vector2*)&left - *(UnityEngine.Vector2*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector3)) { UnityEngine.Vector3 result = *(UnityEngine.Vector3*)&left - *(UnityEngine.Vector3*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector4)) { UnityEngine.Vector4 result = *(UnityEngine.Vector4*)&left - *(UnityEngine.Vector4*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector2Int)) { UnityEngine.Vector2Int result = *(UnityEngine.Vector2Int*)&left - *(UnityEngine.Vector2Int*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Vector3Int)) { UnityEngine.Vector3Int result = *(UnityEngine.Vector3Int*)&left - *(UnityEngine.Vector3Int*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Color)) { UnityEngine.Color result = *(UnityEngine.Color*)&left - *(UnityEngine.Color*)&right; return *(T*)&result; }
#endif
            throw Unsupported("-");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Multiply(T left, T right)
        {
            if (typeof(T) == typeof(byte)) { byte result = unchecked((byte)(*(byte*)&left * *(byte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(sbyte)) { sbyte result = unchecked((sbyte)(*(sbyte*)&left * *(sbyte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(short)) { short result = unchecked((short)(*(short*)&left * *(short*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ushort)) { ushort result = unchecked((ushort)(*(ushort*)&left * *(ushort*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(char)) { char result = unchecked((char)(*(char*)&left * *(char*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(int)) { int result = unchecked((int)(*(int*)&left * *(int*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(uint)) { uint result = unchecked((uint)(*(uint*)&left * *(uint*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(long)) { long result = unchecked((long)(*(long*)&left * *(long*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ulong)) { ulong result = unchecked((ulong)(*(ulong*)&left * *(ulong*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(float)) { float result = unchecked((float)(*(float*)&left * *(float*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(double)) { double result = unchecked((double)(*(double*)&left * *(double*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(decimal)) { decimal result = unchecked((decimal)(*(decimal*)&left * *(decimal*)&right)); return *(T*)&result; }
#if UNITY_6000_0_OR_NEWER
            if (typeof(T) == typeof(UnityEngine.Quaternion)) { UnityEngine.Quaternion result = *(UnityEngine.Quaternion*)&left * *(UnityEngine.Quaternion*)&right; return *(T*)&result; }
            if (typeof(T) == typeof(UnityEngine.Color)) { UnityEngine.Color result = *(UnityEngine.Color*)&left * *(UnityEngine.Color*)&right; return *(T*)&result; }
#endif
            throw Unsupported("*");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Divide(T left, T right)
        {
            if (typeof(T) == typeof(byte)) { byte result = unchecked((byte)(*(byte*)&left / *(byte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(sbyte)) { sbyte result = unchecked((sbyte)(*(sbyte*)&left / *(sbyte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(short)) { short result = unchecked((short)(*(short*)&left / *(short*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ushort)) { ushort result = unchecked((ushort)(*(ushort*)&left / *(ushort*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(char)) { char result = unchecked((char)(*(char*)&left / *(char*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(int)) { int result = unchecked((int)(*(int*)&left / *(int*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(uint)) { uint result = unchecked((uint)(*(uint*)&left / *(uint*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(long)) { long result = unchecked((long)(*(long*)&left / *(long*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ulong)) { ulong result = unchecked((ulong)(*(ulong*)&left / *(ulong*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(float)) { float result = unchecked((float)(*(float*)&left / *(float*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(double)) { double result = unchecked((double)(*(double*)&left / *(double*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(decimal)) { decimal result = unchecked((decimal)(*(decimal*)&left / *(decimal*)&right)); return *(T*)&result; }
            throw Unsupported("/");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Remainder(T left, T right)
        {
            if (typeof(T) == typeof(byte)) { byte result = unchecked((byte)(*(byte*)&left % *(byte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(sbyte)) { sbyte result = unchecked((sbyte)(*(sbyte*)&left % *(sbyte*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(short)) { short result = unchecked((short)(*(short*)&left % *(short*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ushort)) { ushort result = unchecked((ushort)(*(ushort*)&left % *(ushort*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(char)) { char result = unchecked((char)(*(char*)&left % *(char*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(int)) { int result = unchecked((int)(*(int*)&left % *(int*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(uint)) { uint result = unchecked((uint)(*(uint*)&left % *(uint*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(long)) { long result = unchecked((long)(*(long*)&left % *(long*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(ulong)) { ulong result = unchecked((ulong)(*(ulong*)&left % *(ulong*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(float)) { float result = unchecked((float)(*(float*)&left % *(float*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(double)) { double result = unchecked((double)(*(double*)&left % *(double*)&right)); return *(T*)&result; }
            if (typeof(T) == typeof(decimal)) { decimal result = unchecked((decimal)(*(decimal*)&left % *(decimal*)&right)); return *(T*)&result; }
            throw Unsupported("%");
        }

        internal static T One()
        {
            if (typeof(T) == typeof(byte)) { byte result = (byte)1; return *(T*)&result; }
            if (typeof(T) == typeof(sbyte)) { sbyte result = (sbyte)1; return *(T*)&result; }
            if (typeof(T) == typeof(short)) { short result = (short)1; return *(T*)&result; }
            if (typeof(T) == typeof(ushort)) { ushort result = (ushort)1; return *(T*)&result; }
            if (typeof(T) == typeof(char)) { char result = (char)1; return *(T*)&result; }
            if (typeof(T) == typeof(int)) { int result = (int)1; return *(T*)&result; }
            if (typeof(T) == typeof(uint)) { uint result = (uint)1; return *(T*)&result; }
            if (typeof(T) == typeof(long)) { long result = (long)1; return *(T*)&result; }
            if (typeof(T) == typeof(ulong)) { ulong result = (ulong)1; return *(T*)&result; }
            if (typeof(T) == typeof(float)) { float result = (float)1; return *(T*)&result; }
            if (typeof(T) == typeof(double)) { double result = (double)1; return *(T*)&result; }
            if (typeof(T) == typeof(decimal)) { decimal result = (decimal)1; return *(T*)&result; }
            throw Unsupported("++/--");
        }

        private static NotSupportedException Unsupported(string operation) =>
            new NotSupportedException("Operator " + operation + " is not supported for " + typeof(T).Name + ". Use Value for custom type operations.");
    }
}
