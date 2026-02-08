namespace Baksteen.Oscillators;

/// <summary>
/// Oscillator class:
/// An oscillator is characterized by its frequency and amplitude.
/// Waveform values are computed recursively with a complex phasor.
/// Incremental calculations depend on frequency and sampling rate.
/// </summary>
public class Oscillator : Phasor, IOscillatorProtocol
{
    private float _amplitude = 1.0f;

    /// <summary>
    /// Gets or sets the amplitude of the oscillator.
    /// </summary>
    public float Amplitude
    {
        get => _amplitude;
        set => _amplitude = value;
    }

    /// <summary>
    /// Gets the current sample value.
    /// </summary>
    public float Sample => _amplitude * Zc;

    /// <summary>
    /// Initializes a new instance of the Oscillator class.
    /// </summary>
    /// <param name="frequency">The initial frequency.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <param name="amplitude">The amplitude (default: 1.0).</param>
    public Oscillator(float frequency, float sampleRate, float amplitude = 1.0f)
        : base(frequency, sampleRate)
    {
        _amplitude = amplitude;
    }

    /// <summary>
    /// Gets the next sample and increments the phase.
    /// </summary>
    /// <returns>The next sample value.</returns>
    public float GetNextSample()
    {
        float nextSample = Sample;
        IncrementPhase();
        Stabilize();
        return nextSample;
    }

    /// <summary>
    /// Gets the next sequence of samples.
    /// </summary>
    /// <param name="numSamples">The number of samples to generate.</param>
    /// <returns>An array of samples.</returns>
    public float[] GetNextSamples(int numSamples)
    {
        var samples = new float[numSamples];
        GetNextSamples(samples);
        return samples;
    }

    /// <summary>
    /// Fills the provided array with the next sequence of samples.
    /// </summary>
    /// <param name="samples">The array to fill with samples.</param>
    public void GetNextSamples(float[] samples)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Sample;
            IncrementPhase();
        }
        Stabilize();
    }
}