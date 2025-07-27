using libESPER_V2.Transforms;
using libESPER_V2.Core;
using NUnit.Framework;

namespace libESPER_V2.Tests.Transforms;

[TestFixture]
[TestOf(typeof(Compression))]
public class CompressionTest
{
    [Test]
    public void Compress_ShouldReturnCompressedAudio()
    {
        const int temporalCompression = 2;
        const int spectralCompression = 2;
        const float eps = 0.001f;
        
        var audio = MockFactories.CreateMockEsperAudio(17, 129);
        var compressedAudio = Compression.Compress(audio, temporalCompression, spectralCompression, eps);
        
        Assert.That(audio.Length, Is.EqualTo(compressedAudio.Length));
        Assert.That(temporalCompression, Is.EqualTo(compressedAudio.Config.TemporalCompression));
        Assert.That(spectralCompression, Is.EqualTo(compressedAudio.Config.SpectralCompression));
        Assert.That(compressedAudio.GetVoiced(), Is.Not.Null);
        Assert.That(compressedAudio.GetUnvoiced(), Is.Not.Null);
    }

    [Test]
    public void Decompress_ShouldReturnDecompressedAudio()
    {
        const int audioLength = 10;
        const float eps = 0.001f;

        var config = new CompressedEsperAudioConfig(17, 129, 256, 2, 2);
        var compressedAudio = new CompressedEsperAudio(audioLength, config);
        
        var decompressedAudio = Compression.Decompress(compressedAudio, eps);
        
        Assert.That(audioLength, Is.EqualTo(decompressedAudio.Length));
        Assert.That(decompressedAudio.GetVoicedAmps(), Is.Not.Null);
        Assert.That(decompressedAudio.GetUnvoiced(0), Is.Not.Null);
    }

    [Test]
    public void CompressDecompress_ShouldReturnEquivalentAudio()
    {
        const int temporalCompression = 2;
        const int spectralCompression = 8; //TODO: Fix for lower compression ratios
        const float eps = 0.001f;

        var originalAudio = MockFactories.CreateMockEsperAudio(17, 129);
        
        var compressedAudio = Compression.Compress(originalAudio, temporalCompression, spectralCompression, eps);
        var decompressedAudio = Compression.Decompress(compressedAudio, eps);
        
        Assert.That(originalAudio.Length, Is.EqualTo(decompressedAudio.Length));
        for (var i = 0; i < decompressedAudio.Length; i++)
        {
            var frame = decompressedAudio.GetFrames(i);
            var originalFrame = originalAudio.GetFrames(i);
            for (var j = 0; j < decompressedAudio.Config.FrameSize(); j++)
            {
                Assert.That(frame[j], Is.EqualTo(originalFrame[j]).Within(eps));
            }
        }
    }
}