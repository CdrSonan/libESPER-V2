using libESPER_V2.Core;
using libESPER_V2.Utils;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms;

public static class Compression
{
    public static CompressedEsperAudio Compress(EsperAudio audio, int temporalCompression, int spectralCompression,
        float eps)
    {
        CompressedEsperAudio compressedAudio =
            new(audio.Length, new CompressedEsperAudioConfig(audio.Config, temporalCompression, spectralCompression));

        var pitchVector = audio.GetPitch();
        var pitch = Matrix<float>.Build.Dense(audio.Length, 1, (i, j) => pitchVector[i]);
        pitch.MapInplace(x => (float)Math.Log(Math.Max(x, 0) + eps));
        
        var voiced = audio.GetVoicedAmps();
        voiced.MapInplace(x => (float)Math.Log(Math.Max(x, 0) + eps));

        var numMelBands = audio.Config.NUnvoiced / spectralCompression;

        var compressedUnvoiced = Matrix<float>.Build.Dense(audio.Length, numMelBands);
        for (var i = 0; i < audio.Length; i++)
        {
            var unvoiced = audio.GetUnvoiced(i);
            var mel = Mel.MelFwd(unvoiced, numMelBands, 48000);
            compressedUnvoiced.SetRow(i, mel);
        }
        compressedUnvoiced.MapInplace(x => (float)Math.Log(Math.Max(x, 0) + eps));

        var extension = Matrix<float>.Build.Dense(
            audio.Length % temporalCompression, 
            compressedAudio.Config.FrameSize(),
            0);
        var frames = pitch.Append(voiced).Append(compressedUnvoiced).Stack(extension);
        var compressedFrames = Matrix<float>.Build.Dense(
            compressedAudio.CompressedLength,
            compressedAudio.Config.FrameSize(),
            (i, j) => frames.Column(j).SubVector(i * temporalCompression, temporalCompression).Sum() / 
                      (audio.Length - i * temporalCompression <= 0 ? temporalCompression - audio.Length % temporalCompression : temporalCompression)
        );
        compressedAudio.SetFrames(compressedFrames);
        return compressedAudio;
    }

    public static EsperAudio Decompress(CompressedEsperAudio audio, float eps)
    {
        EsperAudio decompressedAudio = new(audio.Length, new EsperAudioConfig(audio.Config));
        var pitchVector = audio.GetPitch();
        var pitch = Matrix<float>.Build.Dense(audio.CompressedLength, 1, (i, j) => pitchVector[i]);
        pitch.MapInplace(x => (float)Math.Max(Math.Exp(x) - eps, 0));

        var voiced = audio.GetVoiced();
        voiced.MapInplace(x => (float)Math.Max(Math.Exp(x) - eps, 0));

        var voicedPhases = Matrix<float>.Build.Dense(voiced.RowCount, voiced.ColumnCount, 0);

        var unvoicedMel = audio.GetUnvoiced();
        unvoicedMel.MapInplace(x => (float)Math.Max(Math.Exp(x) - eps, 0));
        var unvoiced = Matrix<float>.Build.Dense(audio.CompressedLength, audio.Config.NUnvoiced);
        for (var i = 0; i < audio.CompressedLength; i++)
        {
            var mel = unvoicedMel.Row(i);
            var unvoicedFrame = Mel.MelInv(mel, audio.Config.NUnvoiced, 48000);
            unvoiced.SetRow(i, unvoicedFrame);
        }

        var frames = pitch.Append(voiced).Append(voicedPhases).Append(unvoiced);
        var decompressedFrames = Matrix<float>.Build.Dense(
            audio.Length,
            decompressedAudio.Config.FrameSize(),
            (i, j) => frames[i / audio.Config.TemporalCompression, j]);
        decompressedAudio.SetFrames(decompressedFrames);
        return decompressedAudio;
    }
}