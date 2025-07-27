using System;
using libESPER_V2.Core;
using libESPER_V2.Transforms;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace libESPER_V2.Tests.Transforms;

[TestFixture]
[TestOf(typeof(CutCombine))]
public class CutCombineTest
{
    private EsperAudio _audio1;
    private EsperAudio _audio2;

    [SetUp]
    public void SetUp()
    {
        _audio1 = MockFactories.CreateMockEsperAudio(10, 129);
        _audio2 = MockFactories.CreateMockEsperAudio(10, 129);
    }

    [Test]
    public void Cut_ValidRange_ReturnsExpectedAudio()
    {
        var result = CutCombine.Cut(_audio1, 10, 20);
        Assert.That(result.Length, Is.EqualTo(10));
    }

    [Test]
    public void Cut_InvalidRange_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CutCombine.Cut(_audio1, 20, 10));
    }

    [Test]
    public void Concat_ValidInputs_ReturnsCombinedAudio()
    {
        var result = CutCombine.Concat(_audio1, _audio2);
        Assert.That(result.Length, Is.EqualTo(2000));
    }

    [Test]
    public void Concat_MismatchedConfigs_ThrowsArgumentException()
    {
        var audioWithDifferentConfig = new EsperAudio(50, new EsperAudioConfig(5, 21, 20));
        Assert.Throws<ArgumentException>(() => CutCombine.Concat(_audio1, audioWithDifferentConfig));
    }

    [Test]
    public void Crossfade_ValidInputs_ReturnsCrossfadedAudio()
    {
        var result = CutCombine.Crossfade(_audio1, _audio2, 10);
        Assert.That(result.Length, Is.EqualTo(1990)); // Overlap reduces total length
    }

    [Test]
    public void Crossfade_InvalidFadeLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CutCombine.Crossfade(_audio1, _audio2, 1200));
    }

    [Test]
    public void FadeIn_ValidFadeLength_ModifiesAudioCorrectly()
    {
        var result = CutCombine.FadeIn(_audio1, 10);
        // Add assertions to verify fade-in effect
    }

    [Test]
    public void FadeOut_ValidFadeLength_ModifiesAudioCorrectly()
    {
        var result = CutCombine.FadeOut(_audio1, 10);
        // Add assertions to verify fade-out effect
    }

    [Test]
    public void FadeIn_InvalidFadeLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CutCombine.FadeIn(_audio1, 2000));
    }

    [Test]
    public void FadeOut_InvalidFadeLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CutCombine.FadeOut(_audio1, 2000));
    }
}