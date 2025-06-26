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
        for (var i = 0; i < n; i++)
        {
            output[i] = (float)rayleigh.Sample();
        }
        return output;
    }
    public static (Vector<float>, float) ReconstructVoiced(EsperAudio audio, float phase)
    {
        var pitch = audio.GetPitch();
        var amplitudes = audio.GetVoicedAmps();
        var phases = audio.GetVoicedPhases();
        var output = Vector<float>.Build.Dense(audio.Length * audio.Config.StepSize, 0);
        for (var i = 0; i < audio.Config.StepSize / 2; i++)
        {
            var components = phases.Row(0) + Vector<float>.Build.Dense(audio.Config.NUnvoiced, (j) => j * 2 * (float)Math.PI / pitch.First());
            components.MapIndexedInplace((j, value) => amplitudes[0, j] * (float)Math.Cos(value + j * phase));
            output[i] = components.Sum();
        }
        return (output, 0);
    }
    public static Vector<float> ReconstructUnvoiced(EsperAudio audio, long seed)
    {
        var unvoiced = audio.GetUnvoiced();
        var output = Vector<float>.Build.Dense(audio.Length * audio.Config.StepSize, 0);
        return output;
    }
}