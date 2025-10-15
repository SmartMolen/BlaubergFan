using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMolen.BlaubergFan.Models
{
    public enum Function
    {
        Read = 0x01,
        Write = 0x02,
        WriteReturn = 0x03,
        Increase = 0x04,
        Decrease = 0x05,
        Response = 0x06
    }
}
