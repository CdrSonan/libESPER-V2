using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;
using libESPER_V2.Utils;

namespace libESPER_V2.Transforms.Internal;

internal static class PitchSync
{
    private static Vector<float> Resample(Vector<float> signal, int n)
    {
        var scale = Vector<float>.Build.Dense(n, i => i * (float)signal.Count / n);
        return WhittakerShannon.Interpolate(signal, scale);
    }

    public static Matrix<float> ToPitchSync(Vector<float> wave, PitchDetection pitchDetection, int n)
    {
        var markers = pitchDetection.PitchMarkers(null);
        var sections = markers.Count - 1;
        var result = Matrix<float>.Build.Dense(markers.Count - 1, n, 0);
        for (var i = 0; i < sections; i++)
        {
            var start = markers[i];
            var count = markers[i + 1] - markers[i];
            var section = wave.SubVector(start, count);
            var resampled = Resample(section, n);
            result.SetRow(i, resampled);
        }

        return result;
    }

    public static (Vector<float>, bool[]) FromPitchSync(Matrix<float> pitchSyncWave, PitchDetection pitchDetection,
        int length)
    {
        var markers = pitchDetection.PitchMarkers(null);
        var sections = markers.Count - 1;
        var result = Vector<float>.Build.Dense(length, 0);
        var coverage = new bool[length];
        for (var i = 0; i < sections; i++)
        {
            var start = markers[i];
            var count = markers[i + 1] - markers[i];
            var section = pitchSyncWave.Row(i);
            var resampled = Resample(section, count);
            result.SetSubVector(start, count, resampled);
            for (var j = start; j < start + count; j++)
                coverage[j] = true;
        }

        return (result, coverage);
    }
}

internal static class Resolve
{
    public static Matrix<Complex32> ToFourier(Matrix<float> pitchSyncWave)
    {
        var n = pitchSyncWave.ColumnCount;
        var extension = Matrix<float>.Build.Dense(pitchSyncWave.RowCount, 2, 0);
        float[][] coeffs = pitchSyncWave.Append(extension).ToRowArrays();
        for (var i = 0; i < pitchSyncWave.RowCount; i++)
            Fourier.ForwardReal(coeffs[i], n, FourierOptions.NoScaling);
        var result = Matrix<Complex32>.Build.Dense(
            pitchSyncWave.RowCount,
            n / 2 + 1,
            (i, j) => new Complex32(coeffs[i][2 * j] * 2 / n, coeffs[i][2 * j + 1] * 2 / n));
        return result;
    }

    public static Matrix<Complex32> Smoothing(Matrix<Complex32> fourierCoeffs)
    {
        var smoothedCoeffs = Matrix<Complex32>.Build.Dense(
            fourierCoeffs.RowCount,
            fourierCoeffs.ColumnCount,
            (i, j) => (i == 0 || i == fourierCoeffs.RowCount - 1) ?
                fourierCoeffs[i, j] :
                (fourierCoeffs[i, j] + fourierCoeffs[i - 1, j] + fourierCoeffs[i + 1, j]) / 3);
        return smoothedCoeffs; //TODO: Implement smoothing
    }

    public static Matrix<float> FromFourier(Matrix<Complex32> fourierCoeffs)
    {
        var realCoeffs = Matrix<float>.Build.Dense(
            fourierCoeffs.RowCount,
            fourierCoeffs.ColumnCount * 2,
            (i, j) => j % 2 == 0 ? fourierCoeffs[i, j / 2].Real : fourierCoeffs[i, j / 2].Imaginary);
        float[][] coeffs = realCoeffs.ToRowArrays();
        var n = fourierCoeffs.ColumnCount * 2 - 2;
        for (var i = 0; i < fourierCoeffs.RowCount; i++) Fourier.InverseReal(coeffs[i], n, FourierOptions.NoScaling);
        var result = Matrix<float>.Build.Dense(
            fourierCoeffs.RowCount,
            n,
            (i, j) => coeffs[i][j] / 2);
        return result;
    }

    public static Matrix<float> ToUnvoiced(Matrix<Complex32> fourierCoeffs, Vector<float> wave,
        PitchDetection pitchDetection, int stepSize, int n)
    {
        var nBatches = wave.Count / stepSize;
        var pitchSyncWave = FromFourier(fourierCoeffs);
        var (voicedWave, validity) = PitchSync.FromPitchSync(pitchSyncWave, pitchDetection, wave.Count);
        var output = Matrix<float>.Build.Dense(nBatches, n);
        for (var i = 0; i < nBatches; i++)
        {
            var windowStart = i * stepSize + (stepSize / 2 - n - 1);
            var windowLength = 2 * n - 2;
            if (windowStart < 0) windowStart = 0;
            if (windowStart + windowLength > wave.Count) windowStart = wave.Count - windowLength;
            var window = wave.SubVector(windowStart, windowLength).ToArray();
            var voicedWindow = voicedWave.SubVector(windowStart, windowLength);
            var unvoicedWindow = new float[windowLength + 2]; // +2 to have enough storage for the (inplace) result
            for (var j = 0; j < windowLength; j++) unvoicedWindow[j] = validity[windowStart + j] ? window[j] - voicedWindow[j] : 0;
            Fourier.ForwardReal(unvoicedWindow, windowLength);
            for (var j = 0; j < n; j++)
                output[i, j] = (float)Math.Pow(unvoicedWindow[2 + j], 2) +
                               (float)Math.Pow(unvoicedWindow[2 + j + 1], 2);
        }

        return output;
    }

    public static Matrix<float> ToVoiced(
        Matrix<Complex32> fourierCoeffs,
        PitchDetection pitchDetection,
        int stepSize,
        int batches)
    {
        var markers = pitchDetection.PitchMarkers(null);
        var validity = pitchDetection.Validity(null);
        var start = 0;
        var end = 0;
        var output = Matrix<float>.Build.Dense(batches, fourierCoeffs.ColumnCount * 2, 0);
        for (var i = 0; i < batches; i++)
        {
            while (start + 1 < markers.Count && markers[start + 1] < i * stepSize) start++;
            while (end < markers.Count - 1 && markers[end] <= (i + 1) * stepSize) end++;
            var count = end - start;
            var buffer = Vector<Complex32>.Build.Dense(fourierCoeffs.ColumnCount, 0);
            if (count == 0)
            {
                continue;
            }
            for (var j = start; j < end; j++)
                if (validity[j])
                    buffer += fourierCoeffs.Row(j);
                else
                    buffer += (fourierCoeffs.Row(j-1) + fourierCoeffs.Row(j + 1)) / 2;
            buffer /= count;
            output.SetRow(i,
                Vector<float>.Build.Dense(fourierCoeffs.ColumnCount * 2,
                    j => j >= fourierCoeffs.ColumnCount
                        ? buffer[j - fourierCoeffs.ColumnCount].Phase
                        : buffer[j].Magnitude));
        }
        return output;
    }
}