using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms
{
    class EsperTransforms
    {
        public static EsperAudio Forward(Vector<float> x, EsperAudioConfig config)
        {
            int length = x.Count;
            EsperAudio output = new EsperAudio(length, config);
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
