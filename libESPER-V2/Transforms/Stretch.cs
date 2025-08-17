using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Interpolation;

namespace libESPER_V2.Transforms;

public static class Stretch
{
    public static EsperAudio StretchAudio(EsperAudio input, int length)
    {
        var frames = input.GetFrames();
        var config = input.Config;
        var interpolated = StretchData(frames, length);
        var output = new EsperAudio(interpolated, config);
        return output;
    }
    
    public static EsperAudio LoopAudio(EsperAudio input, int length, float overlap = 0.5f)
    {
        var frames = input.GetFrames();
        var config = input.Config;
        var interpolated = LoopData(frames, length, overlap);
        var output = new EsperAudio(interpolated, config);
        return output;
    }
    
    public static EsperAudio StretchLoopHybrid(EsperAudio input, int length, float overlap = 0.5f)
    {
        var pitchFrames = input.GetPitch();
        var amps = input.GetVoicedAmps();
        var phases = input.GetVoicedPhases();
        var unvoiced = input.GetUnvoiced();
        var config = input.Config;
        var interpolatedPitch = StretchData(pitchFrames.ToColumnMatrix(), length).Column(0);
        var loopedAmps = LoopData(amps, length, overlap);
        var loopedPhases = LoopData(phases, length, overlap);
        var interpolatedUnvoiced = StretchData(unvoiced, length);
        var output = new EsperAudio(length, config);
        output.SetPitch(interpolatedPitch);
        output.SetVoicedAmps(loopedAmps);
        output.SetVoicedPhases(loopedPhases);
        output.SetUnvoiced(interpolatedUnvoiced);
        return output;
    }
    
    private static Matrix<float> StretchData(Matrix<float> data, int length)
    {
        var rows = data.RowCount;
        var cols = data.ColumnCount;
        var output = Matrix<float>.Build.Dense(length, cols);
        var scale = Vector<double>.Build.Dense(rows, i => i);
        var newScale = Vector<double>.Build.Dense(length, i => i * (float)rows / length);
        for (var i = 0; i < cols; i++)
        {
            var column = data.Column(i).ToDouble();
            var interpolator = CubicSpline.InterpolatePchip(scale, column);
            var interpolated = Vector<float>.Build.Dense(length, j => (float)interpolator.Interpolate(newScale[j]));
            output.SetColumn(i, interpolated);
        }
        return output;
    }
    
    private static Matrix<float> LoopData(Matrix<float> data, int length, float overlap)
    {
        
    }
}