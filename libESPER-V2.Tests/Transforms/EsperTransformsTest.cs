using System;
using libESPER_V2.Core;
using libESPER_V2.Transforms;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace libESPER_V2.Tests.Transforms;

[TestFixture]
[TestOf(typeof(EsperTransforms))]
public class EsperTransformsTest
{
    [Test]
    [TestCase(10000, 50, 5, 65, 256)]
    [TestCase(1000, 10, 17, 129, 256)]
    public void Forward_SineInput_ReturnsValid(int length, float wavelength, int nVoiced, int nUnvoiced, int stepSize)
    {
        var waveform = Vector<float>.Build.Dense(length,
            i => (float)Math.Sin(i * 2 * Math.PI / wavelength));
        var config = new EsperAudioConfig((ushort)nVoiced, (ushort)nUnvoiced, stepSize);
        var fwdConfig = new EsperForwardConfig(null, null, null);
        var output = EsperTransforms.Forward(waveform, config, fwdConfig);
        Assert.That(output, Is.Not.Null);
    }
}