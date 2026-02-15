// OuijaFirmware.ino
// ESP32/Arduino-compatible firmware with:
// - 1 second timer interrupt (blinks built-in LED)
// - UART RX interrupt that parses protocol frames and responds based on command
//
// Protocol: see docs/Transfer protocol description.md
// Frame:
//   [0]   = 0xAA
//   [1]   = 0x55
//   [2]   = Protocol Version
//   [3]   = Command Type
//   [4]   = Payload Length (L)
//   [5..] = Payload (L bytes)
//   [last]= Checksum = XOR(bytes 2..(4+L))

#include <Arduino.h>

// ---------------- Protocol constants ----------------

static const uint8_t PROTOCOL_MAGIC_0     = 0xAA;
static const uint8_t PROTOCOL_MAGIC_1     = 0x55;
static const uint8_t PROTOCOL_VERSION     = 0x01;

// Command types
static const uint8_t CMD_CHECK_STATUS         = 0x00;
static const uint8_t CMD_PLAY_SEQUENCE_ONCE   = 0x01;
static const uint8_t CMD_PLAY_SEQUENCE_REPEAT = 0x02;
static const uint8_t CMD_STOP_PLAYING         = 0x03;
static const uint8_t CMD_DISPLAY_YES          = 0x04;
static const uint8_t CMD_DISPLAY_NO           = 0x05;

// Response bytes:
// - 0x00 for success
// - 0x01 for any other valid command
// Response codes:
// 0x00 = success
// 0x01 = invalid checksum
// 0x02 = unknown command
static const uint8_t RESPONSE_OK                   = 0x00;
static const uint8_t RESPONSE_INVALID_CHECKSUM    = 0x01;
static const uint8_t RESPONSE_UNKNOWN_COMMAND     = 0x02;

// ---------------- Shift Register configuration ----------------

// Pin definitions for 74HC595 shift registers (6 daisy-chained -> 48 bits)
static const uint8_t SHIFT_DATA_PIN  = 2;  // SER/DS pin
static const uint8_t SHIFT_CLOCK_PIN = 3;  // SRCLK/SHCP pin
static const uint8_t SHIFT_LATCH_PIN = 4;  // RCLK/STCP pin

// ---------------- UART configuration ----------------

// Adjust if you need a different baud rate
static const uint32_t UART_BAUD_RATE = 115200UL;

// Receive buffer size (enough for small frames)
static const uint8_t RX_BUFFER_SIZE = 64;

volatile uint8_t  rxBuffer[RX_BUFFER_SIZE];
volatile uint8_t  rxIndex          = 0;
volatile uint8_t  rxExpectedLength = 0; // Total frame length once known (0 = unknown)

// ---------------- Sequence storage ----------------

static const uint8_t SEQUENCE_BUFFER_SIZE = 64;
static uint8_t sequencePayload[SEQUENCE_BUFFER_SIZE];
static uint8_t sequencePayloadLength = 0;
static uint8_t repeatPlay = 0; // 0 = play once, 1 = repeat
static int8_t sequenceIndex = -1; // -1 = not playing, >= 0 = current position

// ---------------- Timer state ----------------

volatile bool oneSecondElapsed = false;

// ---------------- Utility functions ----------------

// Maps a value (0-47) to a 48-bit mask (uint64_t).
// For input 0-47: sets the corresponding bit position to 1, all others to 0.
// For any other input: returns 0 (all bits zero).
static uint64_t mapValueToBitMask(uint8_t value)
{
  if (value <= 47) {
    return (1ULL << value);
  }
  return 0ULL;
}

// Sends a 48-bit value to 6 daisy-chained shift registers (74HC595).
// Bits are shifted out MSB first, starting with the most distant register.
static void sendToShiftRegisters(uint64_t value)
{
  // Pull latch low to prepare for data
  digitalWrite(SHIFT_LATCH_PIN, LOW);

  // Shift out all 48 bits, MSB first
  for (int16_t i = 47; i >= 0; i--) {
    // Set data pin to the bit value
    digitalWrite(SHIFT_DATA_PIN, (int)((value >> i) & 1ULL));

    // Pulse clock to shift the bit
    digitalWrite(SHIFT_CLOCK_PIN, HIGH);
    digitalWrite(SHIFT_CLOCK_PIN, LOW);
  }

  // Pulse latch to transfer data to output pins
  digitalWrite(SHIFT_LATCH_PIN, HIGH);
}

