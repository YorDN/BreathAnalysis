using BreathAnalysis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace BreathAnalysis.ViewModels;

public enum HomeState
{
    Idle,
    Countdown,
    Analyzing,
    Ventilating,
    Done
}

public partial class HomeViewModel : ObservableObject
{
    private readonly SerialService _serial;
    private readonly MainWindowViewModel _main;
    private System.Threading.Timer? _timer;

    // ── State ─────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsCountdown))]
    [NotifyPropertyChangedFor(nameof(IsAnalyzing))]
    [NotifyPropertyChangedFor(nameof(IsVentilating))]
    [NotifyPropertyChangedFor(nameof(IsDone))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    private HomeState _state = HomeState.Idle;

    public bool IsIdle => State == HomeState.Idle;
    public bool IsCountdown => State == HomeState.Countdown;
    public bool IsAnalyzing => State == HomeState.Analyzing;
    public bool IsVentilating => State == HomeState.Ventilating;
    public bool IsDone => State == HomeState.Done;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    private bool _isConnected = false;

    public bool CanStart => IsIdle && _isConnected;

    [ObservableProperty] private int _countdown = 5;
    [ObservableProperty] private string _statusText = "Ready to start analysis";
    [ObservableProperty] private string _statusSub = "Connect to Arduino and press START";
    [ObservableProperty] private bool _isSessionActive = false;

    // ── Port selection ────────────────────────────────────────────────────
    [ObservableProperty] private string[] _availablePorts = [];
    [ObservableProperty] private string? _selectedPort;
    [ObservableProperty] private string _connectLabel = "Connect";

    // ── Brushes for ventilating label ─────────────────────────────────────
    public string AccentOrangeBrush => "#E67E22";

    public HomeViewModel(SerialService serial, MainWindowViewModel main)
    {
        _serial = serial;
        _main = main;

        _serial.StatusReceived += OnArduinoStatus;
        _serial.ErrorOccurred += OnSerialError;

        RefreshPorts();
    }

    // ── Port management ───────────────────────────────────────────────────
    [RelayCommand]
    public void RefreshPorts()
    {
        AvailablePorts = SerialService.GetAvailablePorts();
        if (AvailablePorts.Length > 0)
            SelectedPort ??= AvailablePorts[0];
    }

    [RelayCommand]
    public void ToggleConnection()
    {
        if (_serial.IsConnected)
        {
            _serial.Disconnect();
            IsConnected = false;
            ConnectLabel = "Connect";
        }
        else
        {
            if (SelectedPort == null) return;
            bool ok = _serial.Connect(SelectedPort);
            IsConnected = ok;
            ConnectLabel = ok ? "Disconnect" : "Connect";
            StatusText = ok ? "Connected — Ready" : "Connection failed";
            StatusSub = ok
                ? "Press START to begin analysis"
                : "Check the COM port and try again";
        }
    }

    // ── Start analysis ────────────────────────────────────────────────────
    [RelayCommand]
    public void StartAnalysis()
    {
        if (!CanStart) return;

        _main.AnalysisReadings.Clear();
        State = HomeState.Countdown;
        Countdown = 5;

        _timer = new System.Threading.Timer(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Countdown--;
                if (Countdown <= 0)
                {
                    _timer?.Dispose();
                    BeginSession();
                }
            });
        }, null, 1000, 1000);
    }

    private void BeginSession()
    {
        State = HomeState.Analyzing;
        IsSessionActive = true;
        _serial.StartSession();
    }

    // ── Arduino status handler ────────────────────────────────────────────
    private void OnArduinoStatus(string status)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            switch (status)
            {
                case "SESSION_STARTED":
                    State = HomeState.Analyzing;
                    break;

                case "SESSION_ENDED":
                    IsSessionActive = false;
                    State = HomeState.Ventilating;
                    _main.NavigateToReport();
                    break;

                case "FAN_ON":
                    State = HomeState.Ventilating;
                    break;

                case "FAN_OFF":
                case "READY":
                    if (State == HomeState.Ventilating)
                    {
                        State = HomeState.Done;
                        Task.Delay(3000).ContinueWith(_ =>
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                State = HomeState.Idle));
                    }
                    break;

                case "BUSY":
                    StatusText = "⚠ Arduino is busy";
                    StatusSub = "Wait for current operation to finish";
                    break;

                case "SYSTEM_STOPPED":
                    State = HomeState.Idle;
                    IsSessionActive = false;
                    StatusText = "Stopped";
                    StatusSub = "Press START to begin analysis";
                    break;
            }
        });
    }
    private void OnSerialError(string error)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusText = "⚠ Error";
            StatusSub = error;
            State = HomeState.Idle;
        });
    }
}