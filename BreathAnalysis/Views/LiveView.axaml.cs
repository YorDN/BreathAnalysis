using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BreathAnalysis.ViewModels;
using ScottPlot;
using ScottPlot.Avalonia;
using System;


namespace BreathAnalysis.Views
{
    public partial class LiveView : UserControl
    {
        private LiveViewModel? _vm;

        public LiveView()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is LiveViewModel vm)
            {
                _vm = vm;
                SetupPlots();
                vm.PlotRefreshRequested += OnPlotRefresh;
            }
        }

        private void SetupPlots()
        {
            Setup(PlotOverview138, "MQ-138 VOCs", Color.FromHex("E74C3C"));
            Setup(PlotOverview7, "MQ-7 CO", Color.FromHex("E67E22"));
            Setup(PlotOverview137, "MQ-137 NH₃", Color.FromHex("8E44AD"));
            Setup(PlotOverviewCo2, "CO₂ ppm", Color.FromHex("2980B9"));
            Setup(PlotMq138, "MQ-138 — VOCs", Color.FromHex("E74C3C"));
            Setup(PlotMq7, "MQ-7 — CO", Color.FromHex("E67E22"));
            Setup(PlotMq137, "MQ-137 — NH₃", Color.FromHex("8E44AD"));
            Setup(PlotCo2, "CO₂ — ppm", Color.FromHex("2980B9"));
            Setup(PlotWinPower, "WinPower", Color.FromHex("27AE60"));
        }

        private static void Setup(AvaPlot plot, string title, Color color)
        {
            plot.Plot.Title(title);
            plot.Plot.Axes.Color(Colors.Black);
            plot.Plot.Axes.Title.Label.ForeColor = color;
            plot.Plot.YLabel("Raw Value");
            plot.Plot.Axes.Left.Label.ForeColor = color;
        }

        private void OnPlotRefresh()
        {
            if (_vm == null) return;

            Dispatcher.UIThread.Post(() =>
            {
                Render(PlotOverview138, _vm.DataMq138, Color.FromHex("E74C3C"));
                Render(PlotOverview7, _vm.DataMq7, Color.FromHex("E67E22"));
                Render(PlotOverview137, _vm.DataMq137, Color.FromHex("8E44AD"));
                Render(PlotOverviewCo2, _vm.DataCo2, Color.FromHex("2980B9"));
                Render(PlotMq138, _vm.DataMq138, Color.FromHex("E74C3C"));
                Render(PlotMq7, _vm.DataMq7, Color.FromHex("E67E22"));
                Render(PlotMq137, _vm.DataMq137, Color.FromHex("8E44AD"));
                Render(PlotCo2, _vm.DataCo2, Color.FromHex("2980B9"));
                Render(PlotWinPower, _vm.DataWinPower, Color.FromHex("27AE60"));
            });
        }

        private static void Render(AvaPlot plot, double[] data, Color color)
        {
            plot.Plot.Clear();
            var sig = plot.Plot.Add.Signal(data);
            sig.Color = color;
            plot.Plot.Axes.AutoScale();
            plot.Refresh();
        }

        // ── Plot image capture for PDF ────────────────────────────────────────
        public byte[] GetPlotImage(AvaPlot plot) =>
            plot.Plot.GetImageBytes(800, 300);

        public byte[] GetOverviewImage()
        {
            // Render all 4 overview plots into one wide image
            return PlotOverview138.Plot.GetImageBytes(800, 300);
        }
    }
}
