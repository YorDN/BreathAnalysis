using System;

namespace BreathAnalysis.Models
{
    public class SensorReading
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double Mq138 { get; set; }  // VOCs
        public double Mq7 { get; set; }  // CO
        public double Mq137 { get; set; }  // Ammonia
        public double Co2 { get; set; }  // ppm
        public double WinPower { get; set; }  // raw ADC
        public bool IsBreath { get; set; }
        public bool IsAnalysis { get; set; }  // recorded during analysis session
    }
}
