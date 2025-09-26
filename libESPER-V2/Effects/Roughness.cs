using libESPER_V2.Core;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Roughness(EsperAudio audio, Vector<float> roughness)
    {
        if (roughness.Count != audio.Length)
            throw new ArgumentException("Roughness vector length must match audio length.", nameof(roughness));
        if (roughness.Any(p => p is < -1 or > 1))
            throw new ArgumentException("Roughness vector must not contain values outside of [-1, 1].",
                nameof(roughness));

        var phases = audio.GetVoicedPhases();
        phases.MapIndexedInplace((i, j, val) =>
            roughness[i] > 0 ? val + (float)Normal.Sample(0, 1) * roughness[i] : val * (1 + roughness[i]));
        phases.MapInplace(val => val % (float)(2 * Math.PI));
        phases.MapInplace(val => val > (float)(2 * Math.PI) ? val - (float)(2 * Math.PI) : val);
        phases.MapInplace(val => val < -(float)(2 * Math.PI) ? val + (float)(2 * Math.PI) : val);
        audio.SetVoicedPhases(phases);
    }
}