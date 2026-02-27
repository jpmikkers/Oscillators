using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Baksteen.Oscillators;

/// <summary>
/// A (ring)buffer similar to a Pipeline except it is typed. It is designed for a single producer and a single consumer, but it 
/// is thread safe in that the producer and consumer can be on different threads.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SpanRing<T>
{
    private readonly object _lock = new();

    private readonly T[] _buffer;

    private int _writePos;
    private int _readPos;
    private int _filled;

    public SpanRing(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        _buffer = new T[size];
    }

    public Span<T> GetWriteSpan(int requested)
    {
        lock (_lock)
        {
            var freespace = _buffer.Length - _filled;

            if(requested > freespace)
            {
                requested = freespace;
            }

            if (_writePos + requested > _buffer.Length)
            {
                requested = _buffer.Length - _writePos;
            }

            return _buffer.AsSpan(_writePos, requested);
        }
    }

    public void AdvanceWrite(int written)
    {
        lock (_lock)
        {
            if(written < 0 || written > _buffer.Length - _filled || _writePos + written > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(written));
            }
            _writePos = (_writePos + written) % _buffer.Length;
            _filled += written;
        }
    }

    public ReadOnlySpan<T> GetReadSpan(int requested)
    {
        lock (_lock)
        {
            if (requested > _filled)
            {
                requested = _filled;
            }

            if (_readPos + requested > _buffer.Length)
            {
                requested = _buffer.Length - _readPos;
            }

            return _buffer.AsSpan(_readPos, requested);
        }
    }

    public void AdvanceRead(int read)
    {
        lock (_lock)
        {
            if(read < 0 || read > _filled || _readPos + read > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(read));
            }
            _readPos = (_readPos + read) % _buffer.Length;
            _filled -= read;
        }
    }
}
