#ifndef HARDWARE_H
#define HARDWARE_H

#include <Arduino.h>

#define DO_BEEPER		4	
#define DO_4902_CS		9

#define OFF		LOW
#define ON		HIGH


void hardware_init();

void beep(uint8_t times);

#endif