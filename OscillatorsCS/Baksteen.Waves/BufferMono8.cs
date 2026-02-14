namespace Baksteen.Waves;

using System;
using static MMInterop;

public class BufferMono8 : BufferBase
{
    public SampleMono8[] Buffer 
    { 
        get; 
    }

    public BufferMono8(IntPtr hWaveHandle, SampleMono8[] buffer, bool isInput = false) : base(hWaveHandle, buffer, isInput)
    {
        Buffer = buffer;
    }
}
