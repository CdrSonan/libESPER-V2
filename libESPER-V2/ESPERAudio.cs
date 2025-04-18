using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.LinearAlgebra;

namespace libESPER_V2
{
    public class ESPERAudio
    {
        private Matrix<float> data;
        public readonly ESPERAudioConfig config;
        public readonly int length;

        // Constructors
        public ESPERAudio(Matrix<float> data, ESPERAudioConfig config)
        {
            this.config = config;
            if (data.ColumnCount != config.FrameSize())
            {
                throw new ArgumentException("Data matrix column count does not match frame size.");
            }
            this.length = data.RowCount;
            this.data = data;
        }
        public ESPERAudio(int length, ESPERAudioConfig config)
        {
            this.config = config;
            this.length = length;
            this.data = Matrix<float>.Build.Dense(length, config.FrameSize(), 0.0f);
        }

        public ESPERAudio(ESPERAudio audio)
        {
            this.config = audio.config;
            this.length = audio.length;
            this.data = audio.data.Clone();
        }
        // Validation
        public void validate()
        {
            Vector<float> pitch = getPitch();
            pitch.MapInplace(x => x < 0 ? 0 : x); // Ensure pitch is non-negative
            setPitch(pitch);
            Matrix<float> amps = getVoicedAmps();
            amps.MapInplace(x => x < 0 ? 0 : x); // Ensure amplitudes are non-negative
            setVoicedAmps(amps);
            Matrix<float> phases = getVoicedPhases();
            phases.MapInplace(x => x < -(float)Math.PI || x > (float)Math.PI ? x % (2 * (float)Math.PI) : x); // Ensure phases are within -pi to pi
            setVoicedPhases(phases);
            Matrix<float> unvoiced = getUnvoiced();
            unvoiced.MapInplace(x => x < 0 ? 0 : x); // Ensure unvoiced amplitudes are non-negative
            setUnvoiced(unvoiced);
        }

