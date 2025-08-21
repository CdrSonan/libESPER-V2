using libESPER_V2.Transforms;
using NUnit.Framework;
using static libESPER_V2.Tests.MockFactories;

namespace libESPER_V2.Tests.Transforms;

[TestFixture]
[TestOf(typeof(Stretch))]
public class StretchTest
{

    [Test]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(10000)]
    public void StretchAudio_ReturnsValid(int length)
    {
        var input = CreateMockEsperAudio(65, 129);
        var output = Stretch.StretchAudio(input, length);
        Assert.That(output, Is.Not.Null);
        Assert.That(output.GetFrames().RowCount, Is.EqualTo(length));
        Assert.That(output.Config.NVoiced, Is.EqualTo(input.Config.NVoiced));
        Assert.That(output.Config.NUnvoiced, Is.EqualTo(input.Config.NUnvoiced));
    }
    
    [Test]
    [TestCase(10, 0.5f)]
    [TestCase(100, 0.75f)]
    [TestCase(1000, 0.25f)]
    [TestCase(10000, 0.1f)]
    public void LoopAudio_ReturnsValid(int length, float overlap)
    {
        var input = CreateMockEsperAudio(65, 129);
        var output = Stretch.LoopAudio(input, length, overlap);
        Assert.That(output, Is.Not.Null);
        Assert.That(output.GetFrames().RowCount, Is.EqualTo(length));
        Assert.That(output.Config.NVoiced, Is.EqualTo(input.Config.NVoiced));
        Assert.That(output.Config.NUnvoiced, Is.EqualTo(input.Config.NUnvoiced));
    }
}