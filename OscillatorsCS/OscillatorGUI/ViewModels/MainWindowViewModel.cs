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

namespace OscillatorGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private WaveRecorder? waveRecorder;
    private Task? waveRecorderTask;
    private CancellationTokenSource waveRecorderCancellationTokenSource = new();
    private IResonatorBank? resonatorBank;
    private int _spectrumLineSamplesCount;
    private ConcurrentQueue<Memory<float>> _spectrumFreed = new();
    private ConcurrentQueue<Memory<float>> _spectrumHistory = new();
    private readonly BoostedFloatConverter _boostedFloatConverter = new();

    [ObservableProperty]
    public partial int StartNote { get; set; } = 12;

    [ObservableProperty]
    public partial int EndNote { get; set; } = 12+(3*12);

    [ObservableProperty]
    public partial string StartNoteAsString { get; set; } = GetNoteName(12);

    [ObservableProperty]
    public partial string EndNoteAsString { get; set; } = GetNoteName(12+(3*12));

    [ObservableProperty]
    public partial int NumBands { get; set; } = 3*12+1;

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
        resonatorBank = new ResonatorBankVectorizedAVXBundled(Frequencies.MusicalPitchFrequencies(StartNote, EndNote), 44100, 1);
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
        //var dispatcherTimer = new DispatcherTimer();
        //dispatcherTimer.Interval = TimeSpan.FromSeconds(1 / 60.0);
        //dispatcherTimer.Tick += (sender, e) => {

        //    for (int i = 0; i < (44100 / 60); i++)
        //    {
        //        resonatorBank.UpdateWithSample(2.0f * (Random.Shared.NextSingle() - 0.5f));
        //    }

        //    var spectrumline = resonatorBank.SmoothResonators.Select(x => x.Magnitude).ToArray();
        //    AddSpectrogramLine(spectrumline);
        //};
        //dispatcherTimer.Start();

        var sampleRate = 44100;
        var driverBufferDuration = TimeSpan.FromMilliseconds(25);
        var driverBufferSize = (int)(sampleRate * driverBufferDuration.TotalSeconds);
        var spectrogramLineDuration = TimeSpan.FromMilliseconds(5);
        var spectrogramLineSamples = (int)(sampleRate * spectrogramLineDuration.TotalSeconds);
        _spectrumLineSamplesCount = 0;

        waveRecorder = new WaveRecorder(sampleRate, SampleFormat.Fmt16, SampleChannels.Mono, driverBufferDuration, 4);
        _boostedFloatConverter.BoostDecibels = BoostDecibels;
        _boostedFloatConverter.Attach(waveRecorder);

        _boostedFloatConverter.ProcessedSamplesCallback = (buffer) =>
        {
            if (resonatorBank is not null)
            {
                var p = 0;
                var samplesToProcess = Math.Min(spectrogramLineSamples - _spectrumLineSamplesCount, buffer.Length);
                var addedNewLines = false;
                while (samplesToProcess > 0)
                {
                    resonatorBank.UpdateWithSamples(buffer.Slice(p, samplesToProcess));
                    _spectrumLineSamplesCount += samplesToProcess;
                    p+= samplesToProcess;

                    if (_spectrumLineSamplesCount >= spectrogramLineSamples)
                    {
                        _spectrumHistory.Enqueue(GetSpectrumLine(resonatorBank));
                        _spectrumLineSamplesCount = 0;
                        addedNewLines = true;
                    }

                    samplesToProcess = Math.Min(spectrogramLineSamples - _spectrumLineSamplesCount, buffer.Length - p);
                }

                if (addedNewLines)
                {
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
        };

        waveRecorderTask = waveRecorder.Main(waveRecorderCancellationTokenSource.Token);
    }

    [RelayCommand]
    public async Task StopSampling()
    {
        if (waveRecorder is not null)
        {
            waveRecorderCancellationTokenSource.Cancel();
            await waveRecorderTask!;
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
