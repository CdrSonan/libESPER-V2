using libESPER_V2.Core;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms.Internal;

internal class Inverse
{
    public static Vector<float> SampleRayleigh(int n, int seed, double scale = 1.0)
    {
        var rng = new System.Random(seed);
        var rayleigh = new Rayleigh(scale, rng);
        var output = Vector<float>.Build.Dense(n, 0);
        for (int i = 0; i < n; i++)
        {
            output[i] = (float)rayleigh.Sample();
        }
        return output;
    }
    public static (Vector<float>, float) ReconstructVoiced(EsperAudio audio, float phase)
    {
        var pitch = audio.GetPitch();
        var amplitudes = audio.GetVoicedAmps();
        Matrix<float> basis = audio.GetVoicedPhases();
        var phaseVector = Vector<float>.Build.Dense(audio.Length, phase);
        for (var i = 1; i < audio.Length; i++)
        {
            phaseVector[i] = phaseVector[i - 1] + 2 * (float)Math.PI / pitch[i];
        }
        basis.MapIndexedInplace((i, j, value) => 
            amplitudes[i, j] * (float)Math.Cos(value + j * phaseVector[i]));
        return (basis.ColumnSums(), phaseVector.Last());// TODO calculate output per sample, not frame
    }
    public static Vector<float> ReconstructUnvoiced(EsperAudio audio, long seed)
    {
        var unvoiced = audio.GetUnvoiced();
        var output = Vector<float>.Build.Dense(audio.Length, 0);
        for (var i = 0; i < unvoiced.Count; i++)
        {
            var start = unvoiced[i].Start;
            var end = unvoiced[i].End;
            if (start < 0 || end >= length) continue;
            output.Add(x.SubVector(start, end - start + 1));
        }
        return output;
    }
}