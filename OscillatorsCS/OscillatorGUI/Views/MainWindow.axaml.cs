using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace OscillatorGUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.AddSpectrogramLine = line =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SpectrogramPlot.AddData(line);
                });
            };
        }
    }


    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree((VisualTreeAttachmentEventArgs)e);

    }
}