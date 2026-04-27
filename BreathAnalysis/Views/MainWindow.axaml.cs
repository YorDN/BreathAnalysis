using Avalonia.Controls;
using Avalonia.Interactivity;
using BreathAnalysis.ViewModels;

namespace BreathAnalysis.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainWindowViewModel();
        DataContext = _vm;

        this.Loaded += (_, _) => _vm.OnWindowResized(Bounds.Width);

        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == BoundsProperty)
                _vm?.OnWindowResized(Bounds.Width);
        };

        this.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.F11)
                WindowState = WindowState == WindowState.FullScreen
                    ? WindowState.Normal
                    : WindowState.FullScreen;
        };

        this.Closed += (_, _) => _vm?.OnClosed();
    }
}