namespace Baksteen.Waves;

using System;
using System.Runtime.InteropServices;
using static MMInterop;


public class BufferBase: IDisposable
{
    private readonly IntPtr hWaveHandle;
    private readonly bool isInput;
    private GCHandle bufferHandle;
    private readonly nint bufferPtr;
    private readonly int numSamples;
    private readonly int bytesPerSample;
    private WAVEHDR waveHeader;
    private readonly nint headerHandle;
    private int recordedSamples;

    public bool IsDone => (waveHeader.dwFlags & WAVEHDRFLAGS.WHDR_DONE) == WAVEHDRFLAGS.WHDR_DONE;
    public int RecordedSamples => recordedSamples;

    protected BufferBase(IntPtr hWaveHandle, Array buffer, bool isInput = false)
    {
        this.hWaveHandle = hWaveHandle;
        this.isInput = isInput;
        numSamples = buffer.Length;
        bytesPerSample = Marshal.SizeOf(buffer.GetType().GetElementType()!);
        bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        headerHandle = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());
        bufferPtr = bufferHandle.AddrOfPinnedObject();
    }

    //dispose pattern
    public void Dispose()
    {
        Marshal.FreeHGlobal(headerHandle);
        bufferHandle.Free();

        // Free the allocated memory
        //unsafe { NativeMemory.Free((void*)bufferPtr); }
        //bufferPtr = IntPtr.Zero;
    }

    public void UpdateHeader()
    {
        waveHeader = Marshal.PtrToStructure<WAVEHDR>(headerHandle);        
    }

    public void Prepare()
    {
        waveHeader.lpData = bufferPtr;
        waveHeader.dwBufferLength = (uint)(numSamples * bytesPerSample);
        waveHeader.dwBytesRecorded = 0;
        waveHeader.dwUser = IntPtr.Zero;
        waveHeader.dwFlags = 0;
        waveHeader.dwLoops = 0;
        waveHeader.lpNext = IntPtr.Zero;
        waveHeader.reserved = IntPtr.Zero;
        Marshal.StructureToPtr(waveHeader, headerHandle, false);

        // Prepare the header
        if (isInput)
        {
            AssertSuccess(waveInPrepareHeader(hWaveHandle, headerHandle, (uint)Marshal.SizeOf<WAVEHDR>()));
        }
        else
        {
            AssertSuccess(waveOutPrepareHeader(hWaveHandle, headerHandle, (uint)Marshal.SizeOf<WAVEHDR>()));
        }
    }

    public void Unprepare()
    {
        // Unprepare the header
        if (isInput)
        {
            AssertSuccess(waveInUnprepareHeader(hWaveHandle, headerHandle, (uint)Marshal.SizeOf<WAVEHDR>()));
        }
        else
        {
            AssertSuccess(waveOutUnprepareHeader(hWaveHandle, headerHandle, (uint)Marshal.SizeOf<WAVEHDR>()));
        }
    }

    public void MarkRecordedSamples()
    {
        // Mark the number of recorded samples (input only)
        if (isInput)
        {
            recordedSamples = (int)waveHeader.dwBytesRecorded / bytesPerSample;
        }
    }

    public void AddBuffer()
    {
        if (isInput)
        {
            // Add buffer for recording (input only)
            AssertSuccess(waveInAddBuffer(hWaveHandle, headerHandle, (uint)Marshal.SizeOf<WAVEHDR>()));
        }
        else
        {
            // Write the header (output only)
            AssertSuccess(waveOutWrite(hWaveHandle, headerHandle, (uint)Marshal.SizeOf<WAVEHDR>()));
        }
    }

    //
    // Reinterpret the Span<byte> as Span<float>
    //Span<float> floatSpan = MemoryMarshal.Cast<byte, float>(byteSpan);

    //unsafe
    //{
    //    bufferPtr = (nint)NativeMemory.AlignedAlloc((nuint)bufferSize, (nuint)alignment);
    //    NativeMemory.Clear((void*)bufferPtr, (nuint)bufferSize);
    //}

    //var memory = Marshal.AllocHGlobal(65536); // Allocate memory for the buffer
    //unsafe
    //{
    //    // Create a Span<byte> over the allocated memory
    //    Span<byte> span = new Span<byte>((void*)memory, 65536);

    //    // Zero out the memory
    //    span.Clear();
    //}
    //unsafe
    //{
    //    // Create a Span<byte> over the allocated memory
    //    var span = new Span<Stereo16Sample>((void*)bufferPtr, bufferSize >> 2); // / Marshal.SizeOf<Stereo16Sample>());
    //    filler(span);
    //}

    public static BufferBase CreateBuffer(SampleFormat sampleFormat, SampleChannels sampleChannels, nint hWaveIn, int samplesPerBuffer, bool isInput)
    {
        return (sampleFormat, sampleChannels)
            switch
        {
            (SampleFormat.Float32, SampleChannels.Stereo) => new BufferStereoFloat32(hWaveIn, new SampleStereoFloat32[samplesPerBuffer], isInput),
            (SampleFormat.Fmt16, SampleChannels.Mono) => new BufferMono16(hWaveIn, new SampleMono16[samplesPerBuffer], isInput),
            (SampleFormat.Fmt16, SampleChannels.Stereo) => new BufferStereo16(hWaveIn, new SampleStereo16[samplesPerBuffer], isInput),
            (SampleFormat.Fmt8, SampleChannels.Mono) => new BufferMono8(hWaveIn, new SampleMono8[samplesPerBuffer], isInput),
            (SampleFormat.Fmt8, SampleChannels.Stereo) => new BufferStereo8(hWaveIn, new SampleStereo8[samplesPerBuffer], isInput),
            _ => throw new NotSupportedException($"Buffer type {sampleFormat}, {sampleChannels} is not supported."),
        };
    }
}