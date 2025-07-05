using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace libESPER_V2.Tests.Transforms.Internal;

[TestFixture]
[TestOf(typeof(InverseResolve))]
public class InverseTest
{

    [Test]
    [TestCase(10000, 50, (ushort)5, (ushort)65, 256)]
    [TestCase(1000, 10, (ushort)17, (ushort)129, 256)]
    public void Voiced_SineInput_ReturnsExpected(int length, float pitch, ushort nVoiced, ushort nUnvoiced, int stepSize)
    {
        var config = new EsperAudioConfig(nVoiced, nUnvoiced, stepSize);
        var audio = new EsperAudio(length, config);
        audio.SetPitch(Vector<float>.Build.Dense(length, pitch));
        audio.SetVoicedAmps(Matrix<float>.Build.Dense(length, nVoiced, (i, j) => j < 3 ? 1 : 0));
        var result = InverseResolve.ReconstructVoiced(audio, 0);
        Assert.That(result, Is.Not.Null);
    }
}