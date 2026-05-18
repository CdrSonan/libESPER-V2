using libESPER_V2.Core;
using libESPER_V2.Utils;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Algorithms;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.CPU;

namespace libESPER_V2.Transforms.Internal;

public static class PitchDetectionGpu
{
    public static void PitchNodeDistanceKernel(
        Index1D index,
        ArrayView1D<int, Stride1D.Dense> start1View,
        ArrayView1D<int, Stride1D.Dense> start2View,
        ArrayView1D<int, Stride1D.Dense> deltaView,
        ArrayView1D<float, Stride1D.Dense> biasView,
        ArrayView1D<float, Stride1D.Dense> oscillatorView,
        ArrayView1D<double, Stride1D.Dense> resultView)
    {
        int start1 = start1View[index];
        int start2 = start2View[index];
        int delta = deltaView[index];
        float bias = biasView[index];
        
        float error = 0f;
        double contrast = 0.0;
        
        for (int i = 0; i < delta; i++)
        {
            float diff = oscillatorView[start1 + i] - oscillatorView[start2 + i];
            error += diff * diff * bias;
            contrast += oscillatorView[start1 + i] * XMath.Sin(2.0 * XMath.PI * ((double)i / delta));
        }

        resultView[index] = error / (contrast * contrast);
    }
}

public class PitchDetection(Vector<float> audio, EsperAudioConfig config, float? oscillatorDamping)
{
    private readonly Graph _graph = new();
    private Vector<float>? _smoothedProxy;
    private List<int>? _pitchMarkers;
    private bool[]? _pitchMarkerValidity;


    private Vector<float> Smoothing()
    {
        if (_smoothedProxy != null)
            return _smoothedProxy;
        if (oscillatorDamping == null)
        {
            _smoothedProxy = Vector<float>.Build.Dense(audio.Count);
            audio.CopyTo(_smoothedProxy);
        }
        else
        {
            _smoothedProxy = Vector<float>.Build.Dense(audio.Count, 0);
            var hannWindow = Vector<float>.Build.Dense(3 * config.StepSize,
                i => float.Pow(float.Sin(i * (float)Math.PI / (3 * config.StepSize)), 2));
            var numWindows = (int)Math.Max(Math.Ceiling(audio.Count / (float)config.StepSize) - 2, 1);
            var paddingLength = (numWindows + 2) * config.StepSize - audio.Count;
            var paddedAudio =
                Vector<float>.Build.DenseOfEnumerable(audio.Concat(Vector<float>.Build.Dense(paddingLength, 0)));
            var paddedOutput = Vector<float>.Build.Dense(audio.Count + paddingLength, 0);
            for (var i = 0; i < numWindows; i++)
            {
                var hannWindowMod = Vector<float>.Build.Dense(3 * config.StepSize);
                hannWindow.CopyTo(hannWindowMod);
                if (i == 0)
                {
                    var subVectorA = hannWindowMod.SubVector(0, config.StepSize);
                    var subVectorB = hannWindow.SubVector(2 * config.StepSize, config.StepSize);
                    hannWindowMod.SetSubVector(0, config.StepSize, subVectorA + subVectorB);
                    subVectorA = hannWindowMod.SubVector(0, 2 * config.StepSize);
                    subVectorB = hannWindow.SubVector(config.StepSize, 2 * config.StepSize);
                    hannWindowMod.SetSubVector(0, 2 * config.StepSize, subVectorA + subVectorB);
                }
                if (i == numWindows - 1)
                {
                    var subVectorA = hannWindowMod.SubVector(2 * config.StepSize, config.StepSize);
                    var subVectorB = hannWindow.SubVector(0, config.StepSize);
                    hannWindowMod.SetSubVector(2 * config.StepSize, config.StepSize, subVectorA + subVectorB);
                    subVectorA = hannWindowMod.SubVector(config.StepSize, 2 * config.StepSize);
                    subVectorB = hannWindow.SubVector(0, 2 * config.StepSize);
                    hannWindowMod.SetSubVector(config.StepSize, 2 * config.StepSize, subVectorA + subVectorB);
                }
                var window = Vector<float>.Build.Dense(3 * config.StepSize + 2, 
                    j => j < 3 * config.StepSize ? paddedAudio[i * config.StepSize + j] : 0);
                var windowArr = window.ToArray();
                Fourier.ForwardReal(windowArr, 3 * config.StepSize);
                window = Vector<float>.Build.DenseOfArray(windowArr);
                window.MapIndexedInplace((j, val) => val * float.Pow(0.93f, (int)(j / 2)));
                windowArr = window.ToArray();
                Fourier.InverseReal(windowArr, 3  * config.StepSize);
                window = Vector<float>.Build.DenseOfArray(windowArr).SubVector(0, 3 * config.StepSize)
                    .PointwiseMultiply(hannWindowMod);
                var existingOutput = paddedOutput.SubVector(i * config.StepSize, 3 * config.StepSize);
                paddedOutput.SetSubVector(i * config.StepSize, 3 * config.StepSize, existingOutput + window);
            }
            _smoothedProxy = paddedOutput.SubVector(0, audio.Count);
        }
        return _smoothedProxy;
    }

