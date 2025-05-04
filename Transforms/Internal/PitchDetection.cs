using MathNet.Numerics.LinearAlgebra;
using libESPER_V2.Utils;
using libESPER_V2.Core;

namespace libESPER_V2.Transforms.Internal
{
    class PitchDetection(Vector<float> audio, float oscillatorDamping, int distanceLimit)
    {
        private Vector<float>? _oscillatorProxy;
        private readonly Dag _graph = new();
        private List<int>? _pitchMarkers;
        private List<bool>? _pitchMarkerValidity;

        private void DrivenOscillator()
        {
            _oscillatorProxy = Vector<float>.Build.Dense(audio.Count, 0);
            float a;
            float v = 0;
            float x = 0;
            for (int i = 1; i < audio.Count; i++)
            {
                a = audio[i] - oscillatorDamping * v - oscillatorDamping * x;
                v += a;
                x += v;
                _oscillatorProxy[i] = x;
            }
        }
        private void BuildPitchGraph(int edgeThreshold)
        {
            for (int i = 1; i < _oscillatorProxy.Count; i++)
            {
                if (_oscillatorProxy[i-1] < 0 && _oscillatorProxy[i] >= 0)
                {
                    bool root = (i < edgeThreshold) || (_graph.Nodes.Count == 0);
                    bool leaf = (i >= _oscillatorProxy.Count - edgeThreshold);
                    Node node = new(i, root, leaf);
                    _graph.AddNode(node);
                }
            }
            if (_graph.Nodes.Count > 0)
            {
                _graph.Nodes.Last().IsLeaf = true;
            }
        }
        private void FillPitchGraph(float? expectedPitch, int? edgeThreshold)
        {
            for (int i = 1; i < _graph.Nodes.Count; i++)
            {
                if (_graph.Nodes[i].IsRoot)
                {
                    continue;
                }
                _graph.Nodes[i].Value = PitchNodeDistance(_graph.Nodes[i].Id, _graph.Nodes[i - 1].Id, expectedPitch, 50, edgeThreshold);
                _graph.Nodes[i].Parent = _graph.Nodes[i - 1];
                int limit = Math.Max(0, i - distanceLimit);
                for (int j = i - 2; j >= limit; j--)
                {
                    double distance = PitchNodeDistance(_graph.Nodes[i].Id, _graph.Nodes[j].Id, expectedPitch, 50, edgeThreshold);
                    if (_graph.Nodes[j].Value + distance < _graph.Nodes[i].Value)
                    {
                        _graph.Nodes[i].Value = _graph.Nodes[j].Value + distance;
                        _graph.Nodes[i].Parent = _graph.Nodes[j];
                    }
                }
            }
        }
        private double PitchNodeDistance(int id1, int id2, float? expectedPitch, long? lowerLimit, long? upperLimit)
        {
            int delta = id2 - id1;
            //TODO: Assertions based on limit values
            if (delta < 0)
            {
                throw new ArgumentException("id2 must be greater than id1");
            }

            if (delta < lowerLimit || delta > upperLimit)
            {
                return Double.PositiveInfinity;
            }
            double bias;
            if (expectedPitch == null)
            {
                bias = 1;
            }
            else
            {
                bias = Math.Abs((double)(delta - expectedPitch));
            }
            double error = 0;
            double contrast = 0;
            int start1, start2;
            if (id1 < delta)
            {
                start1 = id1;
                start2 = id2;
            }
            else if (id2 >= _oscillatorProxy.Count - delta)
            {
                start1 = id1 - delta;
                start2 = id2 - delta;
            }
            else
            {
                start1 = id1 - delta / 2;
                start2 = id2 - delta / 2;
            }
            for (int i = 0; i < delta; i++)
            {
                error += Math.Pow(_oscillatorProxy[start1 + i] - _oscillatorProxy[start2 + i], 2) * bias;
                contrast += _oscillatorProxy[start1 + i] * Math.Sin(2 * Math.PI * ((double)i / delta));
            }
            return error / Math.Pow(contrast, 2);
        }
        private void CheckValidity()
        {
            _pitchMarkerValidity = new List<bool>(_graph.Nodes.Count - 1);
            _pitchMarkerValidity[0] = true;
            _pitchMarkerValidity[^2] = true;
            for (int i = 1; i < _graph.Nodes.Count - 2; i++)
            {
                int sectionSize = _graph.Nodes[i + 1].Id - _graph.Nodes[i].Id;
                int previousSize = _graph.Nodes[i].Id - _graph.Nodes[i - 1].Id;
                int nextSize = _graph.Nodes[i + 2].Id - _graph.Nodes[i + 1].Id;
                if (previousSize <= sectionSize + 2 || nextSize <= sectionSize + 2)
                {
                    _pitchMarkerValidity[i] = true;
                }
                float validError = 0;
                Vector<float> scale = Vector<float>.Build.Dense(sectionSize);
                for (int j = 0; j < sectionSize; j++)
                {
                    scale[j] = j;
                }
                Vector<float> previousScale = Vector<float>.Build.Dense(previousSize);
                for (int j = 0; j < previousSize; j++)
                {
                    previousScale[j] = j * ((float)sectionSize / previousSize);
                }
                Vector<float> nextScale = Vector<float>.Build.Dense(nextSize);
                for (int j = 0; j < nextSize; j++)
                {
                    nextScale[j] = j * ((float)sectionSize / nextSize);
                }
                Vector<float> section = _oscillatorProxy.SubVector(_graph.Nodes[i].Id, sectionSize);
                Vector<float> previousSection = _oscillatorProxy.SubVector(_graph.Nodes[i - 1].Id, previousSize);
                Vector<float> nextSection = _oscillatorProxy.SubVector(_graph.Nodes[i + 1].Id, nextSize);
                Vector<float> previousInterpolated = Interpolation.Interpolate(previousScale, previousSection, scale);
                Vector<float> nextInterpolated = Interpolation.Interpolate(nextScale, nextSection, scale);
                for (int j = 0; j < sectionSize; j++)
                {
                    validError += (float)Math.Pow(section[j] - (previousInterpolated[j] + nextInterpolated[j]) / 2, 2);
                }
                float invalidError = 0;
                for (int j = 0; j < sectionSize; j++)
                {
                    float alternative = _oscillatorProxy[_graph.Nodes[i - 1].Id + j] - _oscillatorProxy[_graph.Nodes[i + 2].Id - sectionSize + j];
                    invalidError += (float)Math.Pow(alternative / 2, 2);
                }
                if (validError < invalidError)
                {
                    _pitchMarkerValidity[i] = true;
                }
                else
                {
                    _pitchMarkerValidity[i] = false;
                }
            }
        }
        public List<int> PitchMarkers(EsperAudioConfig config, float? expectedPitch)
        {
            if (_pitchMarkers == null)
            {
                int edgeThreshold = (config.NUnvoiced - 1) * 2 / 3;
                DrivenOscillator();
                BuildPitchGraph(edgeThreshold);
                FillPitchGraph(expectedPitch, edgeThreshold);
                CheckValidity();
                _pitchMarkers = _graph.Trace();
            }
            return _pitchMarkers;
        }
        private int GetValidPitchDelta(int index)
        {
            if (_pitchMarkerValidity[index])
            {
                return _pitchMarkers[index + 1] - _pitchMarkers[index];
            }
            int previousDelta = _pitchMarkers[index] - _pitchMarkers[index - 1];
            int nextDelta = _pitchMarkers[index + 2] - _pitchMarkers[index + 1];
            return (previousDelta + nextDelta) / 2;
        }
        public Vector<float> PitchDeltas(EsperAudioConfig config, float? expectedPitch)
        {
            _pitchMarkers = PitchMarkers(config, expectedPitch);
            int cursor = 0;
            int batchSize = (config.NUnvoiced - 1) * 2 / 3;
            int batches = (int)Math.Ceiling((double)(_oscillatorProxy.Count / batchSize));
            Vector<float> pitchDeltas = Vector<float>.Build.Dense(batches);
            for (int i = 0; i < batches; i++)
            {
                while (_pitchMarkers[cursor] <= i * batchSize && cursor < _pitchMarkers.Count)
                {
                    cursor++;
                }
                if (cursor == 0)
                {
                    pitchDeltas[i] = _pitchMarkers[cursor + 1] - _pitchMarkers[cursor];
                }
                else if (cursor == _pitchMarkers.Count - 1)
                {
                    pitchDeltas[i] = _pitchMarkers[cursor] - _pitchMarkers[cursor - 1];
                }
                else
                {
                    int delta = GetValidPitchDelta(cursor - 1);
                    if (delta > batchSize)
                    {
                        pitchDeltas[i] = delta;
                    }
                    else
                    {
                        pitchDeltas[i] = batchSize;
                    }
                }
            }
            return pitchDeltas;
        }
    }
}
