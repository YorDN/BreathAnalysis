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

        protected override void OnAttachedToVisualTree(
            Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is LiveViewModel vm)
            {
                // Unsubscribe first to avoid double subscription
                vm.PlotRefreshRequested -= OnPlotRefresh;
                _vm = vm;
                SetupPlots();
                vm.PlotRefreshRequested += OnPlotRefresh;
            }
        }

        protected override void OnDetachedFromVisualTree(
            Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (_vm != null)
                _vm.PlotRefreshRequested -= OnPlotRefresh;
        }

        private void SetupPlots()
        {
            Setup(PlotOverview138, "Volatile Organic Compounds", Color.FromHex("E74C3C"));
            Setup(PlotOverview7, "Carbon Monoxide", Color.FromHex("E67E22"));
            Setup(PlotOverview137, "Ammonia", Color.FromHex("8E44AD"));
            Setup(PlotOverviewCo2, "Carbon Dioxide", Color.FromHex("2980B9"));
            Setup(PlotMq138, "Volatile Organic Compounds (VOCs)", Color.FromHex("E74C3C"));
            Setup(PlotMq7, "Carbon Monoxide (CO)", Color.FromHex("E67E22"));
            Setup(PlotMq137, "Ammonia (NH₃)", Color.FromHex("8E44AD"));
            Setup(PlotCo2, "Carbon Dioxide (CO₂)", Color.FromHex("2980B9"));
            Setup(PlotWinPower, "Oxygen (O₂)", Color.FromHex("27AE60"));
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

                // Pass plot images to ReportViewModel every refresh
                PassImagesToReport();
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

        // ── Pass plot images to ReportViewModel ───────────────────────────────
        private void PassImagesToReport()
        {
            // Walk up the visual tree to find MainWindowViewModel
            var mainVm = (DataContext as LiveViewModel) == null
                ? null
                : FindMainViewModel();

            if (mainVm?.ReportVm == null) return;

            mainVm.ReportVm.PlotMq138 = GetImage(PlotMq138);
            mainVm.ReportVm.PlotMq7 = GetImage(PlotMq7);
            mainVm.ReportVm.PlotMq137 = GetImage(PlotMq137);
            mainVm.ReportVm.PlotCo2 = GetImage(PlotCo2);
            mainVm.ReportVm.PlotWinPower = GetImage(PlotWinPower);
            mainVm.ReportVm.PlotOverview = GetImage(PlotOverview138);
        }

        private MainWindowViewModel? FindMainViewModel()
        {
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent.DataContext is MainWindowViewModel vm)
                    return vm;
                parent = parent.Parent as Avalonia.Controls.Control;
            }
            return null;
        }

        private static byte[] GetImage(AvaPlot plot) =>
            plot.Plot.GetImageBytes(800, 300);
    }

}
