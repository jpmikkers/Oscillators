namespace Baksteen.Oscillators;

/// <summary>
/// An oscillator is characterized by its frequency and amplitude.
/// Phase calculations depend on sampling rate.
/// </summary>
public interface IOscillatorProtocol
{
    /// <summary>
    /// Gets or sets the amplitude of the oscillator.
    /// </summary>
    float Amplitude { get; set; }

    /// <summary>
    /// Gets the current sample value.
    /// </summary>
    float Sample { get; }

    /// <summary>
    /// Gets the next sample and increments the phase.
    /// </summary>
    /// <returns>The next sample value.</returns>
    float GetNextSample();

    /// <summary>
    /// Gets the next sequence of samples.
    /// </summary>
    /// <param name="numSamples">The number of samples to generate.</param>
    /// <returns>An array of samples.</returns>
    float[] GetNextSamples(int numSamples);

    /// <summary>
    /// Fills the provided array with the next sequence of samples.
    /// </summary>
    /// <param name="samples">The array to fill with samples.</param>
    void GetNextSamples(float[] samples);
}