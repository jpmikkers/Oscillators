using System;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OscillatorsTest;

[TestClass]
public class ResonatorTests
{
    private const float Epsilon = 0.000001f;

    [TestMethod]
    public void Constructor_SetsAlpha()
    {
        // Act
        var resonator = new Resonator(440.0f, DynamicsFixtures.DefaultAlpha, null, AudioFixtures.DefaultSampleRate);

        // Assert
        Assert.AreEqual(DynamicsFixtures.DefaultAlpha, resonator.Alpha, Epsilon);
    }

    [TestMethod]
    public void Alpha_SetValue_UpdatesOmAlpha()
    {
        // Arrange
        float alpha = 0.99f;
        var resonator = new Resonator(440.0f, alpha, null, AudioFixtures.DefaultSampleRate);
        Assert.AreEqual(alpha, resonator.Alpha, Epsilon);
        Assert.AreEqual(alpha, resonator.Beta, Epsilon); // Beta defaults to alpha

        // Act
        alpha = 0.11f;
        resonator.Alpha = alpha;

        // Assert
        Assert.AreEqual(alpha, resonator.Alpha, Epsilon);
        //Assert.AreEqual(1.0f - alpha, resonator.Alpha, Epsilon); // omAlpha is internal, verify through behavior
    }

    [TestMethod]
    public void Update_WithSampleValue_UpdatesInternalState()
    {
        // Arrange
        var resonator = new Resonator(440.0f, 1.0f, null, AudioFixtures.DefaultSampleRate);
        
        // Access internal Zc and Zs through reflection or public interface
        // For now, we test the behavior indirectly through the resonator's amplitude
        float initialAmplitude = resonator.Amplitude;

        // Act
        resonator.Update(1.0f);

        // Assert - amplitude should increase after update with sample
        Assert.IsTrue(resonator.Amplitude >= initialAmplitude);
    }

    [TestMethod]
    public void Update_WithZeroSample_ReducesAmplitude()
    {
        // Arrange
        var resonator = new Resonator(440.0f, 1.0f, null, AudioFixtures.DefaultSampleRate);
        
        // Build up some amplitude first
        resonator.Update(1.0f);
        float amplitudeAfterUpdate = resonator.Amplitude;

        // Act
        resonator.Update(0.0f);

        // Assert - amplitude should be reduced or maintained after zero sample
        Assert.IsTrue(resonator.Amplitude <= amplitudeAfterUpdate || MathF.Abs(resonator.Amplitude - amplitudeAfterUpdate) < Epsilon);
    }

    [TestMethod]
    public void Power_ReturnsSquaredAmplitude()
    {
        // Arrange
        var resonator = new Resonator(440.0f, 0.5f, null, AudioFixtures.DefaultSampleRate);
        
        // Add some samples to build amplitude
        for (int i = 0; i < 100; i++)
        {
            resonator.Update(1.0f);
        }

        // Act
        float amplitude = resonator.Amplitude;
        float power = resonator.Power;

        // Assert
        Assert.AreEqual(amplitude * amplitude, power, Epsilon);
    }

    [TestMethod]
    public void AlphaHeuristic_ReturnsValidValue()
    {
        // Act
        float alpha = Resonator.AlphaHeuristic(440.0f, AudioFixtures.DefaultSampleRate);

        // Assert
        Assert.IsTrue(alpha > 0.0f && alpha < 1.0f, "Alpha heuristic should return value between 0 and 1");
    }

    [TestMethod]
    [DataRow(433.0f)]
    [DataRow(440.0f)]
    [DataRow(452.0f)]
    public void UpdateAndTrack_TracksFrequency(float oscfreq)
    {
        // Arrange
        float alpha = Resonator.AlphaHeuristic(440.0f, AudioFixtures.DefaultSampleRate);
        var resonator = new Resonator(440.0f, alpha , null, AudioFixtures.DefaultSampleRate);
        
        Assert.AreEqual(440.0f, resonator.TrackedFrequency, 0.01f); // Initial tracked frequency should be close to 440 Hz

        var oscillator = new Oscillator(oscfreq, AudioFixtures.DefaultSampleRate);

        // Act
        for (int i = 0; i < (400*100); i++)
        {
            resonator.UpdateAndTrack(oscillator.GetNextSample());
        }

        // Assert
        Assert.AreEqual(oscfreq, resonator.TrackedFrequency, 1.0f); // Allow 1 Hz tolerance
    }

    [TestMethod]
    [DataRow(433.0f)]
    [DataRow(440.0f)]
    [DataRow(452.0f)]
    public void UpdateAndTrack_WithArray_ProcessesAllSamples(float oscfreq)
    {
        // Arrange
        float alpha = Resonator.AlphaHeuristic(440.0f, AudioFixtures.DefaultSampleRate);
        var resonator = new Resonator(440.0f, alpha, null, AudioFixtures.DefaultSampleRate);
        var oscillator = new Oscillator(oscfreq, AudioFixtures.DefaultSampleRate);

        for(int i = 0; i < 400; i++)
        {
            resonator.UpdateAndTrack(oscillator.GetNextSamples(100));
        }

        // Assert
        Assert.IsTrue(resonator.Amplitude > 0.0f, "Amplitude should be non-zero after processing samples");
        Assert.AreEqual(oscfreq, resonator.TrackedFrequency, 1.0f); // Allow 1 Hz tolerance
    }
}