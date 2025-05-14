using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using IntegralTransforms = MathNet.Numerics.IntegralTransforms;

namespace libESPER_V2.Utils;

/// <summary>
///     Implementation of forward and inverse real-valued Fourier transforms for non-equispaced sample points.
///     Follows the algorithm described in:
///     Chris Anderson and Marie Dillon Dahleh, "Rapid Computation of the Discrete Fourier Transform"
///     SIAMJ. ScI. COMPUT. Vol. 17, No. 4, pp. 913-919, July 1996
///     https://users.math.msu.edu/users/iwenmark/Teaching/MTH995/Papers/NDFT_Taylor.pdf
///     with some adaptations for the real-valued case.
/// </summary>
internal class Fourier
{
    private static int NextPowerOfTwo(int n)
    {
        if (n <= 0) return 1;
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        n++;
        return n;
    }

    private static void DerivativeCoeffs(float[] coeffs, int n)
    {
        for (var i = 0; i < n / 2; i++)
        {
            var temp = coeffs[2 * i];
            coeffs[2 * i] = -1 + coeffs[2 * i + 1] * i;
            coeffs[2 * i + 1] = coeffs[2 * i] * i;
        }
    }

    public static Vector<float> RNFFT_inv(Vector<Complex32> spectrum, Vector<float> coords)
    {
        const int taylorOrder = 3;
        var n = NextPowerOfTwo(coords.Count);
        var coeffMult = n / (float)coords.Count;
        var indices = new int[coords.Count];
        for (var i = 0; i < coords.Count; i++) indices[i] = (int)Math.Round(coords[i] * n);
        var coeffs = new float[n + 2];
        for (var i = 0; i < coords.Count; i++)
        {
            coeffs[2 + i] = spectrum[i].Real * coeffMult;
            coeffs[2 + i + 1] = spectrum[i].Imaginary * coeffMult;
        }

        for (var i = 2 + coords.Count; i < n + 2; i++) coeffs[i] = 0;
        var result = new float[n + 2];
        var taylorMultiplier = 1.0f;
        for (var i = 0; i < taylorOrder - 1; i++)
        {
            var tempCoeffs = new float[n + 2];
            for (var j = 0; j < n + 2; j++) tempCoeffs[j] = coeffs[j];
            IntegralTransforms.Fourier.InverseReal(tempCoeffs, n);
            for (var j = 0; j < n + 2; j++) result[j] += tempCoeffs[j] / taylorMultiplier;
            DerivativeCoeffs(coeffs, n);
            taylorMultiplier *= i + 1;
        }

        IntegralTransforms.Fourier.InverseReal(coeffs, n);
        for (var i = 0; i < n + 2; i++) result[i] += coeffs[i] / taylorMultiplier;
        return Vector<float>.Build.Dense(coords.Count, i => result[indices[i]]);
    }
}