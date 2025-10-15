using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMolen.BlaubergFan.Models
{
    public class BlaubergFanConfig
    {
        public const string DefaultDeviceId = "DEFAULT_DEVICEID";

        public string Hostname { set; get; } = "127.0.0.1";
        public int Port { set; get; } = 4000;
        public string Password { set; get; } = "1111";
        public string FanId { set; get; } = DefaultDeviceId;
        public string Name { set; get; } = string.Empty;


    }
}
