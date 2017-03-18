using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scope.Interface.Probe
{
    public static class Magic
    {
        public static readonly byte[] WelcomeBytes = Encoding.ASCII.GetBytes("HELO PROBE\n");
    }

    public enum Command
    {
        StartStream         = 0x30,   // '0'   [streams:uint8] [delayuS:uint16]
        StopStream          = 0x31,   // '1'
        SetDAC0             = 0x32,   // '2'   [value:uint8]
        SetDAC1             = 0x33,   // '3'   [value:uint8]
        GetADC              = 0x34,   // '4'   [adc:uint8]
        SetDAC0Buffer       = 0x35,   // '5'   [data: 256 bytes]
        SetDAC1Buffer       = 0x36,   // '6'   [data: 256 bytes]
        DisableDAC0Buffer   = 0x37,   // '7'
        DisableDAC1Buffer   = 0x38,   // '8'
    }

    public enum Response
    {
        Ack             = 0x40, // '@'
        Error           = 0x45, // 'E'
        Finish          = 0x46, // 'F'
        Streaming       = 0x47, // 'G'
        ErrorTooFast    = 0x48, // 'H'
    }
}
