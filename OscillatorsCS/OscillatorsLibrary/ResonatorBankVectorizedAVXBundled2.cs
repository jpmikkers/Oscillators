using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Baksteen.Oscillators;

public class ResonatorBankVectorizedAVXBundled2
{
    private struct Bundle(Vector256<float> phasor, Vector256<float> rotator, Vector128<float> alpha, Vector128<float> beta)
    {
        public Vector256<float> phasor = phasor;
        public readonly Vector256<float> rotator = rotator;
        public Vector256<float> resonator;
        public Vector256<float> smoothresonator;
        public readonly Vector128<float> alpha = alpha;
        public readonly Vector128<float> beta = beta;
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
    public ComplexF[] SmoothResonators => _smoothResonators;    // TODO!!!

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

    public ResonatorBankVectorizedAVXBundled2(float[] frequencies, float sampleRate, int k)
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
            var alphas = new float[_channelsPerBundle];
            var betas = new float[_channelsPerBundle];

            var usedChannels = Math.Min(_channelsPerBundle, _numChannels - (i*_channelsPerBundle));

            for (var f = 0; f < usedChannels; f++)
            {
                var frequency = frequencies[i * _channelsPerBundle + f];

                phasors[f] = ComplexF.FromPolar(1f, 0);
                var radiansPerSample = (MathF.Tau * frequency) / _sampleRate;
                rotators[f] = ComplexF.FromPolar(1f, radiansPerSample);

                var alpha = AlphaHeuristic(frequency, _sampleRate, _k);
                alphas[f] = alpha;
                betas[f] = alpha;
            }

            bundle = new Bundle(
                Vector256.Create(MemoryMarshal.Cast<ComplexF, float>(phasors)),
                Vector256.Create(MemoryMarshal.Cast<ComplexF, float>(rotators)),
                Vector128.Create(alphas),
                Vector128.Create(betas));
        }
    }


    private void StabilizeVectorizedAvx()
    {
        for (var i=0; i < _bundles.Length; i++)
        {
            ref var bundle = ref _bundles[i];

            var item = bundle.phasor;

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

            bundle.phasor = scaled_item;
        }
    }

    public void UpdateWithSample(float sample)
    {
        for (var i=0; i < _bundles.Length; i++)
        {
            ref var bundle = ref _bundles[i];
            
            var phasor = bundle.phasor;
            var resonator = bundle.resonator;
            var smoothresonator = bundle.smoothresonator;

            var alpha = Vector256Helpers.DuplicateInterleaved(bundle.alpha);
            var beta = Vector256Helpers.DuplicateInterleaved(bundle.beta);

            var phasorSample = phasor * sample;

            //resonator = Vector256.Lerp(resonator, phasorSample, bundle.alpha);
            //smoothresonator = Vector256.Lerp(smoothresonator, resonator, bundle.beta);
            resonator = Vector256Helpers.Lerp(resonator, phasorSample, alpha);
            smoothresonator = Vector256Helpers.Lerp(smoothresonator, resonator, beta);

            // advance phasor
            phasor = Vector256Helpers.ComplexMul(phasor, bundle.rotator);

            // store as late as possible, seems like the jit compiler doesn't try to reorder these optimally
            bundle.resonator = resonator;
            bundle.smoothresonator = smoothresonator;
            bundle.phasor = phasor;
        }

        if (updateCount++ >= 3)
        {
            updateCount = 0;
            StabilizeVectorizedAvx();
        }
    }
}
