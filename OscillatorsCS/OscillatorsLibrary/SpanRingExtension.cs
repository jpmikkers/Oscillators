using System;
using System.Collections.Generic;
using System.Text;
using Baksteen.Oscillators;

namespace OscillatorsLibrary;

public static class SpanRingExtension
{
    public static void LossyWriteAll<T>(this SpanRing<T> dest, ReadOnlySpan<T> source, out int lost)
    {
        lost = 0;

        while (source.Length > 0)
        {
            var destspan = dest.GetWriteSpan(source.Length);

            if (destspan.Length == 0)
            {
                lost += source.Length;
                break;
            }

            source[..destspan.Length].CopyTo(destspan);
            dest.AdvanceWrite(destspan.Length);
            source = source[destspan.Length..];
        }
    }
}
