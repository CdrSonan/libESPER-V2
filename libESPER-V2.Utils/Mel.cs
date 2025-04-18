using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils
{
    internal class Mel
    {
        public static float HzToMel(float hz)
        {
            return 2595f * (float)Math.Log10(1 + hz / 700f);
        }
        public static float MelToHz(float mel)
        {
            return 700f * ((float)Math.Pow(10, mel / 2595f) - 1);
        }
        public static Vector<float> MelFwd(Vector<float> x, int numMelBands, float minFreq, float maxFreq)
        {
            int length = x.Count;
            Vector<float> mel = Vector<float>.Build.Dense(numMelBands, 0);
            float minMel = HzToMel(minFreq);
            float maxMel = HzToMel(maxFreq);
            float melStep = (maxMel - minMel) / (numMelBands + 1);
            for (int i = 0; i < length; i++)
            {
                float freq = i * (maxFreq / length);
                float melFreq = HzToMel(freq);
                int melIndex = (int)((melFreq - minMel) / melStep);
                if (melIndex >= 0 && melIndex < numMelBands)
                {
                    mel[melIndex] += x[i];
                }
            }
            return mel;
        }
        public static Vector<float> MelInv(Vector<float> mel, int length, float minFreq, float maxFreq)
        {
            Vector<float> x = Vector<float>.Build.Dense(length, 0);
            int numMelBands = mel.Count;
            float minMel = HzToMel(minFreq);
            float maxMel = HzToMel(maxFreq);
            float melStep = (maxMel - minMel) / (mel.Count + 1);
            for (int i = 0; i < numMelBands; i++)
            {
                float melFreqLower = minMel + i * melStep;
                float melFreqUpper = minMel + (i + 1) * melStep;
                float freqLower = MelToHz(melFreqLower);
                float freqUpper = MelToHz(melFreqUpper);
                int lowerIndex = (int)(freqLower * length / maxFreq);
                int upperIndex = (int)(freqUpper * length / maxFreq);
                for (int j = lowerIndex; j < upperIndex; j++)
                {
                    float freq = j * (maxFreq / length);
                    float melFreq = HzToMel(freq);
                    float melWeight = 1 - Math.Abs((melFreq - (melFreqLower + melFreqUpper / 2)) / (melFreqUpper - melFreqLower));
                    x[j] += mel[i] * melWeight;
                }
            }
            return x;
        }
    }
}
