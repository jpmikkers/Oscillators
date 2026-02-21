
using System.Buffers;
using System.Runtime.InteropServices;

using System;

unsafe class AlignedMemoryManager<T> : MemoryManager<T> where T : struct
{
    private readonly T* _ptr;
    private readonly int _length;
    private bool _disposed;

    public AlignedMemoryManager(int length,int alignment)
    {
        if (Marshal.SizeOf<T>() % alignment != 0)
        {
            throw new ArgumentException($"element length ({Marshal.SizeOf<T>()}) is not a multiple of alignment ({alignment}). Consider padding the element");
        }

        var numBytes = (nuint)(length * Marshal.SizeOf<T>());
        _ptr = (T *)NativeMemory.AlignedAlloc(numBytes, (nuint)alignment);
        NativeMemory.Clear(_ptr, numBytes);
        _length = length;
    }

    public override Span<T> GetSpan() => new(_ptr, _length);

    public override MemoryHandle Pin(int elementIndex = 0)
        => new(_ptr + elementIndex);

    public override void Unpin()
    {
        // Nothing to do — the whole array is pinned
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            NativeMemory.AlignedFree(_ptr);
            _disposed = true; 
        }
    }
}


/*
var manager = new PinnedArrayMemoryManager<byte>(1024);

Memory<byte> mem = manager.Memory;
Span<byte> span = mem.Span;

nuint addr = manager.Address;

Console.WriteLine($"Address: 0x{addr:X}");
*/