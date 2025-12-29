using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils;

public static class MultivariateComplexPELT
{
    public static List<int> DetectChangePoints(Matrix<Complex32> data, int maxChangePoints, double penalty)
    {
        var n = data.RowCount;
        var cost = new double[n + 1];
        var changePoints = new int[n + 1];

        // Initialize the cost and change point arrays
        for (var i = 0; i <= n; i++)
        {
            cost[i] = double.MaxValue;
            changePoints[i] = 0;
        }
        cost[0] = 0;

        // Iterate through the data
        for (var t = 1; t <= n; t++)
        {
            for (var s = 0; s < t; s++)
            {
                // Calculate the cost of adding a change point at position t
                var currentCost = cost[s] + penalty + CostFunction(data, s, t);
                if (currentCost < cost[t])
                {
                    cost[t] = currentCost;
                    changePoints[t] = s;
                }
            }
            // Prune the search space
            for (var s = 0; s < t; s++)
            {
                if (cost[s] + penalty >= cost[t])
                {
                    break;
                }
            }
        }

        // Backtrack to find the optimal set of change points
        var changepoints = new List<int>();
        var tt = n;
        while (tt > 0)
        {
            var s = changePoints[tt];
            changepoints.Add(s);
            tt = s;
        }
        changepoints.Reverse();
        return changepoints;
    }

    private static double CostFunction(Matrix<Complex32> data, int start, int end)
    {
        var mean = Vector<Complex32>.Build.Dense(data.ColumnCount, Complex32.Zero);
        for (var i = start; i < end; i++)
        {
            mean += data.Row(i);
        }
        mean /= (end - start);

        double cost = 0;
        for (var i = start; i < end; i++)
        {
            cost += (data.Row(i) - mean).L2Norm();
        }
        return cost;
    }
}
