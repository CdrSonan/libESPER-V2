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
            this.Config = new EsperAudioConfig(config.NVoiced, config.NUnvoiced, config.StepSize);
            if (data.ColumnCount != Config.FrameSize())
            {
                throw new ArgumentException("Data matrix column count does not match frame size.");
            }
            this.Length = data.RowCount;
            this._data = data;
        }
        public EsperAudio(int length, EsperAudioConfig config)
        {
            this.Config = new EsperAudioConfig(config.NVoiced, config.NUnvoiced, config.StepSize);
            this.Length = length;
            this._data = Matrix<float>.Build.Dense(length, Config.FrameSize(), 0.0f);
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
            var pitch = GetPitch();
            pitch.MapInplace(x => x < 0 ? 0 : x); // Ensure the pitch is non-negative
            SetPitch(pitch);
            var amps = GetVoicedAmps();
            amps.MapInplace(x => x < 0 ? 0 : x); // Ensure amplitudes are non-negative
            SetVoicedAmps(amps);
            var phases = GetVoicedPhases();
            phases.MapInplace(x => x % (2 * (float)Math.PI));
            phases.MapInplace(x => x < -Math.PI ? x + 2 * (float)Math.PI : x);
            phases.MapInplace(x => x > Math.PI ? x - 2 * (float)Math.PI : x);
            SetVoicedPhases(phases);
            var unvoiced = GetUnvoiced();
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
            return _data.SubMatrix(start, end - start, 0, Config.FrameSize());
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
            return _data.SubMatrix(start, end - start, 0, 1).Column(0);
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
            return _data.SubMatrix(start, end - start, 1, Config.NVoiced);
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
            return _data.SubMatrix(start, end - start, 1 + Config.NVoiced, Config.NVoiced);
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
            return _data.SubMatrix(start, end - start, 1 + 2 * Config.NVoiced, Config.NUnvoiced);
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
            if (frames.RowCount != end - start || frames.ColumnCount != Config.FrameSize())
            {
                throw new ArgumentException("Frames size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 0, Config.FrameSize(), frames);
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
            if (pitch.Count != end - start)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 0, 1, pitch.ToColumnMatrix());
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
            _data.SetSubMatrix(index, 1, 1, Config.NVoiced, amps.ToRowMatrix());
        }
        public void SetVoicedAmps(int start, int end, Matrix<float> amps)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (amps.RowCount != end - start || amps.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced amps matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 1, Config.NVoiced, amps);
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
            _data.SetSubMatrix(index, 1, 1 + Config.NVoiced, Config.NVoiced, phases.ToRowMatrix());
        }
        public void SetVoicedPhases(int start, int end, Matrix<float> phases)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (phases.RowCount != end - start || phases.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced phases matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 1 + Config.NVoiced, Config.NVoiced, phases);
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
            _data.SetSubMatrix(index, 1, 1 + 2 * Config.NVoiced, Config.NUnvoiced, unvoiced.ToRowMatrix());
        }
        public void SetUnvoiced(int start, int end, Matrix<float> unvoiced)
        {
            if (start < 0 || end >= Length || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (unvoiced.RowCount != end - start || unvoiced.ColumnCount != Config.NUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 1 + 2 * Config.NVoiced, Config.NUnvoiced, unvoiced);
        }
    }

    public class EsperAudioConfig
    {
        public readonly ushort NVoiced;
        public readonly ushort NUnvoiced;
        public readonly int StepSize;

        public EsperAudioConfig(ushort nVoiced, ushort nUnvoiced, int stepSize)
        {
            if (nVoiced < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nVoiced), "NVoiced must be greater than zero.");
            }
            if (nUnvoiced < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nUnvoiced), "NUnvoiced must be greater than zero.");
            }
            if (stepSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize), "StepSize must be greater than zero.");
            }

            if (stepSize > 2 * nUnvoiced - 2)
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize), "Step size too large relative to unvoiced size.");
            }
            this.NVoiced = nVoiced;
            this.NUnvoiced = nUnvoiced;
            this.StepSize = stepSize;
        }

        public EsperAudioConfig(CompressedEsperAudioConfig config)
        {
            this.NVoiced = config.NVoiced;
            this.NUnvoiced = config.NUnvoiced;
            this.StepSize = config.StepSize;
        }

        public override bool Equals(object? obj)
        {
            if (obj is EsperAudioConfig other)
                return this.Equals(other);
            return false;
        }

        private bool Equals(EsperAudioConfig other)
        {
            return NVoiced == other.NVoiced && NUnvoiced == other.NUnvoiced && StepSize == other.StepSize;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NVoiced, NUnvoiced, StepSize);
        }

        public int FrameSize()
        {
            return 1 + 2 * NVoiced + NUnvoiced;
        }
    }

    public class CompressedEsperAudio
    {
        private Matrix<float> _data;
        public readonly CompressedEsperAudioConfig Config;
        public readonly int Length;
        public readonly int CompressedLength;

        // Constructor
        public CompressedEsperAudio(int length, CompressedEsperAudioConfig config)
        {
            this.Config = config;
            this.Length = length;
            this.CompressedLength = length / Config.TemporalCompression;
            this._data = Matrix<float>.Build.Dense(CompressedLength, Config.FrameSize(), 0.0f);
        }
        public CompressedEsperAudio(int length, CompressedEsperAudioConfig config, Matrix<float> data)
        {
            if (data.RowCount != length / config.TemporalCompression)
                throw new ArgumentException("Compressed array size does not match existing size. (frame count)");
            if (data.ColumnCount != config.FrameSize())
                throw new ArgumentException("Compressed array size does not match existing size. (frame size)");
            this.Config = config;
            this.Length = length;
            this.CompressedLength = length / config.TemporalCompression;
            this._data = data;
        }
        public CompressedEsperAudio(CompressedEsperAudio audio)
        {
            this.Config = audio.Config;
            this.Length = audio.Length;
            this.CompressedLength = audio.CompressedLength;
            this._data = audio._data.Clone();
        }

        // Getters
        public Matrix<float> GetFrames()
        {
            return _data;
        }
        public Vector<float> GetFrames(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index);
        }
        public Matrix<float> GetFrames(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start, 0, Config.FrameSize());
        }
        public Vector<float> GetPitch()
        {
            return _data.Column(0);
        }
        public float GetPitch(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data[index, 0];
        }
        public Vector<float> GetPitch(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start, 0, 1).Column(0);
        }
        public Matrix<float> GetVoiced()
        {
            return _data.SubMatrix(0, CompressedLength, 1, Config.NVoiced);
        }
        public Vector<float> GetVoiced(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index).SubVector(1, Config.NVoiced);
        }
        public Matrix<float> GetVoiced(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start, end - start, 1, Config.NVoiced);
        }
        public Matrix<float> GetUnvoiced()
        {
            return _data.SubMatrix(0,
                CompressedLength,
                1 + Config.NVoiced,
                Config.NUnvoiced / Config.SpectralCompression);
        }
        public Vector<float> GetUnvoiced(int index)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            return _data.Row(index).SubVector(1 + Config.NVoiced, Config.NUnvoiced / Config.SpectralCompression);
        }
        public Matrix<float> GetUnvoiced(int start, int end)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            return _data.SubMatrix(start,
                end - start,
                1 + Config.NVoiced,
                Config.NUnvoiced / Config.SpectralCompression);
        }

        // Setters
        public void SetFrames(Matrix<float> data)
        {
            if (data.ColumnCount != Config.FrameSize() || data.RowCount != CompressedLength)
            {
                throw new ArgumentException("Data matrix size does not match existing size.");
            }
            this._data = data;
        }
        public void SetFrames(int index, Vector<float> frame)
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
        public void SetFrames(int start, int end, Matrix<float> frames)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (frames.RowCount != end - start || frames.ColumnCount != Config.FrameSize())
            {
                throw new ArgumentException("Frames size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 0, Config.FrameSize(), frames);
        }
        public void SetPitch(Vector<float> pitch)
        {
            if (pitch.Count != CompressedLength)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            _data.SetColumn(0, pitch);
        }
        public void SetPitch(int index, float pitch)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            _data[index, 0] = pitch;
        }
        public void SetPitch(int start, int end, Vector<float> pitch)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (pitch.Count != end - start)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 0, 1, pitch.ToColumnMatrix());
        }
        public void SetVoiced(Matrix<float> voiced)
        {
            if (voiced.RowCount != CompressedLength || voiced.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(0, CompressedLength, 1, Config.NVoiced, voiced);
        }
        public void SetVoiced(int index, Vector<float> voiced)
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
        public void SetVoiced(int start, int end, Matrix<float> voiced)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (voiced.RowCount != end - start || voiced.ColumnCount != Config.NVoiced)
            {
                throw new ArgumentException("Voiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(start, end - start, 1, Config.NVoiced, voiced);
        }
        public void SetUnvoiced(Matrix<float> unvoiced)
        {
            if (unvoiced.RowCount != CompressedLength || unvoiced.ColumnCount != Config.NUnvoiced / Config.SpectralCompression)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(
                0,
                CompressedLength,
                1 + Config.NVoiced,
                Config.NUnvoiced / Config.SpectralCompression,
                unvoiced);
        }
        public void SetUnvoiced(int index, Vector<float> unvoiced)
        {
            if (index < 0 || index >= CompressedLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
            }
            if (unvoiced.Count != Config.NUnvoiced / Config.SpectralCompression)
            {
                throw new ArgumentException("Unvoiced vector size does not match existing size.");
            }
            _data.SetSubMatrix(
                index,
                1,
                1 + Config.NVoiced,
                Config.NUnvoiced / Config.SpectralCompression,
                unvoiced.ToColumnMatrix());
        }
        public void SetUnvoiced(int start, int end, Matrix<float> unvoiced)
        {
            if (start < 0 || end >= CompressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start or end index out of range.");
            }
            if (unvoiced.RowCount != end - start || unvoiced.ColumnCount != Config.NUnvoiced / Config.SpectralCompression)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            _data.SetSubMatrix(
                start,
                end - start,
                1 + Config.NVoiced,
                Config.NUnvoiced / Config.SpectralCompression,
                unvoiced);
        }
    }
    
    public class CompressedEsperAudioConfig
    {
        public readonly ushort NVoiced;
        public readonly ushort NUnvoiced;
        public readonly int StepSize;
        public readonly int TemporalCompression;
        public readonly int SpectralCompression;

        public CompressedEsperAudioConfig(ushort nVoiced, ushort nUnvoiced, int stepSize, int temporalCompression, int spectralCompression)
        {
            if (nVoiced < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nVoiced), "NVoiced must be greater than zero.");
            }
            if (nUnvoiced < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nUnvoiced), "NUnvoiced must be greater than zero.");
            }
            if (stepSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize), "StepSize must be greater than zero.");
            }

            if (stepSize > 2 * nUnvoiced - 2)
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize), "Step size too large relative to unvoiced size.");
            }
            this.NVoiced = nVoiced;
            this.NUnvoiced = nUnvoiced;
            this.StepSize = stepSize;
            this.TemporalCompression = temporalCompression;
            this.SpectralCompression = spectralCompression;
        }

        public CompressedEsperAudioConfig(EsperAudioConfig config, int temporalCompression, int spectralCompression)
        {
            this.NVoiced = config.NVoiced;
            this.NUnvoiced = config.NUnvoiced;
            this.StepSize = config.StepSize;
            this.TemporalCompression = temporalCompression;
            this.SpectralCompression = spectralCompression;
        }

        public override bool Equals(object? obj)
        {
            if (obj is CompressedEsperAudioConfig other)
                return this.Equals(other);
            return false;
        }

        private bool Equals(CompressedEsperAudioConfig other)
        {
            return NVoiced == other.NVoiced &&
                   NUnvoiced == other.NUnvoiced &&
                   StepSize == other.StepSize &&
                   SpectralCompression == other.SpectralCompression &&
                   TemporalCompression == other.TemporalCompression;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NVoiced, NUnvoiced, StepSize, SpectralCompression, TemporalCompression);
        }

        public int FrameSize()
        {
            return 1 + NVoiced + NUnvoiced / SpectralCompression;
        }
    }
}
