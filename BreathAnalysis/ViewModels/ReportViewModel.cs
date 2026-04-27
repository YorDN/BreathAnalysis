using BreathAnalysis.Models;
using BreathAnalysis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BreathAnalysis.ViewModels;

public partial class ReportViewModel : ObservableObject
{
    private readonly ReportService _reportService;
    private readonly ObservableCollection<SensorReading> _allReadings;
    private readonly ObservableCollection<SensorReading> _analysisReadings;

    // ── Plot image bytes — set by LiveView before report generation ───────
    public byte[]? PlotMq138 { get; set; }
    public byte[]? PlotMq7 { get; set; }
    public byte[]? PlotMq137 { get; set; }
    public byte[]? PlotCo2 { get; set; }
    public byte[]? PlotWinPower { get; set; }
    public byte[]? PlotOverview { get; set; }

    // ── UI state ──────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastReport))]
    private string _lastReportPath = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvgMq138))]
    [NotifyPropertyChangedFor(nameof(AvgMq7))]
    [NotifyPropertyChangedFor(nameof(AvgMq137))]
    [NotifyPropertyChangedFor(nameof(AvgCo2))]
    [NotifyPropertyChangedFor(nameof(AvgWinPower))]
    [NotifyPropertyChangedFor(nameof(AnalysisReadingCount))]
    private bool _hasAnalysisData = false;

    public bool HasLastReport => !string.IsNullOrEmpty(LastReportPath);
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    public string AutoSavePath => _reportService.AutoSavePath;

    // ── Analysis summary stats ────────────────────────────────────────────
    public double AvgMq138 => _analysisReadings.Any()
        ? _analysisReadings.Average(r => r.Mq138) : 0;
    public double AvgMq7 => _analysisReadings.Any()
        ? _analysisReadings.Average(r => r.Mq7) : 0;
    public double AvgMq137 => _analysisReadings.Any()
        ? _analysisReadings.Average(r => r.Mq137) : 0;
    public double AvgCo2 => _analysisReadings.Any()
        ? _analysisReadings.Average(r => r.Co2) : 0;
    public double AvgWinPower => _analysisReadings.Any()
        ? _analysisReadings.Average(r => r.WinPower) : 0;
    public int AnalysisReadingCount => _analysisReadings.Count;

    public ReportViewModel(
        ReportService reportService,
        ObservableCollection<SensorReading> allReadings,
        ObservableCollection<SensorReading> analysisReadings)
    {
        _reportService = reportService;
        _allReadings = allReadings;
        _analysisReadings = analysisReadings;

        _analysisReadings.CollectionChanged += (_, _) =>
        {
            HasAnalysisData = _analysisReadings.Count > 0;
        };
    }

    // ── Auto save ─────────────────────────────────────────────────────────
    [RelayCommand]
    public void AutoSaveReport()
    {
        if (!_analysisReadings.Any()) return;

        try
        {
            string path = _reportService.GenerateAutoFileName();
            _reportService.GenerateReport(
                path,
                _analysisReadings.ToList(),
                PlotMq138, PlotMq7, PlotMq137,
                PlotCo2, PlotWinPower, PlotOverview,
                isAnalysisReport: true);

            LastReportPath = path;
            StatusMessage = $"✅ Report saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠ Error: {ex.Message}";
        }
    }

    // ── Save as ───────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task SaveReportAs()
    {
        var mainWindow = Avalonia.Application.Current
            ?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes
            .IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (mainWindow == null) return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Breath Analysis Report",
                DefaultExtension = ".pdf",
                SuggestedFileName =
                    $"BreathReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("PDF File")
                    { Patterns = new[] { "*.pdf" } }
                }
            });

        if (file == null) return;

        try
        {
            _reportService.GenerateReport(
                file.Path.LocalPath,
                _analysisReadings.Any()
                    ? _analysisReadings.ToList()
                    : _allReadings.ToList(),
                PlotMq138, PlotMq7, PlotMq137,
                PlotCo2, PlotWinPower, PlotOverview,
                isAnalysisReport: _analysisReadings.Any());

            LastReportPath = file.Path.LocalPath;
            StatusMessage = $"✅ Report saved to {file.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠ Error: {ex.Message}";
        }
    }

    // ── Open reports folder ───────────────────────────────────────────────
    [RelayCommand]
    public void OpenReportsFolder()
    {
        try
        {
            _reportService.EnsureDirectory();
            Process.Start(new ProcessStartInfo
            {
                FileName = _reportService.AutoSavePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠ Could not open folder: {ex.Message}";
        }
    }
}