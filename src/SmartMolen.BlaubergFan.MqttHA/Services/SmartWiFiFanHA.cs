using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SmartMolen.BlaubergFan.Models;
using SmartMolen.BlaubergFan.MqttHA.Models;
using SmartMolen.BlaubergFan.Services;
using SmartMolen.MqttHA.Models;

namespace SmartMolen.BlaubergFan.MqttHA.Services
{
    public class SmartWiFiFanHA : SmartWiFiFan, IHaDiscoveryProvider
    {
        public SmartWiFiFanHA(ILogger<SmartWiFiFanHA> logger, BlaubergFanConfig config) : base(logger, config)
        {
            FanStatus = new();
        }

        public async Task Update()
        {
            var parameters = await UpdateFanData();

            if (parameters.Any())
            {
                var status = FanStatus.FromParameters(parameters.ToList());

                FanStatus = status;
            }

            await Task.CompletedTask;
        }

        public FanStatus FanStatus
        {
            get; private set;
        }

        public HaDevice GetHaDevice()
        {
            var deviceName = HaDevice.SanitizeName(_config.FanId);

            var device = new HaDevice(GetType().Name)
            {
                Name = deviceName,
                Identifiers = [deviceName],
                SerialNumber = _config.FanId,
                Manufacturer = "Blauberg",
                Model = "Smart Wi-Fi Fan"
            };

            return device;
        }

        private async Task SetBoostMode(bool mode)
        {
            var valueByte = (byte)(mode ? 1 : 0);

            await SendCommand(Function.WriteReturn, [new Parameter(0x05, "BoostMode")], [[valueByte]]);
        }

        public Dictionary<string, HaEntity> GetHaEntities()
        {
            var device = GetHaDevice();

            var list = new Dictionary<string, HaEntity>();

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "FanRunning",
                    Device = device,
                    UniqueId = $"{device.Name}_FanRunning",
                    Icon = "mdi:fan",
                    ValueTemplate = "{{ value_json.FanRunning }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "Mode24Hours",
                    Device = device,
                    UniqueId = $"{device.Name}_Mode24Hours",
                    Icon = "mdi:fan-clock",
                    ValueTemplate = "{{ value_json.Mode24Hours }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "FanSpeed",
                    Device = device,
                    UniqueId = $"{device.Name}_FanSpeed",
                    Icon = "mdi:fan",
                    ValueTemplate = "{{ value_json.FanSpeed }}",
                    UnitOfMeasure = "rpm",
                    StateClass = "MEASUREMENT",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaSwitch(false, device.DeviceTopic)
                {
                    InternalName = "ModeBoost",
                    Device = device,
                    UniqueId = $"{device.Name}_ModeBoost",
                    Icon = "mdi:fan-plus",
                    ValueTemplate = "{{ value_json.ModeBoost }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "ModeBoostSecondsRemaining",
                    Device = device,
                    UniqueId = $"{device.Name}_ModeBoostSecondsRemaining",
                    Icon = "mdi:fan-clock",
                    ValueTemplate = "{{ value_json.ModeBoostSecondsRemaining }}",
                    UnitOfMeasure = "s",
                    DeviceClass = "DURATION",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "RunningTimer",
                    Device = device,
                    UniqueId = $"{device.Name}_RunningTimer",
                    Icon = "mdi:timer",
                    ValueTemplate = "{{ value_json.RunningTimer }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "RunningHumiditySensor",
                    Device = device,
                    UniqueId = $"{device.Name}_RunningHumiditySensor",
                    Icon = "mdi:water-percent",
                    ValueTemplate = "{{ value_json.RunningHumiditySensor }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "RunningTemperatureSensor",
                    Device = device,
                    UniqueId = $"{device.Name}_RunningTemperatureSensor",
                    Icon = "mdi:thermometer",
                    ValueTemplate = "{{ value_json.RunningTemperatureSensor }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "RunningExternalSwitch",
                    Device = device,
                    UniqueId = $"{device.Name}_RunningExternalSwitch",
                    Icon = "mdi:light-switch",
                    ValueTemplate = "{{ value_json.RunningExternalSwitch }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "RunningIntervalMode",
                    Device = device,
                    UniqueId = $"{device.Name}_RunningIntervalMode",
                    Icon = "mdi:timer",
                    ValueTemplate = "{{ value_json.RunningIntervalMode }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaBinarySensor(false, device.DeviceTopic)
                {
                    InternalName = "RunningSilentMode",
                    Device = device,
                    UniqueId = $"{device.Name}_RunningSilentMode",
                    Icon = "mdi:speedometer-slow",
                    ValueTemplate = "{{ value_json.RunningSilentMode }}",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "MaxSpeedSetpoint",
                    Device = device,
                    UniqueId = $"{device.Name}_MaxSpeedSetpoint",
                    Icon = "mdi:fan",
                    ValueTemplate = "{{ value_json.MaxSpeedSetpoint }}",
                    UnitOfMeasure = "%",
                    StateClass = "MEASUREMENT",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "SilentSpeedSetpoint",
                    Device = device,
                    UniqueId = $"{device.Name}_SilentSpeedSetpoint",
                    Icon = "mdi:fan",
                    ValueTemplate = "{{ value_json.SilentSpeedSetpoint }}",
                    UnitOfMeasure = "%",
                    StateClass = "MEASUREMENT",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "Humidity",
                    Device = device,
                    UniqueId = $"{device.Name}_Humidity",
                    Icon = "mdi:water-percent",
                    ValueTemplate = "{{ value_json.Humidity }}",
                    UnitOfMeasure = "%",
                    DeviceClass = "HUMIDITY",
                };

                list.Add(entity.InternalName, entity);
            }

            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "Temperature",
                    Device = device,
                    UniqueId = $"{device.Name}_Temperature",
                    Icon = "mdi:thermometer",
                    ValueTemplate = "{{ value_json.Temperature }}",
                    UnitOfMeasure = "°C",
                    DeviceClass = "TEMPERATURE",
                };

                list.Add(entity.InternalName, entity);
            }

            // Diagnostic entities
            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "DeviceId",
                    Device = device,
                    UniqueId = $"{device.Name}_DeviceId",
                    ValueTemplate = "{{ value_json.DeviceId }}",
                    EntityCategory = "diagnostic"
                };

                list.Add(entity.InternalName, entity);
            }
            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "Firmware",
                    Device = device,
                    UniqueId = $"{device.Name}_Firmware",
                    ValueTemplate = "{{ value_json.Firmware }}",
                    EntityCategory = "diagnostic"
                };

                list.Add(entity.InternalName, entity);
            }
            {
                var entity = new HaEntity(false, device.DeviceTopic)
                {
                    InternalName = "UnitType",
                    Device = device,
                    UniqueId = $"{device.Name}_UnitType",
                    ValueTemplate = "{{ value_json.UnitType }}",
                    EntityCategory = "diagnostic"
                };

                list.Add(entity.InternalName, entity);
            }

            return list;
        }

        public async Task ReceiveCommand(HaEntity entity, string payload)
        {
            if (entity.InternalName == "ModeBoost")
            {
                await SetBoostMode(payload == "ON");

            }

            await Task.CompletedTask;
        }
    }
}