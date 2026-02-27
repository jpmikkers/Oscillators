using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Baksteen.Oscillators;
using Baksteen.Waves;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OscillatorsLibrary;

namespace OscillatorGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private WaveRecorder? waveRecorder;
    private Task? waveRecorderTask;
    private Task? sampleProcessingTask;
    private CancellationTokenSource waveRecorderCancellationTokenSource = new();
    private IResonatorBank resonatorBank = default!;
    private int _samplesPerSpectrogramLine;
    private int _spectrogramSampleCounter;
    private readonly ConcurrentQueue<Memory<float>> _spectrumFreed = new();
    private readonly ConcurrentQueue<Memory<float>> _spectrumHistory = new();
    private readonly BoostedFloatConverter _boostedFloatConverter = new();

    private SpanRing<float> _sampleRing = new(0);
    private long _sampleOverflowCount;
    private SemaphoreSlim _sampleRingLock = new(0);

    [ObservableProperty]
    public partial int StartNote { get; set; } = 0;

    [ObservableProperty]
    public partial int EndNote { get; set; } = 127;

    [ObservableProperty]
    public partial string StartNoteAsString { get; set; } = GetNoteName(0);

    [ObservableProperty]
    public partial string EndNoteAsString { get; set; } = GetNoteName(127);

    [ObservableProperty]
    public partial int NumBands { get; set; } = 127+1;

    [ObservableProperty]
    public partial int NumHistory { get; set; } = 100;

    [ObservableProperty]
    public partial float ScaleMinValue { get; set; } = 0.0f;

    [ObservableProperty]
    public partial float ScaleMaxValue { get; set; } = 0.5f;

    [ObservableProperty]
    public partial float BoostDecibels { get; set; } = 0.0f;

    [ObservableProperty]
    public partial List<WaveRecorder.RecorderInfo> Devices
    {
        get;
        set;
    } = new();

    [ObservableProperty]
    public partial WaveRecorder.RecorderInfo? SelectedDevice
    {
        get;
        set;
    } = null;


    [ObservableProperty]
    public partial bool IsCapturing
    {
        get;
        set;
    } = false;

    public Action<ReadOnlySpan<float>> AddSpectrogramLine = x => { };

    partial void OnStartNoteChanged(int value)
    {
        if (value > EndNote)
            EndNote = value;

        NumBands = EndNote - StartNote + 1;
        StartNoteAsString = GetNoteName(value);
        UpdateResonatorBank();
    }

    partial void OnEndNoteChanged(int value)
    {
        if(value < StartNote)
            StartNote = value;

        NumBands = EndNote - StartNote + 1;
        EndNoteAsString = GetNoteName(value);
        UpdateResonatorBank();
    }

    partial void OnScaleMinValueChanged(float value)
    {
        if (value > ScaleMaxValue)
            ScaleMaxValue = value;
    }

    partial void OnScaleMaxValueChanged(float value)
    {
        if (value < ScaleMinValue)
            ScaleMinValue = value;
    }

    partial void OnBoostDecibelsChanged(float value)
    {
        _boostedFloatConverter.BoostDecibels = value;
    }

    private static string GetNoteName(int note)
    {
        string[] noteNames = { "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#", "A-", "A#", "B-" };
        return $"{noteNames[note%12]}{note/12}";
    }

    private void UpdateResonatorBank()
    {
        resonatorBank = new ResonatorBankVectorizedAVXBundled3(Frequencies.MusicalPitchFrequencies(StartNote, EndNote), 44100, 1);
    }

    private Memory<float> GetSpectrumLine(IResonatorBank resonatorBank)
    {
        var resonators = resonatorBank.SmoothResonators;

        Memory<float> spectrumLine;

        do
        {
            if (!_spectrumFreed.TryDequeue(out spectrumLine))
            {
                break;
            }
        } while(spectrumLine.Length != resonators.Length);

        if(spectrumLine.Length != resonators.Length)
        {
            spectrumLine = new float[resonators.Length];
        }

        var spectrumLineSpan = spectrumLine.Span;

        for (var i = 0; i < resonators.Length; i++)
        {
            spectrumLineSpan[i] = resonators.Span[i].Magnitude;
        }

        return spectrumLine;
    }

    [RelayCommand]
    public async Task StartSampling()
    {
        UpdateResonatorBank();

        var sampleRate = 44100;
        var driverBufferDuration = TimeSpan.FromMilliseconds(25);
        var driverBufferSize = (int)(sampleRate * driverBufferDuration.TotalSeconds);
        var spectrogramLineDuration = TimeSpan.FromMilliseconds(10);
        _samplesPerSpectrogramLine = (int)(sampleRate * spectrogramLineDuration.TotalSeconds);
        _spectrogramSampleCounter = 0;

        waveRecorder = new WaveRecorder(sampleRate, SampleFormat.Fmt16, SampleChannels.Mono, driverBufferDuration, 6);
        _boostedFloatConverter.BoostDecibels = BoostDecibels;
        _boostedFloatConverter.Attach(waveRecorder);
        _sampleRing = new SpanRing<float>(sampleRate);      // 1 seconds of audio should be more than enough to avoid overflow 
        _sampleOverflowCount = 0;
        _sampleRingLock = new SemaphoreSlim(0);

        _boostedFloatConverter.ProcessedSamplesCallback = (buffer) =>
        {
            _sampleRing.LossyWriteAll(buffer, out var lost);
            _sampleOverflowCount+=lost;
            _sampleRingLock.Release();
        };

        waveRecorderCancellationTokenSource = new CancellationTokenSource();
        sampleProcessingTask = SampleProcessingTask(waveRecorderCancellationTokenSource.Token);
        waveRecorderTask = waveRecorder.Main(waveRecorderCancellationTokenSource.Token);
        IsCapturing = true;
    }

    private async Task SampleProcessingTask(CancellationToken cts)
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await _sampleRingLock.WaitAsync(cts).ConfigureAwait(false);

                var done = false;

                while (!done)
                {
                    var readBufferSpan = _sampleRing.GetReadSpan(_samplesPerSpectrogramLine - _spectrogramSampleCounter);

                    if (readBufferSpan.Length > 0)
                    {
                        resonatorBank.UpdateWithSamples(readBufferSpan);
                        _sampleRing.AdvanceRead(readBufferSpan.Length);

                        _spectrogramSampleCounter += readBufferSpan.Length;

                        if (_spectrogramSampleCounter >= _samplesPerSpectrogramLine)
                        {
                            _spectrogramSampleCounter -= _samplesPerSpectrogramLine;
                            _spectrumHistory.Enqueue(GetSpectrumLine(resonatorBank));

                            Dispatcher.UIThread.Post(() =>
                            {
                                while (_spectrumHistory.TryDequeue(out var spectrumLine))
                                {
                                    AddSpectrogramLine(spectrumLine.Span);
                                    _spectrumFreed.Enqueue(spectrumLine);
                                }
                            });
                        }
                    }
                    else
                    {
                        done = true;
                    }
                }
            }
        }
        catch(OperationCanceledException)
        {
            // Expected when cancellation is requested, do nothing.
        }
    }


    [RelayCommand]
    public async Task StopSampling()
    {
        if (waveRecorder is not null)
        {
            IsCapturing = false;
            waveRecorderCancellationTokenSource.Cancel();
            await waveRecorderTask!;
            await sampleProcessingTask!;
            //waveRecorder.Dispose();
            waveRecorder = null;
        }
    }

    public MainWindowViewModel()
    {
        Devices = WaveRecorder.Probe();
        if (Devices.Count > 0) { SelectedDevice =  Devices[0]; }
    }
}
