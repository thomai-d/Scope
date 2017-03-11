#include "ext/TimerOne.h"
#include "ext/MemoryFree.h"
#include "ext/DAC_MCP49xx.h"

#include "Upstream.h"
#include "Protocol.h"
#include "Hardware.h"

DAC_MCP49xx dac(DAC_MCP49xx::MCP4902, 9);

void setup() 
{
	hardware_init();

	upstream_init(115200);

	Timer1.attachInterrupt(timerTick);

	// Reset DAC
	dac.outputA(0);
	dac.outputB(0);

	beep(3);
}

void loop() 
{
	uint8_t cmd = upstream_readByte();

	switch (cmd)
	{
	case StartStreamCommand:
		{
			beep(1);
			cmd_readStream();
			beep(1);
			break;
		}
	
	case SetDAC0Command:
		cmd_setDAC0();
		break;

	case SetDAC1Command:
		cmd_setDAC1();
		break;

	case GetADCCommand:
		cmd_getADC();
		break;

	default:
		cmd_unknownCmd(cmd);
		beep(3);
		break;
	}
}

/* COMMANDS */

byte samples[8];
uint8_t streams = 0;

volatile bool samplesAreEmpty = true;
volatile bool errorTooFast = false;

void timerTick()
{
	if (!samplesAreEmpty)
	{
		errorTooFast = true;
		Timer1.stop();
		return;
	}

	for (int n = 0; n < streams; n++)
	{
		analogRead(A0 + n);
		samples[n] = (byte)(analogRead(A0 + n) >> 2);
	}

	samplesAreEmpty = false;
}

void cmd_readStream()
{
	// Read Parameters.
	streams = upstream_readByte();				// [byte]	streams
	uint32_t delay = upstream_readDWord();		// [dword]	delay between samples (us)
	uint16_t burstSize = upstream_readWord();	// [word]	samples/adc before status refresh
	upstream_write(AckResponse);

	// Initialize.
	Timer1.initialize(delay);
	errorTooFast = false;
	samplesAreEmpty = true;
	Timer1.start();

	bool cancel = false;
	int currentBurstCycles = 0;
	while (true)
	{
		if (errorTooFast)
		{
			while (currentBurstCycles++ < burstSize)
			{
				for (int n = 0; n < streams; n++)
					upstream_write(0);
			}

			upstream_write(ErrorTooFast);

			beep(5);
			return;
		}

		// Wait for new samples.
		while (samplesAreEmpty)
		{
			stream_processNextCommand(cancel);
		}

		// Push samples.
		for (int n = 0; n < streams; n++)
			upstream_write(samples[n]);

		samplesAreEmpty = true;

		// Cancel?
		if (++currentBurstCycles == burstSize)
		{
			currentBurstCycles = 0;
			if (cancel)
			{ 
				Timer1.stop();
				upstream_write(FinishResponse);
				return;
			}

			upstream_write(Streaming);
		}
	}

}

void cmd_setDAC0()
{
	byte value = upstream_readByte();
	dac.outputA(value);
}

void cmd_setDAC1()
{
	byte value = upstream_readByte();
	dac.outputB(value);
}

void cmd_getADC()
{
	byte adc = upstream_readByte();
	analogRead(A0 + adc);
	byte sample = (byte)(analogRead(A0 + adc) >> 2);
	upstream_write(sample);
}

void cmd_unknownCmd(byte cmd)
{
	upstream_write(ErrorResponse);
	upstream_write(cmd);
	upstream_dump();
}

// STREAM - Helper methods

void stream_processNextCommand(bool &cancel)
{
	if (!upstream_canReadByte())
		return;

	uint8_t cmd = upstream_readByte();
	if (cmd == StopStreamCommand)
	{
		cancel = true;
	}

	if (cmd == SetDAC0Command)
	{
		cmd_setDAC0();
	}
	if (cmd == SetDAC1Command)
	{
		cmd_setDAC1();
	}
}