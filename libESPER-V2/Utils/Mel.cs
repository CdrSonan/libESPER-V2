using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils;

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

    public static Vector<float> MelFwd(Vector<float> x, int numMelBands, float maxFreq)
    {
        var length = x.Count;
        var mel = Vector<float>.Build.Dense(numMelBands, 0);
        var multipliers = Vector<float>.Build.Dense(numMelBands, 0);
        var maxMel = HzToMel(maxFreq);
        var melStep = maxMel / (numMelBands + 1);
        for (var i = 0; i < length; i++)
        {
            var freq = i * (maxFreq / length);
            var melFreq = HzToMel(freq);
            var melIndex = (int)(melFreq / melStep);
            if (melIndex >= 0 && melIndex < numMelBands)
            {
                mel[melIndex] += x[i];
                multipliers[melIndex]++;
            }
        }

        for (var i = 0; i < mel.Count; i++)
        {
            if (multipliers[i] > 1)
                mel[i] /= multipliers[i];
            mel[i] *= (float)length / numMelBands;
        }

        return mel;
    }

    public static Vector<float> MelInv(Vector<float> mel, int length, float maxFreq)
    {
        var x = Vector<float>.Build.Dense(length, 0);
        var numMelBands = mel.Count;
        var maxMel = HzToMel(maxFreq);
        var melStep = maxMel / numMelBands;
        for (var i = 0; i < numMelBands; i++)
        {
            var melFreqLower = i * melStep;
            var melFreqUpper = (i + 1) * melStep;
            var freqLower = MelToHz(melFreqLower);
            var freqUpper = MelToHz(melFreqUpper);
            var lowerIndex = (int)(freqLower * length / maxFreq);
            var upperIndex = (int)(freqUpper * length / maxFreq);
            for (var j = lowerIndex; j < upperIndex; j++)
            {
                var freq = j * (maxFreq / length);
                var melFreq = HzToMel(freq);
                x[j] = mel[i] * numMelBands / length;
            }

            x[^1] = mel[^1] * numMelBands / length;
        }

        return x;
    }
}