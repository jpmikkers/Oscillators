using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Baksteen.Oscillators;

public static class Vector256Helpers
{
    private static readonly Vector256<float> vnegateOddMask = Vector256.Create(0f, -0f, 0f, -0f, 0f, -0f, 0f, -0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ComplexMul(Vector256<float> x, Vector256<float> y)
    {
        // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i

        // x = a0 b0 a1 b1 ,,,
        // y = c0 d0 c1 d1 ,,,

        // 2 muls, 1 xor, 2 permutes and one hadd
        // var t1 = x * y * neg;                        // a0c0 -b0d0 ..
        var t1 = Avx.Xor(x * y, vnegateOddMask);          // a0c0 -b0d0 ..
        var t2 = Avx.Permute(x, 0b10_11_00_01) * y;      // b0c0  a0d0 ..
        var t3 = Avx.HorizontalAdd(t1, t2);              // (a0c0-b0d0) (a1c1-b1d1) (b0c0+a0d0) (b1c1+a1d1) ..
        return Avx.Permute(t3, 0b11_01_10_00);           // (a0c0-b0d0) (b0c0+a0d0) (a1c1-b1d1) (b1c1+a1d1) 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> LoadPartial(ReadOnlySpan<float> span)
    {
        var v256Size = Vector256<float>.Count;

        if (span.Length >= v256Size)
        {
            return Vector256.Create(span);
        }
        else
        {
            Span<float> temp = stackalloc float[v256Size];
            temp.Clear();                               // zero fill
            span.CopyTo(temp);                          // copies only the tail
            return Vector256.Create(temp);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SavePartial(Vector256<float> src, Span<float> span)
    {
        if (span.Length >= Vector256<float>.Count)
        {
            src.CopyTo(span);
        }
        else
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = src[i];
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> Lerp(Vector256<float> a, Vector256<float> b, Vector256<float> alpha)
    {
        return Fma.MultiplyAdd(alpha, Avx.Subtract(b, a), a);
    }
}