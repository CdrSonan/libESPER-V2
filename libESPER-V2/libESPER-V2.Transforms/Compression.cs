using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libESPER_V2.Transforms
{
    class Compression
    {
        public CompressedESPERAudio compress(ESPERAudio audio, int temporalCompression, int spectralCompression)
        {
            int length = audio.Length;
            CompressedESPERAudio compressedAudio = new CompressedESPERAudio(length, audio.Config);
            compressedAudio.Compress(audio, temporalCompression, spectralCompression);
            return compressedAudio;
        }
    }
}
