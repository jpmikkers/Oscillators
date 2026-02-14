namespace Baksteen.Waves;

using System;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using static MMInterop;
using msgTuple = (Baksteen.Waves.MMInterop.MMMessage msg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

public class WavePlayer
{

    private readonly Channel<msgTuple> _channel = Channel.CreateUnbounded<msgTuple>();
    private readonly Queue<BufferBase> _freeBuffers = new();
    private readonly List<BufferBase> _activeBuffers = new();
    private readonly int sampleRate;
    private readonly SampleFormat sampleFormat;
    private readonly SampleChannels sampleChannels;
    private readonly TimeSpan timePerBuffer;
    private readonly int numberOfBuffers;
    private WaveOutProc? _waveOutCallback;  // Keep callback alive for Windows callback

    public Action<Memory<SampleMono16>>? Mono16Render       { get; set; }
    public Action<Memory<SampleStereo16>>? Stereo16Render   { get; set; }
    public Action<Memory<SampleStereoFloat32>>? StereoFloat32Render  { get; set; }
    public Action<Memory<SampleStereo8>>? Stereo8Render     { get; set; }
    public Action<Memory<SampleMono8>>? Mono8Render         { get; set; }

    // Callback function
    private void WaveOutCallback(IntPtr hWaveOut, MMMessage uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
    {
        _channel.Writer.TryWrite((uMsg, dwInstance, dwParam1, dwParam2));
    }

    private void CheckForDoneBuffers(IntPtr hWaveOut)
    {
        for (int t = (_activeBuffers.Count - 1); t >= 0; t--)
        {
            var buffer = _activeBuffers[t];
            buffer.UpdateHeader();
            if (buffer.IsDone)
            {
                buffer.Unprepare();
                _activeBuffers.RemoveAt(t);
                _freeBuffers.Enqueue(buffer);
            }
        }
    }

    private async Task PlayBuffers(IntPtr hWaveOut)
    {
        while (_freeBuffers.TryDequeue(out var buffer))
        {
            switch (buffer)
            {
                case BufferStereoFloat32 bufStereoFloat32:
                    StereoFloat32Render?.Invoke(bufStereoFloat32.Buffer);
                    break;

                case BufferStereo16 bufStereo16:
                    Stereo16Render?.Invoke(bufStereo16.Buffer);
                    break;

                case BufferMono16 bufMono16:
                    Mono16Render?.Invoke(bufMono16.Buffer);
                    break;

                case BufferStereo8 bufStereo8:
                    if (Stereo8Render is not null)
                    {
                        var buf = bufStereo8.Buffer;
                        Stereo8Render(buf);
                        for (var i = 0; i < buf.Length; i++)
                        {
                            buf[i].Left = (sbyte)(buf[i].Left + 128);
                            buf[i].Right = (sbyte)(buf[i].Right + 128);
                        }
                    }
                    break;

                case BufferMono8 bufMono8:
                    if (Mono8Render is not null)
                    {
                        var buf = bufMono8.Buffer;
                        Mono8Render(buf);
                        for (var i = 0; i < buf.Length; i++)
                        {
                            buf[i].Mono = (sbyte)(buf[i].Mono + 128);
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException($"Buffer type {buffer.GetType()} is not supported.");
            }
            buffer.Prepare();
            buffer.AddBuffer();
            _activeBuffers.Add(buffer);
        }
        await Task.CompletedTask;
    }

    public WavePlayer(int sampleRate, SampleFormat sampleFormat, SampleChannels sampleChannels, TimeSpan timePerBuffer, int numBuffers)
    {
        this.sampleRate = sampleRate;
        this.sampleFormat = sampleFormat;
        this.sampleChannels = sampleChannels;
        this.timePerBuffer = timePerBuffer;
        this.numberOfBuffers = numBuffers;
    }

    public async Task Main(CancellationToken ct)
    {
        uint numDevs = waveOutGetNumDevs();
        Console.WriteLine($"Number of WaveOut devices: {numDevs}");

        for (uint t = 0; t <= numDevs; t++)
        {
            var deviceID = t - 1;
            var caps = new WAVEOUTCAPS2();
            var result = waveOutGetDevCaps(deviceID, ref caps, (uint)Marshal.SizeOf<WAVEOUTCAPS2>());
            AssertSuccess(result);

            Console.WriteLine($"Device Name: {caps.szPname}");
            Console.WriteLine($"Channels: {caps.wChannels}");
            //Console.WriteLine($"Formats: {caps.dwFormats}");
            //Console.WriteLine($"Support: {caps.dwSupport}");
            //Console.WriteLine($"Driver Version: {caps.vDriverVersion}");
            //Console.WriteLine($"Manufacturer guid: {caps.ManufacturerGuid}");
            //Console.WriteLine($"Product guid: {caps.ProductGuid}");
            //Console.WriteLine($"Name guid: {caps.NameGuid}");
        }

        var blockAlign = (int)sampleFormat * (int)sampleChannels / 8;

        //var wf = new WAVEFORMATEX
        //{
        //    wFormatTag = WAVEFORMATTAG.PCM,
        //    nChannels = (ushort)sampleChannels,
        //    nSamplesPerSec = (uint)sampleRate,
        //    nAvgBytesPerSec = (uint)(sampleRate * (int)sampleFormat * (int)sampleChannels / 8),
        //    nBlockAlign = (ushort)blockAlign,
        //    wBitsPerSample = (ushort)sampleFormat,   // bits per sample of one channel
        //    cbSize = 0
        //};

        WAVEFORMATEXTENSIBLE wfe;

        if (sampleFormat == SampleFormat.Float32)
        {
            wfe = new WAVEFORMATEXTENSIBLE
            {
                Format = new WAVEFORMATEX
                {
                    wFormatTag = WAVEFORMATTAG.EXTENSIBLE,
                    nChannels = (ushort)sampleChannels,
                    nSamplesPerSec = (uint)sampleRate,
                    nAvgBytesPerSec = (uint)(sampleRate * (int)sampleFormat * (int)sampleChannels / 8),
                    nBlockAlign = (ushort)blockAlign,
                    wBitsPerSample = (ushort)sampleFormat,   // bits per sample of one channel
                    cbSize = (ushort)(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>() - Marshal.SizeOf<WAVEFORMATEX>())
                },
                Samples = new SamplesUnion { wValidBitsPerSample = (ushort)sampleFormat },
                dwChannelMask = (uint)(ChannelFlags.SPEAKER_FRONT_LEFT | ChannelFlags.SPEAKER_FRONT_RIGHT),
                SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
            };
        }
        else
        {
            wfe = new WAVEFORMATEXTENSIBLE
            {
                Format = new WAVEFORMATEX
                {
                    wFormatTag = WAVEFORMATTAG.PCM,
                    nChannels = (ushort)sampleChannels,
                    nSamplesPerSec = (uint)sampleRate,
                    nAvgBytesPerSec = (uint)(sampleRate * (int)sampleFormat * (int)sampleChannels / 8),
                    nBlockAlign = (ushort)blockAlign,
                    wBitsPerSample = (ushort)sampleFormat,   // bits per sample of one channel
                    cbSize = 0
                },
                Samples = new SamplesUnion { wReserved = 0 },
                dwChannelMask = 0,
                SubFormat = Guid.Empty
            };
        }

        var samplesPerBuffer = (int)(sampleRate * timePerBuffer.TotalSeconds);
        var bytesPerBuffer = samplesPerBuffer * (int)sampleFormat * (int)sampleChannels / 8;

        Console.WriteLine($"Bytes per buffer: {bytesPerBuffer}");

        _waveOutCallback = new WaveOutProc(WaveOutCallback);
        var err = waveOutOpen(out var hWaveOut, 0, ref wfe, _waveOutCallback, IntPtr.Zero, WAVEINOUTOPENFLAGS.CALLBACK_FUNCTION);
        AssertSuccess(err);

        if (wfe.Format.nSamplesPerSec != sampleRate)
            throw new Exception($"Sample rate mismatch: {wfe.Format.nSamplesPerSec} != {sampleRate}");
        
        for (int i = 0; i < numberOfBuffers; i++)
        {
            var buffer = BufferBase.CreateBuffer(sampleFormat,sampleChannels,hWaveOut,samplesPerBuffer, false);
            _freeBuffers.Enqueue(buffer);
        }

        while (!ct.IsCancellationRequested)
        {
            var (msg, dwInstance, dwParam1, dwParam2) = await _channel.Reader.ReadAsync(ct).ConfigureAwait(false);

            //Console.WriteLine("hWaveOut: " + hWaveOut.ToString("X"));
            //Console.WriteLine("dwparam1: " + dwParam1.ToString("X"));
            //Console.WriteLine("dwparam2: " + dwParam2.ToString("X"));

            switch (msg)
            {
                case MMMessage.MM_WOM_OPEN:
                case MMMessage.MM_WOM_DONE:
                    CheckForDoneBuffers(hWaveOut);
                    await PlayBuffers(hWaveOut);
                    break;

                case MMMessage.MM_WOM_CLOSE:
                    Console.WriteLine("WaveOut device closed.");
                    break;

                default:
                    Console.WriteLine($"Message received: {msg}");
                    break;
            }
        }

    }
}
