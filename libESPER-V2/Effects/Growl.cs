using libESPER_V2.Core;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Growl(EsperAudio audio, Vector<float> growl)
    {
        if (growl.Count != audio.Length)
            throw new ArgumentException("Growl vector length must match audio length.", nameof(growl));
        if (growl.Any(p => p is < 0 or > 1))
            throw new ArgumentException("Growl vector must not contain values outside of [0, 1].", nameof(growl));

        const double phaseAdvance = 2 * Math.PI * 0.06f;

        var lfoPhase = 0.0;
        for (var i = 0; i < audio.Length; i++)
        {
            lfoPhase = (lfoPhase + phaseAdvance) % (2 * Math.PI);
            var exponent = ContinuousUniform.Sample(0.1, 4.0);
            var lfo = 1 - (float)Math.Pow(Math.Abs(Math.Sin(lfoPhase)), exponent) * growl[i];
            audio.SetVoicedAmps(i, audio.GetVoicedAmps(i) * lfo);
            audio.SetUnvoiced(i, audio.GetUnvoiced(i) * lfo);
        }
        
    }
}