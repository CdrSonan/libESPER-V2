using System.IO;
using libESPER_V2.Transforms;
using NUnit.Framework;

namespace libESPER_V2.Tests.Transforms;

[TestFixture]
[TestOf(typeof(Serialization))]
public class SerializationTest
{
    [Test]
    public void SerializeEsperAudio_ShouldReturnValidByteArray()
    {
        var audio = MockFactories.CreateMockEsperAudio(10, 129);
        var serializedData = Serialization.Serialize(audio);
        Assert.That(serializedData, Is.Not.Null);
        Assert.That(serializedData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void DeserializeEsperAudio_ShouldReturnValidEsperAudio()
    {
        var audio = MockFactories.CreateMockEsperAudio(10, 129);
        var serializedData = Serialization.Serialize(audio);
        var deserializedAudio = Serialization.Deserialize(serializedData);
        Assert.That(audio.Config, Is.EqualTo(deserializedAudio.Config));
        Assert.That(audio.Length, Is.EqualTo(deserializedAudio.Length));
        Assert.That(audio.GetFrames(), Is.EqualTo(deserializedAudio.GetFrames()));
    }

    [Test]
    public void SerializeCompressedEsperAudio_ShouldReturnValidByteArray()
    {
        var audio = MockFactories.CreateMockCompressedEsperAudio(10, 129);
        var serializedData = Serialization.Serialize(audio);
        Assert.That(serializedData, Is.Not.Null);
        Assert.That(serializedData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void DeserializeCompressedEsperAudio_ShouldReturnValidCompressedEsperAudio()
    {
        var audio = MockFactories.CreateMockCompressedEsperAudio(10, 129);
        var serializedData = Serialization.Serialize(audio);
        var deserializedAudio = Serialization.DeserializeCompressed(serializedData);
        Assert.That(audio.Config, Is.EqualTo(deserializedAudio.Config));
        Assert.That(audio.Length, Is.EqualTo(deserializedAudio.Length));
        Assert.That(audio.CompressedLength, Is.EqualTo(deserializedAudio.CompressedLength));
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