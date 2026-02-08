using System;

namespace Baksteen.Oscillators;

/// <summary>
/// A class to manipulate and compute dynamics parameters in the digital world.
/// </summary>
public static class Dynamics
{
    /// <summary>
    /// Computes the time constant from an alpha value for a given sample rate.
    /// </summary>
    /// <param name="alpha">The alpha smoothing factor.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>The time constant value.</returns>
    public static float TimeConstant(float alpha, float sampleRate)
    {
        return -1.0f / (sampleRate * MathF.Log(1.0f - alpha));
    }

    /// <summary>
    /// Computes the alpha value from a time constant for a given sample rate.
    /// </summary>
    /// <param name="timeConstant">The time constant value.</param>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>The alpha smoothing factor.</returns>
    public static float Alpha(float timeConstant, float sampleRate)
    {
        return 1.0f - MathF.Exp(-1.0f / (sampleRate * timeConstant));
    }
}