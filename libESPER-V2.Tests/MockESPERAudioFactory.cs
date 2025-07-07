using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Tests;

public static class MockESPERAudioFactory
{
    public static EsperAudio CreateMockEsperAudio(int nVoiced, int nUnvoiced)
    {
        var length = 1000;
        var stepSize = 256;
        var config = new EsperAudioConfig((ushort)nVoiced, (ushort)nUnvoiced, stepSize);
        var audio = new EsperAudio(length, config);
        // Set a constant pitch of approx. 440 Hz assuming 44 kHz sample rate
        audio.SetPitch(Vector<float>.Build.Dense(length, 110.0f));
        audio.SetVoicedAmps(Matrix<float>.Build.Dense(length, nVoiced, (i, j) => j < 5 ? 1 : 0));
        audio.SetUnvoiced(Matrix<float>.Build.Dense(length, nUnvoiced, (i, j) => 0.5f));
        return audio;
    }
}