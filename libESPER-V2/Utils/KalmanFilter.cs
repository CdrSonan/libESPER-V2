using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Utils;

public class KalmanFilter
{
    // Process noise variance (q): how fast the mean can change.
    public double ProcessNoiseVariance { get; }

    // Baseline measurement noise variance (r).
    public double MeasurementNoiseVariance { get; }

    // Robustness threshold for normalized residuals (Huber-like).
    // Typical choice: 2.0 - 3.0
    public double RobustThreshold { get; }

    // Exponential forgetting factor for robust scale estimate (0 < alpha <= 1).
    public double ScaleForgettingFactor { get; }

    // Small epsilon to avoid division by zero.
    private const double Epsilon = 1e-9;

    public KalmanFilter(
        double processNoiseVariance,
        double measurementNoiseVariance,
        double robustThreshold = 2.5,
        double scaleForgettingFactor = 0.05)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processNoiseVariance);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(measurementNoiseVariance);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(robustThreshold);
        if (scaleForgettingFactor is <= 0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(scaleForgettingFactor));

        ProcessNoiseVariance = processNoiseVariance;
        MeasurementNoiseVariance = measurementNoiseVariance;
        RobustThreshold = robustThreshold;
        ScaleForgettingFactor = scaleForgettingFactor;
    }

    /// <summary>
    /// Result of filtering a univariate time series.
    /// </summary>
    public class FilterResult
    {
        /// <summary>
        /// Posterior mean μ_t at each time step.
        /// </summary>
        public required Vector<double> Mean;

        /// <summary>
        /// Posterior variance P_t of the latent mean at each time step.
        /// </summary>
        public required Vector<double> StateVariance;

        /// <summary>
        /// Robust scale estimate of residuals at each time step
        /// (can be interpreted as observation std).
        /// </summary>
        public required Vector<double> ObservationStd;

        /// <summary>
        /// Effective measurement variance used at each step (after inflation).
        /// Mainly for debugging / diagnostics.
        /// </summary>
        public required Vector<double> EffectiveMeasurementVariance;
    }

    /// <summary>
    /// Apply robust Kalman filtering to a 1D time series.
    ///
    /// input: Vector(double) observations (length T)
    /// initialMean: initial μ_0
    /// initialVariance: initial P_0 (uncertainty about μ_0)
    /// initialObservationStd: initial σ for residual normalization.
    ///
    /// Returns per-time estimates of mean, state variance,
    /// robust residual scale, and effective measurement variance.
    /// </summary>
    public FilterResult Filter(
        Vector<double> observations,
        double initialMean,
        double initialVariance,
        double initialObservationStd)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var n = observations.Count;
        ArgumentOutOfRangeException.ThrowIfZero(n);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialVariance);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialObservationStd);

        var mean = Vector<double>.Build.Dense(n);
        var stateVar = Vector<double>.Build.Dense(n);
        var obsStd = Vector<double>.Build.Dense(n);
        var effMeasVar = Vector<double>.Build.Dense(n);

        // Initialize state
        var mu = initialMean;
        var p = initialVariance;
        var sigmaObs = initialObservationStd;

        for (var t = 0; t < n; t++)
        {
            var x = observations[t];

            // 1) Predict step (random walk)
            var muPred = mu;                 // F = 1
            var pPred = p + ProcessNoiseVariance;

            // 2) Compute residual
            var residual = x - muPred;

            // 3) Normalize residual by current scale estimate
            var z = residual / (sigmaObs + Epsilon);

            // 4) Compute robust weight (Huber-style)
            var absZ = Math.Abs(z);
            double w;
            if (absZ <= RobustThreshold)
            {
                w = 1.0; // inlier
            }
            else
            {
                // Downweight outliers ~ c / |z|
                w = RobustThreshold / absZ;
            }

            // 5) Inflate measurement variance for outliers
            var rEff = MeasurementNoiseVariance / (w * w);

            // 6) Kalman gain and update
            var s = pPred + rEff;   // innovation variance
            var k = pPred / s;      // Kalman gain

            mu = muPred + k * residual;
            p = (1.0 - k) * pPred;

            // 7) Update robust scale estimate (EWMA on |residual|)
            var absResidual = Math.Abs(residual);
            sigmaObs = (1.0 - ScaleForgettingFactor) * sigmaObs
                           + ScaleForgettingFactor * absResidual;

            // Store results
            mean[t] = mu;
            stateVar[t] = p;
            obsStd[t] = sigmaObs;
            effMeasVar[t] = rEff;
        }

        return new FilterResult
        {
            Mean = mean,
            StateVariance = stateVar,
            ObservationStd = obsStd,
            EffectiveMeasurementVariance = effMeasVar
        };
    }
}
