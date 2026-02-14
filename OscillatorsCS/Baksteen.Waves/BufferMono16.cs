namespace Baksteen.Waves;

using System;
using static MMInterop;

public class BufferMono16 : BufferBase
{
    public SampleMono16[] Buffer 
    { 
        get; 
    }

    public BufferMono16(IntPtr hWaveHandle, SampleMono16[] buffer, bool isInput = false) : base(hWaveHandle, buffer, isInput)
    {
        Buffer = buffer;
    }
}
