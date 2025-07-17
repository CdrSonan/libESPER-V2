using System;
using libESPER_V2.Transforms;
using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace libESPER_V2.Tests.Transforms;

[TestFixture]
[TestOf(typeof(Compression))]
public class CompressionTest
{
    [Test]
    public void Compress_ShouldReturnCompressedAudio()
    {
        // Arrange
        var audioLength = 10;
        var temporalCompression = 2;
        var spectralCompression = 2;
        var eps = 0.001f;
        
        var audio = MockFactories.CreateMockEsperAudio(17, 129);

        // Mock voiced and unvoiced data
        var voicedAmps = Matrix<float>.Build.Dense(audioLength, 16, 1.0f);
        audio.SetVoicedAmps(voicedAmps);

        for (var i = 0; i < audioLength; i++)
        {
            var unvoiced = Vector<float>.Build.Dense(16, 1.0f);
            audio.SetUnvoiced(i, unvoiced);
        }

        // Act
        var compressedAudio = Compression.Compress(audio, temporalCompression, spectralCompression, eps);

        // Assert
        Assert.That(audioLength, Is.EqualTo(compressedAudio.Length));
        Assert.That(temporalCompression, Is.EqualTo(compressedAudio.TemporalCompression));
        Assert.That(spectralCompression, Is.EqualTo(compressedAudio.SpectralCompression));
        Assert.That(compressedAudio.GetVoiced(), Is.Not.Null);
        Assert.That(compressedAudio.GetUnvoiced(), Is.Not.Null);
    }

    [Test]
    public void Decompress_ShouldReturnDecompressedAudio()
    {
        // Arrange
        var audioLength = 10;
        var temporalCompression = 2;
        var spectralCompression = 2;
        var eps = 0.001f;

        var config = new EsperAudioConfig(17, 129, 256, true);
        var compressedAudio = new CompressedEsperAudio(audioLength, temporalCompression, spectralCompression, config);

        // Mock compressed voiced and unvoiced data
        var compressedVoiced = Matrix<Half>.Build.Dense(audioLength, 16, (Half)0.5f);
        compressedAudio.SetVoiced(compressedVoiced);

        var compressedUnvoiced = Matrix<Half>.Build.Dense(audioLength, 8, (Half)0.5f);
        compressedAudio.SetUnvoiced(compressedUnvoiced);

        // Act
        var decompressedAudio = Compression.Decompress(compressedAudio, eps);

        // Assert
        Assert.That(audioLength, Is.EqualTo(decompressedAudio.Length));
        Assert.That(decompressedAudio.GetVoicedAmps(), Is.Not.Null);
        Assert.That(decompressedAudio.GetUnvoiced(0), Is.Not.Null);
    }

    [Test]
    public void CompressDecompress_ShouldReturnEquivalentAudio()
    {
        // Arrange
        var audioLength = 10;
        var temporalCompression = 2;
        var spectralCompression = 2;
        var eps = 0.001f;

        var originalAudio = MockFactories.CreateMockEsperAudio(17, 129);

        // Mock voiced and unvoiced data
        var voicedAmps = Matrix<float>.Build.Dense(audioLength, 16, 1.0f);
        originalAudio.SetVoicedAmps(voicedAmps);

        for (var i = 0; i < audioLength; i++)
        {
            var unvoiced = Vector<float>.Build.Dense(16, 1.0f);
            originalAudio.SetUnvoiced(i, unvoiced);
        }

        // Act
        var compressedAudio = Compression.Compress(originalAudio, temporalCompression, spectralCompression, eps);
        var decompressedAudio = Compression.Decompress(compressedAudio, eps);

        // Assert
        Assert.That(originalAudio.Length, Is.EqualTo(decompressedAudio.Length));
        Assert.That(originalAudio.GetVoicedAmps().ToArray(), Is.EquivalentTo(decompressedAudio.GetVoicedAmps().ToArray()));
    }
}