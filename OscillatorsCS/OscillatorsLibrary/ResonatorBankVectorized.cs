using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Baksteen.Oscillators;

public class ResonatorBankVectorized
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

    public ResonatorBankVectorized(float[] frequencies, float sampleRate, int k)
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

    static readonly Vector<float> vthree = new(3.0f);
    static readonly Vector<float> vhalf = new(0.5f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void StabilizeVec(ref Vector<float> item)
    {
        var squared = item * item;
        var swaptmp = Vector.AsVectorUInt64(squared);
        var reimswapped = ((swaptmp >> 32) | (swaptmp << 32)).As<ulong, float>();
        var magsquared = squared + reimswapped;
        item *= vhalf * (vthree - magsquared);
    }

    private void Stabilize()
    {
        var vecsize = Vector<float>.Count;

        var interleaved = MemoryMarshal.Cast<ComplexF, Vector<float>>(_phasors);

        for (var j = 0; j < interleaved.Length; j++)
        {
            StabilizeVec(ref interleaved[j]);
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

        int remaining = (_phasors.Length * 2) % vecsize;

        if (remaining > 0)
        {
            var taildata = MemoryMarshal.Cast<ComplexF, float>(_phasors)[^remaining..];

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
