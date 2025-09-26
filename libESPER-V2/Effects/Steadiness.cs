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
        
        amps.MapIndexedInplace((i, j, val) => (val - meanAmps[j]) * multiplier[i] + meanAmps[j]);
        amps = amps.PointwiseMaximum(0);
        phases.MapIndexedInplace((i, j, val) => (val - meanPhases[j]) * multiplier[i] + meanPhases[j]);
        phases.MapInplace(val => val % (float)(2 * Math.PI));
        phases.MapInplace(val => val > (float)(2 * Math.PI) ? val - (float)(2 * Math.PI) : val);
        phases.MapInplace(val => val < -(float)(2 * Math.PI) ? val + (float)(2 * Math.PI) : val);
        unvoiced.MapIndexedInplace((i, j, val) => (val - meanUnvoiced[j]) * multiplier[i] + meanUnvoiced[j]);
        unvoiced = unvoiced.PointwiseMaximum(0);
        
        audio.SetVoicedAmps(amps);
        audio.SetVoicedPhases(phases);
        audio.SetUnvoiced(unvoiced);
    }
}