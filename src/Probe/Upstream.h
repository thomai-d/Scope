#ifndef UPSTREAM_H
#define UPSTREAM_H

#include <inttypes.h>
#include <Arduino.h>

#include "Protocol.h"

#ifndef UPSTREAM_SERIAL
#define UPSTREAM_SERIAL Serial
#endif

void upstream_init(unsigned long baud);
void upstream_end();

// Read methods.
uint8_t upstream_readByte();
uint16_t upstream_readWord();
uint32_t upstream_readDWord();
bool upstream_canReadByte();

// Write methods.
void upstream_write(uint8_t value);
void upstream_writeWord(uint16_t value);
void upstream_dump();

#endif