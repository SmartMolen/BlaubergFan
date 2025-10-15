using Microsoft.Extensions.Logging;
using SmartMolen.BlaubergFan.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SmartMolen.BlaubergFan.Services
{
    public class SmartWiFiFan
    {
        protected readonly ILogger _logger;
        protected readonly BlaubergFanConfig _config;

        private readonly List<Parameter> _parameters = [];

        public SmartWiFiFan(ILogger<SmartWiFiFan> logger, BlaubergFanConfig config)
        {
            _logger = logger;
            _config = config;

            // If _config.FanId is empty, use search command to find the fanID based on the hostname.

            _parameters = BuildSupportedParameterList();
        }

        public async Task ValidateConfig()
        {
            var defaultConfig = new BlaubergFanConfig();

            var devices = await DiscoverDevices();
            if (!devices.Any()) return;

            if (string.IsNullOrWhiteSpace(_config.Hostname) || _config.Hostname == defaultConfig.Hostname)
            {
                _config.Hostname = devices.First().Key;
                _config.FanId = devices.First().Value;
            }
            else if (string.IsNullOrWhiteSpace(_config.FanId) || _config.FanId == defaultConfig.FanId)
            {
                // TODO: convert hostname to ipaddress
                if(devices.TryGetValue(_config.Hostname, out var fanId))
                {
                    _config.FanId = fanId;
                }
            }
        }

        private List<Parameter> BuildFullParameterList()
        {
            var list = new List<Parameter>();

            // after 6f, no reply...
            for (byte i = 70; i < 0xef; i++)
            {
                list.Add(new(i));
            }

            return list;
        }

        private List<Parameter> BuildSupportedParameterList()
        {
            var list = new List<Parameter>()
            {
                new(0x0001, "Fan On/Off" ),
                new(0x0003, "24 hours mode selection"),
                new(0x0004, "Current fan speed (rpm)"),
                new(0x0005, "BOOST mode On/Off"),
                new(0x0006, "Current BOOST timer countdown in seconds"),
                new(0x0007, "Current status of the built-in timer"),
                new(0x0008, "Current status of fan operation by humidity sensor"),
                new(0x000a, "Current status of fan operation by temperature sensor"),
                new(0x000c, "Current status of fan operation by signal from an external switch"),
                new(0x000d, "Current status of fan operation in interval ventilation mode"),
                new(0x000e, "Current status of fan operation in SILENT mode"),
                new(0x000f, "Permission of operation based on humidity sensor readings" ),
                new(0x0011),
                new(0x0013),
                new(0x0014),
                new(0x0016),
                new(0x0017),
                new(0x0018, "Max speed setpoint"),
                new(0x001a, "Silent speed setpoint"),
                new(0x001b),
                new(0x001d),
                new(0x001e),
                new(0x001f),
                new(0x0020),
                new(0x0021),
                new(0x0023),
                new(0x0024),
                new(0x002e, "Humidity"),
                new(0x0031, "Temperature"),

                new(0x007c, "Device search on the local Ethernet network"),
                new(0x0086, "Controller base firmware version and date"),
                new(0x0094, "Wi-Fi operation mode"),
                // These parameters contain secrets, so not including it in the request.
                // new(0x0095, "Wi-Fi name in Client mode"),
                // new(0x0096, "Wi-Fi password"),
                // new(0x0099, "Wi-Fi data encryption type" ),
                new(0x009a, "Wi-Fi frequency channel"),
                new(0x009b, "Wi-Fi module DHCP" ),
                new(0x009c, "IP address assigned to Wi-Fi module"),
                new(0x009d, "Wi-Fi module subnet mask"),
                new(0x009e, "Wi-Fi module main gateway"),
                new(0x00a3, "Current Wi-Fi module IP address"),
                new(0x00b9, "Unit type"),

            };


            return list;
        }

        public async Task<ReadOnlyCollection<Parameter>> UpdateFanData()
        {
            var parameters = await SendCommand(Function.Read, _parameters) ?? [];

            _logger.LogDebug(string.Join(Environment.NewLine, parameters.Select(p => $"{(int)p.Address}/0x{p.Address:x4} - {p.Key}: {p.IntValue} / {p.StringValue}")));

            return parameters.AsReadOnly();
        }

        public async Task<Dictionary<string, string>> DiscoverDevices(int port = 4000)
        {
            // TODO: Check if this method can use Send/SendCommand methods.
            byte[] payloadBytes = new List<byte>() {
                    0xfd,
                    0xfd,
                    0x02,
                    0x10,
                    0x44,
                    0x45,
                    0x46,
                    0x41,
                    0x55,
                    0x4c,
                    0x54,
                    0x5f,
                    0x44,
                    0x45,
                    0x56,
                    0x49,
                    0x43,
                    0x45,
                    0x49,
                    0x44,
                    0x04,
                    0x31,
                    0x31,
                    0x31,
                    0x31,
                    0x01,
                    0x7c,
                    0xf8,
                    0x05
                }.ToArray();

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            sock.Bind(new IPEndPoint(IPAddress.Any, port));
            sock.ReceiveTimeout = 1000;

            var list = new Dictionary<string, string>();
            int i = 0;

            while (i++ < 10)
            {
                var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);

                _logger.LogTrace($"SendData: {string.Join(' ', payloadBytes.Select(x => x.ToString("x2")))}");

                sock.SendTo(payloadBytes, broadcastEndPoint);

                try
                {
                    byte[] buffer = new byte[1024];
                    EndPoint senderRemote = new IPEndPoint(IPAddress.Any, 0);
                    int received = sock.ReceiveFrom(buffer, ref senderRemote);

                    byte[] data = new byte[received];
                    Array.Copy(buffer, data, received);

                    _logger.LogTrace($"ReceivedData: {string.Join(' ', data.Select(x => x.ToString("x2")))}");

                    var response = ParseResponse(data.ToList());

                    var fanId = response.FirstOrDefault(x => x.Address == 0x7c)?.StringValue;
                    if(fanId != null && !list.ContainsValue(fanId) && fanId != BlaubergFanConfig.DefaultDeviceId)
                    {
                        string senderIp = ((IPEndPoint)senderRemote).Address.ToString();
                        list.Add(senderIp, fanId);
                    }

                }
                catch (SocketException)
                {
                    // Timeout or other socket error, continue loop
                }

                Thread.Sleep(100);
            }

            sock.Close();

            await Task.CompletedTask;

            return list;

        }

        public async Task<List<Parameter>> SendCommand(Function function, List<Parameter> parameters, List<byte[]>? values = null)
        {
            var dataBytes = new List<byte>()
            {
                (byte)function,
            };

            if (function == Function.Read)
            {
                dataBytes.AddRange(parameters.Select(x => x.Address));
            }
            else if (function == Function.Write || function == Function.WriteReturn)
            {
                if (values == null || parameters.Count != values.Count)
                {
                    throw new ArgumentException("Parameter and Value list must have the same length");
                }

                for (int i = 0; i < parameters.Count; i++)
                {
                    dataBytes.Add(parameters[i].Address);
                    var vals = values[i];
                    if (vals.Length == 0)
                    {
                        // empty data
                        continue;
                    }
                    else if (vals.Length == 1)
                    {
                        dataBytes.Add(vals[0]);
                    }
                    else
                    {
                        dataBytes.Add(0xfe);
                        dataBytes.Add((byte)vals.Length);
                        dataBytes.AddRange(vals);
                    }
                }
            }

            var response = await Send(dataBytes);
            if(response.Any())
            {
                var responseParameters = ParseResponse(response);
                return responseParameters;
            }

            return [];
        }

        public List<Parameter> ParseResponse(List<byte> data)
        {
            var parameters = new List<Parameter>();

            // skip header, type bytes
            var i = 3;
            if (data.Count <= i)
            {
                // if there are not header bytes, invalid data received.
                return parameters;
            }


            // Id
            var idLength = (int)data[i++];
            var id = Encoding.ASCII.GetString(data.Slice(i, idLength).ToArray());
            i += idLength;

            // Password
            var passLength = (int)data[i++];
            var pass = Encoding.ASCII.GetString(data.Slice(i, passLength).ToArray());
            i += passLength;

            // Function
            var function = data[i++];

            // Response
            var responseLength = data.Count - i - 2;
            var response = data.Slice(i, responseLength);

            // 

            var dataIndex = 0;
            while (dataIndex < responseLength)
            {
                var currentByte = response[dataIndex++];
                var valueLength = 1;

                if (currentByte == 0xfc)
                {
                    // change function
                    _logger.LogDebug("\tChange Function");
                    // TODO: What to do???
                    continue;
                }
                else if (currentByte == 0xfd)
                {
                    // parameter not supported
                    currentByte = response[dataIndex++];
                    _logger.LogWarning($"\tParameter ({currentByte:x2}) not supported");

                    continue;
                }
                else if (currentByte == 0xfe)
                {
                    // change size.
                    valueLength = (int)response[dataIndex++];
                    currentByte = response[dataIndex++];
                }
                else if (currentByte == 0xff)
                {
                    // and then - by the Value itself. Change the high byte for parameter numbers within a single packet.
                    _logger.LogDebug("\tHigh Byte change");
                    // TODO: What to do???
                    continue;
                }

                // parameter ID
                var parameterId = currentByte;

                if(response.Count() < dataIndex+ valueLength)
                {
                    _logger.LogDebug($"ResponseContent: {parameterId.ToString("x2")}: NoValue.");
                    continue;
                }

                var parameterValue = response.Slice(dataIndex, valueLength);
                dataIndex += valueLength;

                // _logger.LogTrace($"ResponseContent: {parameterId.ToString("x2")}:{string.Join(" ", parameterValue.Select(x => x.ToString("x2")))}");

                var parameter = _parameters.FirstOrDefault(x => x.Address == parameterId);
                if (parameter != null)
                {
                    parameter.Value = parameterValue;
                    parameters.Add(parameter);
                }
                else
                {
                    _logger.LogWarning($"\tParameter ({parameterId:x2}) not found!");
                }

            }

            // Checksum
            var checksum = data.Slice(responseLength, 2);

            return parameters;
        }

        private async Task<List<byte>> Send(List<byte> dataBytes)
        {
            // TODO: change input to parameter list? Or provide overload method.

            var headerBytes = new List<byte>()
            {
                0x02, // type param
            };
            headerBytes.AddRange([0x10]); // always 16 chars long
            headerBytes.AddRange(Encoding.ASCII.GetBytes(_config.FanId));
            headerBytes.AddRange([byte.Parse(_config.Password.Length.ToString("00"))]);
            headerBytes.AddRange(Encoding.ASCII.GetBytes(_config.Password));

            var headerDataBytes = new List<byte>();
            headerDataBytes.AddRange(headerBytes);
            headerDataBytes.AddRange(dataBytes);

            var sendBufferBytes = new List<byte>()
                {
                    0xfd,
                    0xfd,
                };
            sendBufferBytes.AddRange(headerDataBytes);
            sendBufferBytes.AddRange(GetChecksumBytes(headerDataBytes));

            _logger.LogTrace($"SendingData: {string.Join(' ', sendBufferBytes.Select(x => x.ToString("x2")))}");

            var socket = GetSocket();
            try
            {
                socket.Connect(_config.Hostname, _config.Port);

                socket.Send(sendBufferBytes.ToArray());

                var receiveBuffer = new byte[4096];
                var loopIndex = 0;
                while (loopIndex < 500)
                {
                    if (socket.Available > 0)
                    {
                        break;
                    }

                    await Task.Delay(10);
                    loopIndex++;
                }
                if (loopIndex == 500)
                {
                    _logger.LogError($"No data received after {loopIndex} loops.");
                }
                else
                {
                    var receiveLength = await socket.ReceiveAsync(receiveBuffer);

                    var response = receiveBuffer?.Take(receiveLength).ToList() ?? [];

                    _logger.LogTrace($"ReceivedData: {string.Join(' ', response.Select(x => x.ToString("x2")))}");

                    return response;
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError("Connect timeout: " + _config.Hostname + " " + ex.Message);
            }
            finally
            {
                socket.Close();
            }

            return [];
        }

        #region Utils

        private Socket GetSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 10000; // Timeout in milliseconds

            return socket;
        }

        public byte[] GetChecksumBytes(List<byte> payload)
        {
            // convert byte dec-value to hex
            // sum hex values
            // convert hex to dec (2 bytes)

            var intList = payload.Select(x => (int)x);
            var sum = intList.Sum();
            var subBytes = BitConverter.GetBytes(sum);
            var sumHex = sum.ToString("x4");

            var bytes = new byte[2]
            {
                Convert.ToByte(sumHex.Substring(0,2), 16),
                Convert.ToByte(sumHex.Substring(2,2), 16)
            };

            return [bytes[1], bytes[0]];
        }

        #endregion
    }
}
