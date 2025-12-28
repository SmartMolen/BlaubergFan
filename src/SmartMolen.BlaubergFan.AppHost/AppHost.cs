var builder = DistributedApplication.CreateBuilder(args);

// TODO: Add mosquitto docker image
// TODO: Add Home Assistant docker image

builder.AddProject<Projects.SmartMolen_BlaubergFan_MqttHA>("smartmolen-blaubergfan-mqttha");

builder.Build().Run();