using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;
using libESPER_V2.Utils;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

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

    public static (Vector<float>, bool[], bool[]) FromPitchSync(Matrix<float> pitchSyncWave, PitchDetection pitchDetection,
        int length)
    {
        var markers = pitchDetection.PitchMarkers(null);
        var pitchSyncValidity = pitchDetection.Validity(null);
        var sections = markers.Count - 1;
        var result = Vector<float>.Build.Dense(length, 0);
        var coverage = new bool[length];
        var validity = Enumerable.Repeat(true, length).ToArray();
        for (var i = 0; i < sections; i++)
        {
            var start = markers[i];
            var count = markers[i + 1] - markers[i];
            var section = pitchSyncWave.Row(i);
            var previousSection = pitchSyncWave.Row(i == 0 ? i : i - 1);
            var nextSection = pitchSyncWave.Row(i == sections - 1 ? sections - 1 : i + 1);
            section.MapIndexedInplace((i, val) => (val + previousSection[i] * ((float)i / sections) + nextSection[i] * (1 - (float)i / sections)) / 2);
            var resampled = Resample(section, count);
            result.SetSubVector(start, count, resampled);
            for (var j = start; j < start + count; j++)
            {
                coverage[j] = true;
                validity[j] = pitchSyncValidity[i];
            }
        }

        return (result, coverage, validity);
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

    public static Matrix<Complex32> Smoothing(Matrix<Complex32> fourierCoeffs, bool[] validity)
    {
        const int windowSize = 10;
        if (fourierCoeffs.RowCount < windowSize) return fourierCoeffs;
        fourierCoeffs.MapIndexedInplace((i, j, val) => validity[i] ? val : 0);
        var smoothedCoeffs = Matrix<Complex32>.Build.Dense(fourierCoeffs.RowCount, fourierCoeffs.ColumnCount, 0);
        for (var i = 0; i < fourierCoeffs.RowCount; i++)
        {
            var start = i - windowSize / 2;
            if (start < 0) start = 0;
            if (start >= fourierCoeffs.RowCount - windowSize)  start = fourierCoeffs.RowCount - windowSize;
            var window = fourierCoeffs.SubMatrix(start, windowSize, 0, fourierCoeffs.ColumnCount);
            var amplitudes = Matrix<float>.Build.Dense(windowSize, fourierCoeffs.ColumnCount, 0);
            var phases = Matrix<float>.Build.Dense(windowSize, fourierCoeffs.ColumnCount, 0);
            phases.MapInplace(val => float.IsNaN(val) ? 0 : val);
            window.MapConvert(val => val.Magnitude, amplitudes);
            window.MapConvert(val => val.Phase, phases);

            var principalPhases = phases.Column(1);
            var normalizedPhases = phases.MapIndexed((j, k, val) => val - k * principalPhases[j]);
            normalizedPhases.MapInplace(val => val % (2 * (float)Math.PI));
            normalizedPhases.MapInplace(val => val < -Math.PI ? val + 2 * (float)Math.PI : val);
            normalizedPhases.MapInplace(val => val > Math.PI ? val - 2 * (float)Math.PI : val);
            var normalizedWindow = Matrix<Complex32>.Build.Dense(windowSize, fourierCoeffs.ColumnCount,
                (j, k) => Complex32.FromPolarCoordinates(amplitudes[j, k], normalizedPhases[j, k]));
            var expectedAmplitudesNoise = amplitudes.ColumnSums() * 0.6f;
            // Everything under this value is likely enough to be fully unvoiced to be treated as such.
            // This distribution assumes each fourier component is the result of sampling a normal distribution with equal variance.
            // Under this assumption, the sums of the components follow a normal distribution with variance equal to the sum of their variances.
            // The scale parameter of the Rayleigh distribution is based on this sum distribution variance.
            var expectedAmplitudesVoiced = amplitudes.ColumnSums();
            var realAmplitudes = Vector<float>.Build.Dense(fourierCoeffs.ColumnCount, 0);
            normalizedWindow.ColumnSums().MapConvert(val => val.Magnitude, realAmplitudes);
            var criterion = realAmplitudes.PointwiseMaximum(expectedAmplitudesNoise).PointwiseMinimum(expectedAmplitudesVoiced);
            var multipliers = (criterion - expectedAmplitudesNoise) / (expectedAmplitudesVoiced - expectedAmplitudesNoise);
            var row = window.ColumnSums();
            row.MapIndexedInplace((j, val) => val * multipliers[j] / windowSize);
            smoothedCoeffs.SetRow(i, row);
        }
        return smoothedCoeffs;
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
        var (voicedWave, coverage, validity) = PitchSync.FromPitchSync(pitchSyncWave, pitchDetection, wave.Count);
        var output = Matrix<float>.Build.Dense(nBatches, n, 0);
        var sectionValidity = new bool[nBatches];
        for (var i = 0; i < nBatches; i++)
        {
            var windowStart = i * stepSize + (stepSize / 2 - n - 1);
            var windowLength = 2 * n - 2;
            if (windowStart < 0) windowStart = 0;
            if (windowStart + windowLength > wave.Count) windowStart = wave.Count - windowLength;
            var window = wave.SubVector(windowStart, windowLength).ToArray();
            var voicedWindow = voicedWave.SubVector(windowStart, windowLength);
            var unvoicedWindow = new float[windowLength + 2]; // +2 to have enough storage for the (inplace) result
            for (var j = 0; j < windowLength; j++) unvoicedWindow[j] = coverage[windowStart + j] ? window[j] - voicedWindow[j] : 0;
            Fourier.ForwardReal(unvoicedWindow, windowLength, FourierOptions.AsymmetricScaling);
            for (var j = 0; j < n; j++)
                output[i, j] = (float)Math.Sqrt(Math.Pow(unvoicedWindow[2 * j], 2) + Math.Pow(unvoicedWindow[2 * j + 1], 2));
            sectionValidity[i] = validity[windowStart..(windowStart + windowLength)].All(val => val);
        }

        sectionValidity[0] = true;
        sectionValidity[^1] = true;
        for (var i = 0; i < nBatches; i++)
        {
            if (sectionValidity[i]) continue;
            var leftRowOffset = 0;
            while (!sectionValidity[i - leftRowOffset]) leftRowOffset--;
            var rightRowOffset = 0;
            while (!sectionValidity[i + rightRowOffset]) rightRowOffset++;
            output.SetRow(i, (output.Row(i - leftRowOffset) + output.Row(i + rightRowOffset)) / 2);
        }
        return output;
    }

    public static (Matrix<float>, Matrix<float>) ToVoiced(
        Matrix<Complex32> fourierCoeffs,
        PitchDetection pitchDetection,
        int stepSize,
        int batches)
    {
        var markers = pitchDetection.PitchMarkers(null);
        var validity = pitchDetection.Validity(null);
        var start = 0;
        var end = 0;
        var outputAmps = Matrix<float>.Build.Dense(batches, fourierCoeffs.ColumnCount, 0);
        var outputPhases = Matrix<float>.Build.Dense(batches, fourierCoeffs.ColumnCount, 0);
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
            
            var amps = Vector<float>.Build.Dense(fourierCoeffs.ColumnCount,
                    j => buffer[j].Magnitude);
            var phases = Vector<float>.Build.Dense(fourierCoeffs.ColumnCount,
                j => buffer[j].Phase);
            var principalPhase = phases[1];
            phases.MapIndexedInplace((j, val) => (val - j * principalPhase) % (float)(2 * Math.PI));
            phases.MapInplace(val => val > Math.PI ? val - (float)(2 * Math.PI) : val);
            phases.MapInplace(val => val < -Math.PI ? val + (float)(2 * Math.PI) : val);
            outputAmps.SetRow(i, amps);
            outputPhases.SetRow(i, phases);

        }
        return (outputAmps, outputPhases);
    }
}