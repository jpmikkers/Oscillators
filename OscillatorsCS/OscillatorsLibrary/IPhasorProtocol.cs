namespace Baksteen.Oscillators;

/// <summary>
/// An oscillator is characterized by its frequency and amplitude.
/// Phase calculations depend on sampling rate.
/// </summary>
public interface IPhasorProtocol
{
    /// <summary>
    /// Gets or sets the frequency of the phasor.
    /// </summary>
    float Frequency { get; set; }

    /// <summary>
    /// Gets or sets the sample rate for phase calculations.
    /// </summary>
    float SampleRate { get; set; }
}