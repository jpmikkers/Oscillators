using System;
using System.Diagnostics.CodeAnalysis;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using System.Collections;

namespace OscillatorsTest;

[TestClass]
public class VectorTests
{
    private const float Tolerance = 1e-5f;

    public sealed class FloatComparer(float tolerance) : IComparer<float>, IComparer
    {
        public int Compare(float x, float y)
        {
            if(Math.Abs(x - y) <= tolerance)
            {
                return 0;
            }
            else if (x < y)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        public int Compare(object? x, object? y)
        {
            return Compare((float)x!, (float)y!);
        }
    }

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
            Assert.IsTrue(AreFloatsEqual(pair.First.Real, pair.Second.Real));
            Assert.IsTrue(AreFloatsEqual(pair.First.Imag, pair.Second.Imag));
        }
    }


    [TestMethod]
    public void ComplexMul()
    {
        TestComplexMul(Vector256Helpers.ComplexMul);
    }

    [TestMethod]
    public void ComplexMulAlt3()
    {
        TestComplexMul(Vector256Helpers.ComplexMulAlt3);
    }

    [TestMethod]
    public void ComplexMulAlt4()
    {
        TestComplexMul(Vector256Helpers.ComplexMulAlt4);
    }

    [TestMethod]
    public void ComplexMulAlt5()
    {
        TestComplexMul(Vector256Helpers.ComplexMulAlt5);
    }

    [TestMethod]
    public void TestNormalize()
    {
        ComplexF[] x = [new(1, 2), new(-3, 4), new(5, -6), new(-7, -8)];
        ComplexF[] y = new ComplexF[x.Length];
        var vx = Vector256Helpers.LoadPartial(x);
        var result = Vector256Helpers.ComplexNormalizeFast(vx);
        Vector256Helpers.SavePartial(result, y);
        var expected = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };
        CollectionAssert.AreEqual(expected, y.Select(t => t.Magnitude).ToList(), comparer: new FloatComparer(0.0002f));
    }

    /// <summary>
    /// Helper method to compare two floats with tolerance.
    /// </summary>
    private bool AreFloatsEqual(float x, float y)
    {
        return MathF.Abs(x - y) < Tolerance;
    }
} 