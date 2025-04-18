using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms
{
    class ESPER_Transforms
    {
        public static ESPERAudio Forward(Vector<float> x, ESPERAudioConfig config)
        {
            int length = x.Count;
            ESPERAudio output = new ESPERAudio(length, config);
            return output;
        }

        public static ESPERAudio ForwardApprox(Vector<float> x, ESPERAudioConfig config)
        {
            int length = x.Count;
            ESPERAudio output = new ESPERAudio(length, config);
            return output;
        }

        public static Vector<float> Inverse(ESPERAudio x)
        {
            int length = x.length;
            Vector<float> output = Vector<float>.Build.Dense(length, 0);
            return output;
        }
    }
}
