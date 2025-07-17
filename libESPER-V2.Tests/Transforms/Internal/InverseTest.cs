using System;
using System.Linq;
using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;
using static libESPER_V2.Tests.MockFactories;

namespace libESPER_V2.Tests.Transforms.Internal;

[TestFixture]
[TestOf(typeof(InverseResolve))]
public class InverseResolveTests
{
    [Test]
    public void HanningWindow_ReturnsCorrectSize()
    {
        const int size = 10;
        var result = InverseResolve.HanningWindow(size);
        Assert.That(size, Is.EqualTo(result.Count));
    }

    [Test]
    public void HanningWindow_ReturnsSymmetricValues()
    {
        const int size = 10;
        var result = InverseResolve.HanningWindow(size);
        for (var i = 1; i < size / 2; i++)
        {
            Assert.That(result[i], Is.EqualTo(result[size - i]).Within(0.0001));
        }
    }

    [Test]
    public void ReconstructVoiced_ReturnsCorrectOutputLength()
    {
        var audio = CreateMockEsperAudio(65, 129); // Mocked audio object
        const float phase = 0.0f;
        var (output, _) = InverseResolve.ReconstructVoiced(audio, phase);
        Assert.That(audio.Length * audio.Config.StepSize, Is.EqualTo(output.Count));
    }

    [Test]
    public void ReconstructUnvoiced_ReturnsCorrectOutputLength()
    {
        var audio = CreateMockEsperAudio(65, 129); // Mocked audio object
        const long seed = 12345L;
        var output = InverseResolve.ReconstructUnvoiced(audio, seed);
        Assert.That(audio.Length * audio.Config.StepSize, Is.EqualTo(output.Count));
    }

    [Test]
    [TestCase(10000, 50, (ushort)5, (ushort)257, 256)]
    [TestCase(1000, 10, (ushort)17, (ushort)129, 256)]
    public void ReconstructVoiced_SineInput_ReturnsExpected(int length, float pitch, ushort nVoiced, ushort nUnvoiced,
        int stepSize)
    {
        var config = new EsperAudioConfig(nVoiced, nUnvoiced, stepSize);
        var audio = new EsperAudio(length, config);
        audio.SetPitch(Vector<float>.Build.Dense(length, pitch));
        audio.SetVoicedAmps(Matrix<float>.Build.Dense(length, nVoiced, (i, j) => j < 3 ? 1 : 0));
        var (result, phase) = InverseResolve.ReconstructVoiced(audio, 0);
        Assert.That(result, Is.Not.Null);
        Assert.That(phase, Is.Not.Null);
    }
    
    [Test]
    [TestCase(10000, 50, (ushort)5, (ushort)257, 256)]
    [TestCase(1000, 10, (ushort)17, (ushort)129, 256)]
    public void ReconstructVoiced_ZeroInput_ReturnsExpected(int length, float pitch, ushort nVoiced, ushort nUnvoiced,
        int stepSize)
    {
        var config = new EsperAudioConfig(nVoiced, nUnvoiced, stepSize);
        var audio = new EsperAudio(length, config);
        audio.SetPitch(Vector<float>.Build.Dense(length, pitch));
        var (result, phase) = InverseResolve.ReconstructVoiced(audio, 0);
        Assert.That(result.All(item => item == 0), Is.True);
    }
}