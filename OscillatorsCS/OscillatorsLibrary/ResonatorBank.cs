using System;
using System.Numerics;

namespace Baksteen.Oscillators;

public class ResonatorBank
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

    public ResonatorBank(float[] frequencies, float sampleRate, int k)
    {
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

    private void Stabilize()
    {
        for (int i = 0; i < _phasors.Length; i++)
        {
            // https://github.com/FrankReiser/ReiserRT_FlyingPhasor
            var k = 1.0f - (_phasors[i].MagnitudeSquared - 1.0f) / 2.0f;
            // <=>
            // var k = 1.0f - (0.5*_phasors[i].MagnitudeSquared - 0.5f);
            // <=>
            // var k = 1.5f - 0.5*_phasors[i].MagnitudeSquared;
            // <=>
            // var k = (3.0f - magsquared) / 2.0f;

            _phasors[i] *= k;
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
            Stabilize(); // this is overkill but necessary
        }
    }
}
