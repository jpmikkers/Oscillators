using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Baksteen.Oscillators;

public static class VectorExt
{
    public static Vector<float> CreatePartial(ReadOnlySpan<float> span)
    {
        var vecCount = Vector<float>.Count;
        if (span.Length >= vecCount)
        {
            return new Vector<float>(vecCount);
        }
        else
        {
            Span<float> temp = stackalloc float[vecCount];
            span.CopyTo(temp);                            // copies only the tail
            temp[(vecCount - span.Length)..].Clear();     // zero-fill the rest
            return new Vector<float>(temp);
        }
    }
}
