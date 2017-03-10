#include "Upstream.h"

void upstream_init(unsigned long baud)
{
	UPSTREAM_SERIAL.begin(baud);
	UPSTREAM_SERIAL.print(F("HELO PROBE\n"));
	UPSTREAM_SERIAL.flush();
}

void upstream_end()
{
	UPSTREAM_SERIAL.end();
}

uint8_t upstream_readByte()
{
	while (!UPSTREAM_SERIAL.available());
	return UPSTREAM_SERIAL.read();
}

uint32_t upstream_readDWord()
{
	while (UPSTREAM_SERIAL.available() < 4);
	uint32_t value = UPSTREAM_SERIAL.read();
	value += ((uint32_t)UPSTREAM_SERIAL.read() << 8);
	value += ((uint32_t)UPSTREAM_SERIAL.read() << 16);
	value += ((uint32_t)UPSTREAM_SERIAL.read() << 24);
	return value;
}

uint16_t upstream_readWord()
{
	while (UPSTREAM_SERIAL.available() < 2);
	return (uint16_t)UPSTREAM_SERIAL.read() + (UPSTREAM_SERIAL.read() << 8);
}

bool upstream_canReadByte()
{
	return UPSTREAM_SERIAL.available();
}

void upstream_write(uint8_t value)
{
	UPSTREAM_SERIAL.write(value);
}

void upstream_writeWord(uint16_t value)
{
	UPSTREAM_SERIAL.write(lowByte(value));
	UPSTREAM_SERIAL.write(highByte(value));
}

void upstream_dump()
{
	while (UPSTREAM_SERIAL.available())
		UPSTREAM_SERIAL.write(UPSTREAM_SERIAL.read());
}

