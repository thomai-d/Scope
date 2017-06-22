#ifndef PROTOCOL
#define PROTOCOL

#include <stdint.h>

// Commands 
#define StartStreamCommand			0x30	/* '0' */
#define StopStreamCommand			0x31	/* '1' */
#define SetDAC0Command				0x32	/* '2' */
#define SetDAC1Command				0x33	/* '3' */
#define GetADCCommand				0x34	/* '4' */
#define SetDAC0BufferCommand		0x35	/* '5' */
#define SetDAC1BufferCommand		0x36	/* '6' */
#define DisableDAC0BufferCommand	0x37	/* '7' */
#define DisableDAC1BufferCommand	0x38	/* '8' */
#define SetPoti0Command				0x39	/* '9' */

// Responses
#define AckResponse				0x40	/* '@' */
#define ErrorResponse			0x45	/* 'E' */
#define FinishResponse			0x46	/* 'F' */
#define Streaming				0x47	/* 'G' */
#define ErrorTooFast			0x48	/* 'H' */

#endif