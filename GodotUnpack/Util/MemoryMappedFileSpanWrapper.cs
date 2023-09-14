using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace GodotUnpack.Util
{
    public unsafe class MemoryMappedFileSpanWrapper : IDisposable
    {
        private readonly MemoryMappedFileAccess _access;
        private readonly MemoryMappedFile _file;
        private MemoryMappedViewAccessor? _accessor;
        private byte* _pointer;
        private readonly long _length;

        private static (MemoryMappedFile file, long length) OpenExistingMmf(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                throw new FileNotFoundException("File not found", path);

            var len = info.Length;
            var f = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, len, MemoryMappedFileAccess.Read);

            return (f, len);
        }

        private MemoryMappedFileSpanWrapper((MemoryMappedFile file, long length) data, MemoryMappedFileAccess access)
        {
            _access = access;
            _length = data.length;
            _file = data.file;
        }

        public MemoryMappedFileSpanWrapper(string path) : this(OpenExistingMmf(path), MemoryMappedFileAccess.Read) { }

        public MemoryMappedFileSpanWrapper(string path, long newFileSize, bool create = true) : this((MemoryMappedFile.CreateFromFile(path, create ? FileMode.Create : FileMode.Open, null, newFileSize, MemoryMappedFileAccess.ReadWrite), newFileSize), MemoryMappedFileAccess.ReadWrite) { }

        public long FileSize => _length;

        public void Dispose()
        {
            ReleaseView();
            _file.Dispose();
        }

        private void EnsureReadPointer()
        {
            if(_pointer != null)
                return;
            
            _accessor = _file.CreateViewAccessor(0, _length, _access);
            
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);

            if (_pointer == null)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                _accessor.Dispose();
                _file.Dispose();
                throw new InvalidOperationException("Failed to acquire pointer to memory mapped file");
            }
        }
        
        private Span<byte> GetSpan(long offset, long length)
        {
            EnsureReadPointer();

            if (offset < 0 || offset >= _length)
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset {offset} is out of range [0, {_length})");
            if (length < 0 || offset + length > _length)
                throw new ArgumentOutOfRangeException(nameof(length), $"Length {length} is out of range [0, {_length - offset}) given offset {offset} and total file length {_length}");
            if(length > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(length), $"Length must be less than int.MaxValue, was {length}");
            
            return new(_pointer + offset, (int) length);
        }
        
        public Span<byte> GetSpan(long offset) => GetSpan(offset, _length - offset);

        public ReadOnlySpan<byte> GetReadOnlySpan(long offset, long length) => GetSpan(offset, length);
        
        public T Read<T>(long offset) where T : unmanaged => MemoryMarshal.Read<T>(GetReadOnlySpan(offset, sizeof(T)));
        
        public void Write<T>(long offset, T value) where T : unmanaged => MemoryMarshal.Write(GetSpan(offset, sizeof(T)), ref value);
        
        public void Write(long offset, ReadOnlySpan<byte> span) => span.CopyTo(GetSpan(offset, span.Length));

        public void CopyTo(Stream other, long start, long length = -1)
        {
            if(length == -1)
                length = _length - start;

            //Have to chunk into 2GB chunks because of the 32 bit int limit
            const int chunkSize = int.MaxValue;
            var chunks = length / chunkSize;
            var remainder = length % chunkSize;
            
            for (var i = 0; i < chunks; i++)
            {
                var chunkOffset = start + i * chunkSize;
                other.Write(GetReadOnlySpan(chunkOffset, chunkSize));
            }
            
            if(remainder > 0)
                other.Write(GetReadOnlySpan(start + chunks * chunkSize, remainder));
        }

        private Span<byte> GetBufferForCopyFrom(long pos, long length)
        {
            const int maxChunkSize = 64 * 1024 * 1024;
            
            if(_pointer != null)
                ReleaseView();

            try
            {
                length = Math.Min(maxChunkSize, length);
                _accessor = _file.CreateViewAccessor(pos, length, _access);
                byte* ptr = null;
                _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                return new(ptr, (int)length);
            }
            catch (Exception e)
            {
                throw new IOException($"Failed to get buffer to write to MMF of size {_length} at position {pos} with length {length}", e);
            }
        }
        
        private void ReleaseView()
        {
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _pointer = null;
            _accessor = null;
        }

        public void CopyFrom(Stream other, long initialPos = 0)
        {
            var pos = initialPos;
            var remainingLength = _length - initialPos;

            var buffer = GetBufferForCopyFrom(pos, remainingLength);
            
            while (true)
            {
                var read = other.Read(buffer);
                if (read == 0)
                    break;
                
                pos += read;
                remainingLength -= read;
                
                if(remainingLength <= 0)
                    break;
                
                buffer = GetBufferForCopyFrom(pos, remainingLength);
            }
            
            ReleaseView();
        }

        public void CopyFrom(MemoryMappedFileSpanWrapper other, long readPos = 0, long writePos = 0)
        {
            var pos = writePos;
            var remainingLength = Math.Min(_length - writePos, other._length - readPos);

            var buffer = GetBufferForCopyFrom(pos, remainingLength);
            
            while (true)
            {
                var read = other.GetReadOnlySpan(readPos, buffer.Length);
                read.CopyTo(buffer);
                other.ReleaseView();
                ReleaseView();

                pos += read.Length;
                remainingLength -= read.Length;
                readPos += read.Length;
                
                if(remainingLength <= 0)
                    break;
                
                buffer = GetBufferForCopyFrom(pos, remainingLength);
            }
            
            other.ReleaseView();
            ReleaseView();
        }

        public byte* GetDirectPointer() => _pointer;
        
        public SimpleReadStream CreateReadStream(long start, long length = -1)
        {
            if(length == -1)
                length = _length - start;
            
            return new SimpleReadStream(this, start, length);
        }

        public class SimpleReadStream : Stream
        {
            private readonly MemoryMappedFileSpanWrapper _mmf;
            private long _pos;
            private readonly long _length;

            internal SimpleReadStream(MemoryMappedFileSpanWrapper mmf, long start, long length)
            {
                _mmf = mmf;
                _pos = start;
                _length = length;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                //Check length and cap count
                if (_pos + count >= _mmf._length)
                {
                    var newCount = (int)(_mmf._length - _pos);
                    if (newCount > 0)
                        count = newCount; //Else just throw the ex, it's better for debug reasons
                }

                var span = _mmf.GetSpan(_pos, count);
                span.CopyTo(buffer.AsSpan(offset, count));
                _pos += count;
                return count;
            }

            public override int Read(Span<byte> buffer)
            {
                var span = _mmf.GetReadOnlySpan(_pos, buffer.Length);
                span.CopyTo(buffer);
                _pos += buffer.Length;
                return buffer.Length;
            }

            public override int ReadByte()
            {
                var b = _mmf.Read<byte>(_pos);
                _pos++;
                return b;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _pos = offset;
                        break;
                    case SeekOrigin.Current:
                        _pos += offset;
                        break;
                    case SeekOrigin.End:
                        _pos = _length + offset;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
                }

                return _pos;
            }

            public override void Flush() => throw new NotSupportedException();
            
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _length;
            public override long Position
            {
                get => _pos;
                set => _pos = value;
            }
                
        }
    }
}