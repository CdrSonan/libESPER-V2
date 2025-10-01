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
        voiced = voiced.PointwiseAbs();
        var oldVolumes = voiced.RowNorms(2.0);
        var unvoiced = audio.GetUnvoiced() / (audio.Config.NUnvoiced * 2 - 2);
        var switchPoints = Vector<float>.Build.Dense(audio.Length, i => audio.Config.NVoiced * oldPitch[i] / newPitch[i]);
        for (var i = 0; i < audio.Length; ++i)
        {
            if (oldPitch[i] == 0 || newPitch[i] == 0)
            {
                audio.SetVoicedAmps(i, Vector<float>.Build.Dense(audio.Config.NVoiced, 0));
                continue;
            }
            var switchPoint = switchPoints[i] > audio.Config.NVoiced ? audio.Config.NVoiced : (int)switchPoints[i];
            var pitchFactor = oldPitch[i] / newPitch[i];
            var interpolator = CubicSpline.InterpolatePchip(Vector<double>.Build.Dense(audio.Config.NVoiced, j => j),
                voiced.Row(i).ToDouble());
            var fromVoiced = Vector<float>.Build.Dense(switchPoint,
                j => (float)interpolator.Interpolate(j * pitchFactor));
            var invNewPitch = (audio.Config.NUnvoiced * 2 - 2) / newPitch[i];
            var fromUnvoicedScale = Vector<float>.Build.Dense(audio.Config.NVoiced - switchPoint,
                (j) => (j + switchPoint) * invNewPitch);
            interpolator = CubicSpline.InterpolatePchip(Vector<double>.Build.Dense(audio.Config.NUnvoiced, j => j),
                unvoiced.Row(i).ToDouble());
            var fromUnvoiced = Vector<float>.Build.Dense(audio.Config.NVoiced - switchPoint,
                j => (float)interpolator.Interpolate((j + switchPoint) * invNewPitch));
            var result = Vector<float>.Build.DenseOfEnumerable(fromVoiced.Concat(fromUnvoiced));
            var newVolume = result.L2Norm();
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