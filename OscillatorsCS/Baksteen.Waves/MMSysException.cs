namespace Baksteen.Waves;

using System;

[Serializable]
public class MMSysException : Exception
{
    public uint Code
    {
        get; set;
    } = 0xFFFFFFFF;

    public MMSysException()
    {
    }
    public MMSysException(string message) : base(message) { }
    public MMSysException(string message, Exception inner) : base(message, inner) { }
}
