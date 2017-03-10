using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scope.Data;

namespace Scope.Interface.Probe
{
    public class ReadStreamCommand : CommandBase
    {
        private bool isRunning;
        private bool cancelRequested;
        private uint delayuS;
        private IBufferedStream<double>[] streams;
        private ushort burstSize;

        public event EventHandler BurstReceived;

        public ReadStreamCommand(ProbeConnection probe, int samplesPerSecond, params IBufferedStream<double>[] streams) : base(probe)
        {
            this.streams = streams;
            this.delayuS = (uint)(1000000 / samplesPerSecond);
            this.burstSize = (ushort)Math.Min(Math.Max(1, samplesPerSecond / 10), 300); // default: 10 bursts / second.
        }

        public override async Task Execute()
        {
            Probe.WriteBytes((byte)Command.StartStream);
            Probe.WriteBytes((byte)this.streams.Length);
            Probe.WriteDWord(this.delayuS);
            Probe.WriteWord(this.burstSize);
            Probe.ExpectByte((byte)Response.Ack, "ACK");

            int streamIndex = 0;
            this.cancelRequested = false;
            this.isRunning = true;
            while (this.isRunning)
            {
                if (this.cancelRequested)
                {
                    Probe.WriteBytes((byte)Command.StopStream);
                    this.cancelRequested = false;
                }

                var buffer = await Probe.ReadBuffer(this.burstSize * this.streams.Length, "READ-STREAM");
                this.ValidateStreamingStatusResponse();

                for (int n = 0; n < buffer.Length; n++)
                {
                    var value = (double)buffer[n] / 255 * 5.0;
                    this.streams[streamIndex].Push(value);
                    if (++streamIndex == this.streams.Length)
                        streamIndex = 0;
                }

                this.BurstReceived?.Invoke(this, EventArgs.Empty);
            }
        }

        public override void Cancel()
        {
            if (!this.isRunning)
                throw new InvalidOperationException("Task not running");

            this.cancelRequested = true;
        }

        private void ValidateStreamingStatusResponse()
        {
            var status = Probe.ReadByte("STREAM-STATUS");

            if (status != (byte)Response.Streaming)
            {
                if (status == (byte)Response.ErrorTooFast)
                {
                    throw new CommandProtocolException("STREAM", "Buffer underflow - try a lower samples-per-second setting.");
                }

                if (status == (byte)Response.Finish)
                {
                    this.isRunning = false;
                    return;
                }

                throw new CommandProtocolException("STREAM", $"Expected status '{Response.Streaming}' but found '{(Response)status}'");
            }
        }
    }
}
