#include "Hardware.h"

void hardware_init()
{
	// Initialize outputs.
	pinMode(DO_BEEPER, OUTPUT);

	// Setup fast ADC.
	bitSet(ADCSRA, ADPS2);
	bitClear(ADCSRA, ADPS1);
	bitClear(ADCSRA, ADPS0);
}

void beep(uint8_t times)
{
	while (times > 0)
	{
		digitalWrite(DO_BEEPER, HIGH);
		delay(1);
		digitalWrite(DO_BEEPER, LOW);

		if (--times > 0)
			delay(100);
	}
}