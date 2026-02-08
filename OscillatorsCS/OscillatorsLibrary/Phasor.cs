using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OscillatorsTest")]

namespace Baksteen.Oscillators;

/// <summary>
/// Phasor class:
/// A complex phasor allows to compute sinusoid values recursively.
/// Incremental calculations depend on frequency and sampling rate.
/// This is the base class for individual oscillators and resonators.
/// </summary>
public class Phasor : IPhasorProtocol
{
    private float _frequency;
    private float _sampleRate;

    /// <summary>
    /// Gets or sets the frequency of the phasor.
    /// </summary>
    public float Frequency
    {
        get => _frequency;
        set
        {
            _frequency = value;
            UpdateMultiplier();
        }
    }

    /// <summary>
    /// Gets or sets the sample rate for phase calculations.
    /// </summary>
    public float SampleRate
    {
        get => _sampleRate;
        set
        {
            _sampleRate = value;
            UpdateMultiplier();
        }
    }

    // Phasor variables
    // Phasor: Z = Zc + i Zs
    // Multiplier: W = Wc + i Ws

    /*
        if a time varying signal is decomposed into:
            x(t) = Zc*cos(wt)) + Zs*sin(wt)
     
        then its complex representation becomes:
            Z = Zc + i Zs

        where Zc and Zs are the real and imaginary parts of the phasor, respectively.
    */

    internal float Zc = 1.0f;
    internal float Zs = 0.0f;
    internal float Wc = 0.0f;
    internal float Ws = 0.0f;
    internal float Wcps = 0.0f; // pre-computed Wc + Ws

    /// <summary>
    /// Initializes a new instance of the Phasor class.
    /// </summary>
    /// <param name="frequency">The initial frequency.</param>
    /// <param name="sampleRate">The sample rate.</param>
    public Phasor(float frequency, float sampleRate)
    {
        _sampleRate = sampleRate;
        _frequency = frequency;
        UpdateMultiplier();
    }

    /// <summary>
    /// Updates the multiplier based on current frequency and sample rate.
    /// </summary>
    private void UpdateMultiplier()
    {
        var omega = (MathF.Tau * _frequency) / _sampleRate;
        Wc = MathF.Cos(omega);
        Ws = MathF.Sin(omega);
        Wcps = Wc + Ws;
    }

    /// <summary>
    /// Computes the next value of the phasor.
    /// Z <- Z * W
    /// </summary>
    internal void IncrementPhase()
    {
        // W <- W * O
        // complex multiplication with 3 real multiplications
        float ac = Wc * Zc;
        float bd = Ws * Zs;
        float abcd = Wcps * (Zc + Zs);
        Zc = ac - bd;
        Zs = abcd - ac - bd;
    }

    /// <summary>
    /// Applies re-normalization correction to compensate for numerical drift.
    /// Uses Taylor expansion around 1 to approximate 1/sqrt(x) to reduce computational cost.
    /// This can be applied every few hundred samples.
    /// </summary>
    internal void Stabilize()
    {
        float k = (3.0f - Zc * Zc - Zs * Zs) / 2.0f;
        Zc *= k;
        Zs *= k;
    }
}