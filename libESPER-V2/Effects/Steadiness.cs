using libESPER_V2.Core;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Steadiness(EsperAudio audio, Vector<float> steadiness)
    {
        var multiplier = (1.0f - steadiness).PointwisePower(2.0f);
        var amps = audio.GetVoicedAmps();
        var phases = audio.GetVoicedPhases();
        var unvoiced = audio.GetUnvoiced();
        
        var meanAmps = amps.ColumnSums() / amps.RowCount;
        var meanPhases = phases.ColumnSums() / phases.RowCount;
        var meanUnvoiced = unvoiced.ColumnSums() / unvoiced.RowCount;
        const float eps = 1e-6f;
        var meanAmpsLog = meanAmps.Map(val => (float)Math.Log(val + eps));
        var meanUnvoicedLog = meanUnvoiced.Map(val => (float)Math.Log(val + eps));
        
        amps.MapIndexedInplace((i, j, val) =>
        {
            var valLog = (float)Math.Log(val + eps);
            var transformed = (valLog - meanAmpsLog[j]) * multiplier[i] + meanAmpsLog[j];
            return (float)Math.Max(Math.Exp(transformed) - eps, 0);
        });
        phases.MapIndexedInplace((i, j, val) => (val - meanPhases[j]) * multiplier[i] + meanPhases[j]);
        phases.MapInplace(x => x % (2 * (float)Math.PI));
        phases.MapInplace(x => x < -Math.PI ? x + 2 * (float)Math.PI : x);
        phases.MapInplace(x => x > Math.PI ? x - 2 * (float)Math.PI : x);
        unvoiced.MapIndexedInplace((i, j, val) =>
        {
            var valLog = (float)Math.Log(val + eps);
            var transformed = (valLog - meanUnvoicedLog[j]) * multiplier[i] + meanUnvoicedLog[j];
            return (float)Math.Max(Math.Exp(transformed) - eps, 0);
        });
        
        audio.SetVoicedAmps(amps);
        audio.SetVoicedPhases(phases);
        audio.SetUnvoiced(unvoiced);
    }
}
