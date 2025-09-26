using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Mouth(EsperAudio audio, Vector<float> mouth)
    {
        const float referenceMultiplier = 0.5f;
        const float baseWindowSize = 10;
        
        
        if (mouth.Count != audio.Length)
            throw new ArgumentException("Mouth vector length must match audio length.", nameof(mouth));
        if (mouth.Any(p => p is < -1 or > 1))
            throw new ArgumentException("Mouth vector must not contain values outside of [-1, 1].", nameof(mouth));

        var voiced = audio.GetVoicedAmps();
        var unvoiced = audio.GetUnvoiced();
        
        var exponent = mouth.Map((val) => 1 - 0.5f * val);
        
        var reference = voiced.RowNorms(double.PositiveInfinity).ToSingle() * referenceMultiplier;
        const float eps = 1e-6f;
        voiced.MapIndexedInplace((i, j, val) => float.Pow(val / (reference[i] + eps), exponent[i]) * reference[i]);
        
        reference = unvoiced.RowNorms(double.PositiveInfinity).ToSingle() * referenceMultiplier;
        unvoiced.MapIndexedInplace((i, j, val) => float.Pow(val / (reference[i] + eps), exponent[i]) * reference[i]);

        var windowSizes = mouth.Map((val) => val < 0 ? baseWindowSize * -val : 0);
        for (var i = 0; i < audio.Length; i++)
        {
            var start = (int)(i - windowSizes[i]);
            var end = (int)(i + windowSizes[i]) + 1;
            if (start < 0)
            {
                start = 0;
            }
            if (end > audio.Length)
            {
                end = audio.Length;
            }
            if (start == end)
            {
                end++;
            }
            var voicedSection = voiced.SubMatrix(
                start,
                end - start,
                0,
                audio.Config.NVoiced);
            audio.SetVoicedAmps(i, voicedSection.ColumnSums() / (end - start));
            var unvoicedSection = unvoiced.SubMatrix(
                start,
                end - start,
                0,
                audio.Config.NUnvoiced);
            audio.SetUnvoiced(i, unvoicedSection.ColumnSums() / (end - start));
        }
    }
}