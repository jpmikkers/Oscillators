using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using Baksteen.Oscillators;

ComplexF[] x = [new(1, 2), new(3, 4), new(5, 6), new(7, 8)];
ComplexF[] y = [new(11,12),new(13,14),new(15,16),new(17,18)];

var a = Vector256Helpers.LoadPartial(x);
var b = Vector256Helpers.LoadPartial(y);

// https://www.researchgate.net/figure/ectorized-complex-multiplication-using-AVX-2_fig2_337532904

var bSwap = Avx.Permute(b, 0b10_11_00_01);      // b0i b0r b1i b1r ..
var aIm = Avx.Shuffle(a, a, 0b11110101);        // a0r a0r a1r a1r ..
var aRe = Avx.Shuffle(a, a, 0b10100000);        // a0i a0i a1i a1i ..

var aImbSwap = Avx.Multiply(aIm, bSwap);
var result1 = Fma.MultiplyAddSubtract(aRe, b, aImbSwap);
var result2 = Fma.MultiplySubtractAdd(aRe, b, aImbSwap);

//var r = Avx2.Blend(vx, vy, 0b00001111);
Console.WriteLine($"{result1} {result2}");


[InlineArray(4)]
public struct FixedComplexArray
{
    public ComplexF _element;
}

[StructLayout(LayoutKind.Explicit)]
struct Vector256Complex
{
    [FieldOffset(0)]
    public Vector256<float> vector;

    [FieldOffset(0)]
    public FixedComplexArray items;

    public Vector256Complex()
    {
        this.vector = Vector256<float>.Zero;
    }
}


