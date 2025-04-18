using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using libESPER_V2.Utils;
using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Transforms
{
    class Compression
    {
        public static CompressedESPERAudio Compress(ESPERAudio audio, int temporalCompression, int spectralCompression, float eps)
        {
            CompressedESPERAudio compressedAudio = new(audio.length, temporalCompression, spectralCompression, audio.config);

            Matrix<float> voiced = audio.getVoicedAmps();
            Matrix<Half> compressedVoiced = Matrix<Half>.Build.Dense(voiced.RowCount, voiced.ColumnCount);
            voiced.MapConvert<Half>(x => (Half)(Math.Log(x + eps)), compressedVoiced);
            compressedAudio.setVoiced(compressedVoiced);

            int numMelBands = audio.config.nUnvoiced / spectralCompression;
            
            Matrix<float> unvoicedMel = Matrix<float>.Build.Dense(audio.length, numMelBands);
            for (int i = 0; i < audio.length; i++)
            {
                Vector<float> unvoiced = audio.getUnvoiced(i);
                Vector<float> mel = Mel.MelFwd(unvoiced, numMelBands, 60, 48000);
                unvoicedMel.SetRow(i, mel);
            }
            Matrix<Half> compressedUnvoiced = Matrix<Half>.Build.Dense(unvoicedMel.RowCount, unvoicedMel.ColumnCount);
            unvoicedMel.MapConvert<Half>(x => (Half)(x), compressedUnvoiced);
            compressedAudio.setUnvoiced(compressedUnvoiced);

            return compressedAudio;
        }
        public static ESPERAudio Decompress(CompressedESPERAudio audio, float eps)
        {
            ESPERAudio decompressedAudio = new(audio.length, audio.config);
            Matrix<Half> voiced = audio.getVoiced();
            Matrix<float> decompressedVoiced = Matrix<float>.Build.Dense(voiced.RowCount, voiced.ColumnCount);
            voiced.MapConvert<float>(x => (float)(Math.Exp((float)x) - eps), decompressedVoiced);
            decompressedAudio.setVoicedAmps(decompressedVoiced);

            Matrix<Half> unvoicedMel = audio.getUnvoiced();
            Matrix<float> decompressedUnvoicedMel = Matrix<float>.Build.Dense(unvoicedMel.RowCount, unvoicedMel.ColumnCount);
            unvoicedMel.MapConvert<float>(x => (float)(x), decompressedUnvoicedMel);
            for (int i = 0; i < audio.length; i++)
            {
                Vector<float> mel = decompressedUnvoicedMel.Row(i);
                Vector<float> unvoiced = Mel.MelInv(mel, audio.config.nUnvoiced, 60, 48000);
                decompressedAudio.setUnvoiced(i, unvoiced);
            }
            return decompressedAudio;
        }
    }
}
