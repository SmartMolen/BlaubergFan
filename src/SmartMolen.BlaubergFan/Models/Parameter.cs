using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMolen.BlaubergFan.Models
{
    public class Parameter
    {
        public byte Address { get; set; } = 0x0;
        public string Key { get; set; } = string.Empty;
        public Dictionary<int, string> Mapping { get; set; } = [];

        public List<byte> Value { get; set; } = [];

        public int IntValue
        {
            get
            {
                if (Value.Count == 0)
                {
                    return 0;
                }
                else if (Value.Count == 1)
                {
                    return (int)Value[0];
                }

                return BitConverter.ToInt16(Value.ToArray());
            }
        }
        public bool BoolValue
        {
            get
            {
                return IntValue == 1;
            }
        }

        public string StringValue
        {
            get
            {
                return Encoding.ASCII.GetString(Value.ToArray());
            }
        }

        public Parameter() { }
        public Parameter(byte address)
        {
            Address = address;
            Key = $"Param {(int)address}";
        }
        public Parameter(byte address, string key)
        {
            Address = address;
            Key = key;
        }
        public Parameter(byte address, string key, Dictionary<int, string> mapping)
        {
            Address = address;
            Key = key;
            Mapping = mapping;
        }
    }
}
