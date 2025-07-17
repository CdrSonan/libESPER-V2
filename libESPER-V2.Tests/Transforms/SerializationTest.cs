using System;
using System.IO;
using System.Linq;
using LibESPER_V2.Transforms;
using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace libESPER_V2.Tests.Transforms;

[TestFixture]
[TestOf(typeof(Serialization))]
public class SerializationTest
{
    [Test]
    public void SerializeEsperAudio_ShouldReturnValidByteArray()
    {
        // Arrange
        var config = new EsperAudioConfig(10, 5, 256, false);
        var frames = Matrix<float>.Build.Dense(100, 256, (i, j) => i + j);
        var audio = new EsperAudio(frames, config);

        // Act
        var serializedData = Serialization.Serialize(audio);

        // Assert
        Assert.That(serializedData, Is.Not.Null);
        Assert.That(serializedData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void DeserializeEsperAudio_ShouldReturnValidEsperAudio()
    {
        // Arrange
        var config = new EsperAudioConfig(10, 5, 256, false);
        var frames = Matrix<float>.Build.Dense(100, 256, (i, j) => i + j);
        var audio = new EsperAudio(frames, config);
        var serializedData = Serialization.Serialize(audio);

        // Act
        var deserializedAudio = Serialization.Deserialize(serializedData);

        // Assert
        Assert.That(audio.Config, Is.EqualTo(deserializedAudio.Config));
        Assert.That(audio.Length, Is.EqualTo(deserializedAudio.Length));
        Assert.That(audio.GetFrames(), Is.EqualTo(deserializedAudio.GetFrames()));
    }

    [Test]
    public void SerializeCompressedEsperAudio_ShouldReturnValidByteArray()
    {
        // Arrange
        var config = new EsperAudioConfig(10, 5, 256, true);
        var frames = Matrix<Half>.Build.Dense(100, 128, (i, j) => (Half)(i + j));
        var audio = new CompressedEsperAudio(100, 2, 4, config, frames);

        // Act
        var serializedData = Serialization.Serialize(audio);

        // Assert
        Assert.That(serializedData, Is.Not.Null);
        Assert.That(serializedData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void DeserializeCompressedEsperAudio_ShouldReturnValidCompressedEsperAudio()
    {
        // Arrange
        var config = new EsperAudioConfig(10, 5, 256, true);
        var frames = Matrix<Half>.Build.Dense(100, 128, (i, j) => (Half)(i + j));
        var audio = new CompressedEsperAudio(100, 2, 4, config, frames);
        var serializedData = Serialization.Serialize(audio);

        // Act
        var deserializedAudio = Serialization.DeserializeCompressed(serializedData);

        // Assert
        Assert.That(audio.Config, Is.EqualTo(deserializedAudio.Config));
        Assert.That(audio.Length, Is.EqualTo(deserializedAudio.Length));
        Assert.That(audio.CompressedLength, Is.EqualTo(deserializedAudio.CompressedLength));
        Assert.That(audio.TemporalCompression, Is.EqualTo(deserializedAudio.TemporalCompression));
        Assert.That(audio.SpectralCompression, Is.EqualTo(deserializedAudio.SpectralCompression));
        Assert.That(audio.GetFrames(), Is.EqualTo(deserializedAudio.GetFrames()));
    }

    [Test]
    public void Deserialize_WithInvalidFileStandard_ShouldThrowException()
    {
        // Arrange
        var invalidData = new byte[] { 0, 0, 0, 0 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => Serialization.Deserialize(invalidData));
    }

    [Test]
    public void DeserializeCompressed_WithInvalidFileStandard_ShouldThrowException()
    {
        // Arrange
        var invalidData = new byte[] { 0, 0, 0, 0 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => Serialization.DeserializeCompressed(invalidData));
    }
}