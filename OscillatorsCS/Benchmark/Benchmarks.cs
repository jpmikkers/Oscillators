using System;
using Baksteen.Oscillators;
using BenchmarkDotNet.Attributes;

namespace Benchmark;
// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
//[CPUUsageDiagnoser]
public class Benchmarks
{
    private const int Repeats = 3;
    private float[] samples;
    private ResonatorBank normalBank;
    private ResonatorBankVectorized vectorizedBank;
    private ResonatorBankVectorizedAVX vectorizedAVXBank;
    private ResonatorBankVectorizedAVXBundled vectorizedAVXBankBundled;
    private ResonatorBankVectorizedAVXBundled3 vectorizedAVXBankBundled3;

    [GlobalSetup]
    public void Setup()
    {
        int maxNote = 199;
        samples = new float[44100];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = 2.0f * (Random.Shared.NextSingle() - 0.5f);
        }
        normalBank = new ResonatorBank(Frequencies.MusicalPitchFrequencies(0, maxNote), 44100f, 4);
        vectorizedBank = new ResonatorBankVectorized(Frequencies.MusicalPitchFrequencies(0, maxNote), 44100f, 4);
        vectorizedAVXBank = new ResonatorBankVectorizedAVX(Frequencies.MusicalPitchFrequencies(0, maxNote), 44100f, 4);
        vectorizedAVXBankBundled = new ResonatorBankVectorizedAVXBundled(Frequencies.MusicalPitchFrequencies(0, maxNote), 44100f, 4);
        vectorizedAVXBankBundled3 = new ResonatorBankVectorizedAVXBundled3(Frequencies.MusicalPitchFrequencies(0, maxNote), 44100f, 4);
    }

    [Benchmark(Baseline = true)]
    public void OscillatorBankNormal()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            normalBank.UpdateWithSample(samples[i]);
        }
    }

    //[Benchmark]
    public void OscillatorBankVectorized()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            vectorizedBank.UpdateWithSample(samples[i]);
        }
    }

    //[Benchmark(Baseline = true)]
    //[Benchmark]
    public void OscillatorBankVectorizedAVX()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            vectorizedAVXBank.UpdateWithSample(samples[i]);
        }
    }

    //[Benchmark(Baseline = true)]
    //[Benchmark]
    public void OscillatorBankVectorizedAVXBundled()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            vectorizedAVXBankBundled.UpdateWithSample(samples[i]);
        }
    }

    [Benchmark]
    public void OscillatorBankVectorizedAVXBundled3()
    {
        for (var i = 0; i < samples.Length; i++)
        {
            vectorizedAVXBankBundled3.UpdateWithSample(samples[i]);
        }
    }
}
