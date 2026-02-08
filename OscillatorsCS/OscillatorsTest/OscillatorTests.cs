using System;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OscillatorsTest;

[TestClass]
public class OscillatorTests
{
    private const float Epsilon = 0.0001f;
    private const float TwoPi = MathF.PI * 2.0f;
    private const float DefaultSampleRate = 44100.0f;

    [TestMethod]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        float frequency = 440.0f;
        float sampleRate = DefaultSampleRate;

        // Act
        var oscillator = new Oscillator(frequency, sampleRate);

        // Assert
        Assert.AreEqual(DefaultSampleRate, oscillator.SampleRate);
        Assert.AreEqual(frequency, oscillator.Frequency);
        Assert.AreEqual(sampleRate, oscillator.SampleRate);
        Assert.AreEqual(1.0f, oscillator.Amplitude, Epsilon);
    }

    [TestMethod]
    public void GetNextSample_ReturnsCorrectValue()
    {
        // Arrange
        float amplitude = 0.5f;
        var oscillator = new Oscillator(440.0f, DefaultSampleRate, amplitude);

        // Act
        float nextSample = oscillator.GetNextSample();

        // Assert
        Assert.AreEqual(amplitude, nextSample, Epsilon);
    }

    [TestMethod]
    public void GetNextSamples_ReturnsArrayWithCorrectLength()
    {
        // Arrange
        float frequency = 440.0f;
        float amplitude = 0.5f;
        float sampleRate = DefaultSampleRate;
        var oscillator = new Oscillator(frequency, sampleRate, amplitude);
        int numSamples = 10000;

        // Act
        float[] samples = oscillator.GetNextSamples(numSamples);

        // Assert
        Assert.HasCount(numSamples, samples);

        float twoPiFrequency = TwoPi * frequency;
        float delta = twoPiFrequency / sampleRate;
        Assert.AreEqual(amplitude, samples[0], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(666 * delta), samples[666], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(3333 * delta), samples[3333], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(7777 * delta), samples[7777], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(9999 * delta), samples[9999], Epsilon);
    }

    [TestMethod]
    public void GetNextSamples_FillsArrayWithCorrectValues()
    {
        // Arrange
        float frequency = 440.0f;
        float amplitude = 0.5f;
        float sampleRate = DefaultSampleRate;
        var oscillator = new Oscillator(frequency, sampleRate, amplitude);
        int numSamples = 10000;
        float[] samples = new float[numSamples];

        // Act
        oscillator.GetNextSamples(samples);

        // Assert
        float twoPiFrequency = TwoPi * frequency;
        float delta = twoPiFrequency / sampleRate;
        Assert.AreEqual(amplitude, samples[0], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(666 * delta), samples[666], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(3333 * delta), samples[3333], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(7777 * delta), samples[7777], Epsilon);
        Assert.AreEqual(amplitude * MathF.Cos(9999 * delta), samples[9999], Epsilon);
    }
}