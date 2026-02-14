namespace Baksteen.Waves;

using System;
using static MMInterop;

public class BufferStereo8 : BufferBase
{
    public SampleStereo8[] Buffer
    {
        get;
    }

    public BufferStereo8(IntPtr hWaveHandle, SampleStereo8[] buffer, bool isInput = false) : base(hWaveHandle, buffer, isInput)
    {
        this.Buffer = buffer;
    }
}
