using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Dynamics(EsperAudio audio, Vector<float> dynamics)
    {
        if (dynamics.Count != audio.Length)
            throw new ArgumentException("Dynamics vector length must match audio length.", nameof(dynamics));
        if (dynamics.Any(p => p is < -1 or > 1))
            throw new ArgumentException("Dynamics vector must not contain values outside of [-1, 1].", nameof(dynamics));
        
        const float thresholdBase = 3456.0f;
        const float resoLower = 6.0f;
        const float resoCenter = 24.0f;
        const float resoUpper = 30.0f;
        
        var pitch = audio.GetPitch();
        var voiced = audio.GetVoicedAmps();
        var unvoiced = audio.GetUnvoiced();
        
        var lowerThreshold = pitch.Map((val) => val == 0 ? 0 : thresholdBase / val);
        var upperThreshold = 2 * lowerThreshold;
        const float eps = 1e-6f;
        voiced.MapIndexedInplace((i, j, val) =>
        {
            if (j < lowerThreshold[i])
                return val * (1 + 0.25f * dynamics[i]);
            if (j < upperThreshold[i])
                return val * (1 + 0.25f * dynamics[i] * (j - lowerThreshold[i]) / (upperThreshold[i] - lowerThreshold[i] + eps));
            return val;
        });
        unvoiced.MapIndexedInplace((i, j, val) =>
        {
            var factor = dynamics[i] < 0 ? val * (1 + 0.5f * dynamics[i]) : val;
            if (j < resoLower)
                return factor;
            if (j < resoCenter)
                return factor * (1 - dynamics[i] * (j - resoLower) / (resoCenter - resoLower));
            if (j < resoUpper)
                return factor * (1 - dynamics[i] * (resoUpper - j) / (resoUpper - resoCenter));
            return factor;
        });
        audio.SetVoicedAmps(voiced);
        audio.SetUnvoiced(unvoiced);
    }
}