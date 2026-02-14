using System;
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

    public Action<float[]> AddSpectrogramLine = x => { };

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

    private static string GetNoteName(int note)
    {
        string[] noteNames = { "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#", "A-", "A#", "B-" };
        return $"{noteNames[note%12]}{note/12}";
    }

    private WaveRecorder? waveRecorder;
    private Task? waveRecorderTask;
    private CancellationTokenSource waveRecorderCancellationTokenSource = new();
    private ResonatorBank? resonatorBank;

    private void UpdateResonatorBank()
    {
        resonatorBank = new ResonatorBank(Frequencies.MusicalPitchFrequencies(StartNote, EndNote), 44100, 2);
    }

    private DispatcherTimer dispatcherTimer;

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

        waveRecorder = new WaveRecorder(44100, SampleFormat.Fmt16, SampleChannels.Mono, TimeSpan.FromMilliseconds(25), 4);

        waveRecorder.Mono16Process = (buffer) =>
        {
            if (resonatorBank is not null)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    resonatorBank.UpdateWithSample(buffer.Span[i].Mono / 10000.0f);
                }

                var spectrumline = resonatorBank.SmoothResonators.Select(x => x.Magnitude).ToArray();
                AddSpectrogramLine(spectrumline);
            }
        };

        waveRecorder.Stereo16Process = (buffer) =>
        {
            if (resonatorBank is not null)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    resonatorBank.UpdateWithSample(buffer.Span[i].Left / 10000.0f);
                }

                var spectrumline = resonatorBank.SmoothResonators.Select(x => x.Magnitude).ToArray();
                AddSpectrogramLine(spectrumline);
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
