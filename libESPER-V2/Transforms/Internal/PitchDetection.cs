using libESPER_V2.Core;
using libESPER_V2.Utils;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms.Internal;

public class PitchDetection(Vector<float> audio, EsperAudioConfig config, float? oscillatorDamping, int distanceLimit)
{
    private readonly Graph _graph = new();
    private Vector<float>? _oscillatorProxy;
    private List<int>? _pitchMarkers;
    private bool[]? _pitchMarkerValidity;


    private Vector<float> DrivenOscillator()
    {
        if (_oscillatorProxy != null)
            return _oscillatorProxy;
        if (oscillatorDamping == null)
        {
            _oscillatorProxy = Vector<float>.Build.Dense(audio.Count);
            audio.CopyTo(_oscillatorProxy);
        }
        else
        {
            _oscillatorProxy = Vector<float>.Build.Dense(audio.Count, 0);
            float v = 0;
            float x = 0;
            for (var i = 1; i < audio.Count; i++)
            {
                var a = (float)(audio[i] - oscillatorDamping * v - oscillatorDamping * x);
                v += a;
                x += v;
                _oscillatorProxy[i] = x;
            }
        }
        return _oscillatorProxy;
    }

    private void BuildPitchGraph(int edgeThreshold)
    {
        Vector<float> oscillator = DrivenOscillator();
        for (var i = 1; i < oscillator.Count; i++)
            if (oscillator[i - 1] < 0 && oscillator[i] >= 0)
            {
                var root = i < edgeThreshold || _graph.Nodes.Count == 0;
                var leaf = i >= oscillator.Count - edgeThreshold;
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
        Vector<float> oscillator = DrivenOscillator();
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
        else if (id2 >= oscillator.Count - delta)
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
            error += Math.Pow(oscillator[start1 + i] - oscillator[start2 + i], 2) * bias;
            contrast += oscillator[start1 + i] * Math.Sin(2 * Math.PI * ((double)i / delta));
        }

        return error / Math.Pow(contrast, 2);
    }
    
    public List<int> PitchMarkers(float? expectedPitch)
    {
        if (_pitchMarkers != null) return _pitchMarkers;
        var edgeThreshold = (config.NUnvoiced - 1) * 2 / 3;
        DrivenOscillator();
        BuildPitchGraph(edgeThreshold);
        FillPitchGraph(expectedPitch, edgeThreshold);
        _pitchMarkers = _graph.Trace();
        Validity(expectedPitch);
        return _pitchMarkers;
    }

    public bool[] Validity(float? expectedPitch)
    {
        if (_pitchMarkerValidity != null) return _pitchMarkerValidity;
        if (_pitchMarkers == null) PitchMarkers(expectedPitch);
        Vector<float> oscillator = DrivenOscillator();
        _pitchMarkerValidity = new bool[_pitchMarkers.Count - 1];
        _pitchMarkerValidity[0] = true;
        _pitchMarkerValidity[^1] = true;
        for (var i = 1; i < _pitchMarkers.Count - 2; i++)
        {
            var sectionSize = _pitchMarkers[i + 1] - _pitchMarkers[i];
            var previousSize = _pitchMarkers[i] - _pitchMarkers[i - 1];
            var nextSize = _pitchMarkers[i + 2] - _pitchMarkers[i + 1];
            if (previousSize <= sectionSize + 2 || nextSize <= sectionSize + 2)
            {
                _pitchMarkerValidity[i] = true;
                continue;
            }

            float validError = 0;
            var previousScale = Vector<double>.Build.Dense(previousSize, j => j * ((float)sectionSize / previousSize));
            var nextScale = Vector<double>.Build.Dense(nextSize, j => j * ((float)sectionSize / nextSize));
            var section = oscillator.SubVector(_pitchMarkers[i], sectionSize);
            var previousSection = Vector<double>.Build.Dense(previousSize);
            oscillator.SubVector(_pitchMarkers[i - 1], previousSize).MapConvert(x => x, previousSection);
            var nextSection = Vector<double>.Build.Dense(nextSize);
            oscillator.SubVector(_pitchMarkers[i + 1], nextSize).MapConvert(x => x, nextSection);
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
                var alternative = oscillator[_pitchMarkers[i - 1] + j] -
                                  oscillator[_pitchMarkers[i + 2] - sectionSize + j];
                invalidError += (float)Math.Pow(alternative, 2);
            }

            if (validError < invalidError)
                _pitchMarkerValidity[i] = true;
            else
                _pitchMarkerValidity[i] = false;
        }
        return _pitchMarkerValidity;
    }

    private int GetValidPitchDelta(int index)
    {
        var validity = Validity(null);
        var markers = PitchMarkers(null);
        if (validity[index]) return markers[index + 1] - markers[index];
        var previousDelta = markers[index] - markers[index - 1];
        var nextDelta = markers[index + 2] - markers[index + 1];
        return (previousDelta + nextDelta) / 2;
    }

    public Vector<float> PitchDeltas(float? expectedPitch)
    {
        var oscillator = DrivenOscillator();
        var markers = PitchMarkers(expectedPitch);
        var start = 0;
        var end = 0;
        var batches = oscillator.Count / config.StepSize;
        var pitchDeltas = Vector<float>.Build.Dense(batches);
        for (var i = 0; i < batches; i++)
        {
            while (start + 1 < markers.Count && markers[start + 1] < i * config.StepSize) start++;
            while (end < markers.Count && markers[end] <= (i + 1) * config.StepSize) end++;
            var count = end - start;
            pitchDeltas[i] = 0;
            if (count == 0)
            {
                continue;
            }
            for (var j = start; j < end; j++)
                if (j == 0) 
                    pitchDeltas[i] += markers[j + 1] - markers[j];
                else if (j == markers.Count - 1)
                    pitchDeltas[i] += markers[j] - markers[j - 1];
                else
                    pitchDeltas[i] += GetValidPitchDelta(j);
            pitchDeltas[i] /= count;
        }
        return pitchDeltas;
    }
}