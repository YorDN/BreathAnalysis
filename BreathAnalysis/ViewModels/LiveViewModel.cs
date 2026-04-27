using BreathAnalysis.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BreathAnalysis.ViewModels;

public partial class LiveViewModel : ObservableObject
{
    private readonly ObservableCollection<SensorReading> _allReadings;
    private const int BufferSize = 100;

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

    public string BreathColor => IsBreath ? "#E74C3C" : "#27AE60";
    public string BreathLabel => IsBreath ? "Detected" : "None";

    public event Action? PlotRefreshRequested;

    public LiveViewModel(ObservableCollection<SensorReading> allReadings)
    {
        _allReadings = allReadings;
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
}