using System;
using Baksteen.Oscillators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OscillatorsTest;

[TestClass]
public class DynamicsTests
{
    private const float Epsilon = 0.000001f;

    [TestMethod]
    public void TimeConstant_CalculatesCorrectValue()
    {
        // Act
        float t = Dynamics.TimeConstant(DynamicsFixtures.DefaultAlpha, AudioFixtures.DefaultSampleRate);

        // Assert
        Assert.AreEqual(0.09999806f, t, Epsilon);
    }

    [TestMethod]
    public void Alpha_CalculatesCorrectValue()
    {
        // Act
        float a = Dynamics.Alpha(DynamicsFixtures.DefaultTimeConstant, AudioFixtures.DefaultSampleRate);

        // Assert
        Assert.AreEqual(0.000226736069f, a, Epsilon);
    }
}