        // Getters
        public Matrix<float> getFrames()
        {
            return data;
        }
        public Vector<float> getFrames(int index)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data.Row(index);
        }
        public Matrix<float> getFrames(int start, int end)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 0, config.FrameSize());
        }
        public Vector<float> getPitch()
        {
            return data.Column(0);
        }
        public float getPitch(int index)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data[index, 0];
        }
        public Vector<float> getPitch(int start, int end)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 0, 1).Column(0);
        }
        public Matrix<float> getVoicedAmps()
        {
            return data.SubMatrix(0, length, 1, config.nVoiced);
        }
        public Vector<float> getVoicedAmps(int index)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data.Row(index).SubVector(1, config.nVoiced);
        }
        public Matrix<float> getVoicedAmps(int start, int end)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 1, config.nVoiced);
        }
        public Matrix<float> getVoicedPhases()
        {
            return data.SubMatrix(0, length, 1 + config.nVoiced, config.nVoiced);
        }
        public Vector<float> getVoicedPhases(int index)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data.Row(index).SubVector(1 + config.nVoiced, config.nVoiced);
        }
        public Matrix<float> getVoicedPhases(int start, int end)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 1 + config.nVoiced, config.nVoiced);
        }
        public Matrix<float> getUnvoiced()
        {
            return data.SubMatrix(0, length, 1 + 2 * config.nVoiced, config.nUnvoiced);
        }
        public Vector<float> getUnvoiced(int index)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data.Row(index).SubVector(1 + 2 * config.nVoiced, config.nUnvoiced);
        }
        public Matrix<float> getUnvoiced(int start, int end)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 1 + 2 * config.nVoiced, config.nUnvoiced);
        }

        // Setters
        public void setFrames(Matrix<float> data)
        {
            if (data.ColumnCount != config.FrameSize() || data.RowCount != length)
            {
                throw new ArgumentException("Data matrix size does not match existing size.");
            }
            this.data = data;
        }
        public void setFrames(int index, Vector<float> frame)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            if (frame.Count != config.FrameSize())
            {
                throw new ArgumentException("Frame size does not match existing size.");
            }
            data.SetRow(index, frame);
        }
        public void setFrames(int start, int end, Matrix<float> frames)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (frames.RowCount != end - start + 1 || frames.ColumnCount != config.FrameSize())
            {
                throw new ArgumentException("Frames size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 0, config.FrameSize(), frames);
        }
        public void setPitch(Vector<float> pitch)
        {
            if (pitch.Count != length)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            data.SetColumn(0, pitch);
        }
        public void setPitch(int index, float pitch)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            data[index, 0] = pitch;
        }
        public void setPitch(int start, int end, Vector<float> pitch)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (pitch.Count != end - start + 1)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 0, 1, pitch.ToColumnMatrix());
        }
        public void setVoicedAmps(Matrix<float> amps)
        {
            if (amps.RowCount != length || amps.ColumnCount != config.nVoiced)
            {
                throw new ArgumentException("Voiced amps matrix size does not match existing size.");
            }
            data.SetSubMatrix(0, length, 1, config.nVoiced, amps);
        }
        public void setVoicedAmps(int index, Vector<float> amps)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            if (amps.Count != config.nVoiced)
            {
                throw new ArgumentException("Voiced amps vector size does not match existing size.");
            }
            data.SetSubMatrix(index, 1, 1, config.nVoiced, amps.ToColumnMatrix());
        }
        public void setVoicedAmps(int start, int end, Matrix<float> amps)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (amps.RowCount != end - start + 1 || amps.ColumnCount != config.nVoiced)
            {
                throw new ArgumentException("Voiced amps matrix size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 1, config.nVoiced, amps);
        }
        public void setVoicedPhases(Matrix<float> phases)
        {
            if (phases.RowCount != length || phases.ColumnCount != config.nVoiced)
            {
                throw new ArgumentException("Voiced phases matrix size does not match existing size.");
            }
            data.SetSubMatrix(0, length, 1 + config.nVoiced, config.nVoiced, phases);
        }
        public void setVoicedPhases(int index, Vector<float> phases)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            if (phases.Count != config.nVoiced)
            {
                throw new ArgumentException("Voiced phases vector size does not match existing size.");
            }
            data.SetSubMatrix(index, 1, 1 + config.nVoiced, config.nVoiced, phases.ToColumnMatrix());
        }
        public void setVoicedPhases(int start, int end, Matrix<float> phases)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (phases.RowCount != end - start + 1 || phases.ColumnCount != config.nVoiced)
            {
                throw new ArgumentException("Voiced phases matrix size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 1 + config.nVoiced, config.nVoiced, phases);
        }
        public void setUnvoiced(Matrix<float> unvoiced)
        {
            if (unvoiced.RowCount != length || unvoiced.ColumnCount != config.nUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            data.SetSubMatrix(0, length, 1 + 2 * config.nVoiced, config.nUnvoiced, unvoiced);
        }
        public void setUnvoiced(int index, Vector<float> unvoiced)
        {
            if (index < 0 || index >= length)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            if (unvoiced.Count != config.nUnvoiced)
            {
                throw new ArgumentException("Unvoiced vector size does not match existing size.");
            }
            data.SetSubMatrix(index, 1, 1 + 2 * config.nVoiced, config.nUnvoiced, unvoiced.ToColumnMatrix());
        }
        public void setUnvoiced(int start, int end, Matrix<float> unvoiced)
        {
            if (start < 0 || end >= length || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (unvoiced.RowCount != end - start + 1 || unvoiced.ColumnCount != config.nUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 1 + 2 * config.nVoiced, config.nUnvoiced, unvoiced);
        }
    }

    public class ESPERAudioConfig
    {
        public readonly UInt16 nVoiced;
        public readonly UInt16 nUnvoiced;
        public readonly bool isCompressed;
        public int FrameSize()
        {
            if (isCompressed)
            {
                return 1 + nVoiced + nUnvoiced;
            }
            else
            {
                return 1 + 2 * nVoiced + nUnvoiced;
            }
        }
        public ESPERAudioConfig(UInt16 nVoiced, UInt16 nUnvoiced)
        {
            this.nVoiced = nVoiced;
            this.nUnvoiced = nUnvoiced;
        }
    }

    public class CompressedESPERAudio
    {
        private Matrix<Half> data;
        public readonly ESPERAudioConfig config;
        public readonly int length;
        public readonly int compressedLength;
        public readonly int temporalCompression;
        public readonly int spectralCompression;

        // Constructor
        public CompressedESPERAudio(int length, int temporalCompression, int spectralCompression, ESPERAudioConfig config)
        {
            this.config = config;
            this.length = length;
            this.compressedLength = length / temporalCompression;
            this.temporalCompression = temporalCompression;
            this.spectralCompression = spectralCompression;
            this.data = Matrix<Half>.Build.Dense(compressedLength, config.FrameSize(), (Half)0.0f);
        }
        public CompressedESPERAudio(CompressedESPERAudio audio)
        {
            this.config = audio.config;
            this.length = audio.length;
            this.compressedLength = audio.compressedLength;
            this.temporalCompression = audio.temporalCompression;
            this.spectralCompression = audio.spectralCompression;
            this.data = audio.data.Clone();
        }

        // Getters
        public Matrix<Half> getFrames()
        {
            return data;
        }
        public Vector<Half> getFrames(int index)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data.Row(index);
        }
        public Matrix<Half> getFrames(int start, int end)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 0, config.FrameSize());
        }
        public Vector<Half> getPitch()
        {
            return data.Column(0);
        }
        public Half getPitch(int index)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data[index, 0];
        }
        public Vector<Half> getPitch(int start, int end)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 0, 1).Column(0);
        }
        public Matrix<Half> getVoiced()
        {
            return data.SubMatrix(0, compressedLength, 1, config.nVoiced);
        }
        public Vector<Half> getVoiced(int index)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data.Row(index).SubVector(1, config.nVoiced);
        }
        public Matrix<Half> getVoiced(int start, int end)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 1, config.nVoiced);
        }
        public Matrix<Half> getUnvoiced()
        {
            return data.SubMatrix(0, compressedLength, 1 + config.nVoiced, config.nUnvoiced);
        }
        public Vector<Half> getUnvoiced(int index)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            return data.Row(index).SubVector(1 + config.nVoiced, config.nUnvoiced);
        }
        public Matrix<Half> getUnvoiced(int start, int end)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            return data.SubMatrix(start, end - start + 1, 1 + config.nVoiced, config.nUnvoiced);
        }

        // Setters
        public void setFrames(Matrix<Half> data)
        {
            if (data.ColumnCount != config.FrameSize() || data.RowCount != compressedLength)
            {
                throw new ArgumentException("Data matrix size does not match existing size.");
            }
            this.data = data;
        }
        public void setFrames(int index, Vector<Half> frame)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            if (frame.Count != config.FrameSize())
            {
                throw new ArgumentException("Frame size does not match existing size.");
            }
            data.SetRow(index, frame);
        }
        public void setFrames(int start, int end, Matrix<Half> frames)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (frames.RowCount != end - start + 1 || frames.ColumnCount != config.FrameSize())
            {
                throw new ArgumentException("Frames size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 0, config.FrameSize(), frames);
        }
        public void setPitch(Vector<Half> pitch)
        {
            if (pitch.Count != compressedLength)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            data.SetColumn(0, pitch);
        }
        public void setPitch(int index, Half pitch)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            data[index, 0] = pitch;
        }
        public void setPitch(int start, int end, Vector<Half> pitch)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (pitch.Count != end - start + 1)
            {
                throw new ArgumentException("Pitch vector size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 0, 1, pitch.ToColumnMatrix());
        }
        public void setVoiced(Matrix<Half> voiced)
        {
            if (voiced.RowCount != compressedLength || voiced.ColumnCount != config.nVoiced)
            {
                throw new ArgumentException("Voiced matrix size does not match existing size.");
            }
            data.SetSubMatrix(0, compressedLength, 1, config.nVoiced, voiced);
        }
        public void setVoiced(int index, Vector<Half> voiced)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            if (voiced.Count != config.nVoiced)
            {
                throw new ArgumentException("Voiced vector size does not match existing size.");
            }
            data.SetSubMatrix(index, 1, 1, config.nVoiced, voiced.ToColumnMatrix());
        }
        public void setVoiced(int start, int end, Matrix<Half> voiced)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (voiced.RowCount != end - start + 1 || voiced.ColumnCount != config.nVoiced)
            {
                throw new ArgumentException("Voiced matrix size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 1, config.nVoiced, voiced);
        }
        public void setUnvoiced(Matrix<Half> unvoiced)
        {
            if (unvoiced.RowCount != compressedLength || unvoiced.ColumnCount != config.nUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            data.SetSubMatrix(0, compressedLength, 1 + config.nVoiced, config.nUnvoiced, unvoiced);
        }
        public void setUnvoiced(int index, Vector<Half> unvoiced)
        {
            if (index < 0 || index >= compressedLength)
            {
                throw new ArgumentOutOfRangeException("Index out of range.");
            }
            if (unvoiced.Count != config.nUnvoiced)
            {
                throw new ArgumentException("Unvoiced vector size does not match existing size.");
            }
            data.SetSubMatrix(index, 1, 1 + config.nVoiced, config.nUnvoiced, unvoiced.ToColumnMatrix());
        }
        public void setUnvoiced(int start, int end, Matrix<Half> unvoiced)
        {
            if (start < 0 || end >= compressedLength || start > end)
            {
                throw new ArgumentOutOfRangeException("Start or end index out of range.");
            }
            if (unvoiced.RowCount != end - start + 1 || unvoiced.ColumnCount != config.nUnvoiced)
            {
                throw new ArgumentException("Unvoiced matrix size does not match existing size.");
            }
            data.SetSubMatrix(start, end - start + 1, 1 + config.nVoiced, config.nUnvoiced, unvoiced);
        }
    }
}
