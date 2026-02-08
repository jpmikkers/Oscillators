using System;
using System.Linq;

namespace Baksteen.Oscillators;

/// <summary>
/// Speed of sound at room temperature, in m/s
/// </summary>

/// <summary>
/// A class to manipulate and compute frequencies in the digital world.
/// </summary>
public static class Frequencies
{
    internal const float SpeedOfSound = 346.0f;

    /// <summary>
    /// Computes and returns an array of equal temperament pitch frequencies
    /// between (and including) the notes at the two indices provided.
    /// In equal temperament, an interval of 1 semitone has a frequency ratio of 2^(1/12) (approx. 1.05946).
    /// The tuning is set for A4 which, if index 0 denotes C0, is index 57.
    /// Typical piano range from A0=9 (27.500 Hz) to C8=96 (4186.009 Hz).
    /// </summary>
    /// <param name="from">The starting note index.</param>
    /// <param name="to">The ending note index.</param>
    /// <param name="tuning">The tuning frequency for A4 (default: 440.0 Hz).</param>
    /// <returns>An array of frequencies.</returns>
    public static float[] MusicalPitchFrequencies(int from, int to, float tuning = 440.0f)
    {
        return Enumerable.Range(from, to - from + 1)
            .Select(idx => tuning * MathF.Pow(2.0f, (idx - 57) / 12.0f))
            .ToArray();
    }

    /// <summary>
    /// Computes and returns an array of frequencies with a log uniform distribution
    /// between (and including) the start and end frequencies.
    /// </summary>
    /// <param name="minFrequency">The minimum frequency (default: 32.70 Hz).</param>
    /// <param name="numBins">The number of bins (default: 84).</param>
    /// <param name="numBinsPerOctave">The number of bins per octave (default: 12).</param>
    /// <returns>An array of frequencies.</returns>
    public static float[] LogUniformFrequencies(float minFrequency = 32.70f, int numBins = 84, int numBinsPerOctave = 12)
    {
        return Enumerable.Range(0, numBins)
            .Select(bin => minFrequency * MathF.Pow(2.0f, (float)bin / numBinsPerOctave))
            .ToArray();
    }

    #region Mel Scale Conversions

    /// <summary>
    /// Computes and returns an array of acoustic frequencies tuned to the mel scale.
    /// Two implementations:
    /// Default: Slaney, M. Auditory Toolbox: A MATLAB Toolbox for Auditory Modeling Work. Technical Report, version 2, Interval Research Corporation, 1998.
    /// HTK: Young, S., Evermann, G., Gales, M., Hain, T., Kershaw, D., Liu, X., Moore, G., Odell, J., Ollason, D., Povey, D., Valtchev, V., & Woodland, P. The HTK book, version 3.4. Cambridge University, March 2009.
    /// </summary>
    /// <param name="numMels">The number of mel bins (default: 128).</param>
    /// <param name="minFrequency">The minimum frequency (default: 0.0 Hz).</param>
    /// <param name="maxFrequency">The maximum frequency (default: 11025.0 Hz).</param>
    /// <param name="htk">Use HTK formula if true, Slaney formula if false (default: false).</param>
    /// <returns>An array of mel-scale frequencies.</returns>
    public static float[] MelFrequencies(int numMels = 128, float minFrequency = 0.0f, float maxFrequency = 11025.0f, bool htk = false)
    {
        float minMel = htk ? HzToMelHTK(minFrequency) : HzToMel(minFrequency);
        float maxMel = htk ? HzToMelHTK(maxFrequency) : HzToMel(maxFrequency);

        float[] mels = new float[numMels];
        for (int i = 0; i < numMels; i++)
        {
            mels[i] = minMel + (maxMel - minMel) * i / (numMels - 1);
        }

        return htk ? MelsToHzHTK(mels) : MelsToHz(mels);
    }

    /// <summary>
    /// Converts Hz to Mels using the Matlab Auditory Toolbox formula.
    /// </summary>
    /// <param name="frequency">The frequency in Hz.</param>
    /// <returns>The frequency in mels.</returns>
    public static float HzToMel(float frequency)
    {
        const float minFrequency = 0.0f;
        const float spFrequency = 200.0f / 3.0f;
        const float minLogFrequency = 1000.0f;

        if (frequency < minLogFrequency)
        {
            // Linear range
            return (frequency - minFrequency) / spFrequency;
        }

        // Log range
        float minLogMel = (minLogFrequency - minFrequency) / spFrequency;
        float logstep = MathF.Log(6.4f) / 27.0f;
        return minLogMel + MathF.Log(frequency / minLogFrequency) / logstep;
    }

    /// <summary>
    /// Converts an array of Hz values to Mels using the Matlab Auditory Toolbox formula.
    /// </summary>
    /// <param name="frequencies">The frequencies in Hz.</param>
    /// <returns>An array of frequencies in mels.</returns>
    public static float[] HzToMel(float[] frequencies)
    {
        return frequencies.Select(frequency => HzToMel(frequency)).ToArray();
    }

