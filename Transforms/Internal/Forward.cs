using libESPER_V2.Transforms.Internal;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Interpolation;

namespace libESPER_V2.Transforms.Internal
{
    class PitchSynchronousResample(Vector<float> waveform, PitchDetection pitchDetection)
    {
        private Vector<float>? ResampledWaveform;

        private Vector<float> GetEvaluationPointSection(PitchDetection pitchDetection, int start, int end)
        {
            List<int> markers = pitchDetection.PitchMarkers(null).Slice(start, end - start);
            Vector<double> x = Vector<double>.Build.Dense(end - start, (i) => markers[i]);
            Vector<double> y = Vector<double>.Build.Dense(end - start, (i) => i);
            int windowLength = markers[end] - markers[start];
            Vector<double> xs = Vector<double>.Build.Dense(windowLength, (i) => i);
            CubicSpline interpolator = CubicSpline.InterpolatePchip(x, y);
            return Vector<float>.Build.Dense(windowLength, (i) => (float)interpolator.Interpolate(xs[i]));
        }

        private Vector<float> GetEvaluationPoints(PitchDetection pitchDetection)
        {
            const int maxSectionSize = 8;
            List<int> markers = pitchDetection.PitchMarkers(null);
            int numMarkers = markers.Count;
            if (numMarkers <= maxSectionSize)
            {
                return GetEvaluationPointSection(pitchDetection, 0, numMarkers);
            }
            Vector<float> result = Vector<float>.Build.Dense();
        }
        
        public Vector<float> Resample(Vector<float> waveform, PitchDetection pitchDetection)
        {
            if (ResampledWaveform != null)
            {
                return ResampledWaveform;
            }
            ResampledWaveform = ... //TODO
            return waveform;
        }
    }
}
