using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Baksteen.Oscillators;

public class ResonatorBankVectorizedAVXBundled3 : IResonatorBank
{
    private struct Bundle
    {
        public Vector256<float> phasor;
        public Vector256<float> rotator;
        public Vector256<float> resonator;
        public Vector256<float> smoothresonator;
        public Vector256<float> alpha;
        public Vector256<float> beta;
    }

    private readonly Bundle[] _bundles;
    private readonly ComplexF[] _smoothResonators;

    private readonly float _sampleRate;
    private readonly int _numChannels;
    private readonly int _channelsPerBundle;

    private readonly int _k;
    private int updateCount = 0;

    static readonly Vector256<float> vthree256 = Vector256.Create(3.0f);
    static readonly Vector256<float> vhalf256 = Vector256.Create(0.5f);

    // o/` no need to ask, he's a..
    public ComplexF[] SmoothResonators
    {
        get
        {
            CopySmoothResonators();
            return _smoothResonators;    // TODO!!!
        }
    }

    ReadOnlyMemory<ComplexF> IResonatorBank.SmoothResonators => SmoothResonators;

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

    public ResonatorBankVectorizedAVXBundled3(float[] frequencies, float sampleRate, int k)
    {
        if (!Avx.IsSupported) throw new NotSupportedException("AVX not supported");

        _sampleRate = sampleRate;
        _numChannels = frequencies.Length;
        _channelsPerBundle = Vector256<float>.Count / 2;
        _k = k;

        _bundles = new Bundle[(_numChannels + _channelsPerBundle - 1) / _channelsPerBundle];
        _smoothResonators = new ComplexF[_numChannels];

        for (var i = 0; i < _bundles.Length; i++)
        {
            ref var bundle = ref _bundles[i];

            var phasors = new ComplexF[_channelsPerBundle];
            var rotators = new ComplexF[_channelsPerBundle];
            var alphas = new ComplexF[_channelsPerBundle];
            var betas = new ComplexF[_channelsPerBundle];

            var usedChannels = Math.Min(_channelsPerBundle, _numChannels - (i*_channelsPerBundle));

            for (var f = 0; f < usedChannels; f++)
            {
                var frequency = frequencies[i * _channelsPerBundle + f];

                phasors[f] = ComplexF.FromPolar(1f, 0);
                var radiansPerSample = (MathF.Tau * frequency) / _sampleRate;
                rotators[f] = ComplexF.FromPolar(1f, radiansPerSample);

                var alpha = AlphaHeuristic(frequency, _sampleRate, _k);
                alphas[f] = new ComplexF(alpha, alpha);
                betas[f] = new ComplexF(alpha, alpha);
            }

            bundle.phasor = Vector256.Create(MemoryMarshal.Cast<ComplexF,float>(phasors));
            bundle.rotator = Vector256.Create(MemoryMarshal.Cast<ComplexF, float>(rotators));
            bundle.alpha = Vector256.Create(MemoryMarshal.Cast<ComplexF, float>(alphas));
            bundle.beta = Vector256.Create(MemoryMarshal.Cast<ComplexF, float>(betas));
        }
    }

    private void UpdateWithSample(float sample)
    {
        var bs = _bundles.AsSpan();

        foreach (ref var bundle in bs)
        {
            var phasor = bundle.phasor;
            var resonator = Vector256Helpers.Lerp(bundle.resonator, phasor*sample, bundle.alpha);
            var smoothresonator = Vector256Helpers.Lerp(bundle.smoothresonator, resonator, bundle.beta);

            // advance phasor
            bundle.phasor = Vector256Helpers.ComplexMulAlt4(phasor, bundle.rotator);

            // store new resonator values
            bundle.resonator = resonator;
            bundle.smoothresonator = smoothresonator;
        }

        if (updateCount++ >= 3)
        {
            updateCount = 0;
            foreach (ref var bundle in bs)
            {
                bundle.phasor = Vector256Helpers.ComplexNormalizeFast(bundle.phasor);
            }
        }
    }

    private void CopySmoothResonators()
    {
        var i = 0;
        var bi = 0;

        var fsmoothresonators = MemoryMarshal.Cast<ComplexF, float>(_smoothResonators.AsSpan());
        ref var smoothresonatorsref = ref MemoryMarshal.GetReference(fsmoothresonators);

        for (; i <= (_numChannels - _channelsPerBundle); i+=_channelsPerBundle, bi++)
        {
            Vector256.StoreUnsafe(_bundles[bi].smoothresonator, ref smoothresonatorsref, (nuint)i*2);
        }

        if (i < _numChannels)
        {
            Vector256Helpers.SavePartial(_bundles[bi].smoothresonator, fsmoothresonators[(i*2)..]);
        }
    }

    public void UpdateWithSamples(ReadOnlySpan<float> samples)
    {
        foreach (var sample in samples)
        {
            UpdateWithSample(sample);
        }
    }
}
