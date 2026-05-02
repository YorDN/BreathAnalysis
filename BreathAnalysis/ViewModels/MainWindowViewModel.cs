using BreathAnalysis.Models;
using BreathAnalysis.Services;
using BreathAnalysis.Models;
using BreathAnalysis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BreathAnalysis.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel()
    {
        ReportService.EnsureDirectory();
        SerialService.ReadingReceived += OnReadingReceived;
        SerialService.StatusReceived += OnStatusReceived; // ← add

        _liveVm = new LiveViewModel(AllReadings, SerialService); // ← add SerialService
        _reportVm = new ReportViewModel(ReportService, AllReadings, AnalysisReadings);

        NavigateToHome();
    }
    // ── Services (shared across all pages) ───────────────────────────────
    public SerialService SerialService { get; } = new();
    public ReportService ReportService { get; } = new();

    // ── Shared data ──────────────────────────────────────────────────────
    public ObservableCollection<SensorReading> AllReadings { get; } = new();
    public ObservableCollection<SensorReading> AnalysisReadings { get; } = new();

    // ── Navigation ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableObject? _currentPage;
    [ObservableProperty] private string _currentPageName = "Home";
    [ObservableProperty] private bool _isHomeActive = true;
    [ObservableProperty] private bool _isLiveActive = false;
    [ObservableProperty] private bool _isReportActive = false;

    // ── Layout ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isTouchMode = false;

    // ── Pages ────────────────────────────────────────────────────────────
    private HomeViewModel? _homeVm;
    private LiveViewModel? _liveVm;
    private ReportViewModel? _reportVm;
    public ReportViewModel? ReportVm => _reportVm;

    // ── Reading handler ──────────────────────────────────────────────────
    private void OnReadingReceived(SensorReading reading)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (AllReadings.Count > 500)
                AllReadings.RemoveAt(0);
            AllReadings.Add(reading);

            if (_homeVm?.IsSessionActive == true)
            {
                reading.IsAnalysis = true;
                AnalysisReadings.Add(reading);
            }
        });
    }
    private void OnStatusReceived(string status)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _liveVm?.OnArduinoStatus(status);
        });
    }


    // ── Navigation ───────────────────────────────────────────────────────
    [RelayCommand]
    public void NavigateToHome()
    {
        _homeVm ??= new HomeViewModel(SerialService, this);
        CurrentPage = _homeVm;
        CurrentPageName = "Home";
        IsHomeActive = true;
        IsLiveActive = false;
        IsReportActive = false;
    }

    [RelayCommand]
    public void NavigateToLive()
    {
        // _liveVm already created in constructor
        CurrentPage = _liveVm;
        CurrentPageName = "Live Monitor";
        IsHomeActive = false;
        IsLiveActive = true;
        IsReportActive = false;
    }

    [RelayCommand]
    public void NavigateToReport()
    {
        // _reportVm already created in constructor
        CurrentPage = _reportVm;
        CurrentPageName = "Report";
        IsHomeActive = false;
        IsLiveActive = false;
        IsReportActive = true;
    }

    // ── Layout toggle ────────────────────────────────────────────────────
    [RelayCommand]
    public void ToggleLayout() => IsTouchMode = !IsTouchMode;

    public void OnWindowResized(double width)
    {
        // Auto detect only if not manually overridden
        IsTouchMode = width <= 1000;
    }

    public void OnClosed() => SerialService.Dispose();
}