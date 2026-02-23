namespace Baksteen.Oscillators;

public interface IResonatorBank
{
    ReadOnlyMemory<ComplexF> SmoothResonators { get; }
    void UpdateWithSamples(ReadOnlySpan<float> samples);
}
