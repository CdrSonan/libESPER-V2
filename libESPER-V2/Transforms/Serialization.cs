using libESPER_V2.Core;
using MathNet.Numerics.LinearAlgebra;

namespace LibESPER_V2.Transforms;

public static class Serialization
{
    private const uint FileStandard = 10;
    
    public static byte[] Serialize(EsperAudio audio)
    {
        using var stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(FileStandard));
        stream.Write(BitConverter.GetBytes(audio.Config.NVoiced));
        stream.Write(BitConverter.GetBytes(audio.Config.NUnvoiced));
        stream.Write(BitConverter.GetBytes(audio.Config.StepSize));
        stream.Write(BitConverter.GetBytes(audio.Config.IsCompressed));
        stream.Write(BitConverter.GetBytes(audio.Length));
        var frames = audio.GetFrames();
        var data = frames.ToRowMajorArray();
        var buffer = new byte[data.Length * sizeof(float)];
        Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length);
        stream.Write(buffer, 0, buffer.Length);
        stream.Flush();
        return stream.ToArray();
    }

    public static byte[] Serialize(CompressedEsperAudio audio)
    {
        using var stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(FileStandard));
        stream.Write(BitConverter.GetBytes(audio.Config.NVoiced));
        stream.Write(BitConverter.GetBytes(audio.Config.NUnvoiced));
        stream.Write(BitConverter.GetBytes(audio.Config.StepSize));
        stream.Write(BitConverter.GetBytes(audio.Config.IsCompressed));
        stream.Write(BitConverter.GetBytes(audio.Length));
        stream.Write(BitConverter.GetBytes(audio.CompressedLength));
        stream.Write(BitConverter.GetBytes(audio.TemporalCompression));
        stream.Write(BitConverter.GetBytes(audio.SpectralCompression));
        var frames = audio.GetFrames();
        var data = frames.ToRowMajorArray();
        var buffer = new byte[data.Length * sizeof(float)];
        Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length);
        stream.Write(buffer, 0, buffer.Length);
        stream.Flush();
        return stream.ToArray();
    }
    
    public static EsperAudio Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        
        var readFileStd = reader.ReadUInt32();
        if (readFileStd != FileStandard)
            throw new InvalidDataException("Unsupported file standard version.");

        var nVoiced = reader.ReadUInt16();
        var nUnvoiced = reader.ReadUInt16();
        var stepSize = reader.ReadInt32();
        var isCompressed = reader.ReadBoolean();
        if (isCompressed)
            throw new InvalidDataException("This method does not support compressed EsperAudio data. Use DeserializeCompressed instead.");
        var config = new EsperAudioConfig(nVoiced, nUnvoiced, stepSize, isCompressed);
        
        var length = reader.ReadInt32();
        var width = config.FrameSize();
        
        var framesCount = length * width;
        var framesData = new float[framesCount];
        for (var i = 0; i < framesCount; i++)
            framesData[i] = reader.ReadSingle();
        
        var frames = Matrix<float>.Build.DenseOfRowMajor(length, stepSize, framesData);
        
        return new EsperAudio(frames, config);
    }

    public static CompressedEsperAudio DeserializeCompressed(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        
        var readFileStd = reader.ReadUInt32();
        if (readFileStd != FileStandard)
            throw new InvalidDataException("Unsupported file standard version.");
        
        var nVoiced = reader.ReadUInt16();
        var nUnvoiced = reader.ReadUInt16();
        var stepSize = reader.ReadInt32();
        var isCompressed = reader.ReadBoolean();
        if (!isCompressed)
            throw new InvalidDataException("This method only supports compressed EsperAudio data. Use Deserialize instead.");
        var config = new EsperAudioConfig(nVoiced, nUnvoiced, stepSize, isCompressed);
        var length = reader.ReadInt32();
        var compressedLength = reader.ReadInt32();
        var temporalCompression = reader.ReadInt32();
        var spectralCompression = reader.ReadInt32();
        
        var framesCount = length * compressedLength;
        var framesData = new Half[framesCount];
        for (var i = 0; i < framesCount; i++)
            framesData[i] = reader.ReadHalf();
        
        var frames = Matrix<Half>.Build.DenseOfRowMajor(length, compressedLength, framesData);
        
        var compressedAudio = new CompressedEsperAudio(length, temporalCompression, spectralCompression, config, frames);
        return compressedAudio;
    }
}
