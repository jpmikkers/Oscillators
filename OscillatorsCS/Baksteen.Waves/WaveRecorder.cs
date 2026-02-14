namespace Baksteen.Waves;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using static MMInterop;
using msgTuple = (Baksteen.Waves.MMInterop.MMMessage msg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);


public class WaveRecorder
{
    public record class RecorderInfo
    {
        public uint Id
        {
            get; set;
        }
        public string Name { get; set; } = string.Empty;
    }

    private readonly Channel<msgTuple> _channel = Channel.CreateUnbounded<msgTuple>();
    private readonly Queue<BufferBase> _freeBuffers = new();
    private readonly List<BufferBase> _activeBuffers = new();
    private readonly int sampleRate;
    private readonly SampleFormat sampleFormat;
    private readonly SampleChannels sampleChannels;
    private readonly TimeSpan timePerBuffer;
    private readonly int numberOfBuffers;
    private WaveInProc? _waveInCallback;  // Keep callback alive for Windows callback

    public Action<Memory<SampleMono16>>? Mono16Process       { get; set; }
    public Action<Memory<SampleStereo16>>? Stereo16Process   { get; set; }
    public Action<Memory<SampleStereoFloat32>>? StereoFloat32Process  { get; set; }
    public Action<Memory<SampleStereo8>>? Stereo8Process     { get; set; }
    public Action<Memory<SampleMono8>>? Mono8Process         { get; set; }

    // Callback function
    private void WaveInCallback(IntPtr hWaveIn, MMMessage uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
    {
        _channel.Writer.TryWrite((uMsg, dwInstance, dwParam1, dwParam2));
    }

    private void CheckForFilledBuffers(IntPtr hWaveIn)
    {
        for (int t = 0; t<_activeBuffers.Count; t++)
        {
            var buffer = _activeBuffers[t];
            buffer.UpdateHeader();
            if (buffer.IsDone)
            {
                buffer.MarkRecordedSamples();
                //Console.WriteLine($"Buffer recorded samples: {buffer.RecordedSamples}");
                buffer.Unprepare();
                _activeBuffers.RemoveAt(t);
                if (t < _activeBuffers.Count) t--;
                _freeBuffers.Enqueue(buffer);
            }
        }
    }

    private async Task ProcessBuffers(IntPtr hWaveIn)
    {
        while (_freeBuffers.TryDequeue(out var buffer))
        {
            switch (buffer)
            {
                case BufferStereoFloat32 bufStereoFloat32:
                    StereoFloat32Process?.Invoke(bufStereoFloat32.Buffer);
                    break;

                case BufferStereo16 bufStereo16:
                    Stereo16Process?.Invoke(bufStereo16.Buffer);
                    break;

                case BufferMono16 bufMono16:
                    Mono16Process?.Invoke(bufMono16.Buffer);
                    break;

                case BufferStereo8 bufStereo8:
                    if (Stereo8Process is not null)
                    {
                        var buf = bufStereo8.Buffer;
                        // Convert from unsigned to signed
                        for (var i = 0; i < buf.Length; i++)
                        {
                            buf[i].Left = (sbyte)(buf[i].Left - 128);
                            buf[i].Right = (sbyte)(buf[i].Right - 128);
                        }
                        Stereo8Process(buf);
                    }
                    break;

                case BufferMono8 bufMono8:
                    if (Mono8Process is not null)
                    {
                        var buf = bufMono8.Buffer;
                        // Convert from unsigned to signed
                        for (var i = 0; i < buf.Length; i++)
                        {
                            buf[i].Mono = (sbyte)(buf[i].Mono - 128);
                        }
                        Mono8Process(buf);
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

    public static List<RecorderInfo> Probe()
    {
        var result = new List<RecorderInfo>();
        var numDevs = waveInGetNumDevs();
        for (uint t = 0; t < numDevs; t++)
        {
            var deviceID = t;
            var caps = new WAVEINCAPS2();
            AssertSuccess(waveInGetDevCaps(deviceID, ref caps, (uint)Marshal.SizeOf<WAVEINCAPS2>()));
            result.Add(new RecorderInfo { Id = deviceID, Name = caps.szPname });
        }
        return result;
    }

    public WaveRecorder(int sampleRate, SampleFormat sampleFormat, SampleChannels sampleChannels, TimeSpan timePerBuffer, int numBuffers)
    {
        this.sampleRate = sampleRate;
        this.sampleFormat = sampleFormat;
        this.sampleChannels = sampleChannels;
        this.timePerBuffer = timePerBuffer;
        this.numberOfBuffers = numBuffers;
    }

    public async Task Main(CancellationToken ct)
    {
        uint numDevs = waveInGetNumDevs();
        Console.WriteLine($"Number of WaveIn devices: {numDevs}");

        for (uint t = 0; t < numDevs; t++)
        {
            var deviceID = t;
            var caps = new WAVEINCAPS2();
            var result = waveInGetDevCaps(deviceID, ref caps, (uint)Marshal.SizeOf<WAVEINCAPS2>());
            AssertSuccess(result);
            Console.WriteLine($"Device Name: {caps.szPname}");
            Console.WriteLine($"Channels: {caps.wChannels}");
        }

        var blockAlign = (int)sampleFormat * (int)sampleChannels / 8;

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
                    wBitsPerSample = (ushort)sampleFormat,
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
                    wBitsPerSample = (ushort)sampleFormat,
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

        _waveInCallback = new WaveInProc(WaveInCallback);
        var err = waveInOpen(out var hWaveIn, 0, ref wfe, _waveInCallback, IntPtr.Zero, WAVEINOUTOPENFLAGS.CALLBACK_FUNCTION);
        AssertSuccess(err);

        if (wfe.Format.nSamplesPerSec != sampleRate)
            throw new Exception($"Sample rate mismatch: {wfe.Format.nSamplesPerSec} != {sampleRate}");

        for (int i = 0; i < numberOfBuffers; i++)
        {
            var buffer = BufferBase.CreateBuffer(sampleFormat, sampleChannels, hWaveIn, samplesPerBuffer, true);
            _freeBuffers.Enqueue(buffer);
        }

        // Prepare and add initial buffers
        while (_freeBuffers.TryDequeue(out var buffer))
        {
            buffer.Prepare();
            buffer.AddBuffer();
            _activeBuffers.Add(buffer);
        }

        // Start recording
        var startErr = waveInStart(hWaveIn);
        AssertSuccess(startErr);

        try
        {

            while (!ct.IsCancellationRequested)
            {
                var (msg, dwInstance, dwParam1, dwParam2) = await _channel.Reader.ReadAsync(ct).ConfigureAwait(false);

                switch (msg)
                {
                    case MMMessage.MM_WIM_OPEN:
                        Console.WriteLine("WaveIn device opened.");
                        break;

                    case MMMessage.MM_WIM_DATA:
                        //Console.WriteLine("Buffer filled.");
                        CheckForFilledBuffers(hWaveIn);
                        await ProcessBuffers(hWaveIn);
                        break;

                    case MMMessage.MM_WIM_CLOSE:
                        Console.WriteLine("WaveIn device closed.");
                        break;

                    default:
                        Console.WriteLine($"Message received: {msg}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debugger.Launch();
        }
    }
}
