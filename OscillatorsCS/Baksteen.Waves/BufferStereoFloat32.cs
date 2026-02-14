namespace Baksteen.Waves;

using System;
using static MMInterop;

public class BufferStereoFloat32 : BufferBase
{
    public SampleStereoFloat32[] Buffer
    {
        get;
    }

    public BufferStereoFloat32(IntPtr hWaveHandle, SampleStereoFloat32[] buffer, bool isInput = false) : base(hWaveHandle, buffer, isInput)
    {
        this.Buffer = buffer;
    }
}
