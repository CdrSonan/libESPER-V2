using libESPER_V2.Core;
using libESPER_V2.Transforms.Internal;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms
{
    public class EsperForwardConfig
    {
        public float PitchOscillatorDamping;
        public int PitchDistanceLimit;
        public float? ExpectedPitch;

        public EsperForwardConfig(float? pitchOscillatorDamping, int? pitchDistanceLimit, float? expectedPitch)
        {
            this.PitchOscillatorDamping = pitchOscillatorDamping == null ? 0.0f : pitchOscillatorDamping.Value;
            this.PitchDistanceLimit = pitchDistanceLimit == null ? 0 : pitchDistanceLimit.Value;
            this.ExpectedPitch = expectedPitch;
        }
    }
    public class EsperTransforms
    {
        public static EsperAudio Forward(Vector<float> x, EsperAudioConfig config, EsperForwardConfig forwardConfig)
        {
            int batchSize = (config.NUnvoiced - 1) * 2 / 3;
            int batches = (int)Math.Ceiling((double)(x.Count / batchSize));
            EsperAudio output = new EsperAudio(batches, config);
            
            PitchDetection pitchDetection = new PitchDetection(x, config, forwardConfig.PitchOscillatorDamping, forwardConfig.PitchDistanceLimit);
            List<int> markers = pitchDetection.PitchMarkers(forwardConfig.ExpectedPitch);
            Vector<float> deltas = pitchDetection.PitchDeltas(forwardConfig.ExpectedPitch);
            output.SetPitch(deltas);
            
            
            
            return output;
        }

        public static EsperAudio ForwardApprox(Vector<float> x, EsperAudioConfig config)
        {
            int length = x.Count;
            EsperAudio output = new EsperAudio(length, config);
            return output;
        }

        public static Vector<float> Inverse(EsperAudio x)
        {
            int length = x.Length;
            Vector<float> output = Vector<float>.Build.Dense(length, 0);
            return output;
        }
    }
}
