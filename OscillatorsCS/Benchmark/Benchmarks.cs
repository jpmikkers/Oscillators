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
public class Benchmarks
{
    private const int Repeats = 3;
    private float[] samples;
    private ResonatorBank normalBank;
    private ResonatorBankVectorized vectorizedBank;
    private ResonatorBankVectorizedAVX vectorizedAVXBank;

    [GlobalSetup]
    public void Setup()
    {
        samples = new float[44100];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = 2.0f * (Random.Shared.NextSingle() - 0.5f);
        }
        normalBank = new ResonatorBank(Frequencies.MusicalPitchFrequencies(0, 200), 44100f, 4);
        vectorizedBank = new ResonatorBankVectorized(Frequencies.MusicalPitchFrequencies(0, 200), 44100f, 4);
        vectorizedAVXBank = new ResonatorBankVectorizedAVX(Frequencies.MusicalPitchFrequencies(0, 200), 44100f, 4);
    }

    [Benchmark(Baseline = true)]
    public void OscillatorBankNormal()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            normalBank.UpdateWithSample(samples[i]);
        }
    }

    [Benchmark]
    public void OscillatorBankVectorized()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            vectorizedBank.UpdateWithSample(samples[i]);
        }
    }

    [Benchmark]
    public void OscillatorBankVectorizedAVX()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            vectorizedAVXBank.UpdateWithSample(samples[i]);
        }
    }

