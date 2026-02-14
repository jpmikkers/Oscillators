using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Baksteen.Oscillators;

public class ResonatorBankVectorizedAVX
{
    private readonly float _sampleRate;
    private readonly ComplexF[] _phasors;
    private readonly ComplexF[] _rotators;
    private readonly ComplexF[] _resonators;
    private readonly ComplexF[] _smoothResonators;

    private readonly float[] _alphas;
    private readonly float[] _betas;
    private readonly int _k;
    private int updateCount = 0;

    public ComplexF[] SmoothResonators => _smoothResonators;

    /// <summary>
    /// Computes the alpha heuristic for smoothing factor based on frequency and sample rate.
    /// </summary>
    /// <param name="frequency">The frequency.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <param name="k">The heuristic constant (default: 1).</param>
    /// <returns>The computed alpha value.</returns>
    private static float AlphaHeuristic(float frequency, float sampleRate, float k = 1.0f)
    {
        return 1.0f - MathF.Exp(-frequency / (sampleRate * k * MathF.Log10(1.0f + frequency)));
    }

    public ResonatorBankVectorizedAVX(float[] frequencies, float sampleRate, int k)
    {
        if (!Avx.IsSupported) throw new NotSupportedException("AVX not supported");

        _k = k;
        _sampleRate = sampleRate;
        _phasors = new ComplexF[frequencies.Length];
        _rotators = new ComplexF[frequencies.Length];
        _resonators = new ComplexF[frequencies.Length];
        _smoothResonators = new ComplexF[frequencies.Length];

        _alphas = new float[frequencies.Length];
        _betas = new float[frequencies.Length];

        for (int i = 0; i < frequencies.Length; i++)
        {
            _phasors[i] = ComplexF.FromPolar(1f, 0);
            var radiansPerSample = (MathF.Tau * frequencies[i]) / _sampleRate;
            _rotators[i] = ComplexF.FromPolar(1f, radiansPerSample);

            _alphas[i] = AlphaHeuristic(frequencies[i], sampleRate, k);
            _betas[i] = _alphas[i];
        }
    }

    static readonly Vector256<float> vthree256 = Vector256.Create(3.0f);
    static readonly Vector256<float> vhalf256 = Vector256.Create(0.5f);
    static readonly int v256Size = Vector256<float>.Count; // 8 floats

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> LoadPartial(ReadOnlySpan<float> span)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SavePartial(Vector256<float> src, Span<float> span)
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

    private static void StabilizeVectorizedAvx(ComplexF[] phasors)
    {
        var interleaved = MemoryMarshal.Cast<ComplexF, float>(phasors);

        var i = 0;
        for (; i <= interleaved.Length - v256Size; i += v256Size)
        {
            // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
            var item = Vector256.Create<float>(interleaved[i..]);

            // Compute squared = item * item
            var squared = Avx.Multiply(item, item);
            var reimswapped = Avx.Permute(squared, 0b10_11_00_01);

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
            // Load remaining floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
            var item = LoadPartial(interleaved[i..]);

            // Compute squared = item * item
            var squared = Avx.Multiply(item, item);
            var reimswapped = Avx.Permute(squared, 0b10_11_00_01);

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

    public void UpdateWithSample(float sample)
    {
        for (int i = 0; i < _resonators.Length; i++)
        {
            ref var phasor = ref _phasors[i];
            ref var resonator = ref _resonators[i];
            ref var smoothresonator = ref _smoothResonators[i];

            var alpha = _alphas[i];
            var beta = _betas[i];

            resonator = ((1f - alpha) * resonator) + (alpha * sample) * phasor;
            smoothresonator = ((1f - beta) * smoothresonator) + beta * resonator;

            // advance phasor (rotate by frequency)
            phasor *= _rotators[i];
        }

        if (updateCount++ >= 3)
        {
            updateCount = 0;
            StabilizeVectorizedAvx(_phasors);
        }
    }
}
