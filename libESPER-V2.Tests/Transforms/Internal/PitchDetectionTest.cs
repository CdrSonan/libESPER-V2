using System;
using System.Linq;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;
using System.Collections.Generic;
using libESPER_V2.Core;

namespace libESPER_V2.Tests.Transforms.Internal;

[TestFixture]
[TestOf(typeof(PitchDetection))]
public class PitchDetectionTest
{
    [Test]
    public void PitchMarkers_ReturnsExpectedMarkers()
    {
        // Arrange
        var wave = Vector<float>.Build.Dense(1000, i => (float)(Math.Sin(2 * Math.PI * i / 256)));
        var config = new EsperAudioConfig(10, 129, 256);
        var pitchDetection = new PitchDetection(wave, config, 0.1f);

        // Act
        var markers = pitchDetection.PitchMarkers(null);

        // Assert
        Assert.That(markers, Is.Not.Null);
        Assert.That(markers, Is.InstanceOf<List<int>>());
        Assert.That(markers.Count > 0, "Markers list should not be empty.");
        Assert.That(markers.Zip(markers.Skip(1), (a, b) => b - a).All(diff => diff == 256), 
            "All markers should be 256 units apart.");
    }

    [Test]
    public void Validity_ReturnsExpectedValidity()
    {
        // Arrange
        var wave = Vector<float>.Build.Dense(1000, i => (float)(Math.Sin(2 * Math.PI * i / 256)));
        var config = new EsperAudioConfig(10, 129, 256);
        var pitchDetection = new PitchDetection(wave, config, 0.1f);

        // Act
        var validity = pitchDetection.Validity(null);

        // Assert
        Assert.That(validity, Is.Not.Null);
        Assert.That(validity, Is.InstanceOf<bool[]>());
        Assert.That(validity.Length > 0);
        Assert.That(validity.All(v => v), "All items in the validity array should be true.");
    }

    [Test]
    public void PitchDeltas_ReturnsExpectedDeltas()
    {
        // Arrange
        var wave = Vector<float>.Build.Dense(1000, i => (float)(Math.Sin(2 * Math.PI * i / 256)));
        var config = new EsperAudioConfig(10, 129, 256);
        var pitchDetection = new PitchDetection(wave, config, 0.1f);

        // Act
        var deltas = pitchDetection.PitchDeltas(null);

        // Assert
        Assert.That(deltas, Is.Not.Null);
        Assert.That(deltas, Is.InstanceOf<Vector<float>>());
        Assert.That(deltas.Count > 0);
        Assert.That(deltas.All(delta => Math.Abs(delta - 256) < 1e-5 || delta == 0), 
            "All pitch deltas should be approximately 256, or 0.");
    }
}