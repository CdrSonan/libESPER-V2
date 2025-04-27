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
        private float oscillatorDamping = oscillatorDamping;
        private int distanceLimit = distanceLimit;
        private Vector<float>? oscillatorProxy;
        private readonly DAG graph = new();
        private List<long>? pitchMarkers;

        private void DrivenOscillator()
        {
            oscillatorProxy = Vector<float>.Build.Dense(audio.Count, 0);
            for (int i = 1; i < audio.Count; i++)
            {
                oscillatorProxy[i] = (oscillatorProxy[i - 1] + audio[i]) * oscillatorDamping;
            }
        }
        private void BuildPitchGraph()
        {
            for (int i = 1; i < audio.Count; i++)
            {
                if (audio[i-1] < 0 && audio[i] > 0)
                {
                    Node node = new(i, true, false);
                    graph.AddNode(node);
                }
            }
        }
        private void FillPitchGraph()
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
                    double distance = PitchNodeDistance(graph.nodes[i].id, graph.nodes[j].id);
                    if (graph.nodes[j].value + distance < graph.nodes[i].value)
                    {
                        graph.nodes[i].parent = graph.nodes[j];
                    }
                }
            }
        }
        private double PitchNodeDistance(long id1, long id2)
        {
            return Math.Abs(id1 - id2);
        }
        public List<long> PitchMarkers()
        {
            if (pitchMarkers == null)
            {
                DrivenOscillator();
                BuildPitchGraph();
                FillPitchGraph();
                pitchMarkers = graph.trace();
            }
            return pitchMarkers;
        }
    }
}
