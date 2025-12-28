using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using SmartMolen.BlaubergFan.Models;

namespace SmartMolen.BlaubergFan.MqttHA.Models
{
    public class FanStatus
    {
        public string DeviceId { get; set; } = string.Empty;

        public string FanRunning { get; set; } = "OFF";
        public string Mode24Hours { get; set; } = "OFF";
        public int FanSpeed { get; set; } = 0;
        public int MaxSpeedSetpoint { get; set; } = 0;
        public int SilentSpeedSetpoint { get; set; } = 0;

        public string ModeBoost { get; set; } = "OFF";
        public int ModeBoostSecondsRemaining { get; set; } = 0;

        public int Humidity { get; set; } = 0;
        public decimal Temperature { get; set; } = 0.0m;

        public string RunningTimer { get; set; } = "OFF";
        public string RunningHumiditySensor { get; set; } = "OFF";
        public string RunningTemperatureSensor { get; set; } = "OFF";
        public string RunningExternalSwitch { get; set; } = "OFF";
        public string RunningIntervalMode { get; set; } = "OFF";
        public string RunningSilentMode { get; set; } = "OFF";

        public string Firmware { get; set; } = "";
        public string UnitType { get; set; } = "";

        public static FanStatus FromParameters(List<Parameter> parameters)
        {
            var fm = parameters.FirstOrDefault(x => x.Address == 0x86)?.Value;
            var firmware = "";
            if (fm != null && fm.Count == 6)
            {
                var fmYear = BitConverter.ToInt16([fm[4], fm[5]]);
                firmware = $"{fm[0]}.{fm[1]} / {fm[2]}-{fm[3]}-{fmYear}";
            }

            var status = new FanStatus()
            {
                FanRunning = parameters.FirstOrDefault(x => x.Address == 0x01)?.BoolValue ?? false ? "ON" : "OFF",
                Mode24Hours = parameters.FirstOrDefault(x => x.Address == 0x03)?.BoolValue ?? false ? "ON" : "OFF",
                FanSpeed = parameters.FirstOrDefault(x => x.Address == 0x04)?.IntValue ?? 0,
                ModeBoost = parameters.FirstOrDefault(x => x.Address == 0x05)?.BoolValue ?? false ? "ON" : "OFF",
                ModeBoostSecondsRemaining = parameters.FirstOrDefault(x => x.Address == 0x06)?.IntValue ?? 0,
                RunningTimer = parameters.FirstOrDefault(x => x.Address == 0x07)?.BoolValue ?? false ? "ON" : "OFF",
                RunningHumiditySensor = parameters.FirstOrDefault(x => x.Address == 0x08)?.BoolValue ?? false ? "ON" : "OFF",
                RunningTemperatureSensor = parameters.FirstOrDefault(x => x.Address == 0x0a)?.BoolValue ?? false ? "ON" : "OFF",
                RunningExternalSwitch = parameters.FirstOrDefault(x => x.Address == 0x0c)?.BoolValue ?? false ? "ON" : "OFF",
                RunningIntervalMode = parameters.FirstOrDefault(x => x.Address == 0x0d)?.BoolValue ?? false ? "ON" : "OFF",
                RunningSilentMode = parameters.FirstOrDefault(x => x.Address == 0x0e)?.BoolValue ?? false ? "ON" : "OFF",

                // 0f: Permission of operation based on humidity sensor readings

                MaxSpeedSetpoint = parameters.FirstOrDefault(x => x.Address == 0x18)?.IntValue ?? 0, // in %
                SilentSpeedSetpoint = parameters.FirstOrDefault(x => x.Address == 0x1a)?.IntValue ?? 0, // in %

                Humidity = parameters.FirstOrDefault(x => x.Address == 0x2e)?.IntValue ?? 0,
                Temperature = parameters.FirstOrDefault(x => x.Address == 0x31)?.IntValue ?? 0,

                DeviceId = parameters.FirstOrDefault(x => x.Address == 0x7c)?.StringValue ?? string.Empty,
                Firmware = firmware,
                UnitType = parameters.FirstOrDefault(x => x.Address == 0xb9)?.IntValue.ToString() ?? string.Empty,
            };

            return status;
        }


    }
}