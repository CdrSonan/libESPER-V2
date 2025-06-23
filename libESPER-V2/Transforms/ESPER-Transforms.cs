using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms;

public class EsperForwardConfig(float? pitchOscillatorDamping, int? pitchDistanceLimit, float? expectedPitch)
{
    public readonly int PitchDistanceLimit = pitchDistanceLimit ?? 10;
    public readonly float PitchOscillatorDamping = pitchOscillatorDamping ?? 0.1f;
    public float? ExpectedPitch = expectedPitch;
}

public class EsperTransforms
{
    public static EsperAudio Forward(Vector<float> x, EsperAudioConfig config, EsperForwardConfig forwardConfig)
    {
        var batches = (int)(x.Count / config.StepSize);
        var output = new EsperAudio(batches, config);
        var pitchDetection = new PitchDetection(x, config, forwardConfig.PitchOscillatorDamping,
            forwardConfig.PitchDistanceLimit);
        var deltas = pitchDetection.PitchDeltas(forwardConfig.ExpectedPitch);
        output.SetPitch(deltas);

        var pitchSync = PitchSync.ToPitchSync(x, pitchDetection, (config.NVoiced - 1) * 2);
        var coeffs = Resolve.ToFourier(pitchSync);
        var smoothed = Resolve.Smoothing(coeffs);
        var voiced = Resolve.ToVoiced(smoothed, pitchDetection, config.StepSize, batches);
        output.SetVoicedAmps(voiced.SubMatrix(0, voiced.RowCount, 0, config.NVoiced));
        output.SetVoicedPhases(voiced.SubMatrix(0, voiced.RowCount, config.NVoiced, config.NVoiced));
        var unvoiced = Resolve.ToUnvoiced(smoothed, x, pitchDetection, config.StepSize, config.NUnvoiced);
        output.SetUnvoiced(unvoiced);
        return output;
    }

    public static EsperAudio ForwardApprox(Vector<float> x, EsperAudioConfig config)
    {
        var length = x.Count;
        var output = new EsperAudio(length, config);
        return output;
    }

    public static Vector<float> Inverse(EsperAudio x)
    {
        var length = x.Length;
        var output = Vector<float>.Build.Dense(length, 0);
        return output;
    }
}