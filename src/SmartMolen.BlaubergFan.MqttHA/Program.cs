using SmartMolen.BlaubergFan.MqttHA.Services;
using SmartMolen.MqttHA.Services;
using SmartMolen.BlaubergFan.Models;
using SmartMolen.MqttHA.Models;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(x =>
{
    var config = builder.Configuration.GetSection("SmartMolen.MqttHA:MqttClient").Get<MqttClientConfig>() ?? new ();

    return new HaMqttService(x.GetRequiredService<ILogger<HaMqttService>>(), config);
});

builder.Services.AddSingleton(x =>
{
    var config = builder.Configuration.GetSection("SmartMolen.BlaubergFan").Get<BlaubergFanConfig>() ?? new();

    return new SmartWiFiFanHA(x.GetRequiredService<ILogger<SmartWiFiFanHA>>(), config);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();