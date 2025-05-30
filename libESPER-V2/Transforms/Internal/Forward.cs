﻿using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms.Internal;

internal class PitchSync
{
    private static Vector<float> WhittakerShannon(Vector<float> wave, Vector<float> coords)
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

    private static Vector<float> Resample(Vector<float> signal, int n)
    {
        var scale = Vector<float>.Build.Dense(n, i => i * (float)signal.Count / n);
        return WhittakerShannon(signal, scale);
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

    public static Vector<float> FromPitchSync(Matrix<float> pitchSyncWave, PitchDetection pitchDetection,
        int length)
    {
        var markers = pitchDetection.PitchMarkers(null);
        var sections = markers.Count - 1;
        var result = Vector<float>.Build.Dense(length, 0);
        for (var i = 0; i < sections; i++)
        {
            var start = markers[i];
            var count = markers[i + 1] - markers[i];
            var section = pitchSyncWave.Row(i);
            var resampled = Resample(section, count);
            result.SetSubVector(start, count, resampled);
        }

        return result;
    }
}

internal class Resolve
{
    public static Matrix<Complex32> ToFourier(Matrix<float> pitchSyncWave)
    {
        var n = pitchSyncWave.ColumnCount;
        var extension = Matrix<float>.Build.Dense(pitchSyncWave.RowCount, 2, 0);
        float[][] coeffs = pitchSyncWave.Append(extension).ToRowArrays();
        for (var i = 0; i < pitchSyncWave.RowCount; i++) Fourier.ForwardReal(coeffs[i], n);
        var result = Matrix<Complex32>.Build.Dense(
            pitchSyncWave.RowCount,
            n / 2 + 1,
            (i, j) => new Complex32(coeffs[i][2 * j], coeffs[i][2 * j + 1]));
        return result;
    }

    public static Matrix<Complex32> Smoothing(Matrix<Complex32> fourierCoeffs)
    {
        return fourierCoeffs; //TODO: Implement smoothing
    }

    public static Matrix<float> FromFourier(Matrix<Complex32> fourierCoeffs)
    {
        var realCoeffs = Matrix<float>.Build.Dense(
            fourierCoeffs.RowCount,
            fourierCoeffs.ColumnCount * 2,
            (i, j) => j % 2 == 0 ? fourierCoeffs[i, j / 2].Real : fourierCoeffs[i, j / 2].Imaginary);
        float[][] coeffs = realCoeffs.ToRowArrays();
        var n = fourierCoeffs.ColumnCount * 2 - 2;
        for (var i = 0; i < fourierCoeffs.RowCount; i++) Fourier.InverseReal(coeffs[i], n);
        var result = Matrix<float>.Build.Dense(
            fourierCoeffs.RowCount,
            n,
            (i, j) => coeffs[i][j]);
        return result;
    }

    public static Matrix<float> ToUnvoiced(Matrix<Complex32> fourierCoeffs, Vector<float> wave,
        PitchDetection pitchDetection,
        int batchSize, int nBatches)
    {
        var pitchSyncWave = FromFourier(fourierCoeffs);
        var voicedWave = PitchSync.FromPitchSync(pitchSyncWave, pitchDetection, nBatches * batchSize);
        var output = Matrix<float>.Build.Dense(nBatches, batchSize / 2 + 1);
        for (var i = 0; i < nBatches; i++)
        {
            var window = wave.SubVector(i * batchSize, batchSize).ToArray();
            var voicedWindow = voicedWave.SubVector(i * batchSize, batchSize);
            var unvoicedWindow = new float[batchSize + 2];
            for (var j = 0; j < batchSize; j++) unvoicedWindow[j] = window[j] - voicedWindow[j];
            Fourier.ForwardReal(unvoicedWindow, batchSize);
            for (var j = 0; j < batchSize / 2 + 1; j++)
                output[i, j] = (float)Math.Pow(unvoicedWindow[2 + j], 2) +
                               (float)Math.Pow(unvoicedWindow[2 + j + 1], 2);
        }

        return output;
    }

    public static Matrix<float> ToVoiced(
        Matrix<Complex32> fourierCoeffs,
        PitchDetection pitchDetection,
        int batchSize,
        int nBatches)
    {
        var markers = pitchDetection.PitchMarkers(null);
        var validity = pitchDetection.Validity(null);
        var centers = new double[markers.Count - 1];
        for (var i = 0; i < markers.Count - 1; i++)
            centers[i] = (double)(markers[i + 1] + markers[i]) / 2;
        var sections = markers.Count - 1;
        var output = Matrix<float>.Build.Dense(nBatches, fourierCoeffs.ColumnCount * 2, 0);
        var currentCenter = 0;
        for (var i = 0; i < nBatches; i++)
        {
            var pos = i * batchSize;
            var nCenters = 0;
            var buffer = Vector<Complex32>.Build.Dense(fourierCoeffs.ColumnCount, 0);
            while (centers[currentCenter] < pos)
            {
                if (validity[currentCenter])
                {
                    buffer += fourierCoeffs.Row(currentCenter);
                    nCenters++;
                }

                currentCenter++;
            }

            if (nCenters > 0) buffer /= nCenters;
            output.SetRow(i,
                Vector<float>.Build.Dense(fourierCoeffs.ColumnCount * 2,
                    j => j >= fourierCoeffs.ColumnCount
                        ? buffer[j - fourierCoeffs.ColumnCount].Phase
                        : buffer[j].Magnitude));
        }

        return output;
    }
}