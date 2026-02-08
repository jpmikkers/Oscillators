using System;
using System.Linq;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OscillatorsTest;

[TestClass]
public class FrequenciesTests
{
    private const float Epsilon = 0.000001f;

    [TestMethod]
    public void MusicalPitchFrequencies_DefaultTuning()
    {
        // Act
        float[] frequencies440 = Frequencies.MusicalPitchFrequencies(0, 116);

        // Assert
        Assert.AreEqual(16.3515968f, frequencies440[0], Epsilon);
        Assert.AreEqual(27.5f, frequencies440[9], Epsilon);
        Assert.AreEqual(440.0f, frequencies440[57], Epsilon);
        Assert.AreEqual(4186.00879f, frequencies440[96], Epsilon);
        Assert.AreEqual(13289.748f, frequencies440[116], Epsilon);
    }

    [TestMethod]
    public void MusicalPitchFrequencies_CustomTuning()
    {
        // Act
        float[] frequencies441 = Frequencies.MusicalPitchFrequencies(0, 116, 441.0f);

        // Assert
        Assert.AreEqual(16.38876f, frequencies441[0], Epsilon);
        Assert.AreEqual(27.5625f, frequencies441[9], Epsilon);
        Assert.AreEqual(441.0f, frequencies441[57], Epsilon);
        Assert.AreEqual(4195.5225f, frequencies441[96], Epsilon);
        Assert.AreEqual(13319.952f, frequencies441[116], Epsilon);
    }

    [TestMethod]
    public void LogUniformFrequencies_ReturnsCorrectDistribution()
    {
        // Arrange
        float fMin = 32.70f;
        float fMax = 3950.68f;
        int numBins = 84;
        int numBinsPerOctaves = 12;

        // Act
        float[] frequencies = Frequencies.LogUniformFrequencies(fMin, numBins, numBinsPerOctaves);

        // Assert
        Assert.HasCount(numBins, frequencies);
        Assert.AreEqual(fMin, frequencies[0], Epsilon);
        Assert.AreEqual(fMax, frequencies[numBins - 1], 0.01f);

        // Ratios of consecutive frequencies should be constant
        Assert.AreEqual(frequencies[2] / frequencies[1], frequencies[1] / frequencies[0], 0.00001f);
        Assert.AreEqual(
            frequencies[numBins - 1] / frequencies[numBins - 2],
            frequencies[numBins - 2] / frequencies[numBins - 3],
            0.00001f
        );
    }

    [TestMethod]
    public void MelFrequencies_SlaneyFormula()
    {
        // Arrange
        int numMels = 128;
        float minFrequency = 0.0f;
        float maxFrequency = 11025.0f;

        // Act
        float[] frequencies = Frequencies.MelFrequencies(numMels, minFrequency, maxFrequency, false);

        // Assert
        Assert.HasCount(numMels, frequencies);
        Assert.AreEqual(minFrequency, frequencies[0]);
        Assert.AreEqual(maxFrequency, frequencies[numMels - 1], 0.001f);

        // Check against known values
        for (int i = 0; i < numMels; i++)
        {
            Assert.AreEqual(FrequenciesFixtures.MelFrequencies[i], frequencies[i], 0.01f);
        }
    }

    [TestMethod]
    public void MelFrequencies_HTKFormula()
    {
        // Arrange
        int numMels = 128;
        float minFrequency = 0.0f;
        float maxFrequency = 11025.0f;

        // Act
        float[] frequenciesHTK = Frequencies.MelFrequencies(numMels, minFrequency, maxFrequency, true);

        // Assert
        Assert.HasCount(numMels, frequenciesHTK);
        Assert.AreEqual(minFrequency, frequenciesHTK[0]);
        Assert.AreEqual(maxFrequency, frequenciesHTK[numMels - 1], 0.001f);

        // Check against known values
        for (int i = 0; i < numMels; i++)
        {
            Assert.AreEqual(FrequenciesFixtures.MelFrequenciesHTK[i], frequenciesHTK[i], 0.01f);
        }
    }

    [TestMethod]
    public void DopplerVelocity_440to441Hz()
    {
        // Act
        float v440441 = Frequencies.DopplerVelocity(440, 441);

        // Assert
        Assert.AreEqual(-0.78458047f, v440441, Epsilon);
    }

    [TestMethod]
    public void DopplerVelocity_441to440Hz()
    {
        // Act
        float v441440 = Frequencies.DopplerVelocity(441, 440);

        // Assert
        Assert.AreEqual(0.78636366f, v441440, Epsilon);
    }

    [TestMethod]
    [Ignore("Requires ResonatorBankArray implementation")]
    public void FrequencySweep_ReturnsCorrectCoefficients()
    {
        // Arrange
        float fMin = 32.70f;
        int numBins = 100;
        int numBinsPerOctaves = 12;

        float[] frequencies = Frequencies.LogUniformFrequencies(fMin, numBins, numBinsPerOctaves);
        float sampleRate = 44100.0f;
        
        // This would require ResonatorBankArray to be ported
        // float[] alphas = ResonatorBankArray.AlphasHeuristic(frequencies, sampleRate);

        // Act
        // float[] eq = Frequencies.FrequencySweep(frequencies, alphas, sampleRate: sampleRate);

        // Assert
        // Assert.AreEqual(frequencies.Length, eq.Length);
        // Assert.AreEqual(0.35677117f, eq[0], Epsilon);
        // Assert.AreEqual(0.31568912f, eq[8], Epsilon);
        // Assert.AreEqual(0.37487006f, eq[38], Epsilon);
        // Assert.AreEqual(0.4486485f, eq[97], Epsilon);
        // Assert.AreEqual(0.47431275f, eq[99], Epsilon);
    }
}