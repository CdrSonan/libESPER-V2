using System;
using libESPER_V2.Utils;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace libESPER_V2.Tests.Utils;

[TestFixture]
[TestOf(typeof(Phases))]
public class PhasesTest
{
    [Test]
    public void Interpolate_ThrowsArgumentOutOfRangeException_WhenTIsLessThanZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Phases.Interpolate(0.0f, 1.0f, -0.1f));
    }

    [Test]
    public void Interpolate_ThrowsArgumentOutOfRangeException_WhenTIsGreaterThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Phases.Interpolate(0.0f, 1.0f, 1.1f));
    }

    [Test]
    public void Interpolate_ReturnsA_WhenAEqualsB()
    {
        var result = Phases.Interpolate(1.0f, 1.0f, 0.5f);
        Assert.That(result, Is.EqualTo(1.0f));
    }

    [Test]
    public void Interpolate_CorrectlyInterpolatesForwardDifference()
    {
        var result = Phases.Interpolate(0.0f, (float)Math.PI / 2, 0.5f);
        Assert.That(result, Is.EqualTo((float)Math.PI / 4).Within(0.0001f));
    }

    [Test]
    public void Interpolate_CorrectlyInterpolatesBackwardDifference()
    {
        var result = Phases.Interpolate((float)Math.PI / 2, 0.0f, 0.5f);
        Assert.That(result, Is.EqualTo((float)Math.PI / 4).Within(0.0001f));
    }
    
    [Test]
    public void Interpolate_WithOverflow_CorrectlyInterpolatesForwardDifference()
    {
        var result = Phases.Interpolate(-(float)Math.PI / 4, (float)Math.PI / 4, 0.5f);
        Assert.That(result, Is.EqualTo(0).Within(0.0001f));
    }

    [Test]
    public void Interpolate_WithOverflow_CorrectlyInterpolatesBackwardDifference()
    {
        var result = Phases.Interpolate((float)Math.PI / 4, -(float)Math.PI / 4, 0.5f);
        Assert.That(result, Is.EqualTo(0).Within(0.0001f));
    }

    [Test]
    public void InterpolateVector_ThrowsArgumentException_WhenVectorsHaveDifferentLengths()
    {
        var a = Vector<float>.Build.Dense(3, i => i);
        var b = Vector<float>.Build.Dense(2, i => i);
        Assert.Throws<ArgumentException>(() => Phases.Interpolate(a, b, 0.5f));
    }

    [Test]
    public void InterpolateVector_CorrectlyInterpolatesEachElement()
    {
        var a = Vector<float>.Build.Dense(3, i => (i - 2) * (float)Math.PI / 2);
        var b = Vector<float>.Build.Dense(3, i => (i - 1) * (float)Math.PI / 2);
        var result = Phases.Interpolate(a, b, 0.5f);
        var reference = Vector<float>.Build.Dense(3, i => (i - 1.5f) * (float)Math.PI / 2);
        for (var i = 0; i < 3; i++)
        {
            Assert.That(result[i], Is.EqualTo(reference[i]).Within(0.0001f));
        }

    }
}