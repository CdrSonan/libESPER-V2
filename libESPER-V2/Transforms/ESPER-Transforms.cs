using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms;

public class EsperForwardConfig(float? pitchOscillatorDamping, int? pitchDistanceLimit, float? expectedPitch)
{
    public readonly int PitchDistanceLimit = pitchDistanceLimit ?? 10;
    public readonly float? PitchOscillatorDamping = pitchOscillatorDamping;
    public float? ExpectedPitch = expectedPitch;
}

public static class EsperTransforms
{
    public static EsperAudio Forward(Vector<float> x, EsperAudioConfig config, EsperForwardConfig forwardConfig)
    {
        var batches = x.Count / config.StepSize;
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
        throw new NotImplementedException();
        var length = x.Count;
        var output = new EsperAudio(length, config);
        return output;
    }

    public static (Vector<float>, float) Inverse(EsperAudio x, float phase = 0)
    {
        var (voiced, newPhase) = InverseResolve.ReconstructVoiced(x, phase);
        var unvoiced = InverseResolve.ReconstructUnvoiced(x, 727);
        return (voiced + unvoiced, newPhase);
    }
    
    public static (Vector<float>, float) InverseApprox(EsperAudio x, float phase = 0)
    {
        var (voiced, newPhase) = InverseResolve.ReconstructVoicedFourier(x, phase);
        var unvoiced = InverseResolve.ReconstructUnvoiced(x, 727);
        return (voiced + unvoiced, newPhase);
    }
}