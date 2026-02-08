using System;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OscillatorsTest;

[TestClass]
public class PhasorTests
{
    private const float Tolerance = 1e-5f;

    [TestMethod]
    public void Constructor_SetsFrequencyAndSampleRate()
    {
        // Arrange
        float frequency = 440.0f;
        float sampleRate = 44100.0f;

        // Act
        var phasor = new Phasor(frequency, sampleRate);

        // Assert
        Assert.AreEqual(frequency, phasor.Frequency);
        Assert.AreEqual(sampleRate, phasor.SampleRate);
    }

    [TestMethod]
    public void Constructor_InitializesZcAndZs()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);

        // Assert
        Assert.AreEqual(1.0f, phasor.Zc);
        Assert.AreEqual(0.0f, phasor.Zs);
    }

    [TestMethod]
    public void FrequencyProperty_SetValue_UpdatesFrequency()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);
        float newFrequency = 880.0f;

        // Act
        phasor.Frequency = newFrequency;

        // Assert
        Assert.AreEqual(newFrequency, phasor.Frequency);
    }

    [TestMethod]
    public void SampleRateProperty_SetValue_UpdatesSampleRate()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);
        float newSampleRate = 48000.0f;

        // Act
        phasor.SampleRate = newSampleRate;

        // Assert
        Assert.AreEqual(newSampleRate, phasor.SampleRate);
    }

    [TestMethod]
    public void FrequencyProperty_SetValue_UpdatesMultiplier()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);
        float originalWc = phasor.Wc;
        float originalWs = phasor.Ws;

        // Act
        phasor.Frequency = 880.0f;

        // Assert - multiplier values should change
        Assert.IsFalse(AreFloatsEqual(originalWc, phasor.Wc), "Wc should have changed");
        Assert.IsFalse(AreFloatsEqual(originalWs, phasor.Ws), "Ws should have changed");
    }

    [TestMethod]
    public void SampleRateProperty_SetValue_UpdatesMultiplier()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);
        float originalWc = phasor.Wc;
        float originalWs = phasor.Ws;

        // Act
        phasor.SampleRate = 48000.0f;

        // Assert - multiplier values should change
        Assert.IsFalse(AreFloatsEqual(originalWc, phasor.Wc), "Wc should have changed");
        Assert.IsFalse(AreFloatsEqual(originalWs, phasor.Ws), "Ws should have changed");
    }

    [TestMethod]
    public void IncrementPhase_UpdatesZcAndZs()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);
        float originalZc = phasor.Zc;
        float originalZs = phasor.Zs;

        // Act
        phasor.IncrementPhase();

        // Assert
        Assert.IsFalse(AreFloatsEqual(originalZc, phasor.Zc), "Zc should have changed");
        Assert.IsFalse(AreFloatsEqual(originalZs, phasor.Zs), "Zs should have changed");
    }

    [TestMethod]
    public void IncrementPhase_PreservesNorm()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);

        // Act
        phasor.IncrementPhase();

        // Assert - norm should remain approximately 1 (since we start with unit norm)
        float norm = phasor.Zc * phasor.Zc + phasor.Zs * phasor.Zs;
        Assert.IsTrue(norm >= 0.999f && norm <= 1.001f, "Norm should be within tolerance of 1.0");
    }

    [TestMethod]
    public void IncrementPhase_Multiple_CreatesOscillation()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);
        var values = new float[4];

        // Act
        for (int i = 0; i < 4; i++)
        {
            values[i] = phasor.Zc;
            phasor.IncrementPhase();
        }

        // Assert - values should oscillate (not all equal)
        Assert.IsTrue(
            !values[0].Equals(values[1]) ||
            !values[1].Equals(values[2]) ||
            !values[2].Equals(values[3]),
            "Values should change after incrementing phase"
        );
    }

    [TestMethod]
    public void Stabilize_CorrectsDrift()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);

        // Introduce drift by incrementing many times
        for (int i = 0; i < 1000; i++)
        {
            phasor.IncrementPhase();
        }

        float driftedNorm = phasor.Zc * phasor.Zc + phasor.Zs * phasor.Zs;

        // Act
        phasor.Stabilize();

        // Assert - norm should be closer to 1 after stabilization
        float stabilizedNorm = phasor.Zc * phasor.Zc + phasor.Zs * phasor.Zs;
        Assert.IsLessThan(
            MathF.Abs(driftedNorm - 1.0f),
            MathF.Abs(stabilizedNorm - 1.0f), "Stabilize should reduce drift from unit norm"
        );
    }

    [TestMethod]
    public void Stabilize_NormApproachesOne()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);

        // Introduce drift
        for (int i = 0; i < 500; i++)
        {
            phasor.IncrementPhase();
        }

        // Act
        phasor.Stabilize();

        // Assert
        float norm = phasor.Zc * phasor.Zc + phasor.Zs * phasor.Zs;
        Assert.IsTrue(norm >= 0.99f && norm <= 1.01f, "Norm should be within tolerance of 1.0");
    }

    [TestMethod]
    [DataRow(220.0f, 44100.0f)]
    [DataRow(440.0f, 44100.0f)]
    [DataRow(880.0f, 44100.0f)]
    [DataRow(1000.0f, 48000.0f)]
    public void Constructor_WithVariousFrequenciesAndSampleRates(float frequency, float sampleRate)
    {
        // Act
        var phasor = new Phasor(frequency, sampleRate);

        // Assert
        Assert.AreEqual(frequency, phasor.Frequency);
        Assert.AreEqual(sampleRate, phasor.SampleRate);
        Assert.AreEqual(1.0f, phasor.Zc);
        Assert.AreEqual(0.0f, phasor.Zs);
    }

    [TestMethod]
    public void Wcps_EqualsWcPlusWs()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);

        // Assert - Wcps should be pre-computed as Wc + Ws
        Assert.IsTrue(
            AreFloatsEqual(phasor.Wc + phasor.Ws, phasor.Wcps),
            "Wcps should equal Wc + Ws"
        );
    }

    [TestMethod]
    public void IncrementPhase_ZcAndZsRemainFinite()
    {
        // Arrange
        var phasor = new Phasor(440.0f, 44100.0f);

        // Act & Assert - should not throw and values should remain finite
        for (int i = 0; i < 10000; i++)
        {
            phasor.IncrementPhase();
            Assert.IsTrue(float.IsFinite(phasor.Zc), "Zc should be finite");
            Assert.IsTrue(float.IsFinite(phasor.Zs), "Zs should be finite");

            // Stabilize occasionally to prevent drift
            if (i % 500 == 0)
            {
                phasor.Stabilize();
            }
        }
    }

    /// <summary>
    /// Helper method to compare two floats with tolerance.
    /// </summary>
    private bool AreFloatsEqual(float x, float y)
    {
        return MathF.Abs(x - y) < Tolerance;
    }
} 