    /// <summary>
    /// Converts Hz to Mels using the HTK formula.
    /// </summary>
    /// <param name="frequency">The frequency in Hz.</param>
    /// <returns>The frequency in mels.</returns>
    public static float HzToMelHTK(float frequency)
    {
        return 2595.0f * MathF.Log10(1.0f + frequency / 700.0f);
    }

    /// <summary>
    /// Converts an array of Hz values to Mels using the HTK formula.
    /// </summary>
    /// <param name="frequencies">The frequencies in Hz.</param>
    /// <returns>An array of frequencies in mels.</returns>
    public static float[] HzToMelHTK(float[] frequencies)
    {
        return frequencies.Select(frequency => HzToMelHTK(frequency)).ToArray();
    }

    /// <summary>
    /// Converts mel value to Hz using the Matlab Auditory Toolbox formula.
    /// </summary>
    /// <param name="mel">The frequency in mels.</param>
    /// <returns>The frequency in Hz.</returns>
    public static float MelToHz(float mel)
    {
        const float minFrequency = 0.0f;
        const float spFrequency = 200.0f / 3.0f;
        const float minLogFrequency = 1000.0f;

        float minLogMel = (minLogFrequency - minFrequency) / spFrequency;

        if (mel < minLogMel)
        {
            // Linear range
            return minFrequency + spFrequency * mel;
        }

        // Log range
        float logstep = MathF.Log(6.4f) / 27.0f;
        return minLogFrequency * MathF.Exp(logstep * (mel - minLogMel));
    }

    /// <summary>
    /// Converts an array of mel values to Hz using the Matlab Auditory Toolbox formula.
    /// </summary>
    /// <param name="mels">The frequencies in mels.</param>
    /// <returns>An array of frequencies in Hz.</returns>
    public static float[] MelsToHz(float[] mels)
    {
        return mels.Select(mel => MelToHz(mel)).ToArray();
    }

    /// <summary>
    /// Converts mel value to Hz using the HTK formula.
    /// </summary>
    /// <param name="mel">The frequency in mels.</param>
    /// <returns>The frequency in Hz.</returns>
    public static float MelToHzHTK(float mel)
    {
        return 700.0f * (MathF.Pow(10.0f, mel / 2595.0f) - 1.0f);
    }

    /// <summary>
    /// Converts an array of mel values to Hz using the HTK formula.
    /// </summary>
    /// <param name="mels">The frequencies in mels.</param>
    /// <returns>An array of frequencies in Hz.</returns>
    public static float[] MelsToHzHTK(float[] mels)
    {
        return mels.Select(mel => MelToHzHTK(mel)).ToArray();
    }

    #endregion

    #region Doppler Effect

    /// <summary>
    /// Computes the Doppler velocity from an observed and source frequency.
    /// </summary>
    /// <param name="observedFrequency">The frequency measured by the observer.</param>
    /// <param name="referenceFrequency">The frequency of the sound emitted by the source.</param>
    /// <returns>The relative velocity of the source to the receiver (positive when they are getting closer).</returns>
    public static float DopplerVelocity(float observedFrequency, float referenceFrequency)
    {
        if (referenceFrequency <= 0)
        {
            return 0.0f;
        }

        return SpeedOfSound * (observedFrequency - referenceFrequency) / referenceFrequency;
    }

    #endregion

#region Frequency Sweep

#if BROKEN
    /// <summary>
    /// Computes the equalizer coefficients for given frequencies and alphas.
    /// </summary>
    /// <param name="frequencies">The frequencies for the resonator bank.</param>
    /// <param name="alphas">Alphas (accumulation) for the resonator bank.</param>
    /// <param name="betas">The betas (smoothing) for the resonator bank. If null, uses alphas.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>An array of coefficients (one per frequency).</returns>
    public static float[] FrequencySweep(float[] frequencies, float[] alphas, float[] betas = null, float sampleRate = 44100.0f)
    {
        var output = new float[frequencies.Length];
        
        // Note: This implementation assumes ResonatorBankVec and Dynamics classes are available
        // The ResonatorBankVec would need to be ported from Swift for this to work
        var bank = new ResonatorBankVec(frequencies, alphas, betas, sampleRate);

        for (int idx = 0; idx < frequencies.Length; idx++)
        {
            var oscillator = new Oscillator(frequencies[idx], sampleRate);
            float duration = 40 * Dynamics.TimeConstant(alphas[idx], sampleRate);
            int numSamples = (int)(duration * sampleRate);
            float[] frame = oscillator.GetNextSamples(numSamples);
            
            bank.Update(frame);
            float[] powers = bank.Powers;
            
            float powerSum = powers.Sum();
            output[idx] = 0.25f / MathF.Sqrt(powerSum);
            
            bank.Reset();
        }

        return output;
    }
#endif

#endregion
}