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
    [TestCase(10000, 50, 5, 129, 256)]
    [TestCase(1000, 10, 17, 257, 256)]
    public void Forward_SineInput_ReturnsValid(int length, float wavelength, int nVoiced, int nUnvoiced, int stepSize)
    {
        var waveform = Vector<float>.Build.Dense(length,
            i => (float)Math.Sin(i * 2 * Math.PI / wavelength));
        var config = new EsperAudioConfig((ushort)nVoiced, (ushort)nUnvoiced, stepSize);
        var fwdConfig = new EsperForwardConfig(null, null);
        var output = EsperTransforms.Forward(waveform, config, fwdConfig);
        Assert.That(output, Is.Not.Null);
    }
    
    [Test]
    [TestCase(10000, 50, 65, 129, 256)]
    [TestCase(1000, 10, 17, 257, 256)]
    public void Inverse_SineInput_ReturnsValid(int length, float pitch, int nVoiced, int nUnvoiced, int stepSize)
    {
        var config = new EsperAudioConfig((ushort)nVoiced, (ushort)nUnvoiced, stepSize);
        var audio = new EsperAudio(length, config);
        audio.SetPitch(Vector<float>.Build.Dense(length, pitch));
        audio.SetVoicedAmps(Matrix<float>.Build.Dense(length, nVoiced, (i, j) => j < 3 ? 1 : 0));
        var (result, phase) = EsperTransforms.Inverse(audio);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    [TestCase(9984, 50, 65, 129, 256)]
    [TestCase(1024, 50, 17, 257, 256)]
    public void Loop_SineInput_ReturnsOriginal(int length, int wavelength, int nVoiced, int nUnvoiced, int stepSize)
    {
        var waveform = Vector<float>.Build.Dense(length,
            i => (float)Math.Cos(i * 2 * Math.PI / wavelength));
        var config = new EsperAudioConfig((ushort)nVoiced, (ushort)nUnvoiced, stepSize);
        var fwdConfig = new EsperForwardConfig(null, null);
        var esperAudio = EsperTransforms.Forward(waveform, config, fwdConfig);
        var (result, phase) = EsperTransforms.Inverse(esperAudio, (float)Math.PI/2);
        Assert.That(waveform.Count, Is.EqualTo(result.Count));
        for (var i = 0; i < result.Count; i++)
            Assert.That(result[i], Is.EqualTo(waveform[i]).Within(0.33));
    }
}