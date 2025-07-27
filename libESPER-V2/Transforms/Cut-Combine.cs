using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms;

public static class CutCombine
{
    public static EsperAudio Cut(EsperAudio input, int start, int end)
    {
        if (start < 0) start += input.Length;
        if (end < 0) end += input.Length;
        
        if (start < 0 || end > input.Length || start >= end)
            throw new ArgumentException("Invalid cut range specified.");
        
        var data = input.GetFrames(start, end);
        var config = input.Config;
        var output = new EsperAudio(data, config);
        return output;
    }
    
    public static EsperAudio Concat(EsperAudio first, EsperAudio second)
    {
        if (!Equals(first.Config, second.Config))
            throw new ArgumentException("Audio configurations do not match.");
        
        var combinedData = first.GetFrames().Stack(second.GetFrames());
        var combinedAudio = new EsperAudio(combinedData, first.Config);
        return combinedAudio;
    }
    
    public static EsperAudio Crossfade(EsperAudio first, EsperAudio second, int fadeLength)
    {
        if (!Equals(first.Config, second.Config))
            throw new ArgumentException("Audio configurations do not match.");
        
        if (fadeLength <= 0 || fadeLength > first.Length || fadeLength > second.Length)
            throw new ArgumentOutOfRangeException(nameof(fadeLength), "Invalid fade length specified.");
        
        var firstFrames = first.GetFrames();
        var secondFrames = second.GetFrames();
        
        // Create a new array for the combined audio
        var combinedFrames = Matrix<float>.Build.Dense(
            firstFrames.RowCount + secondFrames.RowCount - fadeLength, firstFrames.ColumnCount);
        
        // Add the first audio frames
        combinedFrames.SetSubMatrix(0, 0,
            firstFrames.SubMatrix(0, firstFrames.RowCount - fadeLength, 0, firstFrames.ColumnCount));
        
        // Apply crossfade
        for (var i = 0; i < fadeLength; i++)
        {
            var fadeFactor = (float)(i + 1) / (fadeLength + 1);
            combinedFrames.SetRow(firstFrames.RowCount - fadeLength + i,
                firstFrames.Row(firstFrames.RowCount - fadeLength + i) * (1 - fadeFactor) +
                secondFrames.Row(i) * fadeFactor);
        }
        
        // Add the remaining frames of the second audio
        combinedFrames.SetSubMatrix(firstFrames.RowCount, 0,
            secondFrames.SubMatrix(fadeLength, secondFrames.RowCount - fadeLength, 0, secondFrames.ColumnCount));
        
        return new EsperAudio(combinedFrames, first.Config);
    }
    
    public static EsperAudio FadeIn(EsperAudio audio, int fadeLength)
    {
        if (fadeLength <= 0 || fadeLength > audio.Length)
            throw new ArgumentOutOfRangeException(nameof(fadeLength), "Invalid fade length specified.");
        
        var frames = audio.GetFrames();
        for (var i = 0; i < fadeLength; i++)
        {
            var fadeFactor = (float)(i + 1) / (fadeLength + 1);
            frames.SetRow(i, frames.Row(i) * fadeFactor);
        }
        
        return new EsperAudio(frames, audio.Config);
    }

    public static EsperAudio FadeOut(EsperAudio audio, int fadeLength)
    {
        if (fadeLength <= 0 || fadeLength > audio.Length)
            throw new ArgumentOutOfRangeException(nameof(fadeLength), "Invalid fade length specified.");
        
        var frames = audio.GetFrames();
        for (var i = 0; i < fadeLength; i++)
        {
            var fadeFactor = (float)(fadeLength - i) / (fadeLength + 1);
            frames.SetRow(audio.Length - fadeLength + i, frames.Row(audio.Length - fadeLength + i) * fadeFactor);
        }
        
        return new EsperAudio(frames, audio.Config);
    }
}