using libESPER_V2.Core;
using libESPER_V2.Utils;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms.Internal;

internal static class InverseResolve
{
    public static Vector<float> HanningWindow(int size)
    {
        var middle = (float)size / 2;
        return Vector<float>.Build.Dense(size, (i) => 
            (float)Math.Pow(Math.Cos(Math.PI * (i - middle) / size), 2) / size);
    }
    public static (Vector<float>, float) ReconstructVoiced(EsperAudio audio, float phase)
    {
        var pitch = audio.GetPitch();
        var amplitudes = audio.GetVoicedAmps();
        var phases = audio.GetVoicedPhases();
        var output = Vector<float>.Build.Dense(audio.Length * audio.Config.StepSize, 0);
        for (var i = 0; i < audio.Config.StepSize / 2; i++)
        {
            var currentPhase = phase;
            var components = 
                phases.Row(0) +
                Vector<float>.Build.Dense(audio.Config.NVoiced, (j) => j * currentPhase);
            components.MapIndexedInplace((j, value) => amplitudes[0, j] * (float)Math.Cos(value));
            output[i] = components.Sum();
            phase += 2 * (float)Math.PI / pitch.First();
            phase %= 2 * (float)Math.PI;
        }
        for (var i = 0; i < audio.Length - 1; i++)
        {
            for (var j = 0; j < audio.Config.StepSize; j++)
            {
                var currentPitch = pitch[i] + (pitch[i + 1] - pitch[i]) * j / audio.Config.StepSize;
                var currentPhases = Phases.Interpolate(
                    phases.Row(i),
                    phases.Row(i + 1),
                    j / (float)audio.Config.StepSize);
                var currentAmplitudes =
                    amplitudes.Row(i) +
                    (amplitudes.Row(i + 1) - amplitudes.Row(i)) * j / audio.Config.StepSize;
                var currentPhase = phase;
                var components =
                    currentPhases +
                    Vector<float>.Build.Dense(audio.Config.NVoiced, (k) => k * currentPhase);
                components.MapIndexedInplace((k, value) => currentAmplitudes[k] * (float)Math.Cos(value));
                output[audio.Config.StepSize / 2 + i * audio.Config.StepSize + j] = components.Sum();
                phase += 2 * (float)Math.PI / currentPitch;
                phase %= 2 * (float)Math.PI;
            }
        }
        for (var i = 0; i < audio.Config.StepSize / 2; i++)
        {
            var currentPhase = phase;
            var components =
                phases.Row(audio.Length - 1) +
                Vector<float>.Build.Dense(audio.Config.NVoiced, (j) => j * currentPhase);
            components.MapIndexedInplace((j, value) => amplitudes[audio.Length - 1, j] * (float)Math.Cos(value));
            output[i] = components.Sum();
            phase += 2 * (float)Math.PI / pitch.Last();
            phase %= 2 * (float)Math.PI;
        }
        return (output, phase);
    }

    public static (Vector<float>, float) ReconstructVoicedFourier(EsperAudio audio, float phase)
    {
        var pitch = audio.GetPitch();
        var amplitudes = audio.GetVoicedAmps();
        var phases = audio.GetVoicedPhases();
        var output = Vector<float>.Build.Dense(audio.Length * audio.Config.StepSize, 0);
        for (var i = 1; i < audio.Length;i++)
        {
            var il = i;
            phases.SetRow(i, phases.Row(i).MapIndexed((j, value) => 
                value + phases[il - 1, j] + j * (float)Math.PI * (pitch[il - 1] + pitch[il]) / audio.Config.StepSize));
        }
        phases.MapInplace((value) => value % 2 * (float)Math.PI);
        phases.MapInplace((value) => value < -Math.PI ? value + (float)Math.PI : value);
        phases.MapInplace((value) => value > Math.PI ? value - (float)Math.PI : value);
        for (var i = 0; i < audio.Length; i++)
        {
            var il = i;
            var rowArr = Vector<float>.Build.Dense(audio.Length * audio.Config.StepSize * 2, 
                (j) => j % 2 == 0 ? 
                    amplitudes[il, j / 2] * (float)Math.Cos(phases[il, j / 2]) : 
                    amplitudes[il, j / 2] * (float)Math.Sin(phases[il, j / 2])).ToArray();
            Fourier.InverseReal(rowArr, audio.Config.NVoiced * 2 - 2);
            //TODO complete implementation
        }
        return (output, phases[audio.Length - 1, 0]);
    }

    public static Vector<float> ReconstructUnvoiced(EsperAudio audio, long seed)
    {
        var unvoiced = audio.GetUnvoiced();
        var coeffs = Matrix<double>.Build.Dense(
            audio.Length,
            2 * audio.Config.NUnvoiced,
            (i, j) => Normal.Sample(0, unvoiced[i, j / 2] / Math.Sqrt(Math.PI / 2)));
        var output = Vector<float>.Build.Dense(audio.Length * audio.Config.StepSize, 0);
        var norm = Vector<float>.Build.Dense(audio.Length * audio.Config.StepSize, 0);
        var offset = audio.Config.StepSize / 2 - audio.Config.NUnvoiced - 1;
        var window = HanningWindow(audio.Config.NUnvoiced * 2 - 2);
        for (var i = 0; i < audio.Length; i++)
        {
            var coeffsArr = coeffs.Row(i).ToArray();
            Fourier.InverseReal(coeffsArr, audio.Config.NUnvoiced * 2 - 2);
            var index = i * audio.Config.StepSize - offset;
            var count = audio.Config.NUnvoiced * 2 - 2;
            var localOffset = 0;
            if (index < 0)
            {
                count += index;
                localOffset -= index;
                index = 0;
            }

            if (index + count > audio.Length * audio.Config.StepSize)
            {
                count = audio.Length * audio.Config.StepSize - index;
            }

            var wave = Vector<float>.Build.Dense(count,
                (j) => window[localOffset + j] * (float)coeffsArr[localOffset + j]);
            var buffer = output.SubVector(index, count);
            output.SetSubVector(index, count, buffer + wave);
            var normBuffer = norm.SubVector(index, count);
            norm.SetSubVector(index, count, normBuffer + window.SubVector(localOffset, count));
        }
        return output / norm;
    }
}