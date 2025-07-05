using libESPER_V2.Core;
using libESPER_V2.Utils;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms.Internal;

internal class PitchDetection(Vector<float> audio, EsperAudioConfig config, float oscillatorDamping, int distanceLimit)
{
    private readonly Dag _graph = new();
    private Vector<float>? _oscillatorProxy;
    private List<int>? _pitchMarkers;
    private bool[]? _pitchMarkerValidity;


    private void DrivenOscillator()
    {
        _oscillatorProxy = Vector<float>.Build.Dense(audio.Count, 0);
        float a;
        float v = 0;
        float x = 0;
        for (var i = 1; i < audio.Count; i++)
        {
            a = audio[i] - oscillatorDamping * v - oscillatorDamping * x;
            v += a;
            x += v;
            _oscillatorProxy[i] = x;
        }
    }

    private void BuildPitchGraph(int edgeThreshold)
    {
        if (_oscillatorProxy == null) throw new InvalidOperationException("Oscillator proxy is not initialized.");
        for (var i = 1; i < _oscillatorProxy.Count; i++)
            if (_oscillatorProxy[i - 1] < 0 && _oscillatorProxy[i] >= 0)
            {
                var root = i < edgeThreshold || _graph.Nodes.Count == 0;
                var leaf = i >= _oscillatorProxy.Count - edgeThreshold;
                Node node = new(i, root, leaf);
                _graph.AddNode(node);
            }

        if (_graph.Nodes.Count > 0) _graph.Nodes.Last().IsLeaf = true;
    }