    private void BuildPitchGraph(int edgeThreshold)
    {
        var oscillator = Smoothing();
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

    private void FillPitchGraph(Vector<float>? expectedPitch, int? edgeThreshold)
    {
        var oscillator = Smoothing();
        int lowerLimit = 25;
        int maxEdgeThreshold = edgeThreshold ?? int.MaxValue;
        
        var pairIndices = new List<(int j, int i)>();
        var start1List = new List<int>();
        var start2List = new List<int>();
        var deltaList = new List<int>();
        var biasList = new List<float>();

        for (int i = 0; i < _graph.Nodes.Count; i++)
        {
            if (i == 0 || _graph.Nodes[i].IsRoot)
            {
                _graph.Nodes[i].Value = 0;
                continue;
            }

            var reachable = false;
            for (int j = i - 1; j >= 0; j--)
            {
                int id1 = _graph.Nodes[j].Id;
                int id2 = _graph.Nodes[i].Id;
                int delta = id2 - id1;

                if (delta < lowerLimit) continue;
                if (delta > maxEdgeThreshold && reachable) break;
                
                int start1, start2;
                if (id1 < delta)
                {
                    start1 = id1; start2 = id2;
                    if (id2 >= oscillator.Count - delta) continue;
                }
                else if (id2 >= oscillator.Count - delta)
                {
                    start1 = id1 - delta; start2 = id2 - delta;
                }
                else
                {
                    start1 = id1 - delta / 2; start2 = id2 - delta / 2;
                }

                float bias = 1.0f;
                if (expectedPitch != null)
                {
                    float expectedIndex1 = (float)id1 * expectedPitch.Count / oscillator.Count;
                    float expectedIndex2 = (float)id2 * expectedPitch.Count / oscillator.Count;
                    float ep1 = expectedPitch[(int)expectedIndex1];
                    float ep2 = expectedPitch[(int)expectedIndex2];
                    if (ep1 != 0.0f && ep2 != 0.0f)
                    {
                        bias += delta;
                    }
                    else
                    {
                        float ep = (ep1 + ep2) / 2f;
                        bias += float.Pow(delta - ep, 2) / ep;
                    }
                }
                else
                {
                    bias += delta;
                }

                pairIndices.Add((j, i));
                start1List.Add(start1);
                start2List.Add(start2);
                deltaList.Add(delta);
                biasList.Add(bias);
                reachable = true;
            }
        }

        if (pairIndices.Count > 0)
        {
            using var context = Context.Create(builder => builder.Cuda().CPU().EnableAlgorithms());
            var accelerator = context.GetPreferredDevice(preferCPU: false).CreateAccelerator(context);
            
            using var start1Buffer = accelerator.Allocate1D(start1List.ToArray());
            using var start2Buffer = accelerator.Allocate1D(start2List.ToArray());
            using var deltaBuffer = accelerator.Allocate1D(deltaList.ToArray());
            using var biasBuffer = accelerator.Allocate1D(biasList.ToArray());
            using var oscBuffer = accelerator.Allocate1D(oscillator.ToArray());
            using var resultBuffer = accelerator.Allocate1D<double>(pairIndices.Count);

            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D, ArrayView1D<int, Stride1D.Dense>, ArrayView1D<int, Stride1D.Dense>, 
                ArrayView1D<int, Stride1D.Dense>, ArrayView1D<float, Stride1D.Dense>, 
                ArrayView1D<float, Stride1D.Dense>, ArrayView1D<double, Stride1D.Dense>>(
                PitchDetectionGpu.PitchNodeDistanceKernel);

            kernel((int)pairIndices.Count, start1Buffer.View, start2Buffer.View, deltaBuffer.View, 
                   biasBuffer.View, oscBuffer.View, resultBuffer.View);
                   
            accelerator.Synchronize();
            
            double[] results = resultBuffer.GetAsArray1D();
            accelerator.Dispose();

            for (int k = 0; k < pairIndices.Count; k++)
            {
                var (j, i) = pairIndices[k];
                double distance = results[k];
                
                if (double.IsPositiveInfinity(_graph.Nodes[i].Value) || _graph.Nodes[j].Value + distance < _graph.Nodes[i].Value)
                {
                    _graph.Nodes[i].Value = _graph.Nodes[j].Value + distance;
                    _graph.Nodes[i].Parent = _graph.Nodes[j];
                }
            }
        }
    }

    
    public List<int> PitchMarkers(Vector<float>? expectedPitch)
    {
        if (_pitchMarkers != null) return _pitchMarkers;
        var edgeThreshold = 3 * config.StepSize;
        Smoothing();
        BuildPitchGraph(config.StepSize);
        FillPitchGraph(expectedPitch, edgeThreshold);
        _pitchMarkers = _graph.Trace();
        Validity(expectedPitch);
        return _pitchMarkers;
    }

    public bool[] Validity(Vector<float>? expectedPitch)
    {
        if (_pitchMarkerValidity != null) return _pitchMarkerValidity;
        if (_pitchMarkers == null) PitchMarkers(expectedPitch);
        var oscillator = Smoothing();
        _pitchMarkerValidity = new bool[_pitchMarkers!.Count - 1];
        _pitchMarkerValidity[0] = true;
        _pitchMarkerValidity[^1] = true;
        for (var i = 1; i < _pitchMarkers.Count - 2; i++)
        {
            var sectionSize = _pitchMarkers[i + 1] - _pitchMarkers[i];
            var previousSize = _pitchMarkers[i] - _pitchMarkers[i - 1];
            var nextSize = _pitchMarkers[i + 2] - _pitchMarkers[i + 1];
            if (Math.Abs(previousSize - sectionSize) <= 2 && Math.Abs(nextSize - sectionSize) <= 2)
            //if (previousSize <= sectionSize + 2 || nextSize <= sectionSize + 2)
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
                Vector<float>.Build.Dense(sectionSize, j => (float)previousInterpolator.Interpolate(j));
            var nextInterpolator = CubicSpline.InterpolatePchip(nextScale, nextSection);
            var nextInterpolated = Vector<float>.Build.Dense(sectionSize, j => (float)nextInterpolator.Interpolate(j));
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

    public Vector<float> PitchDeltas(Vector<float>? expectedPitch)
    {
        var oscillator = Smoothing();
        var markers = PitchMarkers(expectedPitch);
        var markerDiffsDebug = Vector<float>.Build.Dense(markers.Count - 1, i => markers[i + 1] - markers[i]);
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