using Scope.Data;
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
        private const int DACStreams = 2;
        private const double DACRef = 5.0;

        protected readonly SerialPort port;

        private double[] currentDacValues;

        public Task Properties { get; private set; }

        public ProbeConnection(string comPort, int baud)
        {
            this.port = new SerialPort(comPort, baud);
            this.port.ReadBufferSize = 4096;
            this.port.ReadTimeout = 3000;
            this.port.WriteTimeout = 500;

            this.currentDacValues = new double[DACStreams];
        }

        public event EventHandler BurstReceived;

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

        public async Task StartStream(int samplesPerSecond, IBufferedStream<double>[] dacStreams, IBufferedStream<double>[] adcStreams, CancellationToken cancellationToken)
        {
            var delayuS = (uint)(1000000 / samplesPerSecond);
            var burstSize = (ushort)Math.Min(Math.Max(1, samplesPerSecond / 10), 300); // default: 10 bursts / second.

            this.WriteBytes((byte)Command.StartStream);
            this.WriteBytes((byte)adcStreams.Length);
            this.WriteDWord(delayuS);
            this.WriteWord(burstSize);
            this.ExpectByte((byte)Response.Ack, "ACK");

            bool cancelled = false;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested && !cancelled)
                {
                    this.WriteBytes((byte)Command.StopStream);
                    cancelled = true;
                }

                var buffer = await this.ReadBuffer(burstSize * adcStreams.Length, "READ-STREAM");
                var status = this.ReadByte("STREAM-STATUS");
                if (status != (byte)Response.Streaming)
                {
                    if (status == (byte)Response.ErrorTooFast)
                    {
                        throw new CommandProtocolException("STREAM", "Buffer underflow - try a lower samples-per-second setting.");
                    }

                    if (status == (byte)Response.Finish)
                        return;

                    throw new CommandProtocolException("STREAM", $"Expected status '{Response.Streaming}' but found '{(Response)status}'");
                }

                for (int dacStreamIndex = 0; dacStreamIndex < dacStreams.Length; dacStreamIndex++)
                {
                    for (int n = 0; n < burstSize; n++)
                    {
                        dacStreams[dacStreamIndex].Push(this.currentDacValues[dacStreamIndex]);
                    }
                }

                for (int adcStreamIndex = 0; adcStreamIndex < adcStreams.Length; adcStreamIndex++)
                {
                    for (int n = adcStreamIndex; n < buffer.Length; n += adcStreams.Length)
                    {
                        var value = (double)buffer[n] / 255 * 5.0;
                        adcStreams[adcStreamIndex].Push(value);
                    }
                }

                this.BurstReceived?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetDAC(uint index, double voltage)
        {
            if (index >= DACStreams)
                throw new InvalidOperationException($"Unsupported DAC index: {index}");

            this.currentDacValues[index] = voltage;

            var value = (byte)(voltage / DACRef * 256);
            this.WriteBytes((byte)((byte)Command.SetDAC0 + index), value);
        }

        public void Dispose()
        {
            this.port.Close();
        }

        private void WriteBytes(params byte[] data)
        {
            this.port.Write(data, 0, data.Length);
        }

        private void ExpectByte(byte expectedResponse, string step)
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

        private void WriteWord(ushort value)
        {
            this.WriteBytes(BitConverter.GetBytes(value));
        }

        private void WriteDWord(uint value)
        {
            this.WriteBytes(BitConverter.GetBytes(value));
        }

        private void ExpectWord(ushort expected, string step)
        {
            var response = this.ReadWord(step);
            if (response != expected)
            {
                throw new CommandProtocolException(step, $"Expected {expected} but found {response}", this.ReadAll());
            }
        }

        private ushort ReadWord(string step)
        {
            var buf = this.ReadExactly(2, step);
            return BitConverter.ToUInt16(buf, 0);
        }

        private byte ReadByte(string step)
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

        private Task<byte[]> ReadBuffer(int bytes, string step)
        {
            return Task.Run(() =>
            {
                return this.ReadExactly(bytes, step);
            });
        }

        private byte[] ReadAll()
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
