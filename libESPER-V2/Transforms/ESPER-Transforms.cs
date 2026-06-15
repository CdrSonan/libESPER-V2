using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms;

public class EsperForwardConfig
{
    public float? PitchOscillatorDamping { get; init; } = 0.1f;
    public Vector<float>? ExpectedPitch { get; init; }
    public double ProcessNoiseVariance { get; init; } = 0.01;
    public double MeasurementNoiseVariance { get; init; } = 0.1;
    public double RobustThreshold { get; init; } = 2.5;
    public double ScaleForgettingFactor { get; init; } = 0.05;
    public double InitialVarianceMultiplier { get; init; } = 1.0;
    public double InitialObsStdMultiplier { get; init; } = 0.5;
}

public static class EsperTransforms
{
    public static EsperAudio Forward(Vector<float> x, EsperAudioConfig config, EsperForwardConfig forwardConfig)
    {
        var batches = x.Count / config.StepSize;
        var output = new EsperAudio(batches, config);
        var pitchDetection = new PitchDetection(x, config, forwardConfig.PitchOscillatorDamping);
        var deltas = pitchDetection.PitchDeltas(forwardConfig.ExpectedPitch);
        output.SetPitch(deltas);

        var pitchSyncWave = PitchSyncWave.ConvertTo(x, pitchDetection, (config.NVoiced - 1) * 2);
        var coeffs = PitchSyncFourierCoeffs.ConvertTo(pitchSyncWave);
        var smoothedCoeffs = PitchSyncFourierCoeffs.SmoothCoeffs(
            coeffs,
            forwardConfig.ProcessNoiseVariance,
            forwardConfig.MeasurementNoiseVariance,
            forwardConfig.RobustThreshold,
            forwardConfig.ScaleForgettingFactor,
            forwardConfig.InitialVarianceMultiplier,
            forwardConfig.InitialObsStdMultiplier
            );
        var (voicedAmps, voicedPhases) = VoicedAnalysis.MakeVoicedPart(smoothedCoeffs, pitchDetection, config.StepSize, batches);
        output.SetVoicedAmps(voicedAmps);
        output.SetVoicedPhases(voicedPhases);
        var unvoiced = UnvoicedAnalysis.MakeUnvoicedPart(smoothedCoeffs, x, pitchDetection, config.StepSize, config.NUnvoiced);
        output.SetUnvoiced(unvoiced);
        output.Validate();
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