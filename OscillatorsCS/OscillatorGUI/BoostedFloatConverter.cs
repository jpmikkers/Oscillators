using System;
using Baksteen.Waves;

namespace OscillatorGUI;

internal class BoostedFloatConverter
{
    private float[] _mixBuffer = [];

    public float BoostDecibels
    {
        get;
        set;
    }

    public Action<ReadOnlySpan<float>>? ProcessedSamplesCallback
    {
        get;
        set;
    }

    public BoostedFloatConverter()
    {
    }

    public void Attach(WaveRecorder recorder)
    {
        recorder.Mono16Process = ProcessMono16;
        recorder.Stereo16Process = ProcessStereo16;
        recorder.StereoFloat32Process = ProcessStereoFloat32;
    }

    private static float DbToLinear(float dB)
    {
        return MathF.Exp((MathF.Log(10.0f) / 20.0f) * dB);
    }

    private void ResizeMixBuffer(int requiredLength)
    {
        if (_mixBuffer.Length < requiredLength)
        {
            _mixBuffer = new float[requiredLength];
        }
    }

    private void ProcessMono16(Memory<SampleMono16> buffer)
    {
        // resize the mix buffer if needed
        ResizeMixBuffer(buffer.Length);

        var mixSpan = _mixBuffer.AsSpan(0, buffer.Length);
        var multiplier = DbToLinear(BoostDecibels) / 32768f;

        for (var i = 0; i < buffer.Length; i++)
        {
            mixSpan[i] = Math.Clamp(buffer.Span[i].Mono * multiplier, -1f, 1f);
        }

        ProcessedSamplesCallback?.Invoke(mixSpan);
    }

    private void ProcessStereo16(Memory<SampleStereo16> buffer)
    {
        // resize the mix buffer if needed
        ResizeMixBuffer(buffer.Length);

        var mixSpan = _mixBuffer.AsSpan(0, buffer.Length);
        var multiplier = 0.5f * (DbToLinear(BoostDecibels) / 32768f);

        for (var i = 0; i < buffer.Length; i++)
        {
            var s = buffer.Span[i];
            mixSpan[i] = Math.Clamp((s.Left + s.Right) * multiplier, -1f, 1f);
        }

        ProcessedSamplesCallback?.Invoke(mixSpan);
    }

    private void ProcessStereoFloat32(Memory<SampleStereoFloat32> buffer)
    {
        // resize the mix buffer if needed
        ResizeMixBuffer(buffer.Length);

        var mixSpan = _mixBuffer.AsSpan(0, buffer.Length);
        var multiplier = 0.5f * DbToLinear(BoostDecibels);

        for (var i = 0; i < buffer.Length; i++)
        {
            var s = buffer.Span[i];
            mixSpan[i] = Math.Clamp((s.Left + s.Right) * multiplier, -1f, 1f);
        }

        ProcessedSamplesCallback?.Invoke(mixSpan);
    }
}
