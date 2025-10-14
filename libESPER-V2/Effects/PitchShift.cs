using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;
using libESPER_V2.Utils;
using MathNet.Numerics.Interpolation;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    private static void DoPitchShift(EsperAudio audio, Vector<float> newPitch)
    {
        var oldPitch = audio.GetPitch();
        var voiced = audio.GetVoicedAmps();
        voiced = voiced.PointwiseMaximum(0);
        var oldVolumes = voiced.RowNorms(2.0);
        var unvoiced = audio.GetUnvoiced() / (audio.Config.NUnvoiced * 2 - 2);
        
        var unvoicedBatchSize = audio.Config.NUnvoiced * 2 - 2;
        var invOldPitch = unvoicedBatchSize / oldPitch;
        var invNewPitch = unvoicedBatchSize / newPitch;
        var reprSwitch = audio.Config.NVoiced * invOldPitch;
        reprSwitch = reprSwitch.PointwiseMinimum(audio.Config.NUnvoiced);
        var srcSize = audio.Config.NVoiced + audio.Config.NUnvoiced - reprSwitch;
        srcSize = srcSize.PointwiseMaximum(audio.Config.NVoiced);
        for (var i = 0; i < audio.Length; ++i)
        {
            if (oldPitch[i] == 0 || newPitch[i] == 0)
            {
                audio.SetVoicedAmps(i, Vector<float>.Build.Dense(audio.Config.NVoiced, 0));
                continue;
            }
            var srcSpace = Vector<double>.Build.Dense((int)srcSize[i]);
            var srcVals = Vector<double>.Build.Dense((int)srcSize[i]);
            var tgtSpace = Vector<float>.Build.Dense(audio.Config.NVoiced, j => j * invNewPitch[i]);
            var voicedCoords = Vector<double>.Build.Dense(audio.Config.NVoiced,
                j => j * invOldPitch[i] > audio.Config.NUnvoiced ? audio.Config.NUnvoiced + (float)(j + 1) / (audio.Config.NVoiced + 1) : j * invOldPitch[i]);
            srcSpace.SetSubVector(0, audio.Config.NVoiced, voicedCoords);
            var unvoicedCoords = Vector<double>.Build.Dense((int)srcSize[i] - audio.Config.NVoiced, j => reprSwitch[i] + j);
            srcSpace.SetSubVector(audio.Config.NVoiced, (int)srcSize[i] - audio.Config.NVoiced, unvoicedCoords);
            srcVals.SetSubVector(0, audio.Config.NVoiced, voiced.Row(i).ToDouble());
            srcVals.SetSubVector(audio.Config.NVoiced, (int)srcSize[i] - audio.Config.NVoiced,
                unvoiced.Row(i).SubVector((int)reprSwitch[i], audio.Config.NUnvoiced - (int)reprSwitch[i]).ToDouble());
            var interpolator = CubicSpline.InterpolatePchipSorted(srcSpace.ToArray(), srcVals.ToArray());

            var result = Vector<float>.Build.Dense(audio.Config.NVoiced, j => (float)interpolator.Interpolate(tgtSpace[j]));
            var newVolume = result.L2Norm() + 1e-6;
            audio.SetVoicedAmps(i, result * (float)(oldVolumes[i] / newVolume));
        }
    }
    public static void PitchShift(EsperAudio audio, Vector<float> pitch)
    {
        if (pitch.Count != audio.Length)
            throw new ArgumentException("Pitch vector length must match audio length.", nameof(pitch));
        if (pitch.Any(p => p < 0))
            throw new ArgumentException("Pitch vector must not contain negative elements.", nameof(pitch));
        DoPitchShift(audio, pitch);
        audio.SetPitch(pitch);
    }
}