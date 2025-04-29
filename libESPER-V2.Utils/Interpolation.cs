using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils
{
    public class InterpolationPlan
    {
        public Vector<float> x;
        public Vector<float> xi;
        public Vector<int> idxs;
        public Vector<float> dx;
        public Matrix<float> h;
        public InterpolationPlan(Vector<float> x, Vector<float> xi)
        {
            this.x = x;
            this.xi = xi;
            int len = x.Count;
            int lenXi = xi.Count;
            idxs = Vector<int>.Build.Dense(lenXi);
            dx = Vector<float>.Build.Dense(lenXi);
            int i = 0;
            int j = 0;
            while (i < lenXi && j < len - 1)
            {
                if (xi[i] > x[j + 1])
                {
                    j++;
                }
                else
                {
                    idxs[i] = j;
                    i++;
                }
            }
            while (i < lenXi)
            {
                idxs[i] = j;
                i++;
            }
            Vector<float> hBase = Vector<float>.Build.Dense(lenXi);
            for (int k = 0; k < lenXi; k++) {
                int offset = idxs[k];
                dx[k] = x[offset + 1] - x[offset];
                hBase[k] = (xi[k] - x[offset]) / dx[k];
            }
            h = HPoly(hBase, lenXi);
        }
        private static Matrix<float> HPoly(Vector<float> input, int length)
        {
            Matrix<float> temp = Matrix<float>.Build.Dense(4, length, (i, j) => (float)Math.Pow(input[j], i));
            float[,] coeffsArr = { {1, 0, -3, 2},
                                   {0, 1, -2, 1},
                                   {0, 0, 3, -2},
                                   {0, 0, -1, 1} };
            Matrix<float> coeffs = Matrix<float>.Build.DenseOfArray(coeffsArr);
            return temp * coeffs;
        }
    }
    public class Interpolation
    {
        public static Vector<float> Interpolate(Vector<float> x, Vector<float> y, Vector<float> xi)
        {
            if (x.Count != y.Count)
                throw new ArgumentException("x and y must have the same length");
            if (xi.Count == 0)
                throw new ArgumentException("xi must not be empty");
            InterpolationPlan plan = new InterpolationPlan(x, xi);
            return ExecutePlan(plan, y);

        }
        public static Vector<float> ExecutePlan(InterpolationPlan plan, Vector<float> y)
        {
            if (plan.x.Count != y.Count)
                throw new ArgumentException("x (specified at plan creation) and y must have the same length");
            Vector<float> result = Vector<float>.Build.Dense(plan.xi.Count);
            for (int i = 0; i < plan.xi.Count; i++)
            {
                int offset = plan.idxs[0];
                float m = meanHelper(plan.x, y, offset);
                float mPlus = meanHelper(plan.x, y, offset + 1);
                result[i] = plan.h[0, i] * y[offset] + plan.h[1, i] * m * plan.dx[i] + plan.h[2, i] * y[offset + 1] + plan.h[3, i] * mPlus * plan.dx[i];
            }
            return result;
        }
        private static float meanHelper(Vector<float> x, Vector<float> y, int idx)
        {
            if (idx == 0)
                idx = 1;
            float dxLeft = x[idx] - x[idx - 1];
            float dyLeft = y[idx] - y[idx - 1];
            float dxRight = x[idx + 1] - x[idx];
            float dyRight = y[idx + 1] - y[idx];
            return (dxLeft / dyLeft + dxRight / dyRight) / 2;
        }
    }
}
