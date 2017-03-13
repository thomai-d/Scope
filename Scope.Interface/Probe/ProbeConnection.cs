using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scope.Interface.Probe
{
    public class ProbeConnection : IDisposable
    {
        private const int DACBufferSize = 256;

        protected readonly SerialPort port;

        public Task Properties { get; private set; }

        public ProbeConnection(string comPort, int baud)
        {
            this.port = new SerialPort(comPort, baud);
            this.port.ReadBufferSize = 4096;
            this.port.ReadTimeout = 3000;
            this.port.WriteTimeout = 500;
        }

        public Task OpenAsync()
        {
            this.port.Open();
            this.port.DtrEnable = true;
            this.port.DtrEnable = false;

            return Task.Run(async () =>
            {
                await Task.Delay(Scope.Interface.Properties.Settings.Default.BootDelayMs);

                var data = this.ReadAll();
                if (!data.EndsWith(Magic.WelcomeBytes))
                {
                    this.port.Close();
                    throw new CommandProtocolException("PROBE-WELCOME", $"Expected {Magic.WelcomeBytes.Dump()} but found: {data.Dump()}");
                }
            });
        }

        public void EnableDACBuffer(byte index, byte prescaler, byte[] buffer)
        {
            if (buffer.Length != DACBufferSize)
                throw new InvalidOperationException($"Expected buffer length of {DACBufferSize}, but found {buffer.Length}");

            if (index == 0)
                this.WriteBytes((byte)Command.SetDAC0Buffer);
            else if (index == 1)
                this.WriteBytes((byte)Command.SetDAC1Buffer);
            else throw new InvalidOperationException($"DAC {index} not supported.");

            this.WriteBytes(prescaler);

            this.WriteBytes(buffer);
            this.ExpectByte((byte)Response.Ack, "SET-BUFFER-ACK");
        }

        public void DisableDACBuffer(byte index)
        {
            if (index == 0)
                this.WriteBytes((byte)Command.DisableDAC0Buffer);
            else if (index == 1)
                this.WriteBytes((byte)Command.DisableDAC1Buffer);
            else throw new InvalidOperationException($"DAC {index} not supported.");

            this.ExpectByte((byte)Response.Ack, "DISABLE-BUFFER-ACK");
        }

        public void WriteBytes(params byte[] data)
        {
            this.port.Write(data, 0, data.Length);
        }

        public void ExpectByte(byte expectedResponse, string step)
        {
            try
            {
                var response = this.port.ReadByte();
                if (response != expectedResponse)
                {
                    throw new CommandProtocolException(step, $"Expected {expectedResponse:x2} but found {response:x2}", this.ReadAll());
                }
            }
            catch (TimeoutException)
            {
                throw new CommandProtocolException(step, $"Expected {expectedResponse:x2} but got timeout");
            }
        }

        public void WriteWord(ushort value)
        {
            this.WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteDWord(uint value)
        {
            this.WriteBytes(BitConverter.GetBytes(value));
        }

        public void ExpectWord(ushort expected, string step)
        {
            var response = this.ReadWord(step);
            if (response != expected)
            {
                throw new CommandProtocolException(step, $"Expected {expected} but found {response}", this.ReadAll());
            }
        }

        public ushort ReadWord(string step)
        {
            var buf = this.ReadExactly(2, step);
            return BitConverter.ToUInt16(buf, 0);
        }

        public byte ReadByte(string step)
        {
            try
            {
                return (byte)this.port.ReadByte();
            }
            catch (TimeoutException)
            {
                throw new CommandProtocolException(step, $"Exptected 1 byte but got timeout");
            }
        }

        public Task<byte[]> ReadBuffer(int bytes, string step)
        {
            return Task.Run(() =>
            {
                return this.ReadExactly(bytes, step);
            });
        }

        public void Dispose()
        {
            this.port.Close();
        }

        public byte[] ReadAll()
        {
            Thread.Sleep(100);
            var bytesLeft = this.port.BytesToRead;
            var buffer = new byte[bytesLeft];
            this.port.Read(buffer, 0, bytesLeft);
            return buffer;
        }

        private byte[] ReadExactly(int bytes, string step)
        {
            var buffer = new byte[bytes];

            int read = 0;
            int totalRead = 0;

            try
            {
                do
                {
                    read = this.port.Read(buffer, totalRead, bytes - totalRead);
                    totalRead += read;
                }
                while (read > 0 && totalRead < bytes);
            }
            catch (TimeoutException)
            {
                throw new CommandProtocolException(step, $"Exptected {bytes} bytes but got timeout after {totalRead} bytes");
            }

            return buffer;
        }
    }
}
