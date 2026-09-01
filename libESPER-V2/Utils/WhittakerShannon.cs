using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils;

internal static class WhittakerShannon
{
    public static Vector<float> Interpolate(Vector<float> wave, Vector<float> coords)
    {
        var alternatingWave = wave.MapIndexed((i, val) => i % 2 == 0 ? val : -val);
        var result = Vector<float>.Build.Dense(coords.Count, 0);
        for (var i = 0; i < coords.Count; i++)
        {
            var coord = coords[i];
            var nearest = (int)MathF.Round(coord);
            var multiplier = float.Sin((coord % 2.0f + 2.0f) % 2.0f * MathF.PI) / MathF.PI;
            var sum = 0.0f;
            if (nearest < wave.Count && nearest >= 0 && Math.Abs(coord - nearest) < 0.0001f)
            {
                for (var j = 0; j < nearest; j++)
                    sum += alternatingWave[j] / (coord - j);
                for (var j = nearest + 1; j < wave.Count; j++)
                    sum += alternatingWave[j] / (coord - j);
                sum *= multiplier;
                sum += wave[nearest];
            }
            else
            {
                for (var j = 0; j < wave.Count; j++)
                    sum += alternatingWave[j] / (coord - j);
                sum *= multiplier;
            }
            result[i] = sum;
        }

        return result;
    }
    public static Vector<float> Resample(Vector<float> signal, int n)
    {
        var scale = Vector<float>.Build.Dense(n, i => i * (float)signal.Count / n);
        return Interpolate(signal, scale);
    }
}