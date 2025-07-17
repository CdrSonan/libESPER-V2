using System.Linq;
using libESPER_V2.Utils;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace libESPER_V2.Tests.Utils;

[TestFixture]
[TestOf(typeof(WhittakerShannon))]
public class WhittakerShannonTest
{

    [Test]
    public void Interpolate_ReturnsInput_WhenCoordsAreIndices()
    {
        var wave = Vector<float>.Build.DenseOfArray(new float[] { 1f, 2f, 3f });
        var coords = Vector<float>.Build.DenseOfArray(new float[] { 0f, 1f, 2f });

        var result = WhittakerShannon.Interpolate(wave, coords);
        for (var i = 0; i < wave.Count; i++)
            Assert.That(result[i], Is.EqualTo(wave[i]).Within(1e-4));
    }

    [Test]
    public void Interpolate_InterpolatesBetweenPoints()
    {
        var wave = Vector<float>.Build.DenseOfArray(new float[] { 1f, 3f });
        var coords = Vector<float>.Build.DenseOfArray(new float[] { 0.5f });

        var result = WhittakerShannon.Interpolate(wave, coords);

        // The expected value is not trivial; just check it's between the two
        Assert.That(result[0], Is.GreaterThan(1f).And.LessThan(3f));
    }

    [Test]
    public void Interpolate_EmptyWave_ReturnsEmpty()
    {
        var wave = Vector<float>.Build.Dense(0);
        var coords = Vector<float>.Build.DenseOfArray(new float[] { 0f, 1f });

        var result = WhittakerShannon.Interpolate(wave, coords);

        Assert.That(result.Count, Is.EqualTo(coords.Count));
        Assert.That(result.All(x => x == 0f), Is.True);
    }

    [Test]
    public void Interpolate_EmptyCoords_ReturnsEmpty()
    {
        var wave = Vector<float>.Build.DenseOfArray(new float[] { 1f, 2f, 3f });
        var coords = Vector<float>.Build.Dense(0);

        var result = WhittakerShannon.Interpolate(wave, coords);

        Assert.That(result.Count, Is.EqualTo(0));
    }
}