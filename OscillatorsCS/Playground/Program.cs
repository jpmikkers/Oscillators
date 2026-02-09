
using System.Numerics;
using System.Runtime.InteropServices;
using Baksteen.Oscillators;

var sampleRate = 44100f;
var frequencies = Frequencies.MusicalPitchFrequencies(2*12, 8*12);  // c2 to c8
var resonatorBank = new ResonatorBank(frequencies, sampleRate, 4);
var updateInterval = TimeSpan.FromSeconds(0.02);
var samplesPerUpdate = (int)(sampleRate * updateInterval.TotalSeconds);

int oscillatorBin = Random.Shared.Next(0, frequencies.Length);
var oscillator = new Oscillator(frequencies[oscillatorBin], sampleRate);

var indicators = new char[] { 
    ' ', '.', ':', '░', '▒', '▓', '█',
};

char GetIndicator(float magnitude)
{
    var index = (int)(2.0f * magnitude * indicators.Length);
    return indicators[Math.Clamp(index, 0, indicators.Length - 1)];
}

int updateCounter = 0;

Console.OutputEncoding = System.Text.Encoding.UTF8;

while (true)
{
    for (int i = 0; i < samplesPerUpdate; i++)
    {
        resonatorBank.UpdateWithSample(oscillator.GetNextSample());
    }

    Console.Write($"{oscillatorBin:D3} |");
    foreach(var sr in resonatorBank.SmoothResonators)
    {
        Console.Write($"{GetIndicator(sr.Magnitude)}");
    }

    Console.WriteLine("|");
    Thread.Sleep(updateInterval);

    if(updateCounter++ > 25)
    {
        oscillatorBin = Random.Shared.Next(0, frequencies.Length);
        oscillator.Frequency = frequencies[oscillatorBin];
        updateCounter = 0;
    }
}

#if NEVER
//var phasor = new ComplexF(1f, 0f);
//var omega = (MathF.Tau * frequency) / sampleRate;

var phasor = ComplexF.FromPolar(1f, 0);
//var omega = ComplexF.FromPolar(1f, (MathF.Tau * frequency) / sampleRate);

var radiansPerSample = (MathF.Tau * frequency) / sampleRate;
var multiplier = ComplexF.FromPolar(1f, radiansPerSample);

Oscillator x = new Oscillator(100f, 10000f);

for(int i = 0; i < 200; i++)
{
    Console.WriteLine($"{i} {x.GetNextSample()}\t{phasor}");
    phasor *= multiplier;
    Stabilize(ref phasor);
}

void Stabilize(ref ComplexF z)
{
    // https://github.com/FrankReiser/ReiserRT_FlyingPhasor
    var k = 1.0f - (z.MagnitudeSquared - 1.0f) / 2.0f;
    z.Real *= k;
    z.Imag *= k;
}
#endif