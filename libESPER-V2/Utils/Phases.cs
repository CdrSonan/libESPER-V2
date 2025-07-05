using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils;

internal static class Phases
{
    public static float Interpolate(float a, float b, float t)
    {
        if (t is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(t), "T must be between 0 and 1");
        float diffFwd;
        float diffBwd;
        if (b > a)
        {
            diffFwd = b - a;
            diffBwd = a - b + 2 * (float)Math.PI;
        }
        else if (a > b)
        {
            diffFwd = b - a + 2 * (float)Math.PI;
            diffBwd = a - b;
        }
        else return a;

        float result;
        if (diffFwd <= diffBwd)
        {
            result = a + t * diffFwd;
        }
        else
        {
            result = a - t * diffBwd;
        }
        result %= (2 * (float)Math.PI);
        if (result > Math.PI) result -= 2 * (float)Math.PI;
        else if (result < -Math.PI) result += 2 * (float)Math.PI;
        return result;
    }

    public static Vector<float> Interpolate(Vector<float> a, Vector<float> b, float t)
    {
        if (a.Count  != b.Count) throw new ArgumentException("Vectors must have the same length");
        return Vector<float>.Build.Dense(a.Count, (i) => Interpolate(a[i], b[i], t));
    }
}