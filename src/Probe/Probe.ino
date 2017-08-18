#include "ext/TimerOne.h"
#include "ext/MemoryFree.h"
#include "ext/DAC_MCP49xx.h"

#include "Upstream.h"
#include "Protocol.h"
#include "Hardware.h"

DAC_MCP49xx dac(DAC_MCP49xx::MCP4902, DO_4902_CS);

#define DAC_DATA_LEN	256
byte dac0_buffer[DAC_DATA_LEN];
byte dac1_buffer[DAC_DATA_LEN];
bool dac0_buffer_enabled;
bool dac1_buffer_enabled;
uint8_t dac0_counter = 0;
uint8_t dac1_counter = 0;
uint8_t dac0_pos = 0;
uint8_t dac1_pos = 0;
uint8_t dac0_prescaler = 0;
uint8_t dac1_prescaler = 0;

void setup() 
{
	hardware_init();

	upstream_init(128000);

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

	case SetPoti0Command:
		cmd_setPoti0();
		break;

	case GetADCCommand:
		cmd_getADC();
		break;

	case SetDAC0BufferCommand:
		dac0_prescaler = upstream_readByte();
		cmd_setDACData(dac0_buffer);
		dac0_buffer_enabled = true;
		break;

	case SetDAC1BufferCommand:
		dac1_prescaler = upstream_readByte();
		cmd_setDACData(dac1_buffer);
		dac1_buffer_enabled = true;
		break;

	case DisableDAC0BufferCommand:
		dac0_buffer_enabled = false;
		upstream_write(AckResponse);
		break;

	case DisableDAC1BufferCommand:
		dac1_buffer_enabled = false;
		upstream_write(AckResponse);
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
	dac0_counter = 0;
	dac1_counter = 0;
	dac0_pos = 0;
	dac1_pos = 0;
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

		// Set DAC data
		if (dac0_buffer_enabled)
			dac.outputA(dac0_buffer[dac0_pos]);
		if (dac1_buffer_enabled)
			dac.outputB(dac1_buffer[dac1_pos]);

		// Wait for new samples.
		while (samplesAreEmpty)
		{
			stream_processNextCommand(cancel);
		}

		// Push samples.
		for (int n = 0; n < streams; n++)
			upstream_write(samples[n]);

		samplesAreEmpty = true;

		if (dac0_buffer_enabled)
		{
			dac0_counter++;

			if (dac0_counter == dac0_prescaler)
			{
				dac0_counter = 0;
				dac0_pos++;

				if (dac0_pos == DAC_DATA_LEN)
					dac0_pos = 0;
			}
		}

		if (dac1_buffer_enabled)
		{
			dac1_counter++;

			if (dac1_counter == dac1_prescaler)
			{
				dac1_counter = 0;
				dac1_pos++;

				if (dac1_pos == DAC_DATA_LEN)
					dac1_pos = 0;
			}
		}

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

void cmd_setPoti0()
{
	SPI.begin();
	uint16_t value = upstream_readWord();
	digitalWrite(DO_4151_CS, LOW);
	SPI.transfer(value >> 8);
	SPI.transfer(value & 0xff);
	digitalWrite(DO_4151_CS, HIGH);
	SPI.end();
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

void cmd_setDACData(byte *dacData)
{
	for (int n = 0; n < DAC_DATA_LEN; n++)
	{
		dacData[n] = upstream_readByte();
	}

	upstream_write(AckResponse);
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
	else if (cmd == SetDAC0Command)
	{
		cmd_setDAC0();
	}
	else if (cmd == SetDAC1Command)
	{
		cmd_setDAC1();
	}
	else if (cmd == SetPoti0Command)
	{
		cmd_setPoti0();
	}
}
