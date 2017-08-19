using Scope.Data;
using Scope.UI.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMD.Extensions;

namespace Scope.Interface.Probe
{
    public class ProbeConnection : IDisposable
    {
        private const int DACBufferSize = 256;
        private const int DACmaxValue = 256;
        private const double DACRef = 5.0;
        private const int MaxDACStreams = 2;

        protected readonly SerialPort port;

        private double[] currentDacValues;
        private ushort currentPotiValue;
        private byte[][] dacBuffers;
        private byte[] dacPrescaler;

        public Task Properties { get; private set; }

        public ProbeConnection(string comPort, int baud)
        {
            this.port = new SerialPort(comPort, baud);
            this.port.ReadBufferSize = 4096;
            this.port.ReadTimeout = 3000;
            this.port.WriteTimeout = 500;

            this.currentDacValues = new double[MaxDACStreams];
            this.dacBuffers = new byte[MaxDACStreams][];
            this.dacPrescaler = new byte[MaxDACStreams];
        }

        public event EventHandler BurstReceived;

        public Task OpenAsync()
        {
            this.port.Open();
            this.port.DtrEnable = true;
            this.port.DtrEnable = false;

            return Task.Run(async () =>
            {
                await Task.Delay(Settings.Default.BootDelayMs);

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

            this.dacBuffers[index] = buffer;
            this.dacPrescaler[index] = prescaler;
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

            this.dacBuffers[index] = null;
            this.dacPrescaler[index] = 0;
            this.ExpectByte((byte)Response.Ack, "DISABLE-BUFFER-ACK");
        }

        public async Task StartStream(int samplesPerSecond, IBufferedStream<double>[] dacStreams, IBufferedStream<double>[] adcStreams, IBufferedStream<double> potiStream, CancellationToken cancellationToken)
        {
            var delayuS = (uint)(1000000 / samplesPerSecond);
            var burstSize = (ushort)Math.Min(Math.Max(1, samplesPerSecond / 10), 300); // default: 10 bursts / second.

            // Initialize DACs/Potis.
            var dacBufferPos = new int[dacStreams.Length];
            for (byte n = 0; n < dacStreams.Length; n++)
                this.SetDAC(n, 0.0);
            this.SetPoti0(0);

            // Start stream.
            this.WriteBytes((byte)Command.StartStream);
            this.WriteBytes((byte)adcStreams.Length);
            this.WriteDWord(delayuS);
            this.WriteWord(burstSize);
            this.ExpectByte((byte)Response.Ack, "ACK");

            int samplesRead = 0;
            bool cancelled = false;
            while (true)
            {
                // Cancel stream?
                if (cancellationToken.IsCancellationRequested && !cancelled)
                {
                    this.WriteBytes((byte)Command.StopStream);
                    cancelled = true;
                }

                var buffer = await this.ReadBuffer(adcStreams.Length, "READ-SAMPLES");
                for (int dacStreamIndex = 0; dacStreamIndex < dacStreams.Length; dacStreamIndex++)
                {
                    if (this.dacBuffers[dacStreamIndex] != null)
                    {
                        // Set the current DAC voltage to the DAC buffer value.
                        var value = this.dacBuffers[dacStreamIndex][dacBufferPos[dacStreamIndex] / this.dacPrescaler[dacStreamIndex]] / (double)DACmaxValue;
                        if (++dacBufferPos[dacStreamIndex] == this.dacBuffers[dacStreamIndex].Length * this.dacPrescaler[dacStreamIndex])
                            dacBufferPos[dacStreamIndex] = 0;

                        dacStreams[dacStreamIndex].Push(value);
                    }
                    else
                    {
                        // Set the current DAC voltage to last known DAC value.
                        dacStreams[dacStreamIndex].Push(this.currentDacValues[dacStreamIndex] / DACRef);
                    }
                }

                // Set poti value (0-10k)
                potiStream.Push(this.currentPotiValue / 256.0);

                // Read samples.
                for (int adcStreamIndex = 0; adcStreamIndex < adcStreams.Length; adcStreamIndex++)
                {
                    var value = (double)buffer[adcStreamIndex] / 255;
                    adcStreams[adcStreamIndex].Push(value);
                }

                if (++samplesRead == burstSize)
                {
                    // Burst received?
                    samplesRead = 0;

                    this.BurstReceived?.Invoke(this, EventArgs.Empty);

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
                }
            }
        }

        public void SetDAC(uint index, double voltage)
        {
            if (this.dacBuffers == null)
                throw new InvalidOperationException("Start stream before setting DAC.");
            if (index >= this.dacBuffers.Length)
                throw new InvalidOperationException($"Unsupported DAC index: {index}");

            this.currentDacValues[index] = voltage;

            var value = (byte)(voltage / DACRef * DACmaxValue);
            this.WriteBytes((byte)((byte)Command.SetDAC0 + index), value);
        }

        public void SetPoti0(ushort value)
        {
            if (value > 256)
                value = 256;

            this.WriteBytes(((byte)Command.SetPoti0));
            this.WriteWord(value);
            this.currentPotiValue = value;
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
