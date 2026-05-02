using BreathAnalysis.Models;
using System;
using System.IO.Ports;

namespace BreathAnalysis.Services
{
    public class SerialService
    {
        private SerialPort? _port;

        // ── Events ───────────────────────────────────────────────────────────
        public event Action<SensorReading>? ReadingReceived;
        public event Action<string>? StatusReceived;   // SESSION_STARTED etc.
        public event Action<string>? ErrorOccurred;

        // ── State ────────────────────────────────────────────────────────────
        public bool IsConnected => _port?.IsOpen == true;
        public string CurrentPort { get; private set; } = "";

        // ── Breath detection ─────────────────────────────────────────────────
        private double _avgMq138 = 0;
        private const double Sensitivity = 50;

        // ── Available ports ──────────────────────────────────────────────────
        public static string[] GetAvailablePorts() =>
            SerialPort.GetPortNames();

        // ── Connect / Disconnect ─────────────────────────────────────────────
        public bool Connect(string portName, int baud = 9600)
        {
            try
            {
                Disconnect();
                _port = new SerialPort(portName, baud) { ReadTimeout = 3000 };
                _port.DataReceived += OnDataReceived;
                _port.Open();
                CurrentPort = portName;
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Cannot connect to {portName}: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (_port?.IsOpen == true) _port.Close();
            _port = null;
            CurrentPort = "";
            _avgMq138 = 0;
        }

        // ── Send command to Arduino ───────────────────────────────────────────
        public void SendCommand(string cmd)
        {
            if (_port?.IsOpen == true)
                _port.WriteLine(cmd);
        }

        public void StartSession() => SendCommand("START");
        public void StartContinuous() => SendCommand("CONTINUOUS");
        public void EndContinuous() => SendCommand("END");
        public void StopAll() => SendCommand("STOP");

        // ── Parse incoming data ───────────────────────────────────────────────
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = _port!.ReadLine().Trim();

                if (string.IsNullOrWhiteSpace(line)) return;

                // Status messages from Arduino
                if (line is "SESSION_STARTED" or "SESSION_ENDED"
                        or "CONTINUOUS_STARTED" or "CONTINUOUS_ENDED"
                        or "FAN_ON" or "FAN_OFF"
                        or "SYSTEM_STOPPED" or "READY"
                        or "BUSY" or "NO_CONTINUOUS_SESSION"
                        or "Breath Analysis System Ready")
                {
                    StatusReceived?.Invoke(line);
                    return;
                }

                // Skip header lines
                if (line.Contains("Send") ||
                    line.Contains("Commands") ||
                    line.Contains("STATE=") ||
                    line.StartsWith("MQ138")) return;

                // Parse sensor CSV: MQ138, MQ7, MQ137, CO2, WinPower
                string[] parts = line.Split(',');
                if (parts.Length < 5) return;

                if (!double.TryParse(parts[0],
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double v138)) return;
                if (!double.TryParse(parts[1],
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double v7)) return;
                if (!double.TryParse(parts[2],
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double v137)) return;
                if (!double.TryParse(parts[3],
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double vCo2)) return;
                if (!double.TryParse(parts[4],
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double vWin)) return;

                // Breath detection on MQ138
                _avgMq138 = (_avgMq138 == 0)
                    ? v138
                    : (_avgMq138 * 0.98) + (v138 * 0.02);
                bool isBreath = v138 > (_avgMq138 + Sensitivity);

                var reading = new SensorReading
                {
                    Timestamp = DateTime.Now,
                    Mq138 = v138,
                    Mq7 = v7,
                    Mq137 = v137,
                    Co2 = vCo2,
                    WinPower = vWin,
                    IsBreath = isBreath,
                    IsAnalysis = false   // set by HomeViewModel during session
                };

                ReadingReceived?.Invoke(reading);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Serial error: {ex.Message}");
            }
        }

        public void Dispose() => Disconnect();
    }
}
