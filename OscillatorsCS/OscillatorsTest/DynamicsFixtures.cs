
using System;
using System.Linq;
using Baksteen.Oscillators;

namespace OscillatorsTest;

internal static class DynamicsFixtures
{
    public const float DefaultAlpha = 1.0f / (AudioFixtures.DefaultSampleRate * 0.1f);
    public const float DefaultTimeConstant = 0.1f;

    private static readonly float[] TimeConstantsValue = { 0.01f, 0.02f, 0.05f, 0.10f, 0.20f, 0.50f, 0.80f, 1.0f, 1.5f, 2.0f };

    public static float[] TimeConstants => TimeConstantsValue;

    public static float[] Alphas => TimeConstantsValue
        .Select(tc => Dynamics.Alpha(tc, AudioFixtures.DefaultSampleRate))
        .ToArray();
}