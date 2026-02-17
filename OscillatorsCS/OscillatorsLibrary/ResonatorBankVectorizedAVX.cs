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

    // o/` no need to ask, he's a..
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

        _alphas = new float[frequencies.Length*2];
        _betas = new float[frequencies.Length*2];

        for (int i = 0; i < frequencies.Length; i++)
        {
            _phasors[i] = ComplexF.FromPolar(1f, 0);
            var radiansPerSample = (MathF.Tau * frequencies[i]) / _sampleRate;
            _rotators[i] = ComplexF.FromPolar(1f, radiansPerSample);

            _alphas[i*2] = AlphaHeuristic(frequencies[i], sampleRate, k);
            _alphas[i*2+1] = _alphas[i*2];
            _betas[i*2] = _alphas[i*2];
            _betas[i * 2 + 1] = _betas[i * 2];
        }
    }

    static readonly Vector256<float> vthree256 = Vector256.Create(3.0f);
    static readonly Vector256<float> vhalf256 = Vector256.Create(0.5f);

    static readonly int v256Size = Vector256<float>.Count; // 8 floats

    private static void StabilizeVectorizedAvx(ComplexF[] phasors)
    {
#if SCALAR
        for (int idx = 0; idx < phasors.Length; idx++)
        {
            // https://github.com/FrankReiser/ReiserRT_FlyingPhasor

            var k = 0.5f * (3.0f - phasors[idx].MagnitudeSquared);
            // var k = 1.0f - (phasors[idx].MagnitudeSquared - 1.0f) / 2.0f;
            // <=>
            // var k = 1.0f - (0.5*_phasors[i].MagnitudeSquared - 0.5f);
            // <=>
            // var k = 1.5f - 0.5*_phasors[i].MagnitudeSquared;
            // <=>
            // var k = (3.0f - magsquared) / 2.0f;

            phasors[idx] *= k;
        }
#endif

        var interleaved = MemoryMarshal.Cast<ComplexF, float>(phasors.AsSpan());

        var i = 0;
        for (; i <= interleaved.Length - v256Size; i += v256Size)
        {
            // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
            var item = Vector256.Create(interleaved[i..]);

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
            var item = Vector256Helpers.LoadPartial(interleaved[i..]);

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
            Vector256Helpers.SavePartial(scaled_item, interleaved[i..]);
        }
    }

    private static void AdvancePhasors(Span<ComplexF> phasors, ReadOnlySpan<ComplexF> rotators)
    {
        var fphasors = MemoryMarshal.Cast<ComplexF, float>(phasors);
        var frotators = MemoryMarshal.Cast<ComplexF, float>(rotators);

        ref var phasorsref = ref MemoryMarshal.GetReference(fphasors);
        ref var rotatorsref = ref MemoryMarshal.GetReference(frotators);

        var i = 0;
        for (; i <= fphasors.Length - v256Size; i += v256Size)
        {
            var x = Vector256.LoadUnsafe(ref phasorsref,(nuint)i);
            var y = Vector256.LoadUnsafe(ref rotatorsref,(nuint)i);
            Vector256Helpers.ComplexMul(x, y).StoreUnsafe(ref phasorsref, (nuint)i);
        }

        // Handle remaining elements with generic vectorized approach
        if (i < fphasors.Length)
        {
            var x = Vector256Helpers.LoadPartial(fphasors[i..]);
            var y = Vector256Helpers.LoadPartial(frotators[i..]);
            Vector256Helpers.SavePartial(Vector256Helpers.ComplexMul(x, y), fphasors[i..]);
        }
    }

    public void UpdateWithSample(float sample)
    {
        var fphasors = MemoryMarshal.Cast<ComplexF, float>(_phasors.AsSpan());
        var fresonators = MemoryMarshal.Cast<ComplexF, float>(_resonators.AsSpan());
        var fsmoothresonators = MemoryMarshal.Cast<ComplexF, float>(_smoothResonators.AsSpan());
        var frotators = MemoryMarshal.Cast<ComplexF, float>(_rotators);

        ref var phasorsref = ref MemoryMarshal.GetReference(fphasors);
        ref var rotatorsref = ref MemoryMarshal.GetReference(frotators);
        ref var resonatorsref = ref MemoryMarshal.GetReference(fresonators);
        ref var smoothresonatorsref = ref MemoryMarshal.GetReference(fsmoothresonators);
        ref var alphasref = ref MemoryMarshal.GetReference(_alphas);
        ref var betasref = ref MemoryMarshal.GetReference(_betas);

        var i = 0;
        for (; i <= (fresonators.Length - v256Size); i+= v256Size)
        {
            var phasor = Vector256.LoadUnsafe(ref phasorsref, (nuint)i);
            var resonator = Vector256.LoadUnsafe(ref resonatorsref, (nuint)i);
            var smoothresonator = Vector256.LoadUnsafe(ref smoothresonatorsref, (nuint)i);

            var alpha = Vector256.LoadUnsafe(ref alphasref, (nuint)i);
            var beta = Vector256.LoadUnsafe(ref betasref, (nuint)i);

            var phasorSample = phasor * sample;

            // faster than builtin Vector256.Lerp
            resonator = Vector256Helpers.Lerp(resonator, phasorSample, alpha);
            smoothresonator = Vector256Helpers.Lerp(smoothresonator, resonator, beta);

            Vector256.StoreUnsafe(resonator, ref resonatorsref, (nuint)i);
            Vector256.StoreUnsafe(smoothresonator, ref smoothresonatorsref, (nuint)i);

            // advance phasor
            var rotator = Vector256.LoadUnsafe(ref rotatorsref, (nuint)i);
            Vector256Helpers.ComplexMul(phasor,rotator).StoreUnsafe(ref phasorsref, (nuint)i);
        }

        for (var idx = i / 2; idx < _resonators.Length; idx++)
        {
            ref var phasor = ref _phasors[idx];
            ref var resonator = ref _resonators[idx];
            ref var smoothresonator = ref _smoothResonators[idx];

            resonator = ComplexF.Lerp(resonator, sample * phasor, _alphas[idx * 2]);
            smoothresonator = ComplexF.Lerp(smoothresonator, resonator, _betas[idx * 2]);

            // advance phasor (rotate by frequency)
            phasor *= _rotators[idx];
        }

#if TOOSLOW
        if (i < fresonators.Length)
        {
            var phasor = Vector256Helpers.LoadPartial(fphasors[i..]);
            var resonator = Vector256Helpers.LoadPartial(fresonators[i..]);
            var smoothresonator = Vector256Helpers.LoadPartial(fsmoothresonators[i..]);

            var alpha = Vector256Helpers.LoadPartial(_alphas[i..]);
            var beta = Vector256Helpers.LoadPartial(_betas[i..]);

            var phasorSample = phasor * sample;

            resonator = Vector256.Lerp(resonator, phasorSample, alpha);
            smoothresonator = Vector256.Lerp(smoothresonator, resonator, beta);

            Vector256Helpers.SavePartial(resonator, fresonators[i..]);
            Vector256Helpers.SavePartial(smoothresonator, fsmoothresonators[i..]);

            // advance phasor
            var rotator = Vector256Helpers.LoadPartial(fresonators[i..]);
            Vector256Helpers.SavePartial(Vector256Helpers.ComplexMul(phasor, rotator), fphasors[i..]);
        }
#endif

        if (updateCount++ >= 3)
        {
            updateCount = 0;
            StabilizeVectorizedAvx(_phasors);
        }
    }
}
