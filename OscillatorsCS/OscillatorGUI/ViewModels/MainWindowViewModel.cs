using CommunityToolkit.Mvvm.ComponentModel;

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

    partial void OnStartNoteChanged(int value)
    {
        if (value > EndNote)
            EndNote = value;

        NumBands = EndNote - StartNote + 1;
        StartNoteAsString = GetNoteName(value);
    }

    partial void OnEndNoteChanged(int value)
    {
        if(value < StartNote)
            StartNote = value;

        NumBands = EndNote - StartNote + 1;
        EndNoteAsString = GetNoteName(value);
    }

    private static string GetNoteName(int note)
    {
        string[] noteNames = { "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#", "A-", "A#", "B-" };
        return $"{noteNames[note%12]}{note/12}";
    }
}
