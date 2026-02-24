using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Baksteen.Oscillators;

/// <summary>
/// A (ring)buffer similar to a Pipeline except it is typed and it can always present a contiguous span for reading and writing.
/// It is designed for a single producer and a single consumer, but it is thread safe in that the producer and consumer can be 
/// on different threads.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SpanRing<T>
{
    private readonly object _lock = new();

    private readonly T[] _buffer;
    private readonly int _maxSpan;
    private readonly int _size;

    private int _writePos;
    private int _readPos;
    private int _filled;
    private int _pendingWrite = -1;
    private int _pendingRead = -1;

    public SpanRing(int size, int maxSpan)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        ArgumentOutOfRangeException.ThrowIfNegative(maxSpan);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, maxSpan);

        _buffer = new T[size + maxSpan];
        _size = size;
        _maxSpan = maxSpan;
    }

    public Span<T> GetWriteSpan(int requested)
    {
        if (requested > _maxSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(requested), $"Requested span exceeds maximum allowed span of {_maxSpan}.");
        }

        lock (_lock)
        {
            if(_pendingWrite >= 0)
            {
                throw new InvalidOperationException("Must commit or cancel previous write before requesting a new span.");
            }

            var availablespace = _size - _filled;
            if (requested > availablespace) requested = availablespace;

            _pendingWrite = requested;
            return _buffer.AsSpan(_writePos, requested);
        }
    }

    public void WriteCommit()
    {
        lock (_lock)
        {
            if(_pendingWrite < 0) throw new InvalidOperationException("No pending write to commit.");

            if (_writePos + _pendingWrite > _size)
            {
                // the write wrapped around, so we need to copy the overflow to the start of the buffer
                var overflow = (_writePos + _pendingWrite) - _size;
                var overflowSpan = _buffer.AsSpan(_size, overflow);
                overflowSpan.CopyTo(_buffer.AsSpan(0));
            }
                
            _writePos = (_writePos + _pendingWrite) % _size;
            _filled += _pendingWrite;
            _pendingWrite = -1;
        }
    }

    public void WriteCancel()
    {
        lock (_lock)
        {
            if(_pendingWrite < 0) throw new InvalidOperationException("No pending write to cancel.");
            _pendingWrite = -1;
        }
    }

    public ReadOnlySpan<T> GetReadSpan(int requested)
    {
        if (requested > _maxSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(requested), $"Requested span exceeds maximum allowed span of {_maxSpan}.");
        }

        lock (_lock)
        {
            if (_pendingRead >= 0)
            {
                throw new InvalidOperationException("Must commit or cancel previous read before requesting a new span.");
            }

            if (requested > _filled) requested = _filled;

            if (_readPos + requested > _size)
            {
                // the read wraps around, so copy the overflow to the end so we can return a contiguous span
                var overflow = (_readPos + requested) - _size;
                var overflowSpan = _buffer.AsSpan(0, overflow);
                overflowSpan.CopyTo(_buffer.AsSpan(_size));
            }

            _pendingRead = requested;
            return _buffer.AsSpan(_readPos, requested);
        }
    }

    public void ReadCommit()
    {
        lock (_lock)
        {
            if (_pendingRead < 0) throw new InvalidOperationException("No pending read to commit.");
            _readPos = (_readPos + _pendingRead) % _size;
            _filled -= _pendingRead;
            _pendingRead = -1;
        }
    }

    public void ReadCancel()
    {
        lock (_lock)
        {
            if (_pendingRead < 0) throw new InvalidOperationException("No pending read to cancel.");
            _pendingRead = -1;
        }
    }
}