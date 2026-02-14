
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Baksteen.Oscillators;

var sampleRate = 44100f;
var frequency = 8000f;

ComplexF[] data = new ComplexF[]{
    new(1,2),
    new(3,4),
    new(5,6),
    new(7,8),

    new(9,10),
    new(11,12),
    new(13,14),
    new(15,16),

    new(17,18),
    new(19,20),
    new(21,22),
    new(23,24),

    new(25, 26),
    new(27, 28)};

#if NEVER
void StabilizeVectorized(ComplexF[] data)
{
    var vecsize = Vector<float>.Count;
    var interleaved = MemoryMarshal.Cast<ComplexF, Vector<float>>(data);
    var vthree = new Vector<float>(3.0f);
    var vhalf = new Vector<float>(0.5f);

    foreach (ref var item in interleaved)
    {
        var squared = item * item;
        var swaptmp = Vector.AsVectorUInt64(squared);
        var reimswapped = ((swaptmp >> 32) | (swaptmp << 32)).As<ulong,float>();
        var magsquared = squared + reimswapped;
        var k = vhalf * (vthree - magsquared);
        item *= k;
    }

    int remaining = (data.Length * 2) % vecsize;

    if (remaining > 0)
    {
        var taildata = MemoryMarshal.Cast<ComplexF, float>(data)[^remaining..];

        var item = VectorExt.CreatePartial(taildata);
        var squared = item * item;
        var swaptmp = Vector.AsVectorUInt64(squared);
        var reimswapped = ((swaptmp >> 32) | (swaptmp << 32)).As<ulong, float>();
        var magsquared = squared + reimswapped;
        var k = vhalf * (vthree - magsquared);
        item *= k;

        for (var i = 0; i < remaining; i++)
        {
            taildata[i] = item[i];
        }
    }
}

void StabilizeNormal(ComplexF[] data)
{
    foreach (ref var z in data.AsSpan())
    {
        var k = 1.0f - (z.MagnitudeSquared - 1.0f) / 2.0f;
        z.Real *= k;
        z.Imag *= k;
    }
}

void StabilizeVectorizedAvx(ComplexF[] phasors)
{
    if (!Avx.IsSupported)
    {
        StabilizeVectorized(phasors); // fallback to generic vectorized version
        return;
    }

    var v256Size = Vector256<float>.Count; // 8 floats
    var interleaved = MemoryMarshal.Cast<ComplexF, float>(phasors);

    var vthree256 = Vector256.Create(3.0f);
    var vhalf256 = Vector256.Create(0.5f);

    Vector256<float> LoadPartial(ReadOnlySpan<float> span)
    {
        if (span.Length >= v256Size)
        {
            return Vector256.Create<float>(span);
        }
        else
        {
            Span<float> temp = stackalloc float[v256Size];
            span.CopyTo(temp);                            // copies only the tail
            temp[(v256Size - span.Length)..].Clear();     // zero-fill the rest
            return Vector256.Create<float>(temp);
        }
    }

    void SavePartial(Vector256<float> src, Span<float> span)
    {
        if (span.Length >= v256Size)
        {
            src.CopyTo(span);
        }
        else
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = src[i];
            }
        }
    }

    int i = 0;
    for (; i <= interleaved.Length - v256Size; i += v256Size)
    {
        // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
        var item = Vector256.Create<float>(interleaved[i..]);

        // Compute squared = item * item
        var squared = Avx.Multiply(item, item);
        //var reimswapped = Avx.Shuffle(squared, squared, 0b10110001);
        var reimswapped = Avx.Permute(squared, 0b10110001);

        // Compute magnitude squared: (r*r + i*i) for each complex pair
        var magsquared = Avx.Add(squared, reimswapped);

        // Compute k = 0.5f * (3.0f - magsquared)
        var diff = Avx.Subtract(vthree256, magsquared);
        var k = Avx.Multiply(vhalf256, diff);

        // Apply scaling: item = item * k
        var scaled_item = Avx.Multiply(item, k);

        scaled_item.CopyTo(interleaved[i..]);
    }

    // Handle remaining elements with generic vectorized approach
    if (i < interleaved.Length)
    {
        // Load 8 floats from interleaved data: [r0, i0, r1, i1, r2, i2, r3, i3]
        var item = LoadPartial(interleaved[i..]);

        // Compute squared = item * item
        var squared = Avx.Multiply(item, item);
        var reimswapped = Avx.Shuffle(squared, squared, 0b10_11_00_01);

        // Compute magnitude squared: (r*r + i*i) for each complex pair
        var magsquared = Avx.Add(squared, reimswapped);

        // Compute k = 0.5f * (3.0f - magsquared)
        var diff = Avx.Subtract(vthree256, magsquared);
        var k = Avx.Multiply(vhalf256, diff);

        // Apply scaling: item = item * k
        var scaled_item = Avx.Multiply(item, k);

        SavePartial(scaled_item, interleaved[i..]);
    }
}

StabilizeVectorizedAvx(data);
//StabilizeNormal(data);
foreach (var item in data)
{
    Console.WriteLine(item);
}

//Console.WriteLine($"I have {items} items");

#endif

var frequencies = Frequencies.MusicalPitchFrequencies(2*12, 8*12);  // c2 to c8
var resonatorBank = new ResonatorBankVectorizedAVX(frequencies, sampleRate, 4);
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

Oscillator x = new Oscillator(frequency, sampleRate);
Resonator r = new Resonator(frequency, 
    Resonator.AlphaHeuristic(frequency,sampleRate), 
    Resonator.AlphaHeuristic(frequency,sampleRate), 
    sampleRate);

for(int i = 0; i < 10000; i++)
{
    var sample = x.GetNextSample();
    r.Update(sample);
    Console.WriteLine($"{i} {sample}\t{phasor}\t{r.Amplitude}");

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