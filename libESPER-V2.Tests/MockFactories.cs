using System;
using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Tests;

public static class MockFactories
{
    public static EsperAudio CreateMockEsperAudio(int nVoiced, int nUnvoiced)
    {
        const int length = 1000;
        const int stepSize = 256;
        var config = new EsperAudioConfig((ushort)nVoiced, (ushort)nUnvoiced, stepSize);
        var audio = new EsperAudio(length, config);
        // Set a constant pitch of approx. 440 Hz assuming 44 kHz sample rate
        audio.SetPitch(Vector<float>.Build.Dense(length, 110.0f));
        audio.SetVoicedAmps(Matrix<float>.Build.Dense(length, nVoiced, (i, j) => j < 5 ? 1 : 0));
        audio.SetUnvoiced(Matrix<float>.Build.Dense(length, nUnvoiced, (i, j) => 0.5f));
        return audio;
    }
    
    private static float StackedSines(int i, int n)
    {
        return (float)(Math.Sin(2 * Math.PI * i / n) + Math.Sin(4 * Math.PI * i / n) +
                       Math.Sin(6 * Math.PI * i / n));
    }
    
    public static PitchDetection CreateMockPitchDetection(int nVoiced, int nUnvoiced)
    {
        var wave = Vector<float>.Build.Dense(1000, i => StackedSines(i, 256));
        var config = new EsperAudioConfig((ushort)nVoiced, (ushort)nUnvoiced, 256);
        return new PitchDetection(wave, config, 0.1f, 10);
    }
}