    private void FillPitchGraph(float? expectedPitch, int? edgeThreshold)
    {
        for (var i = 1; i < _graph.Nodes.Count; i++)
        {
            if (_graph.Nodes[i].IsRoot) continue;
            _graph.Nodes[i].Value = PitchNodeDistance(_graph.Nodes[i - 1].Id, _graph.Nodes[i].Id, expectedPitch, 50,
                edgeThreshold);
            _graph.Nodes[i].Parent = _graph.Nodes[i - 1];
            var limit = Math.Max(0, i - distanceLimit);
            for (var j = i - 2; j >= limit; j--)
            {
                var distance = PitchNodeDistance(_graph.Nodes[j].Id, _graph.Nodes[i].Id, expectedPitch, 50,
                    edgeThreshold);
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
        var delta = id2 - id1;
        if (_oscillatorProxy == null) throw new InvalidOperationException("Oscillator proxy is not initialized.");
        if (delta < 0) throw new ArgumentException("id2 must be greater than id1");

        if (delta < lowerLimit || delta > upperLimit) return double.PositiveInfinity;
        double bias = expectedPitch == null
                ? 1
                : Math.Abs((double)(delta - expectedPitch.Value));
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

        for (var i = 0; i < delta; i++)
        {
            error += Math.Pow(_oscillatorProxy[start1 + i] - _oscillatorProxy[start2 + i], 2) * bias;
            contrast += _oscillatorProxy[start1 + i] * Math.Sin(2 * Math.PI * ((double)i / delta));
        }

        return error / Math.Pow(contrast, 2);
    }

    private void CheckValidity()
    {
        if (_oscillatorProxy == null) throw new InvalidOperationException("Oscillator proxy is not initialized.");
        _pitchMarkerValidity = new bool[_graph.Nodes.Count - 1];
        _pitchMarkerValidity[0] = true;
        _pitchMarkerValidity[^1] = true;
        for (var i = 1; i < _graph.Nodes.Count - 2; i++)
        {
            var sectionSize = _graph.Nodes[i + 1].Id - _graph.Nodes[i].Id;
            var previousSize = _graph.Nodes[i].Id - _graph.Nodes[i - 1].Id;
            var nextSize = _graph.Nodes[i + 2].Id - _graph.Nodes[i + 1].Id;
            if (previousSize <= sectionSize + 2 || nextSize <= sectionSize + 2)
            {
                _pitchMarkerValidity[i] = true;
                continue;
            }

            float validError = 0;
            var previousScale = Vector<double>.Build.Dense(previousSize, j => j * ((float)sectionSize / previousSize));
            var nextScale = Vector<double>.Build.Dense(nextSize, j => j * ((float)sectionSize / nextSize));
            var section = _oscillatorProxy.SubVector(_graph.Nodes[i].Id, sectionSize);
            var previousSection = Vector<double>.Build.Dense(previousSize);
            _oscillatorProxy.SubVector(_graph.Nodes[i - 1].Id, previousSize).MapConvert(x => x, previousSection);
            var nextSection = Vector<double>.Build.Dense(nextSize);
            _oscillatorProxy.SubVector(_graph.Nodes[i + 1].Id, nextSize).MapConvert(x => x, nextSection);
            var previousInterpolator = CubicSpline.InterpolatePchip(previousScale, previousSection);
            var previousInterpolated =
                Vector<float>.Build.Dense(previousSize, j => (float)previousInterpolator.Interpolate(j));
            var nextInterpolator = CubicSpline.InterpolatePchip(nextScale, nextSection);
            var nextInterpolated = Vector<float>.Build.Dense(nextSize, j => (float)nextInterpolator.Interpolate(j));
            for (var j = 0; j < sectionSize; j++)
                validError += (float)Math.Pow(section[j] - (previousInterpolated[j] + nextInterpolated[j]) / 2, 2);
            float invalidError = 0;
            for (var j = 0; j < sectionSize; j++)
            {
                var alternative = _oscillatorProxy[_graph.Nodes[i - 1].Id + j] -
                                  _oscillatorProxy[_graph.Nodes[i + 2].Id - sectionSize + j];
                invalidError += (float)Math.Pow(alternative, 2);
            }

            if (validError < invalidError)
                _pitchMarkerValidity[i] = true;
            else
                _pitchMarkerValidity[i] = false;
        }
    }

    public List<int> PitchMarkers(float? expectedPitch)
    {
        if (_pitchMarkers == null)
        {
            var edgeThreshold = (config.NUnvoiced - 1) * 2 / 3;
            DrivenOscillator();
            BuildPitchGraph(edgeThreshold);
            FillPitchGraph(expectedPitch, edgeThreshold);
            CheckValidity();
            _pitchMarkers = _graph.Trace();
        }
        return _pitchMarkers;
    }

    public bool[] Validity(float? expectedPitch)
    {
        if (_pitchMarkerValidity == null) PitchMarkers(expectedPitch);
        return _pitchMarkerValidity;
    }

    private int GetValidPitchDelta(int index)
    {
        if (_pitchMarkerValidity[index]) return _pitchMarkers[index + 1] - _pitchMarkers[index];
        var previousDelta = _pitchMarkers[index] - _pitchMarkers[index - 1];
        var nextDelta = _pitchMarkers[index + 2] - _pitchMarkers[index + 1];
        return (previousDelta + nextDelta) / 2;
    }

    public Vector<float> PitchDeltas(float? expectedPitch)
    {
        _pitchMarkers = PitchMarkers(expectedPitch);
        var start = 0;
        var end = 0;
        var batches = (int)Math.Ceiling((double)(_oscillatorProxy.Count / config.StepSize));
        var pitchDeltas = Vector<float>.Build.Dense(batches);
        for (var i = 0; i < batches; i++)
        {
            while (start + 1 < _pitchMarkers.Count && _pitchMarkers[start + 1] < i * config.StepSize) start++;
            while (end < _pitchMarkers.Count && _pitchMarkers[end] <= (i + 1) * config.StepSize) end++;
            var count = end - start;
            pitchDeltas[i] = 0;
            if (count == 0)
            {
                continue;
            }
            for (var j = start; j < end; j++)
                if (j == 0) 
                    pitchDeltas[i] += _pitchMarkers[j + 1] - _pitchMarkers[j];
                else if (j == _pitchMarkers.Count - 1)
                    pitchDeltas[i] += _pitchMarkers[j] - _pitchMarkers[j - 1];
                else
                    pitchDeltas[i] += GetValidPitchDelta(j);
            pitchDeltas[i] /= count;
        }
        return pitchDeltas;
    }
}