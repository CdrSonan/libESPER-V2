using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2.Core
{
    public class EsperAudio
    {
        private Matrix<float> _data;
        public readonly EsperAudioConfig Config;
        public readonly int Length;

        // Constructors
        public EsperAudio(Matrix<float> data, EsperAudioConfig config)
        {
            this.Config = config;
            if (data.ColumnCount != config.FrameSize())
            {
                throw new ArgumentException("Data matrix column count does not match frame size.");
            }
            this.Length = data.RowCount;
            this._data = data;
        }
        public EsperAudio(int length, EsperAudioConfig config)
        {
            this.Config = config;
            this.Length = length;
            this._data = Matrix<float>.Build.Dense(length, config.FrameSize(), 0.0f);
        }

        public EsperAudio(EsperAudio audio)
        {
            this.Config = audio.Config;
            this.Length = audio.Length;
            this._data = audio._data.Clone();
        }
        // Validation
        public void Validate()
        {
            Vector<float> pitch = GetPitch();
            pitch.MapInplace(x => x < 0 ? 0 : x); // Ensure the pitch is non-negative
            SetPitch(pitch);
            Matrix<float> amps = GetVoicedAmps();
            amps.MapInplace(x => x < 0 ? 0 : x); // Ensure amplitudes are non-negative
            SetVoicedAmps(amps);
            Matrix<float> phases = GetVoicedPhases();
            phases.MapInplace(x => x < -(float)Math.PI || x > (float)Math.PI ? x % (2 * (float)Math.PI) : x); // Ensure phases are between - pi and pi
            SetVoicedPhases(phases);
            Matrix<float> unvoiced = GetUnvoiced();
            unvoiced.MapInplace(x => x < 0 ? 0 : x); // Ensure unvoiced amplitudes are non-negative
            SetUnvoiced(unvoiced);
        }

        // Getters
        public Matrix<float>GetFrames()
        {
            return _data;
        }
        public Vector<float> GetFrames(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index);
        }
        public Matrix<float> GetFrames(int start, int end)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 0, Config.FrameSize());
        }
        public Vector<float> GetPitch()
        {
            return _data.Column(0);
        }
        public float GetPitch(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data[index, 0];
        }
        public Vector<float> GetPitch(int start, int end)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 0, 1).Column(0);
        }
        public Matrix<float> GetVoicedAmps()
        {
            return _data.SubMatrix(0, Length, 1, Config.NVoiced);
        }
        public Vector<float> GetVoicedAmps(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index).SubVector(1, Config.NVoiced);
        }
        public Matrix<float> GetVoicedAmps(int start, int end)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 1, Config.NVoiced);
        }
        public Matrix<float> GetVoicedPhases()
        {
            return _data.SubMatrix(0, Length, 1 + Config.NVoiced, Config.NVoiced);
        }
        public Vector<float> GetVoicedPhases(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index).SubVector(1 + Config.NVoiced, Config.NVoiced);
        }
        public Matrix<float> GetVoicedPhases(int start, int end)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 1 + Config.NVoiced, Config.NVoiced);
        }
        public Matrix<float> GetUnvoiced()
        {
            return _data.SubMatrix(0, Length, 1 + 2 * Config.NVoiced, Config.NUnvoiced);
        }
        public Vector<float> GetUnvoiced(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index).SubVector(1 + 2 * Config.NVoiced, Config.NUnvoiced);
        }
        public Matrix<float> GetUnvoiced(int start, int end)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 1 + 2 * Config.NVoiced, Config.NUnvoiced);
        }

        // Setters
        public void SetFrames(Matrix<float> data)
        {
            if (data.ColumnCount != Config.FrameSize() || data.RowCount != Length)
            {
                throw new ArgumentException("Data matrix size does not match existing size.");
            }
            this._data = data;
        }
        public void SetFrames(int index, Vector<float> frame)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (frame.Count != Config.FrameSize())
            {
                throw new ArgumentException("Frame size does not match existing size.");
            }
            _data.SetRow(index, frame);
        }
        public void SetFrames(int start, int end, Matrix<float> frames)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (frames.RowCount != end - start + 1 || frames.ColumnCount != Config.FrameSize())
            {
                throw new ArgumentException("Frames size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 0, Config.FrameSize(), frames);
        }
        public void SetPitch(Vector<float> pitch)
        {
            if (pitch.Count != Length)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            _data.SetColumn(0, pitch);
        }
        public void SetPitch(int index, float pitch)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            _data[index, 0] = pitch;
        }
        public void SetPitch(int start, int end, Vector<float> pitch)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (pitch.Count != end - start + 1)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 0, 1, pitch.ToColumnMatrix());
        }
        public void SetVoicedAmps(Matrix<float> amps)
        {
            if (amps.RowCount != Length || amps.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced amps matrix size does not match existing size.");
            }
            _data.SetSubMatrix(0, Length, 1, Config.NVoiced, amps);
        }
        public void SetVoicedAmps(int index, Vector<float> amps)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (amps.Count != Config.NVoiced)
            {
                throw new ArgumentException("Voiced amps vector size does not match existing size.");
            }
            _data.SetSubMatrix(index, 1, 1, Config.NVoiced, amps.ToColumnMatrix());
        }
        public void SetVoicedAmps(int start, int end, Matrix<float> amps)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (amps.RowCount != end - start + 1 || amps.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced amps matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 1, Config.NVoiced, amps);
        }
        public void SetVoicedPhases(Matrix<float> phases)
        {
            if (phases.RowCount != Length || phases.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced phases matrix size does not match existing size.");
            }
            _data.SetSubMatrix(0, Length, 1 + Config.NVoiced, Config.NVoiced, phases);
        }
        public void SetVoicedPhases(int index, Vector<float> phases)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (phases.Count != Config.NVoiced)
            {
                throw new ArgumentException("Voiced phases vector size does not match existing size.");
            }
            _data.SetSubMatrix(index, 1, 1 + Config.NVoiced, Config.NVoiced, phases.ToColumnMatrix());
        }
        public void SetVoicedPhases(int start, int end, Matrix<float> phases)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (phases.RowCount != end - start + 1 || phases.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced phases matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 1 + Config.NVoiced, Config.NVoiced, phases);
        }
        public void SetUnvoiced(Matrix<float> unvoiced)
        {
            if (unvoiced.RowCount != Length || unvoiced.ColumnCount != Config.NUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(0, Length, 1 + 2 * Config.NVoiced, Config.NUnvoiced, unvoiced);
        }
        public void SetUnvoiced(int index, Vector<float> unvoiced)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (unvoiced.Count != Config.NUnvoiced)
            {
                throw new ArgumentException("Unvoiced vector size does not match existing size.");
            }
            _data.SetSubMatrix(index, 1, 1 + 2 * Config.NVoiced, Config.NUnvoiced, unvoiced.ToColumnMatrix());
        }
        public void SetUnvoiced(int start, int end, Matrix<float> unvoiced)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (unvoiced.RowCount != end - start + 1 || unvoiced.ColumnCount != Config.NUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 1 + 2 * Config.NVoiced, Config.NUnvoiced, unvoiced);
        }
    }

    public class EsperAudioConfig
    {
        public readonly UInt16 NVoiced;
        public readonly UInt16 NUnvoiced;
        public readonly bool IsCompressed;
        public int FrameSize()
        {
            if (IsCompressed)
            {
                return 1 + NVoiced + NUnvoiced;
            }
            else
            {
                return 1 + 2 * NVoiced + NUnvoiced;
            }
        }
        public EsperAudioConfig(UInt16 nVoiced, UInt16 nUnvoiced, bool isCompressed = false)
        {
            this.NVoiced = nVoiced;
            this.NUnvoiced = nUnvoiced;
            this.IsCompressed = isCompressed;
        }
    }

    public class CompressedEsperAudio
    {
        private Matrix<Half> _data;
        public readonly EsperAudioConfig Config;
        public readonly int Length;
        public readonly int CompressedLength;
        public readonly int TemporalCompression;
        public readonly int SpectralCompression;

        // Constructor
        public CompressedEsperAudio(int length, int temporalCompression, int spectralCompression, EsperAudioConfig config)
        {
            this.Config = config;
            this.Length = length;
            this.CompressedLength = length / temporalCompression;
            this.TemporalCompression = temporalCompression;
            this.SpectralCompression = spectralCompression;
            this._data = Matrix<Half>.Build.Dense(CompressedLength, config.FrameSize(), (Half)0.0f);
        }
        public CompressedEsperAudio(CompressedEsperAudio audio)
        {
            this.Config = audio.Config;
            this.Length = audio.Length;
            this.CompressedLength = audio.CompressedLength;
            this.TemporalCompression = audio.TemporalCompression;
            this.SpectralCompression = audio.SpectralCompression;
            this._data = audio._data.Clone();
        }

        // Getters
        public Matrix<Half> GetFrames()
        {
            return _data;
        }
        public Vector<Half> GetFrames(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index);
        }
        public Matrix<Half> GetFrames(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 0, Config.FrameSize());
        }
        public Vector<Half> GetPitch()
        {
            return _data.Column(0);
        }
        public Half GetPitch(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data[index, 0];
        }
        public Vector<Half> GetPitch(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 0, 1).Column(0);
        }
        public Matrix<Half> GetVoiced()
        {
            return _data.SubMatrix(0, CompressedLength, 1, Config.NVoiced);
        }
        public Vector<Half> GetVoiced(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index).SubVector(1, Config.NVoiced);
        }
        public Matrix<Half> GetVoiced(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 1, Config.NVoiced);
        }
        public Matrix<Half> GetUnvoiced()
        {
            return _data.SubMatrix(0, CompressedLength, 1 + Config.NVoiced, Config.NUnvoiced);
        }
        public Vector<Half> GetUnvoiced(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index).SubVector(1 + Config.NVoiced, Config.NUnvoiced);
        }
        public Matrix<Half> GetUnvoiced(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start + 1, 1 + Config.NVoiced, Config.NUnvoiced);
        }

        // Setters
        public void SetFrames(Matrix<Half> data)
        {
            if (data.ColumnCount != Config.FrameSize() || data.RowCount != CompressedLength)
            {
                throw new ArgumentException("Data matrix size does not match existing size.");
            }
            this._data = data;
        }
        public void SetFrames(int index, Vector<Half> frame)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (frame.Count != Config.FrameSize())
            {
                throw new ArgumentException("Frame size does not match existing size.");
            }
            _data.SetRow(index, frame);
        }
        public void SetFrames(int start, int end, Matrix<Half> frames)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (frames.RowCount != end - start + 1 || frames.ColumnCount != Config.FrameSize())
            {
                throw new ArgumentException("Frames size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 0, Config.FrameSize(), frames);
        }
        public void SetPitch(Vector<Half> pitch)
        {
            if (pitch.Count != CompressedLength)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            _data.SetColumn(0, pitch);
        }
        public void SetPitch(int index, Half pitch)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            _data[index, 0] = pitch;
        }
        public void SetPitch(int start, int end, Vector<Half> pitch)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (pitch.Count != end - start + 1)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 0, 1, pitch.ToColumnMatrix());
        }
        public void SetVoiced(Matrix<Half> voiced)
        {
            if (voiced.RowCount != CompressedLength || voiced.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(0, CompressedLength, 1, Config.NVoiced, voiced);
        }
        public void SetVoiced(int index, Vector<Half> voiced)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (voiced.Count != Config.NVoiced)
            {
                throw new ArgumentException("Voiced vector size does not match existing size.");
            }
            _data.SetSubMatrix(index, 1, 1, Config.NVoiced, voiced.ToColumnMatrix());
        }
        public void SetVoiced(int start, int end, Matrix<Half> voiced)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (voiced.RowCount != end - start + 1 || voiced.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 1, Config.NVoiced, voiced);
        }
        public void SetUnvoiced(Matrix<Half> unvoiced)
        {
            if (unvoiced.RowCount != CompressedLength || unvoiced.ColumnCount != Config.NUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(0, CompressedLength, 1 + Config.NVoiced, Config.NUnvoiced, unvoiced);
        }
        public void SetUnvoiced(int index, Vector<Half> unvoiced)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (unvoiced.Count != Config.NUnvoiced)
            {
                throw new ArgumentException("Unvoiced vector size does not match existing size.");
            }
            _data.SetSubMatrix(index, 1, 1 + Config.NVoiced, Config.NUnvoiced, unvoiced.ToColumnMatrix());
        }
        public void SetUnvoiced(int start, int end, Matrix<Half> unvoiced)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (unvoiced.RowCount != end - start + 1 || unvoiced.ColumnCount != Config.NUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start + 1, 1 + Config.NVoiced, Config.NUnvoiced, unvoiced);
        }
    }
}
