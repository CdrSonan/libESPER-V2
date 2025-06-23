using System;
using System.Linq;
using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace libESPER_V2.Tests.Core;

[TestFixture]
public class EsperAudioTest
{
    [SetUp]
    public void Setup()
    {
        _defaultConfig = new EsperAudioConfig(2, 3, 10);
    }

    private EsperAudioConfig _defaultConfig;

    [Test]
    public void Constructor_DataMatrixAndConfig_ShouldInitializeCorrectly()
    {
        var data = Matrix<float>.Build.Dense(5, _defaultConfig.FrameSize(), 0.5f);
        var esperAudio = new EsperAudio(data, _defaultConfig);

        ClassicAssert.AreEqual(_defaultConfig, esperAudio.Config);
        ClassicAssert.AreEqual(5, esperAudio.Length);
        ClassicAssert.AreEqual(data, esperAudio.GetFrames());
    }

    [Test]
    public void Constructor_DataMatrixWithInvalidColumnCount_ShouldThrowException()
    {
        var data = Matrix<float>.Build.Dense(3, 4, 0.5f);

        Assert.Throws<ArgumentException>(() => new EsperAudio(data, _defaultConfig));
    }

    [Test]
    public void Constructor_LengthAndConfig_ShouldCreateMatrixWithZeros()
    {
        var esperAudio = new EsperAudio(5, _defaultConfig);

        var frames = esperAudio.GetFrames();
        ClassicAssert.AreEqual(5, esperAudio.Length);
        ClassicAssert.AreEqual(5, frames.RowCount);
        ClassicAssert.AreEqual(_defaultConfig.FrameSize(), frames.ColumnCount);
    }

    [Test]
    public void Constructor_CopyEsperAudio_ShouldCloneCorrectly()
    {
        var original = new EsperAudio(5, _defaultConfig);
        var esperAudio = new EsperAudio(original);

        ClassicAssert.AreEqual(original.Config, esperAudio.Config);
        ClassicAssert.AreEqual(original.Length, esperAudio.Length);
        ClassicAssert.AreEqual(original.GetFrames(), esperAudio.GetFrames());
    }

    [Test]
    public void GetFrames_ValidIndex_ReturnRowVector()
    {
        var esperAudio = new EsperAudio(5, _defaultConfig);
        var frame = esperAudio.GetFrames(2);

        ClassicAssert.AreEqual(_defaultConfig.FrameSize(), frame.Count);
    }

    [Test]
    public void GetFrames_InvalidIndex_ShouldThrowException()
    {
        var esperAudio = new EsperAudio(5, _defaultConfig);

        Assert.Throws<ArgumentOutOfRangeException>(() => esperAudio.GetFrames(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => esperAudio.GetFrames(5));
    }

    [Test]
    public void GetFrames_ValidRange_ShouldReturnMatrix()
    {
        var esperAudio = new EsperAudio(10, _defaultConfig);
        var range = esperAudio.GetFrames(2, 5);

        ClassicAssert.AreEqual(4, range.RowCount);
        ClassicAssert.AreEqual(_defaultConfig.FrameSize(), range.ColumnCount);
    }

    [Test]
    public void GetFrames_InvalidRange_ShouldThrowException()
    {
        var esperAudio = new EsperAudio(10, _defaultConfig);

        Assert.Throws<ArgumentOutOfRangeException>(() => esperAudio.GetFrames(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => esperAudio.GetFrames(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => esperAudio.GetFrames(6, 5));
    }

    [Test]
    public void GetPitch_ReturnCorrectVector()
    {
        var data = Matrix<float>.Build.Dense(5, _defaultConfig.FrameSize(), (i, j) => i * 0.1f);
        var esperAudio = new EsperAudio(data, _defaultConfig);
        var pitch = esperAudio.GetPitch();

        ClassicAssert.AreEqual(5, pitch.Count);
    }

    [Test]
    public void GetPitch_ValidIndex_ShouldReturnCorrectPitch()
    {
        var data = Matrix<float>.Build.Dense(5, _defaultConfig.FrameSize(), (i, j) => i * 0.1f);
        var esperAudio = new EsperAudio(data, _defaultConfig);
        var pitch = esperAudio.GetPitch(2);

        ClassicAssert.AreEqual(0.2f, pitch);
    }

    [Test]
    public void GetPitch_InvalidIndex_ShouldThrowException()
    {
        var esperAudio = new EsperAudio(5, _defaultConfig);

        Assert.Throws<ArgumentOutOfRangeException>(() => esperAudio.GetPitch(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => esperAudio.GetPitch(5));
    }

    [Test]
    public void SetPitch_ValidVector_ShouldUpdateCorrectly()
    {
        var esperAudio = new EsperAudio(5, _defaultConfig);
        var newPitch = Vector<float>.Build.Dense(5, 1.1f);

        esperAudio.SetPitch(newPitch);

        ClassicAssert.AreEqual(newPitch, esperAudio.GetPitch());
    }

    [Test]
    public void SetPitch_InvalidVector_ShouldThrowException()
    {
        var esperAudio = new EsperAudio(5, _defaultConfig);
        var invalidPitch = Vector<float>.Build.Dense(4, 1.1f);

        Assert.Throws<ArgumentException>(() => esperAudio.SetPitch(invalidPitch));
    }

    [Test]
    public void GetVoicedAmps_CorrectValuesReturned()
    {
        var data = Matrix<float>.Build.Dense(5, _defaultConfig.FrameSize(), (i, j) => i * 0.1f);
        var esperAudio = new EsperAudio(data, _defaultConfig);
        var amps = esperAudio.GetVoicedAmps();

        ClassicAssert.AreEqual(5, amps.RowCount);
        ClassicAssert.AreEqual(_defaultConfig.NVoiced, amps.ColumnCount);
    }

    [Test]
    public void Validate_EnsureValuesAreNormalized()
    {
        var data = Matrix<float>.Build.Dense(5, _defaultConfig.FrameSize(), (i, j) => j == 0 ? -1.0f : j * 0.5f);
        var esperAudio = new EsperAudio(data, _defaultConfig);
        esperAudio.Validate();

        var pitch = esperAudio.GetPitch();
        ClassicAssert.True(pitch.All(x => x >= 0));

        var unvoiced = esperAudio.GetUnvoiced();
        ClassicAssert.True(unvoiced.ToRowMajorArray().All(x => x >= 0));
    }
}