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
        var t1 = Avx.Xor(Avx.Multiply(x,y), vnegateOddMask);          // a0c0 -b0d0 ..
        var t2 = Avx.Multiply(Avx.Permute(x, 0b10_11_00_01), y);      // b0c0  a0d0 ..
        var t3 = Avx.HorizontalAdd(t1, t2);              // (a0c0-b0d0) (a1c1-b1d1) (b0c0+a0d0) (b1c1+a1d1) ..
        return Avx.Permute(t3, 0b11_01_10_00);           // (a0c0-b0d0) (b0c0+a0d0) (a1c1-b1d1) (b1c1+a1d1) 
    }

#if BROKEN
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ComplexMulAlt(Vector256<float> x, Vector256<float> y)
    {
        // x = [a0, b0, a1, b1, ...]
        // y = [c0, d0, c1, d1, ...]

        // Duplicate real parts of y: [c0, c0, c1, c1, ...]
        var yRealDup = Avx.Permute(y, 0b01_01_00_00);

        // Duplicate imag parts of y: [d0, d0, d1, d1, ...]
        var yImagDup = Avx.Permute(y, 0b11_11_10_10);

        // ac / bc: [a0c0, b0c0, a1c1, b1c1, ...]
        var ac_bc = Avx.Multiply(x, yRealDup);

        // Swap real/imag of x: [b0, a0, b1, a1, ...]
        var xSwapped = Avx.Permute(x, 0b10_11_00_01);

        // bd / ad: [b0d0, a0d0, b1d1, a1d1, ...]
        var bd_ad = Avx.Multiply(xSwapped, yImagDup);

        // AddSubtract:
        // even lanes: ac - bd  → real parts
        // odd  lanes: bc + ad  → imag parts
        return Avx.AddSubtract(ac_bc, bd_ad); // result = [real0, imag0, real1, imag1, ...]
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ComplexMulAlt3(Vector256<float> a, Vector256<float> b)
    {
        // https://www.researchgate.net/figure/ectorized-complex-multiplication-using-AVX-2_fig2_337532904
        var bSwap = Avx.Permute(b, 0b10_11_00_01);          // b0i b0r b1i b1r ..
        var aIm = Avx.Shuffle(a, a, 0b11110101);            // a0i a0i a1i a1i ..
        var aImbSwap = Avx.Multiply(aIm, bSwap);            // a0i*b0i a0i*b0r a1i*b1i a1i*b1r ..
        var aRe = Avx.Shuffle(a, a, 0b10100000);            // a0r a0r a1r a1r ..
        return Fma.MultiplyAddSubtract(aRe, b, aImbSwap);   // (a0r*b0r - a0i*b0i) (a0r*b0i + a0i*b0r) ...
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ComplexMulAlt4(Vector256<float> a, Vector256<float> b)
    {
        var bSwap = Avx.Permute(b, 0b10_11_00_01);          // b0i b0r b1i b1r ..
        var aRe = Avx.DuplicateEvenIndexed(a);               // a0r a0r a1r a1r ..
        var aIm = Avx.DuplicateOddIndexed(a);              // a0i a0i a1i a1i ..
        var aImbSwap = Avx.Multiply(aIm, bSwap);            // a0i*b0i a0i*b0r a1i*b1i a1i*b1r ..
        return Fma.MultiplyAddSubtract(aRe, b, aImbSwap);   // (a0r*b0r - a0i*b0i) (a0r*b0i + a0i*b0r) ...
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ComplexMulAlt5(Vector256<float> a, Vector256<float> b)
    {
        var aRe = Avx.DuplicateEvenIndexed(a);              // a0r a0r a1r a1r ..
        var aIm = Avx.DuplicateOddIndexed(a);               // a0i a0i a1i a1i ..
        var aImb = Avx.Multiply(aIm, b);                    // a0i*b0r a0i*b0i a1i*b1r a1i*b1i ..
        return Fma.MultiplyAddSubtract(aRe, b, Avx.Permute(aImb, 0b10_11_00_01));   // (a0r*b0r - a0i*b0i) (a0r*b0i + a0i*b0r) ...
    }

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

    public static Vector256<float> LoadPartial(ReadOnlySpan<ComplexF> span)
    {
        return LoadPartial(MemoryMarshal.Cast<ComplexF,float>(span));
    }

    public static void SavePartial(Vector256<float> src, Span<ComplexF> span)
    {
        SavePartial(src, MemoryMarshal.Cast<ComplexF, float>(span));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> Lerp(Vector256<float> a, Vector256<float> b, Vector256<float> alpha)
    {
        return Fma.MultiplyAdd(alpha, Avx.Subtract(b, a), a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> DuplicateInterleaved(Vector128<float> a)
    {
        return Vector256.Create(Avx2.UnpackLow(a, a), Avx2.UnpackHigh(a, a));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> MagnitudeSquaredDuplicated(Vector256<float> c)
    {
        var squared = Avx.Multiply(c,c);                            // r0^2 i0^2 r1^2 i1^2 ..
        var reimswapped = Avx.Permute(squared, 0b10_11_00_01);      // i0^2 r0^2 i1^2 r1^2 ..
        return Avx.Add(squared, reimswapped);                       // msq0 msq0 msq1 msq1 .. 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> MagnitudeSquaredDuplicatedAlt(Vector256<float> c)
    {
        var squared = Avx.Multiply(c, c);                           // r0^2 i0^2 r1^2 i1^2 ..
        return Avx.HorizontalAdd(squared,squared);                  // msq0 msq0 msq1 msq1 ..
    }
}