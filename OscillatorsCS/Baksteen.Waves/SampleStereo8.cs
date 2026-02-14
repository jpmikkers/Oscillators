namespace Baksteen.Waves;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0, Size = 2)]
public struct SampleStereo8
{
    public sbyte Left;
    public sbyte Right;
}
