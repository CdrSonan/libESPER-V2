using libESPER_V2.Core;
using libESPER_V2.Utils;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms
{
    class Compression
    {
        public static CompressedEsperAudio Compress(EsperAudio audio, int temporalCompression, int spectralCompression, float eps)
        {
            CompressedEsperAudio compressedAudio = new(audio.Length, temporalCompression, spectralCompression, audio.Config);

            Matrix<float> voiced = audio.GetVoicedAmps();
            Matrix<Half> compressedVoiced = Matrix<Half>.Build.Dense(voiced.RowCount, voiced.ColumnCount);
            voiced.MapConvert(x => (Half)(Math.Log(x + eps)), compressedVoiced);
            compressedAudio.SetVoiced(compressedVoiced);

            int numMelBands = audio.Config.NUnvoiced / spectralCompression;
            
            Matrix<float> unvoicedMel = Matrix<float>.Build.Dense(audio.Length, numMelBands);
            for (int i = 0; i < audio.Length; i++)
            {
                Vector<float> unvoiced = audio.GetUnvoiced(i);
                Vector<float> mel = Mel.MelFwd(unvoiced, numMelBands, 60, 48000);
                unvoicedMel.SetRow(i, mel);
            }
            Matrix<Half> compressedUnvoiced = Matrix<Half>.Build.Dense(unvoicedMel.RowCount, unvoicedMel.ColumnCount);
            unvoicedMel.MapConvert(x => (Half)(x), compressedUnvoiced);
            compressedAudio.SetUnvoiced(compressedUnvoiced);

            return compressedAudio;
        }
        public static EsperAudio Decompress(CompressedEsperAudio audio, float eps)
        {
            EsperAudio decompressedAudio = new(audio.Length, audio.Config);
            Matrix<Half> voiced = audio.GetVoiced();
            Matrix<float> decompressedVoiced = Matrix<float>.Build.Dense(voiced.RowCount, voiced.ColumnCount);
            voiced.MapConvert(x => (float)(Math.Exp((float)x) - eps), decompressedVoiced);
            decompressedAudio.SetVoicedAmps(decompressedVoiced);

            Matrix<Half> unvoicedMel = audio.GetUnvoiced();
            Matrix<float> decompressedUnvoicedMel = Matrix<float>.Build.Dense(unvoicedMel.RowCount, unvoicedMel.ColumnCount);
            unvoicedMel.MapConvert(x => (float)(x), decompressedUnvoicedMel);
            for (int i = 0; i < audio.Length; i++)
            {
                Vector<float> mel = decompressedUnvoicedMel.Row(i);
                Vector<float> unvoiced = Mel.MelInv(mel, audio.Config.NUnvoiced, 60, 48000);
                decompressedAudio.SetUnvoiced(i, unvoiced);
            }
            return decompressedAudio;
        }
    }
}