#if NEVER
    [IterationSetup]
    public void IterationSetup()
    {
        phasors = Enumerable.Range(0, 500).Select(x => new ComplexF(
            2.0f * (Random.Shared.NextSingle() - 0.5f),
            2.0f * (Random.Shared.NextSingle() - 0.5f))).ToArray();
    }

    private static void StabilizeNormal(ComplexF[] phasors)
    {
        for (int i = 0; i < phasors.Length; i++)
        {
            // https://github.com/FrankReiser/ReiserRT_FlyingPhasor
            var k = 1.0f - (phasors[i].MagnitudeSquared - 1.0f) / 2.0f;
            // <=>
            // var k = 1.0f - (0.5*_phasors[i].MagnitudeSquared - 0.5f);
            // <=>
            // var k = 1.5f - 0.5*_phasors[i].MagnitudeSquared;
            // <=>
            // var k = (3.0f - magsquared) / 2.0f;

            phasors[i] *= k;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void StabilizeVec(ref Vector<float> item)
    {
        var squared = item * item;
        var swaptmp = Vector.AsVectorUInt64(squared);
        var reimswapped = ((swaptmp >> 32) | (swaptmp << 32)).As<ulong, float>();
        var magsquared = squared + reimswapped;
        item *= vhalf * (vthree - magsquared);
    }

    private void StabilizeVectorized(ComplexF[] phasors)
    {
        var vecsize = Vector<float>.Count;

        var interleaved = MemoryMarshal.Cast<ComplexF, Vector<float>>(phasors);

        foreach (ref var item in interleaved)
        {
            var squared = item * item;
            var swaptmp = Vector.AsVectorUInt64(squared);
            var reimswapped = ((swaptmp >> 32) | (swaptmp << 32)).As<ulong, float>();
            var magsquared = squared + reimswapped;
            item *= vhalf * (vthree - magsquared);
        }

        //var interleaved = MemoryMarshal.Cast<ComplexF, float>(_phasors);

        //for(var j=0; j <= (interleaved.Length-vecsize); j+=vecsize)
        //{
        //    var item = new Vector<float>(interleaved[j..]);
        //    var squared = item * item;
        //    var swaptmp = Vector.AsVectorUInt64(squared);
        //    var reimswapped = ((swaptmp >> 32) | (swaptmp << 32)).As<ulong, float>();
        //    var magsquared = squared + reimswapped;
        //    item *= vhalf * (vthree - magsquared);
        //    item.CopyTo(interleaved[j..]);
        //}

        int remaining = (phasors.Length * 2) % vecsize;

        if (remaining > 0)
        {
            var taildata = MemoryMarshal.Cast<ComplexF, float>(phasors)[^remaining..];

            var item = VectorExt.CreatePartial(taildata);
            var squared = item * item;
            var swaptmp = Vector.AsVectorUInt64(squared);
            var reimswapped = ((swaptmp >> 32) | (swaptmp << 32)).As<ulong, float>();
            var magsquared = squared + reimswapped;
            var k = vhalf * (vthree - magsquared);
            item *= k;

            for (var i = 0; i < remaining; i++)
            {
                taildata[i] = item[i];
            }
        }

        //for (int i = 0; i < _phasors.Length; i++)
        //{
        //    // https://github.com/FrankReiser/ReiserRT_FlyingPhasor
        //    //var k = 1.0f - (_phasors[i].MagnitudeSquared - 1.0f) / 2.0f;
        //    //var k = 1.0f - (_phasors[i].MagnitudeSquared - 1.0f) / 2.0f;
        //    //var k = 1.0f - (0.5 * _phasors[i].MagnitudeSquared - 0.5f);
        //    //var k = 1.5f - 0.5 * _phasors[i].MagnitudeSquared;
        //    var k = 0.5f * (3.0f - _phasors[i].MagnitudeSquared);
        //    _phasors[i] *= k;
        //}
    }

    void StabilizeVectorizedAvx(ComplexF[] phasors)
    {
        if (!Avx.IsSupported)
        {
            StabilizeVectorized(phasors); // fallback to generic vectorized version
            return;
        }

        var v256Size = Vector256<float>.Count; // 8 floats
        var interleaved = MemoryMarshal.Cast<ComplexF, float>(phasors);

        var vthree256 = Vector256.Create(3.0f);
        var vhalf256 = Vector256.Create(0.5f);

        Vector256<float> LoadPartial(ReadOnlySpan<float> span)
        {
            if (span.Length >= v256Size)
            {
                return Vector256.Create<float>(span);
            }
            else
            {
                Span<float> temp = stackalloc float[v256Size];
                span.CopyTo(temp);                            // copies only the tail
                temp[(v256Size - span.Length)..].Clear();     // zero-fill the rest
                return Vector256.Create<float>(temp);
            }
        }

        void SavePartial(Vector256<float> src, Span<float> span)
        {
            if (span.Length >= v256Size)
            {
                src.CopyTo(span);
            }
            else
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = src[i];
                }
            }
        }

        int i = 0;
        for (; i <= interleaved.Length - v256Size; i += v256Size)
        {
            // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
            var item = Vector256.Create<float>(interleaved[i..]);

            // Compute squared = item * item
            var squared = Avx.Multiply(item, item);
            var reimswapped = Avx.Shuffle(squared, squared, 0b10110001);

            // Compute magnitude squared: (r*r + i*i) for each complex pair
            var magsquared = Avx.Add(squared, reimswapped);

            // Compute k = 0.5f * (3.0f - magsquared)
            var diff = Avx.Subtract(vthree256, magsquared);
            var k = Avx.Multiply(vhalf256, diff);

            // Apply scaling: item = item * k
            var scaled_item = Avx.Multiply(item, k);

            scaled_item.CopyTo(interleaved[i..]);
        }

        // Handle remaining elements with generic vectorized approach
        if (i < interleaved.Length)
        {
            // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
            var item = LoadPartial(interleaved[i..]);

            // Compute squared = item * item
            var squared = Avx.Multiply(item, item);
            var reimswapped = Avx.Shuffle(squared, squared, 0b10_11_00_01);

            // Compute magnitude squared: (r*r + i*i) for each complex pair
            var magsquared = Avx.Add(squared, reimswapped);

            // Compute k = 0.5f * (3.0f - magsquared)
            var diff = Avx.Subtract(vthree256, magsquared);
            var k = Avx.Multiply(vhalf256, diff);

            // Apply scaling: item = item * k
            var scaled_item = Avx.Multiply(item, k);

            SavePartial(scaled_item, interleaved[i..]);
        }
    }

    void StabilizeVectorizedAvxP(ComplexF[] phasors)
    {
        if (!Avx.IsSupported)
        {
            StabilizeVectorized(phasors); // fallback to generic vectorized version
            return;
        }

        var v256Size = Vector256<float>.Count; // 8 floats
        var interleaved = MemoryMarshal.Cast<ComplexF, float>(phasors);

        var vthree256 = Vector256.Create(3.0f);
        var vhalf256 = Vector256.Create(0.5f);

        Vector256<float> LoadPartial(ReadOnlySpan<float> span)
        {
            if (span.Length >= v256Size)
            {
                return Vector256.Create<float>(span);
            }
            else
            {
                Span<float> temp = stackalloc float[v256Size];
                span.CopyTo(temp);                            // copies only the tail
                temp[(v256Size - span.Length)..].Clear();     // zero-fill the rest
                return Vector256.Create<float>(temp);
            }
        }

        void SavePartial(Vector256<float> src, Span<float> span)
        {
            if (span.Length >= v256Size)
            {
                src.CopyTo(span);
            }
            else
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = src[i];
                }
            }
        }

        int i = 0;
        for (; i <= interleaved.Length - v256Size; i += v256Size)
        {
            // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
            var item = Vector256.Create<float>(interleaved[i..]);

            // Compute squared = item * item
            var squared = Avx.Multiply(item, item);
            var reimswapped = Avx.Permute(squared, 0b10110001);

            // Compute magnitude squared: (r*r + i*i) for each complex pair
            var magsquared = Avx.Add(squared, reimswapped);

            // Compute k = 0.5f * (3.0f - magsquared)
            var diff = Avx.Subtract(vthree256, magsquared);
            var k = Avx.Multiply(vhalf256, diff);

            // Apply scaling: item = item * k
            var scaled_item = Avx.Multiply(item, k);

            scaled_item.CopyTo(interleaved[i..]);
        }

        // Handle remaining elements with generic vectorized approach
        if (i < interleaved.Length)
        {
            // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
            var item = LoadPartial(interleaved[i..]);

            // Compute squared = item * item
            var squared = Avx.Multiply(item, item);
            var reimswapped = Avx.Permute(squared, 0b10110001);

            // Compute magnitude squared: (r*r + i*i) for each complex pair
            var magsquared = Avx.Add(squared, reimswapped);

            // Compute k = 0.5f * (3.0f - magsquared)
            var diff = Avx.Subtract(vthree256, magsquared);
            var k = Avx.Multiply(vhalf256, diff);

            // Apply scaling: item = item * k
            var scaled_item = Avx.Multiply(item, k);

            SavePartial(scaled_item, interleaved[i..]);
        }
    }

    [Benchmark(Baseline = true)]
    public void OscillatorBankNormal()
    {
        for (int i = 0; i < 1000000; i++)
        {
            StabilizeNormal(phasors);
        }
    }

    //[Benchmark]
    //public void OscillatorBankVectorized()
    //{
    //    for (int i = 0; i < 1000000; i++)
    //    {
    //        StabilizeVectorized(phasors);
    //    }
    //}

    [Benchmark]
    public void OscillatorBankVectorizedAvx()
    {
        for (int i = 0; i < 1000000; i++)
        {
            StabilizeVectorizedAvx(phasors);
        }
    }

    [Benchmark]
    public void OscillatorBankVectorizedAvxP()
    {
        for (int i = 0; i < 1000000; i++)
        {
            StabilizeVectorizedAvxP(phasors);
        }
    }
#endif
}
