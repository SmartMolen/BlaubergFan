using SmartMolen.MqttHA.Services;
using System.Text.Json;

namespace SmartMolen.BlaubergFan.MqttHA.Services;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HaMqttService _mqtt;
    private readonly SmartWiFiFanHA _blaubergFan;

    public Worker(ILogger<Worker> logger, HaMqttService mqtt, SmartWiFiFanHA blaubergFan)
    {
        _logger = logger;
        _mqtt = mqtt;
        _blaubergFan = blaubergFan;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _blaubergFan.ValidateConfig();

        await _mqtt.Connect();

        await _mqtt.SendHaDiscovery(_blaubergFan);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _blaubergFan.Update();

            var json = JsonSerializer.Serialize(_blaubergFan.FanStatus, _blaubergFan.FanStatus.GetType(), new JsonSerializerOptions()
            {
                WriteIndented = true
            });

            await _mqtt.SendJsonString(_blaubergFan.GetHaDevice().DeviceTopic, json);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
