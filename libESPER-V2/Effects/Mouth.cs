using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Effects;

public static partial class Effects
{
    public static void Mouth(EsperAudio audio, Vector<float> mouth)
    {
        if (mouth.Count != audio.Length)
            throw new ArgumentException("Mouth vector length must match audio length.", nameof(mouth));
        if (mouth.Any(p => p is < -1 or > 1))
            throw new ArgumentException("Mouth vector must not contain values outside of [-1, 1].", nameof(mouth));
    }
}