static inline void uartSendByte(uint8_t b)
{
  // Wait until transmit buffer is empty
#if defined(UDR0)
  while (!(UCSR0A & (1 << UDRE0))) {
    // wait
  }
  UDR0 = b;
#else
  // Fallback to Arduino Serial if direct UART registers are not available
  Serial.write(b);
#endif
}

static void processValidFrame(const uint8_t *frame, uint8_t length)
{
  // Frame layout:
  // [0]  = 0xAA
  // [1]  = 0x55
  // [2]  = version
  // [3]  = command
  // [4]  = payload length (L)
  // [5..(5+L-1)] = payload
  // [5+L] = checksum

  if (length < 6) {
    return; // too short to be valid
  }

  uint8_t version      = frame[2];
  uint8_t command      = frame[3];
  uint8_t payloadLen   = frame[4];
  uint8_t expectedSize = (uint8_t)(6 + payloadLen); // 2 magic + 1 ver + 1 cmd + 1 len + L + 1 checksum

  if (length != expectedSize) {
    return; // length mismatch, discard
  }

  // Validate version
  if (version != PROTOCOL_VERSION) {
    return;
  }

  // Compute checksum: XOR bytes from index 2 up to last payload byte (index 4 + payloadLen)
  uint8_t checksum = 0;
  uint8_t lastPayloadIndex = (uint8_t)(4 + payloadLen);
  for (uint8_t i = 2; i <= lastPayloadIndex; ++i) {
    checksum ^= frame[i];
  }

  uint8_t receivedChecksum = frame[lastPayloadIndex + 1];
  if (checksum != receivedChecksum) {
    // Invalid checksum – reply with error code and discard
    uartSendByte(RESPONSE_INVALID_CHECKSUM);
    return;
  }

  // At this point, the frame is valid.
  // Respond according to the command type:
  if (command == CMD_CHECK_STATUS) {
    uartSendByte(RESPONSE_OK);
  } else if (command == CMD_PLAY_SEQUENCE_ONCE || command == CMD_PLAY_SEQUENCE_REPEAT) {
    // Store payload for sequence playback
    sequencePayloadLength = (payloadLen <= SEQUENCE_BUFFER_SIZE) ? payloadLen : SEQUENCE_BUFFER_SIZE;
    for (uint8_t i = 0; i < sequencePayloadLength; ++i) {
      sequencePayload[i] = frame[5 + i];
    }
    // Set repeat flag
    repeatPlay = (command == CMD_PLAY_SEQUENCE_REPEAT) ? 1 : 0;
    // Start playback from beginning
    sequenceIndex = 0;
    uartSendByte(RESPONSE_OK);
  } else if (command == CMD_STOP_PLAYING) {
    // Stop any active sequence playback
    sequenceIndex = -1;
    uartSendByte(RESPONSE_OK);
  } else if (command == CMD_DISPLAY_YES) {
    // Display YES: single-byte sequence with value 0x00
    sequencePayloadLength = 1;
    sequencePayload[0] = 0x00;
    repeatPlay = 0;
    sequenceIndex = 0;
    uartSendByte(RESPONSE_OK);
  } else if (command == CMD_DISPLAY_NO) {
    // Display NO: single-byte sequence with value 0x01
    sequencePayloadLength = 1;
    sequencePayload[0] = 0x01;
    repeatPlay = 0;
    sequenceIndex = 0;
    uartSendByte(RESPONSE_OK);
  } else {
    // Unknown/unsupported command
    uartSendByte(RESPONSE_UNKNOWN_COMMAND);
  }
}

// ---------------- Interrupt Service Routines ----------------

