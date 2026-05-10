using BreathAnalysis.Models;
using BreathAnalysis.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BreathAnalysis.Services;

public class ReportService
{
    public string AutoSavePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "BreathAnalysis", "Reports");

    public void EnsureDirectory()
    {
        if (!Directory.Exists(AutoSavePath))
            Directory.CreateDirectory(AutoSavePath);
    }

    public string GenerateAutoFileName() =>
        Path.Combine(AutoSavePath,
            $"BreathReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf");

    public void GenerateReport(
        string filePath,
        List<SensorReading> readings,
        byte[]? plotMq138,
        byte[]? plotMq7,
        byte[]? plotMq137,
        byte[]? plotCo2,
        byte[]? plotWinPower,
        byte[]? plotOverview,
        bool isAnalysisReport = false)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var analysisReadings = isAnalysisReport
            ? readings.Where(r => r.IsAnalysis).ToList()
            : readings;

        // ── Local cell style helpers ──────────────────────────────────────
        IContainer HeaderCell(IContainer c) => c
            .DefaultTextStyle(t => t.SemiBold().FontSize(10))
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Black)
            .Background(Colors.Grey.Lighten3);

        IContainer RowCell(IContainer c) => c
            .DefaultTextStyle(t => t.FontSize(9))
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);

        IContainer RowCellColored(IContainer c, string bgColor) => c
            .DefaultTextStyle(t => t.FontSize(9))
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(bgColor);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily("Arial"));

                // ── Header ────────────────────────────────────────────────
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item()
                            .Text("Breath Analysis Report")
                            .FontSize(22)
                            .SemiBold()
                            .FontColor(Colors.Blue.Medium);

                        col.Item()
                            .Text(isAnalysisReport
                                ? $"Analysis Session — {DateTime.Now:dd MMMM yyyy HH:mm}"
                                : $"Live Monitor Report — {DateTime.Now:dd MMMM yyyy HH:mm}")
                            .FontSize(11)
                            .FontColor(Colors.Grey.Medium);
                    });

                    row.ConstantItem(60).Column(col =>
                    {
                        col.Item()
                            .Text("")
                            .FontSize(36)
                            .AlignRight();
                    });
                });

                // ── Content ───────────────────────────────────────────────
                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(16);

                    // ── Summary stats table ───────────────────────────────
                    if (analysisReadings.Any())
                    {
                        column.Item()
                            .Text("Summary Statistics")
                            .FontSize(14)
                            .SemiBold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.5f); // sensor name
                                c.RelativeColumn();     // min
                                c.RelativeColumn();     // max
                                c.RelativeColumn();     // avg
                            });

                            // Header
                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Sensor");
                                h.Cell().Element(HeaderCell).Text("Min");
                                h.Cell().Element(HeaderCell).Text("Max");
                                h.Cell().Element(HeaderCell).Text("Average");
                            });

                            // Data rows
                            var sensors = new[]
                            {
                                ("MQ-138 (VOCs)",
                                    analysisReadings.Select(r => r.Mq138).ToList()),
                                ("MQ-7 (CO)",
                                    analysisReadings.Select(r => r.Mq7).ToList()),
                                ("MQ-137 (NH₃)",
                                    analysisReadings.Select(r => r.Mq137).ToList()),
                                ("MH-Z19C (CO₂ ppm)",
                                    analysisReadings.Select(r => r.Co2).ToList()),
                                ("WinPower GSTB",
                                    analysisReadings.Select(r => r.WinPower).ToList()),
                            };

                            foreach (var (name, vals) in sensors)
                            {
                                table.Cell().Element(RowCell)
                                    .Text(name).SemiBold();
                                table.Cell().Element(RowCell)
                                    .Text(vals.Min().ToString("F1"));
                                table.Cell().Element(RowCell)
                                    .Text(vals.Max().ToString("F1"));
                                table.Cell().Element(RowCell)
                                    .Text(vals.Average().ToString("F1"));
                            }
                        });
                    }

                    // ── Overview chart ────────────────────────────────────
                    if (plotOverview != null)
                    {
                        column.Item()
                            .Text("Sensor Overview")
                            .FontSize(14)
                            .SemiBold();

                        column.Item().Image(plotOverview);
                    }

                    column.Item().PageBreak();

                    // ── Individual charts ─────────────────────────────────
                    column.Item()
                        .Text("Individual Sensor Graphs")
                        .FontSize(14)
                        .SemiBold();

                    var charts = new[]
                    {
                        ("MQ-138 — Volatile Organic Compounds (VOCs)", plotMq138),
                        ("MQ-7 — Carbon Monoxide (CO)",                plotMq7),
                        ("MQ-137 — Ammonia (NH₃)",                     plotMq137),
                        ("MH-Z19C — Carbon Dioxide (CO₂)",             plotCo2),
                        ("WinPower GSTB 400H",                         plotWinPower),
                    };

                    foreach (var (title, img) in charts)
                    {
                        if (img == null) continue;

                        column.Item()
                            .Text(title)
                            .FontSize(11)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken1);

                        column.Item().Image(img);
                        column.Item().PaddingBottom(8);
                    }

                    column.Item().PageBreak();

                    // ── Raw data table ────────────────────────────────────
                    column.Item()
                        .Text("Raw Sensor Measurements")
                        .FontSize(14)
                        .SemiBold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f); // timestamp
                            c.RelativeColumn();     // mq138
                            c.RelativeColumn();     // mq7
                            c.RelativeColumn();     // mq137
                            c.RelativeColumn();     // co2
                            c.RelativeColumn();     // winpower
                            c.RelativeColumn(0.7f); // breath
                        });

                        // Header
                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Time");
                            h.Cell().Element(HeaderCell).Text("MQ138");
                            h.Cell().Element(HeaderCell).Text("MQ7");
                            h.Cell().Element(HeaderCell).Text("MQ137");
                            h.Cell().Element(HeaderCell).Text("CO₂");
                            h.Cell().Element(HeaderCell).Text("WinPwr");
                            h.Cell().Element(HeaderCell).Text("Breath");
                        });

                        // Data rows
                        foreach (var r in analysisReadings)
                        {
                            string bg = r.IsBreath
                                ? Colors.Red.Lighten4
                                : Colors.White;

                            table.Cell()
                                .Element(c => RowCellColored(c, bg))
                                .Text(r.Timestamp.ToString("HH:mm:ss"));

                            table.Cell()
                                .Element(c => RowCellColored(c, bg))
                                .Text(r.Mq138.ToString("F0"));

                            table.Cell()
                                .Element(c => RowCellColored(c, bg))
                                .Text(r.Mq7.ToString("F0"));

                            table.Cell()
                                .Element(c => RowCellColored(c, bg))
                                .Text(r.Mq137.ToString("F0"));

                            table.Cell()
                                .Element(c => RowCellColored(c, bg))
                                .Text(r.Co2.ToString("F0"));

                            table.Cell()
                                .Element(c => RowCellColored(c, bg))
                                .Text(r.WinPower.ToString("F0"));

                            table.Cell()
                                .Element(c => RowCellColored(c, bg))
                                .Text(r.IsBreath ? "✓" : "");
                        }
                    });
                });
                // ── Footer ────────────────────────────────────────────────────────────
page.Footer().AlignCenter().Text(t =>
{
    t.Span("Breath Analysis System  —  Page ");
    t.CurrentPageNumber();
    t.Span(" of ");
    t.TotalPages();
});
            });
        }).GeneratePdf(filePath);
    }
}