using libESPER_V2.Core;
using libESPER_V2.Utils;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms;

public class Compression
{
    public static CompressedEsperAudio Compress(EsperAudio audio, int temporalCompression, int spectralCompression,
        float eps)
    {
        CompressedEsperAudio compressedAudio =
            new(audio.Length, temporalCompression, spectralCompression, audio.Config);

        var voiced = audio.GetVoicedAmps();
        var compressedVoiced = Matrix<Half>.Build.Dense(voiced.RowCount, voiced.ColumnCount);
        voiced.MapConvert(x => (Half)Math.Log(x + eps), compressedVoiced);
        compressedAudio.SetVoiced(compressedVoiced);

        var numMelBands = audio.Config.NUnvoiced / spectralCompression;

        var unvoicedMel = Matrix<float>.Build.Dense(audio.Length, numMelBands);
        for (var i = 0; i < audio.Length; i++)
        {
            var unvoiced = audio.GetUnvoiced(i);
            var mel = Mel.MelFwd(unvoiced, numMelBands, 48000);
            unvoicedMel.SetRow(i, mel);
        }

        var compressedUnvoiced = Matrix<Half>.Build.Dense(unvoicedMel.RowCount, unvoicedMel.ColumnCount);
        unvoicedMel.MapConvert(x => (Half)x, compressedUnvoiced);
        compressedAudio.SetUnvoiced(compressedUnvoiced);

        return compressedAudio;
    }

    public static EsperAudio Decompress(CompressedEsperAudio audio, float eps)
    {
        EsperAudio decompressedAudio = new(audio.Length, audio.Config);
        var voiced = audio.GetVoiced();
        var decompressedVoiced = Matrix<float>.Build.Dense(voiced.RowCount, voiced.ColumnCount);
        voiced.MapConvert(x => (float)(Math.Exp((float)x) - eps), decompressedVoiced);
        decompressedAudio.SetVoicedAmps(decompressedVoiced);

        var unvoicedMel = audio.GetUnvoiced();
        var decompressedUnvoicedMel = Matrix<float>.Build.Dense(unvoicedMel.RowCount, unvoicedMel.ColumnCount);
        unvoicedMel.MapConvert(x => (float)x, decompressedUnvoicedMel);
        for (var i = 0; i < audio.Length; i++)
        {
            var mel = decompressedUnvoicedMel.Row(i);
            var unvoiced = Mel.MelInv(mel, audio.Config.NUnvoiced, 48000);
            decompressedAudio.SetUnvoiced(i, unvoiced);
        }

        return decompressedAudio;
    }
}