#if defined(UDR0)
// UART RX complete interrupt (ATmega-style UART; used on many Arduino boards)
ISR(USART_RX_vect)
{
  uint8_t byteReceived = UDR0;

  // Simple state machine driven by rxIndex and rxExpectedLength.
  if (rxIndex == 0) {
    // Expect first magic byte
    if (byteReceived != PROTOCOL_MAGIC_0) {
      // Stay at index 0 until we see the first magic byte
      return;
    }
    rxBuffer[rxIndex++] = byteReceived;
    rxExpectedLength = 0;
    return;
  }

  if (rxIndex == 1) {
    // Expect second magic byte
    if (byteReceived != PROTOCOL_MAGIC_1) {
      // If this byte happens to be the first magic byte, start over from there
      rxIndex = 0;
      if (byteReceived == PROTOCOL_MAGIC_0) {
        rxBuffer[rxIndex++] = byteReceived;
      }
      return;
    }
    rxBuffer[rxIndex++] = byteReceived;
    return;
  }

  // Store subsequent bytes as long as we have buffer space
  if (rxIndex < RX_BUFFER_SIZE) {
    rxBuffer[rxIndex++] = byteReceived;
  } else {
    // Buffer overflow – reset state
    rxIndex = 0;
    rxExpectedLength = 0;
    return;
  }

  // Once we have received the payload length byte (index 4), we can calculate total frame length.
  if (rxIndex == 5) {
    uint8_t payloadLen = rxBuffer[4];
    rxExpectedLength = (uint8_t)(6 + payloadLen); // full frame size
  }

  // If we know the expected frame length and have received that many bytes, process the frame.
  if (rxExpectedLength != 0 && rxIndex >= rxExpectedLength) {
    processValidFrame((const uint8_t *)rxBuffer, rxExpectedLength);

    // Reset for the next frame
    rxIndex          = 0;
    rxExpectedLength = 0;
  }
}
#endif

// Timer1 compare interrupt – fires every 1 second
ISR(TIMER1_COMPA_vect)
{
  oneSecondElapsed = true;
}

// ---------------- Initialization helpers ----------------

static void initTimer1ForOneSecondInterrupt()
{
  // Configure Timer1 in CTC mode with 1 Hz period.
  // Assumes 16 MHz clock.
  // 16,000,000 / 1024 = 15625 ticks per second
  // We want interrupt at 1 Hz -> OCR1A = 15625 - 1 = 15624

  noInterrupts();

  TCCR1A = 0;
  TCCR1B = 0;
  TCNT1  = 0;

  // CTC mode
  TCCR1B |= (1 << WGM12);

  // Set compare value for 1 second period
  OCR1A = 15624;

  // Enable Timer1 compare interrupt A
  TIMSK1 |= (1 << OCIE1A);

  // Start Timer1 with prescaler 1024
  TCCR1B |= (1 << CS12) | (1 << CS10);

  interrupts();
}

static void initUart()
{
#if defined(UBRR0H)
  // Configure UART0 manually (typical on AVR-based Arduinos like Uno)
  uint16_t ubrr = (uint16_t)((F_CPU / (16UL * UART_BAUD_RATE)) - 1UL);

  // Set baud rate
  UBRR0H = (uint8_t)(ubrr >> 8);
  UBRR0L = (uint8_t)(ubrr & 0xFF);

  // Enable receiver, transmitter and RX complete interrupt
  UCSR0B = (1 << RXEN0) | (1 << TXEN0) | (1 << RXCIE0);

  // 8 data bits, no parity, 1 stop bit
  UCSR0C = (1 << UCSZ01) | (1 << UCSZ00);
#else
  // Fallback for boards without direct register access: use Serial and polling.
  Serial.begin(UART_BAUD_RATE);
#endif
}

// ---------------- Arduino entry points ----------------

void setup()
{
  // Configure built-in LED so we can see the timer working
  pinMode(LED_BUILTIN, OUTPUT);

  // Configure shift register pins
  pinMode(SHIFT_DATA_PIN, OUTPUT);
  pinMode(SHIFT_CLOCK_PIN, OUTPUT);
  pinMode(SHIFT_LATCH_PIN, OUTPUT);
  
  // Initialize shift register outputs to 0
  digitalWrite(SHIFT_LATCH_PIN, LOW);
  digitalWrite(SHIFT_CLOCK_PIN, LOW);
  digitalWrite(SHIFT_DATA_PIN, LOW);
  sendToShiftRegisters(0ULL);

  initTimer1ForOneSecondInterrupt();
  initUart();
}

void loop()
{
  // Handle 1-second timer: toggle LED
  if (oneSecondElapsed) {
    oneSecondElapsed = false;
    digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
    
    // Process sequence playback if active
    if (sequenceIndex > -1) {
      // Send current sequence byte to shift registers
      uint64_t bitMask = mapValueToBitMask(sequencePayload[sequenceIndex]);
      sendToShiftRegisters(bitMask);
      
      // Move to next position
      sequenceIndex++;
      
      // Check if we reached the end
      if (sequenceIndex >= sequencePayloadLength) {
        if (repeatPlay == 1) {
          sequenceIndex = 0; // Loop back to start
        } else {
          sequenceIndex = -1; // Stop playback
        }
      }
    }
  }

  // Main loop can remain mostly idle; all protocol work is done in the UART ISR.
}

