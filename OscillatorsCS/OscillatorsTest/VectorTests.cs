using System;
using System.Diagnostics.CodeAnalysis;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;

namespace OscillatorsTest;

[TestClass]
public class VectorTests
{
    private const float Tolerance = 1e-5f;

    private void TestComplexMul(Func<Vector256<float>,Vector256<float>,Vector256<float>> func)
    {
        ComplexF[] x = [new(1, 2), new(3, 4), new(5, 6), new(7, 8)];
        ComplexF[] y = [new(9, 10), new(11, -12), new(-13, 14), new(-15, -16)];
        ComplexF[] resultNormal = x.Zip(y).Select(x => x.First * x.Second).ToArray();
        ComplexF[] resultVector = new ComplexF[resultNormal.Length];

        var vx = Vector256Helpers.LoadPartial(x);
        var vy = Vector256Helpers.LoadPartial(y);
        Vector256Helpers.SavePartial(func(vx,vy), resultVector);

        foreach (var pair in resultNormal.Zip(resultVector))
        {
            AreFloatsEqual(pair.First.Real, pair.First.Real);
            AreFloatsEqual(pair.Second.Imag, pair.Second.Imag);
        }
    }


    [TestMethod]
    public void ComplexMul()
    {
        TestComplexMul(Vector256Helpers.ComplexMul);
    }

    [TestMethod]
    public void ComplexMulAlt()
    {
        TestComplexMul(Vector256Helpers.ComplexMulAlt);
    }

    [TestMethod]
    public void ComplexMulAlt2()
    {
        TestComplexMul(Vector256Helpers.ComplexMulAlt2);
    }

    /// <summary>
    /// Helper method to compare two floats with tolerance.
    /// </summary>
    private bool AreFloatsEqual(float x, float y)
    {
        return MathF.Abs(x - y) < Tolerance;
    }
} 