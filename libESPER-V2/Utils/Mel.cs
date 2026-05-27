using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils;

internal static class Mel
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
        var weightSums = Vector<float>.Build.Dense(numMelBands, 0);
        var maxMel = HzToMel(maxFreq);
        
        // Compute Mel band edges: numMelBands + 2 edges for numMelBands triangular filters
        var melEdges = new float[numMelBands + 2];
        var hzEdges = new float[numMelBands + 2];
        var melStep = maxMel / (numMelBands + 1);
        
        for (var i = 0; i < melEdges.Length; i++)
        {
            melEdges[i] = i * melStep;
            hzEdges[i] = MelToHz(melEdges[i]);
        }
        
        // Apply triangular Mel filterbank and track weight sums for normalization
        for (var i = 0; i < length; i++)
        {
            var freq = i * (maxFreq / length);
            
            // Find which Mel bands this frequency bin overlaps with
            for (var m = 0; m < numMelBands; m++)
            {
                var leftEdge = hzEdges[m];
                var centerEdge = hzEdges[m + 1];
                var rightEdge = hzEdges[m + 2];
                
                float weight = 0;
                
                if (freq >= leftEdge && freq < centerEdge)
                {
                    // Left slope of triangle
                    if (centerEdge > leftEdge)
                        weight = (freq - leftEdge) / (centerEdge - leftEdge);
                }
                else if (freq >= centerEdge && freq < rightEdge)
                {
                    // Right slope of triangle
                    if (rightEdge > centerEdge)
                        weight = (rightEdge - freq) / (rightEdge - centerEdge);
                }
                
                if (weight > 0)
                {
                    mel[m] += x[i] * weight;
                    weightSums[m] += weight;
                }
            }
        }
        
        // Normalize by weight sums and scale appropriately
        var scaleFactor = (float)length / numMelBands;
        for (var m = 0; m < numMelBands; m++)
        {
            if (weightSums[m] > 0)
            {
                mel[m] /= weightSums[m];
                mel[m] *= scaleFactor;
            }
        }
        
        return mel;
    }

    public static Vector<float> MelInv(Vector<float> mel, int length, float maxFreq)
    {
        var x = Vector<float>.Build.Dense(length, 0);
        var numMelBands = mel.Count;
        var maxMel = HzToMel(maxFreq);
        var scaleFactor = (float)length / numMelBands;
        
        // Compute Mel band edges: numMelBands + 2 edges for numMelBands triangular filters
        var melEdges = new float[numMelBands + 2];
        var hzEdges = new float[numMelBands + 2];
        var melStep = maxMel / (numMelBands + 1);
        
        for (var i = 0; i < melEdges.Length; i++)
        {
            melEdges[i] = i * melStep;
            hzEdges[i] = MelToHz(melEdges[i]);
        }
        
        // Track weight sums for each frequency bin for normalization
        var weightSums = Vector<float>.Build.Dense(length, 0);
        
        // Distribute energy from Mel bands back to frequency domain using triangular filters
        for (var m = 0; m < numMelBands; m++)
        {
            var leftEdge = hzEdges[m];
            var centerEdge = hzEdges[m + 1];
            var rightEdge = hzEdges[m + 2];
            
            // Scale mel value using inverse of the forward scaling
            var melValueScaled = mel[m] / scaleFactor;
            
            for (var i = 0; i < length; i++)
            {
                var freq = i * (maxFreq / length);
                
                float weight = 0;
                
                if (freq >= leftEdge && freq < centerEdge)
                {
                    // Left slope of triangle
                    if (centerEdge > leftEdge)
                        weight = (freq - leftEdge) / (centerEdge - leftEdge);
                }
                else if (freq >= centerEdge && freq < rightEdge)
                {
                    // Right slope of triangle
                    if (rightEdge > centerEdge)
                        weight = (rightEdge - freq) / (rightEdge - centerEdge);
                }
                
                if (weight > 0)
                {
                    x[i] += melValueScaled * weight;
                    weightSums[i] += weight;
                }
            }
        }
        
        // Normalize by weight sums to account for overlapping triangles
        for (var i = 0; i < length; i++)
        {
            if (weightSums[i] > 0)
                x[i] /= weightSums[i];
        }
        
        return x;
    }
}