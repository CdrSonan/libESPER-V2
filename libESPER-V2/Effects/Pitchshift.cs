using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;
using libESPER_V2.Utils;

namespace libESPER_V2.Effects;

public static class Pitchshift
{
    public static void PitchShift(EsperAudio audio, Vector<float> pitch)
    {
        if (pitch.Count != audio.Length)
            throw new ArgumentException("Pitch vector length must match audio length.", nameof(pitch));
        if (pitch.Any(p => p < 0))
            throw new ArgumentException("Pitch vector must not contain negative elements.", nameof(pitch));
        var oldPitch = audio.GetPitch();
        var newPitch = pitch;
        var voiced = audio.GetVoicedAmps();
        var unvoiced = audio.GetUnvoiced();
        var switchPoints = Enumerable.Range(0, audio.Length)
                .Select(i => audio.Config.NVoiced * (int)(oldPitch[i] / newPitch[i]))
                .ToArray();
        for (var i = 0; i < audio.Length; ++i)
        {
            var switchPoint = switchPoints[i] > audio.Config.NVoiced ? audio.Config.NVoiced : switchPoints[i];
            var pitchFactor = oldPitch[i] / newPitch[i];
            var fromVoicedScale = Vector<float>.Build.Dense(switchPoint, (j) => j * pitchFactor);
            var fromVoiced = WhittakerShannon.Interpolate(voiced.Row(i), fromVoicedScale);
            var invNewPitch = (audio.Config.NUnvoiced * 2 - 2) / newPitch[i];
            var fromUnvoicedScale = Vector<float>.Build.Dense(audio.Config.NVoiced - switchPoint,
                (j) => (j + switchPoint) * invNewPitch);
            var fromUnvoiced = WhittakerShannon.Interpolate(unvoiced.Row(i), fromUnvoicedScale);
            var result = Vector<float>.Build.DenseOfEnumerable(fromVoiced.Concat(fromUnvoiced));
            audio.SetVoicedAmps(i, result);
        }
        audio.SetPitch(newPitch);
    }
}