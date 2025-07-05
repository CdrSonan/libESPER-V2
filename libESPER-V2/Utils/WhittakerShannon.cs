using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils;

internal static class WhittakerShannon
{
    public static Vector<float> Interpolate(Vector<float> wave, Vector<float> coords)
    {
        var result = Vector<float>.Build.Dense(coords.Count, 0);
        for (var i = 0; i < coords.Count; i++)
        {
            var coord = coords[i];
            var multiplier = float.Sin((coord % 2.0f + 2.0f) % 2.0f * (float)Math.PI) / (float)Math.PI;
            for (var j = 0; j < wave.Count; j++)
                if (Math.Abs(coord - j) < 0.0001f)
                    result[i] += wave[j];
                else if (j % 2 == 0)
                    result[i] += wave[j] * multiplier / (coord - j);
                else
                    result[i] -= wave[j] * multiplier / (coord - j);
        }

        return result;
    }
}