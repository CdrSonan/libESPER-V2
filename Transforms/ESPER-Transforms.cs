using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms;

public class EsperForwardConfig(float? pitchOscillatorDamping, int? pitchDistanceLimit, float? expectedPitch)
{
    public readonly int PitchDistanceLimit = pitchDistanceLimit ?? 0;
    public readonly float PitchOscillatorDamping = pitchOscillatorDamping ?? 0.0f;
    public float? ExpectedPitch = expectedPitch;
}

public class EsperTransforms
{
    public static EsperAudio Forward(Vector<float> x, EsperAudioConfig config, EsperForwardConfig forwardConfig)
    {
        var batchSize = (config.NUnvoiced - 1) * 2 / 3;
        var batches = (int)Math.Ceiling((float)x.Count / batchSize);
        var output = new EsperAudio(batches, config);

        var pitchDetection = new PitchDetection(x, config, forwardConfig.PitchOscillatorDamping,
            forwardConfig.PitchDistanceLimit);
        var markers = pitchDetection.PitchMarkers(forwardConfig.ExpectedPitch);
        var deltas = pitchDetection.PitchDeltas(forwardConfig.ExpectedPitch);
        output.SetPitch(deltas);


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