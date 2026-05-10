using BreathAnalysis.Models;
using BreathAnalysis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BreathAnalysis.ViewModels;

public partial class LiveViewModel : ObservableObject
{
    private readonly ObservableCollection<SensorReading> _allReadings;
    private const int BufferSize = 100;

    // ── Layout awareness ──────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTouch))]
    private bool _isDesktop = true;
    public bool IsTouch => !IsDesktop;

    // ── Touch sensor selection ────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMq138Selected))]
    [NotifyPropertyChangedFor(nameof(IsMq7Selected))]
    [NotifyPropertyChangedFor(nameof(IsMq137Selected))]
    [NotifyPropertyChangedFor(nameof(IsCo2Selected))]
    [NotifyPropertyChangedFor(nameof(IsWinSelected))]
    private string _selectedSensor = "MQ138";

    public bool IsMq138Selected => SelectedSensor == "MQ138";
    public bool IsMq7Selected => SelectedSensor == "MQ7";
    public bool IsMq137Selected => SelectedSensor == "MQ137";
    public bool IsCo2Selected => SelectedSensor == "CO2";
    public bool IsWinSelected => SelectedSensor == "WIN";

    [RelayCommand]
    public void SelectSensor(string sensor)
    {
        SelectedSensor = sensor;
        TouchPlotRefreshRequested?.Invoke(sensor);
    }

    public event Action<string>? TouchPlotRefreshRequested;

    // ── Plot buffers ──────────────────────────────────────────────────────
    public double[] DataMq138 { get; } = new double[BufferSize];
    public double[] DataMq7 { get; } = new double[BufferSize];
    public double[] DataMq137 { get; } = new double[BufferSize];
    public double[] DataCo2 { get; } = new double[BufferSize];
    public double[] DataWinPower { get; } = new double[BufferSize];

    // ── Latest values ─────────────────────────────────────────────────────
    [ObservableProperty] private double _latestMq138 = 0;
    [ObservableProperty] private double _latestMq7 = 0;
    [ObservableProperty] private double _latestMq137 = 0;
    [ObservableProperty] private double _latestCo2 = 0;
    [ObservableProperty] private double _latestWinPower = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BreathColor))]
    [NotifyPropertyChangedFor(nameof(BreathLabel))]
    private bool _isBreath = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StreamButtonLabel))]
    [NotifyPropertyChangedFor(nameof(StreamIndicatorColor))]
    [NotifyPropertyChangedFor(nameof(StreamStatusText))]
    private bool _isStreaming = false;

    public string BreathColor => IsBreath ? "#E74C3C" : "#27AE60";
    public string BreathLabel => IsBreath ? "Detected" : "None";
    public string StreamButtonLabel => IsStreaming ? "⏹ Stop Live Stream" : "▶ Start Live Stream";
    public string StreamIndicatorColor => IsStreaming ? "#27AE60" : "#BDC3C7";
    public string StreamStatusText => IsStreaming ? "Streaming live data..." : "Stream stopped";


    public event Action? PlotRefreshRequested;

    private readonly SerialService _serial;

    public LiveViewModel(
        ObservableCollection<SensorReading> allReadings,
        SerialService serial)
    {
        _allReadings = allReadings;
        _serial = serial;
        _allReadings.CollectionChanged += OnReadingsChanged;
    }

    private void OnReadingsChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null) return;

        foreach (SensorReading r in e.NewItems)
        {
            Array.Copy(DataMq138, 1, DataMq138, 0, BufferSize - 1);
            Array.Copy(DataMq7, 1, DataMq7, 0, BufferSize - 1);
            Array.Copy(DataMq137, 1, DataMq137, 0, BufferSize - 1);
            Array.Copy(DataCo2, 1, DataCo2, 0, BufferSize - 1);
            Array.Copy(DataWinPower, 1, DataWinPower, 0, BufferSize - 1);

            DataMq138[^1] = r.Mq138;
            DataMq7[^1] = r.Mq7;
            DataMq137[^1] = r.Mq137;
            DataCo2[^1] = r.Co2;
            DataWinPower[^1] = r.WinPower;

            LatestMq138 = r.Mq138;
            LatestMq7 = r.Mq7;
            LatestMq137 = r.Mq137;
            LatestCo2 = r.Co2;
            LatestWinPower = r.WinPower;
            IsBreath = r.IsBreath;

            PlotRefreshRequested?.Invoke();
        }
    }
    [RelayCommand]
    public void ToggleStream()
    {
        if (IsStreaming)
        {
            _serial.EndContinuous();
            IsStreaming = false;
        }
        else
        {
            _serial.StartContinuous();
            IsStreaming = true;
        }
    }

    public void OnArduinoStatus(string status)
    {
        switch (status)
        {
            case "CONTINUOUS_STARTED":
                IsStreaming = true;
                break;
            case "CONTINUOUS_ENDED":
            case "SYSTEM_STOPPED":
            case "READY":
                IsStreaming = false;
                break;
        }
    }

}