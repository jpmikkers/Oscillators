namespace Baksteen.Oscillators;

/// <summary>
/// An oscillator that resonates with a specific frequency if present in an input signal,
/// i.e. that naturally oscillates with greater amplitude at a given frequency, than at other frequencies.
/// </summary>
public interface IResonatorProtocol
{
    /// <summary>
    /// Gets the power (squared amplitude) of the resonator.
    /// </summary>
    float Power { get; }

    /// <summary>
    /// Gets the amplitude of the resonator.
    /// </summary>
    float Amplitude { get; }

    /// <summary>
    /// Gets or sets the smoothing factor for the resonator output (range: 0-1).
    /// </summary>
    float Alpha { get; set; }

    /// <summary>
    /// Gets the current frequency of the resonator.
    /// </summary>
    float Frequency { get; }

    /// <summary>
    /// Gets the tracked frequency (only updated during UpdateAndTrack operations).
    /// </summary>
    float TrackedFrequency { get; }

    /// <summary>
    /// Performs an update of the resonator amplitude from a single sample.
    /// </summary>
    /// <param name="sample">The input sample.</param>
    void Update(float sample);

    /// <summary>
    /// Performs an update of the resonator amplitude from an array of samples.
    /// </summary>
    /// <param name="samples">The input samples.</param>
    void Update(float[] samples);

    /// <summary>
    /// Performs an update of the resonator amplitude from a buffer of samples.
    /// </summary>
    /// <param name="frameData">Pointer to the frame data.</param>
    /// <param name="frameLength">The number of frames.</param>
    /// <param name="sampleStride">The stride between samples in the buffer.</param>
    void Update(float[] frameData, int frameLength, int sampleStride);

    /// <summary>
    /// Performs an update of the resonator amplitude from a single sample and tracks frequency.
    /// </summary>
    /// <param name="sample">The input sample.</param>
    void UpdateAndTrack(float sample);

    /// <summary>
    /// Performs an update of the resonator amplitude from an array of samples and tracks frequency.
    /// </summary>
    /// <param name="samples">The input samples.</param>
    void UpdateAndTrack(float[] samples);

    /// <summary>
    /// Performs an update of the resonator amplitude from a buffer of samples and tracks frequency.
    /// </summary>
    /// <param name="frameData">The frame data buffer.</param>
    /// <param name="frameLength">The number of frames.</param>
    /// <param name="sampleStride">The stride between samples in the buffer.</param>
    void UpdateAndTrack(float[] frameData, int frameLength, int sampleStride);
}