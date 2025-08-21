using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;
using static libESPER_V2.Tests.MockFactories;

namespace libESPER_V2.Tests.Effects;

[TestFixture]
[TestOf(typeof(libESPER_V2.Effects.Effects))]
public class EffectsTest
{

    [Test]
    public void PitchShift_LengthMismatch_ThrowsArgumentException()
    {
        var audio = CreateMockEsperAudio(25, 129);
        var pitch = Vector<float>.Build.Dense(5); // Length does not match audio length

        Assert.Throws<ArgumentException>(() => libESPER_V2.Effects.Effects.PitchShift(audio, pitch));
    }

    [Test]
    public void PitchShift_UpdatesPitch()
    {
        var audio = CreateMockEsperAudio(25, 129);
        var pitch = Vector<float>.Build.Dense(1000, i => 1.0f); // Valid pitch vector

        libESPER_V2.Effects.Effects.PitchShift(audio, pitch);

        Assert.That(audio.GetPitch(), Is.EqualTo(pitch));
    }

    [Test]
    public void PitchShift_PitchContainsNegative_ThrowsException()
    {
        var audio = CreateMockEsperAudio(25, 129);
        var pitch = Vector<float>.Build.Dense(1000, i => i == 5 ? -1.0f : 1.0f);

        Assert.Throws<ArgumentException>(() => libESPER_V2.Effects.Effects.PitchShift(audio, pitch));
    }

    [Test]
    public void PitchShift_PitchContainsZero_HandlesCorrectly()
    {
        var audio = CreateMockEsperAudio(25, 129);
        var pitch = Vector<float>.Build.Dense(1000, 0);

        libESPER_V2.Effects.Effects.PitchShift(audio, pitch);

        Assert.That(audio.GetPitch().All(p => p == 0), Is.True);
    }

    [Test]
    public void PitchShift_HandlesSmallPitchValues()
    {
        var audio = CreateMockEsperAudio(25, 129);
        var pitch = Vector<float>.Build.Dense(1000, i => 0.9f); // Large values

        libESPER_V2.Effects.Effects.PitchShift(audio, pitch);

        Assert.That(audio.GetPitch().All(p => p == 0.9f), Is.True);
    }
    
    [Test]
    public void PitchShift_HandlesLargePitchValues()
    {
        var audio = CreateMockEsperAudio(25, 129);
        var pitch = Vector<float>.Build.Dense(1000, i => 10000.0f); // Large values

        libESPER_V2.Effects.Effects.PitchShift(audio, pitch);

        Assert.That(audio.GetPitch().All(p => p == 10000.0f), Is.True);
    }
    
    [Test]
    public void PitchShift_EqualInput_ReturnsSameOutput()
    {
        var audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var pitch = audio.GetPitch();

        libESPER_V2.Effects.Effects.PitchShift(audio, pitch);

        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }

    [Test]
    public void Breathiness_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var breathiness = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.Breathiness(audio, breathiness);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void Brightness_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var brightness = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.Brightness(audio, brightness);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void Dynamics_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var dynamics = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.Dynamics(audio, dynamics);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void FormantShift_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var shift = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.FormantShift(audio, shift);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void FusedPitchFormantShift_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var pitch = Vector<float>.Build.Dense(audio.Length, 0);
        var formant = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.FusedPitchFormantShift(audio, pitch, formant);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void Growl_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var growl = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.Growl(audio, growl);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void Mouth_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var mouth = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.Mouth(audio, mouth);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void Roughness_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var roughness = Vector<float>.Build.Dense(audio.Length, 0);
        
        libESPER_V2.Effects.Effects.Roughness(audio, roughness);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
    
    [Test]
    public void Steadiness_zeroInput_ReturnsSameOutput()
    {
        var  audio = CreateMockEsperAudio(25, 129);
        var frames = audio.GetFrames();
        var steadiness = Vector<float>.Build.Dense(audio.Length, 0);
        

        libESPER_V2.Effects.Effects.Steadiness(audio, steadiness);
        
        Assert.That(audio.GetFrames(), Is.EqualTo(frames));
    }
}