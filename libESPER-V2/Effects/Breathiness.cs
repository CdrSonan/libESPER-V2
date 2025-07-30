using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Breathiness(EsperAudio audio, Vector<float> breathiness)
    {
        if (breathiness.Count != audio.Length)
            throw new ArgumentException("Breathiness vector length must match audio length.", nameof(breathiness));
        if (breathiness.Any(p => p is < -1 or > 1))
            throw new ArgumentException("Breathiness vector must not contain values outside of [-1, 1].", nameof(breathiness));
        var voiced = audio.GetVoicedAmps();
        var unvoiced = audio.GetUnvoiced();
        voiced.MapIndexedInplace((i, j, val) => breathiness[i] < 0 ? val * (1 + breathiness[i]) : val);
        unvoiced.MapIndexedInplace((i, j, val) => breathiness[i] > 0 ? val * (1 - breathiness[i]) : val);
        audio.SetVoicedAmps(voiced);
        audio.SetUnvoiced(unvoiced);
    }
}