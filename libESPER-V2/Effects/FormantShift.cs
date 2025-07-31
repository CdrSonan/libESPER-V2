using libESPER_V2.Core;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void FormantShift(EsperAudio audio, Vector<float> formantShift)
    {
        var pitch = audio.GetPitch();
        var pseudoPitch = pitch.Map2((val1, val2) => val1 * (1 + 0.5f * val2), formantShift);
        DoPitchShift(audio, pseudoPitch);
    }
    
    public static void FusedPitchFormantShift(EsperAudio audio, Vector<float> pitch, Vector<float> formantShift)
    {
        var pseudoPitch = pitch.Map2((val1, val2) => val1 * (1 + 0.5f * val2), formantShift);
        DoPitchShift(audio, pseudoPitch);
        audio.SetPitch(pitch);
    }
}