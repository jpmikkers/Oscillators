namespace Baksteen.Oscillators;

/// <summary>
/// An oscillator that resonates with a specific frequency if present in an input signal,
/// i.e. that naturally oscillates with greater amplitude at a given frequency, than at other frequencies.
/// </summary>
public class Resonator : Phasor, IResonatorProtocol
{
    private const float TwoPi = MathF.PI * 2.0f;
    private const float TrackFrequencyThreshold = 0.001f;

    private float _alpha;
    private float _omAlpha = 0.0f;
    private float _beta;
    private float _omBeta = 0.0f;

    // Complex: r = c + j s
    private float _c = 0.0f;
    private float _s = 0.0f;

    // Smoothed resonator output
    private float _cc = 0.0f;
    private float _ss = 0.0f;

    private float _phase = 0.0f;
    private float _trackedFrequency = 0.0f;

    /// <summary>
    /// Gets the power (squared amplitude) of the resonator.
    /// </summary>
    public float Power => _cc * _cc + _ss * _ss;

    /// <summary>
    /// Gets the amplitude of the resonator.
    /// </summary>
    public float Amplitude => MathF.Sqrt(_cc * _cc + _ss * _ss);

    /// <summary>
    /// Gets or sets the smoothing factor for the resonator output (range: 0-1).
    /// </summary>
    public float Alpha
    {
        get => _alpha;
        set
        {
            _alpha = value;
            _omAlpha = 1.0f - _alpha;
        }
    }

    /// <summary>
    /// Gets or sets the secondary smoothing factor for the resonator output (range: 0-1).
    /// </summary>
    public float Beta
    {
        get => _beta;
        set
        {
            _beta = value;
            _omBeta = 1.0f - _beta;
        }
    }

    /// <summary>
    /// Gets the current phase.
    /// </summary>
    public float Phase => _phase;

    /// <summary>
    /// Gets the tracked frequency (only updated during UpdateAndTrack operations).
    /// </summary>
    public float TrackedFrequency => _trackedFrequency;

    /// <summary>
    /// Initializes a new instance of the Resonator class.
    /// </summary>
    /// <param name="frequency">The resonant frequency.</param>
    /// <param name="alpha">The primary smoothing factor (range: 0-1).</param>
    /// <param name="beta">The secondary smoothing factor (range: 0-1). If null, uses alpha value.</param>
    /// <param name="sampleRate">The sample rate.</param>
    public Resonator(float frequency, float alpha, float? beta, float sampleRate)
        : base(frequency, sampleRate)
    {
        Alpha = alpha;
        Beta = beta ?? alpha;
        _trackedFrequency = frequency;
    }

    /// <summary>
    /// Computes the alpha heuristic for smoothing factor based on frequency and sample rate.
    /// </summary>
    /// <param name="frequency">The frequency.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <param name="k">The heuristic constant (default: 1).</param>
    /// <returns>The computed alpha value.</returns>
    public static float AlphaHeuristic(float frequency, float sampleRate, float k = 1.0f)
    {
        return 1.0f - MathF.Exp(-frequency / (sampleRate * k * MathF.Log10(1.0f + frequency)));
    }

    /// <summary>
    /// Updates the resonator with a single sample (internal method).
    /// </summary>
    private void UpdateWithSample(float sample)
    {
        float alphaSample = Alpha * sample;
        _c = _omAlpha * _c + alphaSample * Zc;
        _s = _omAlpha * _s + alphaSample * Zs;
        _cc = _omBeta * _cc + Beta * _c;
        _ss = _omBeta * _ss + Beta * _s;
        IncrementPhase();
    }

    /// <summary>
    /// Performs an update of the resonator amplitude from a single sample.
    /// </summary>
    /// <param name="sample">The input sample.</param>
    public void Update(float sample)
    {
        UpdateWithSample(sample);
        Stabilize();
    }

    /// <summary>
    /// Performs an update of the resonator amplitude from an array of samples.
    /// </summary>
    /// <param name="samples">The input samples.</param>
    public void Update(float[] samples)
    {
        foreach (float sample in samples)
        {
            UpdateWithSample(sample);
        }
        Stabilize();
    }

    /// <summary>
    /// Performs an update of the resonator amplitude from a buffer of samples.
    /// </summary>
    /// <param name="frameData">The frame data buffer.</param>
    /// <param name="frameLength">The number of frames.</param>
    /// <param name="sampleStride">The stride between samples in the buffer.</param>
    public void Update(float[] frameData, int frameLength, int sampleStride)
    {
        for (int sampleIndex = 0; sampleIndex < sampleStride * frameLength; sampleIndex += sampleStride)
        {
            UpdateWithSample(frameData[sampleIndex]);
        }
        Stabilize();
    }

    /// <summary>
    /// Performs an update of the resonator amplitude from a single sample and tracks frequency.
    /// </summary>
    /// <param name="sample">The input sample.</param>
    public void UpdateAndTrack(float sample)
    {
        UpdateWithSample(sample);
        Stabilize();
        if (Amplitude > TrackFrequencyThreshold)
        {
            UpdateTrackedFrequency(numSamples: 1);
        }
        else
        {
            _trackedFrequency = Frequency;
        }
    }

    /// <summary>
    /// Performs an update of the resonator amplitude from an array of samples and tracks frequency.
    /// </summary>
    /// <param name="samples">The input samples.</param>
    public void UpdateAndTrack(float[] samples)
    {
        foreach (float sample in samples)
        {
            UpdateWithSample(sample);
        }
        Stabilize();
        if (Amplitude > TrackFrequencyThreshold)
        {
            UpdateTrackedFrequency(numSamples: samples.Length);
        }
        else
        {
            _trackedFrequency = Frequency;
        }
    }

    /// <summary>
    /// Performs an update of the resonator amplitude from a buffer of samples and tracks frequency.
    /// </summary>
    /// <param name="frameData">The frame data buffer.</param>
    /// <param name="frameLength">The number of frames.</param>
    /// <param name="sampleStride">The stride between samples in the buffer.</param>
    public void UpdateAndTrack(float[] frameData, int frameLength, int sampleStride)
    {
        for (int sampleIndex = 0; sampleIndex < sampleStride * frameLength; sampleIndex += sampleStride)
        {
            UpdateWithSample(frameData[sampleIndex]);
        }
        Stabilize();
        if (Amplitude > TrackFrequencyThreshold)
        {
            UpdateTrackedFrequency(numSamples: frameLength);
        }
        else
        {
            _trackedFrequency = Frequency;
        }
    }

    /// <summary>
    /// Updates the tracked frequency based on phase drift.
    /// </summary>
    private void UpdateTrackedFrequency(int numSamples)
    {
        float newPhase = MathF.Atan2(_ss, _cc);
        float phaseDrift = newPhase - _phase;
        _phase = newPhase;

        if (phaseDrift <= -MathF.PI)
        {
            phaseDrift += TwoPi;
        }
        else if (phaseDrift > MathF.PI)
        {
            phaseDrift -= TwoPi;
        }

        _trackedFrequency = Frequency - phaseDrift * SampleRate / (TwoPi * numSamples);
    }
}