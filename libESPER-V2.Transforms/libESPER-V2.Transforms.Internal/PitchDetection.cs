using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.LinearAlgebra;
using libESPER_V2.Utils;
using System.Xml.Linq;

namespace libESPER_V2.Transforms.Internal
{
    class PitchDetection(Vector<float> audio, float oscillatorDamping, int distanceLimit)
    {
        private Vector<float> audio = audio;
        private readonly float oscillatorDamping = oscillatorDamping;
        private readonly int distanceLimit = distanceLimit;
        private Vector<float>? oscillatorProxy;
        private readonly DAG graph = new();
        private List<int>? pitchMarkers;
        private List<bool>? pitchMarkerValidity;

        private void DrivenOscillator()
        {
            oscillatorProxy = Vector<float>.Build.Dense(audio.Count, 0);
            float a;
            float v = 0;
            float x = 0;
            for (int i = 1; i < audio.Count; i++)
            {
                a = audio[i] - oscillatorDamping * v - oscillatorDamping * x;
                v += a;
                x += v;
                oscillatorProxy[i] = x;
            }
        }
        private void BuildPitchGraph(int edgeThreshold)
        {
            for (int i = 1; i < oscillatorProxy.Count; i++)
            {
                if (oscillatorProxy[i-1] < 0 && oscillatorProxy[i] >= 0)
                {
                    bool root = (i < edgeThreshold) || (graph.nodes.Count == 0);
                    bool leaf = (i >= oscillatorProxy.Count - edgeThreshold);
                    Node node = new(i, root, leaf);
                    graph.AddNode(node);
                }
            }
            if (graph.nodes.Count > 0)
            {
                graph.nodes.Last().isLeaf = true;
            }
        }
        private void FillPitchGraph(float? expectedPitch, int? edgeThreshold)
        {
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i].isRoot)
                {
                    continue;
                }
                int limit = Math.Max(0, i - distanceLimit);
                for (int j = i - 1; j >= limit; j--)
                {
                    double distance = PitchNodeDistance(graph.nodes[i].id, graph.nodes[j].id, expectedPitch, 50, edgeThreshold);
                    if (graph.nodes[j].value + distance < graph.nodes[i].value)
                    {
                        graph.nodes[i].parent = graph.nodes[j];
                    }
                }
            }
        }
        private double PitchNodeDistance(int id1, int id2, float? expectedPitch, long? lowerLimit, long? upperLimit)
        {
            int delta = id2 - id1;
            //TODO: Assertions based on limit values
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
            else if (id2 >= oscillatorProxy.Count - delta)
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
                error += Math.Pow(oscillatorProxy[start1 + i] - oscillatorProxy[start2 + i], 2) * bias;
                contrast += oscillatorProxy[start1 + i] * Math.Sin(2 * Math.PI * (i / delta));
            }
            return error / Math.Pow(contrast, 2);
        }
        private void CheckValidity()
        {
            pitchMarkerValidity = new List<bool>(graph.nodes.Count - 1);
            pitchMarkerValidity[0] = true;
            pitchMarkerValidity[pitchMarkerValidity.Count - 2] = true;
            for (int i = 1; i < graph.nodes.Count - 2; i++)
            {
                int sectionSize = graph.nodes[i + 1].id - graph.nodes[i].id;
                int previousSize = graph.nodes[i].id - graph.nodes[i - 1].id;
                int nextSize = graph.nodes[i + 2].id - graph.nodes[i + 1].id;
                if (previousSize <= sectionSize + 2 || nextSize <= sectionSize + 2)
                {
                    pitchMarkerValidity[i] = true;
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
                    previousScale[j] = j * ((float)sectionSize / (float)previousSize);
                }
                Vector<float> nextScale = Vector<float>.Build.Dense(nextSize);
                for (int j = 0; j < nextSize; j++)
                {
                    nextScale[j] = j * ((float)sectionSize / (float)nextSize);
                }
                Vector<float> section = oscillatorProxy.SubVector(graph.nodes[i].id, sectionSize);
                Vector<float> previousSection = oscillatorProxy.SubVector(graph.nodes[i - 1].id, previousSize);
                Vector<float> nextSection = oscillatorProxy.SubVector(graph.nodes[i + 1].id, nextSize);
                Vector<float> previousInterpolated = Interpolation.Interpolate(previousScale, previousSection, scale);
                Vector<float> nextInterpolated = Interpolation.Interpolate(nextScale, nextSection, scale);
                for (int j = 0; j < sectionSize; j++)
                {
                    validError += (float)Math.Pow(section[j] - (previousInterpolated[j] + nextInterpolated[j]) / 2, 2);
                }
                float invalidError = 0;
                for (int j = 0; j < sectionSize; j++)
                {
                    float alternative = oscillatorProxy[graph.nodes[i - 1].id + j] - oscillatorProxy[graph.nodes[i + 2].id - sectionSize + j];
                    invalidError += (float)Math.Pow(alternative / 2, 2);
                }
                if (validError < invalidError)
                {
                    pitchMarkerValidity[i] = true;
                }
                else
                {
                    pitchMarkerValidity[i] = false;
                }
            }
        }
        public List<int> PitchMarkers(ESPERAudioConfig config, float? expectedPitch)
        {
            if (pitchMarkers == null)
            {
                int edgeThreshold = (config.nUnvoiced - 1) * 2 / 3;
                DrivenOscillator();
                BuildPitchGraph(edgeThreshold);
                FillPitchGraph(expectedPitch, edgeThreshold);
                CheckValidity();
                pitchMarkers = graph.trace();
            }
            return pitchMarkers;
        }
        private int GetValidPitchDelta(int index)
        {
            if (pitchMarkerValidity[index])
            {
                return pitchMarkers[index + 1] - pitchMarkers[index];
            }
            int previousDelta = pitchMarkers[index] - pitchMarkers[index - 1];
            int nextDelta = pitchMarkers[index + 2] - pitchMarkers[index + 1];
            return (previousDelta + nextDelta) / 2;
        }
        public Vector<float> PitchDeltas(ESPERAudioConfig config, float? expectedPitch)
        {
            pitchMarkers = PitchMarkers(config, expectedPitch);
            int cursor = 0;
            int batchSize = (config.nUnvoiced - 1) * 2 / 3;
            int batches = (int)Math.Ceiling((double)(oscillatorProxy.Count / batchSize));
            Vector<float> pitchDeltas = Vector<float>.Build.Dense(batches);
            for (int i = 0; i < batches; i++)
            {
                while (pitchMarkers[cursor] <= i * batchSize && cursor < pitchMarkers.Count)
                {
                    cursor++;
                }
                if (cursor == 0)
                {
                    pitchDeltas[i] = pitchMarkers[cursor + 1] - pitchMarkers[cursor];
                }
                else if (cursor == pitchMarkers.Count - 1)
                {
                    pitchDeltas[i] = pitchMarkers[cursor] - pitchMarkers[cursor - 1];
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
