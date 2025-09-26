using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Brightness(EsperAudio audio, Vector<float> brightness)
    {
        if (brightness.Count != audio.Length)
            throw new ArgumentException("Brightness vector length must match audio length.", nameof(brightness));
        if (brightness.Any(p => p is < -1 or > 1))
            throw new ArgumentException("Brightness vector must not contain values outside of [-1, 1].", nameof(brightness));

        const float referenceMultiplier = 0.9f;
        
        var voiced = audio.GetVoicedAmps();
        var phases = audio.GetVoicedPhases();
        var unvoiced = audio.GetUnvoiced();
        
        var exponent = brightness.Map((val) => 1 - 0.5f * val);
        
        var reference = voiced.RowNorms(double.PositiveInfinity).ToSingle() * referenceMultiplier;
        const float eps = 1e-6f;
        voiced.MapIndexedInplace((i, j, val) => float.Pow(val / (reference[i] + eps), exponent[i]) * reference[i]);
        
        phases.MapIndexedInplace((i, j, val) => brightness[i] > 0 ? val * (1 - brightness[i]) : val);
        
        reference = unvoiced.RowNorms(double.PositiveInfinity).ToSingle() * referenceMultiplier;
        unvoiced.MapIndexedInplace((i, j, val) => float.Pow(val / reference[i], exponent[i]) * reference[i]);
    }
}