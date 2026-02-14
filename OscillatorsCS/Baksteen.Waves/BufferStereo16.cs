namespace Baksteen.Waves;

using System;
using static MMInterop;

public class BufferStereo16 : BufferBase
{
    public SampleStereo16[] Buffer
    {
        get;
    }

    public BufferStereo16(IntPtr hWaveHandle, SampleStereo16[] buffer, bool isInput) : base(hWaveHandle, buffer, isInput)
    {
        this.Buffer = buffer;
    }
}
