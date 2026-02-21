using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Baksteen.Oscillators;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
using Microsoft.VSDiagnostics;

namespace Benchmark;
// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
//[CPUUsageDiagnoser]
public class ComplexBenchmarks
{
    private float[] x;
    private float[] y;
    private float[] z;
    private const int repeats = 100;

    [GlobalSetup]
    public void Setup()
    {
        x = Enumerable.Range(0, 10000 * 8).Select(_ => Random.Shared.NextSingle()).ToArray();
        y = Enumerable.Range(0, 10000 * 8).Select(_ => Random.Shared.NextSingle()).ToArray();
        z = new float[x.Length];
    }

    [Benchmark(Baseline = true)]
    public void ComplexMulNormal()
    {
        int vc = Vector256<float>.Count;
        ref var rx=ref MemoryMarshal.GetReference(x);
        ref var ry=ref MemoryMarshal.GetReference(y);
        ref var rz=ref MemoryMarshal.GetReference(z.AsSpan());

        for (var r = 0; r < repeats; r++)
        {
            for (int i = 0; i <= (x.Length - vc); i += vc)
            {
                Vector256.StoreUnsafe(
                    Vector256Helpers.ComplexMul(
                        Vector256.LoadUnsafe(ref rx, (nuint)i),
                        Vector256.LoadUnsafe(ref ry, (nuint)i)
                    ),
                    ref rz,
                    (nuint)i);
            }
        }
    }

    [Benchmark]
    public void ComplexMulAlt3()
    {
        int vc = Vector256<float>.Count;
        ref var rx = ref MemoryMarshal.GetReference(x);
        ref var ry = ref MemoryMarshal.GetReference(y);
        ref var rz = ref MemoryMarshal.GetReference(z.AsSpan());

        for (var r = 0; r < repeats; r++)
        {
            for (var i = 0; i <= (x.Length - vc); i += vc)
            {
                Vector256.StoreUnsafe(
                    Vector256Helpers.ComplexMulAlt3(
                        Vector256.LoadUnsafe(ref rx, (nuint)i),
                        Vector256.LoadUnsafe(ref ry, (nuint)i)
                    ),
                    ref rz,
                    (nuint)i);
            }
        }
    }

    [Benchmark]
    public void ComplexMulAlt4()
    {
        int vc = Vector256<float>.Count;
        ref var rx = ref MemoryMarshal.GetReference(x);
        ref var ry = ref MemoryMarshal.GetReference(y);
        ref var rz = ref MemoryMarshal.GetReference(z.AsSpan());

        for (var r = 0; r < repeats; r++)
        {
            for (var i = 0; i <= (x.Length - vc); i += vc)
            {
                Vector256.StoreUnsafe(
                    Vector256Helpers.ComplexMulAlt4(
                        Vector256.LoadUnsafe(ref rx, (nuint)i),
                        Vector256.LoadUnsafe(ref ry, (nuint)i)
                    ),
                    ref rz,
                    (nuint)i);
            }
        }
    }

    [Benchmark]
    public void ComplexMulAlt5()
    {
        int vc = Vector256<float>.Count;
        ref var rx = ref MemoryMarshal.GetReference(x);
        ref var ry = ref MemoryMarshal.GetReference(y);
        ref var rz = ref MemoryMarshal.GetReference(z.AsSpan());

        for (var r = 0; r < repeats; r++)
        {
            for (var i = 0; i <= (x.Length - vc); i += vc)
            {
                Vector256.StoreUnsafe(
                    Vector256Helpers.ComplexMulAlt5(
                        Vector256.LoadUnsafe(ref rx, (nuint)i),
                        Vector256.LoadUnsafe(ref ry, (nuint)i)
                    ),
                    ref rz,
                    (nuint)i);
            }
        }
    }
}
