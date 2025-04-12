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
        public ESPERAudio forward(Vector<float> x, ESPERAudioConfig config)
        {
            int length = x.Count;
            ESPERAudio output = new ESPERAudio(length, config);
            return output;
        }

        public ESPERAudio forwardApprox(Vector<float> x, ESPERAudioConfig config)
        {
            int length = x.Count;
            ESPERAudio output = new ESPERAudio(length, config);
            return output;
        }

        public Vector<float> inverse(ESPERAudio x)
        {
            int length = x.Length;
            ESPERAudio output = new ESPERAudio(length, x.Config);
            return output;
        }
    }
}
