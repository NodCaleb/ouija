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

#include <avr/io.h>
#include <avr/interrupt.h>
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

// Response codes:
// 0x00 = success
// 0x01 = invalid message
// 0x02 = invalid checksum
// 0x03 = unknown command
// 0x04 = invalid version 
static const uint8_t RESPONSE_OK                  = 0x00;
static const uint8_t RESPONSE_INVALID_MESSAGE     = 0x01;
static const uint8_t RESPONSE_INVALID_CHECKSUM    = 0x02;
static const uint8_t RESPONSE_UNKNOWN_COMMAND     = 0x03;
static const uint8_t RESPONSE_INVALID_VERSION     = 0x04;

// ---------------- Shift Register configuration ----------------

// Pin definitions for 74HC595 shift registers (6 daisy-chained -> 48 bits)
static const uint8_t SHIFT_DATA_PIN  = 2;  // SER/DS pin
static const uint8_t SHIFT_CLOCK_PIN = 3;  // SRCLK/SHCP pin
static const uint8_t SHIFT_LATCH_PIN = 4;  // RCLK/STCP pin

// ---------------- UART configuration ----------------

#define RX_BUF_SIZE 128
volatile uint8_t rxBuf[RX_BUF_SIZE];
volatile uint8_t rxHead = 0;
volatile uint8_t rxTail = 0;

// Application buffer: store received bytes until flush trigger
#define APP_BUF_SIZE RX_BUF_SIZE
static uint8_t appBuf[APP_BUF_SIZE];
static uint8_t appLen = 0;
static unsigned long lastRxMillis = 0;
static const unsigned long RX_IDLE_TIMEOUT_MS = 50; // flush after 50ms idle


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

void uart_putchar(uint8_t c) {
  while (!(UCSR0A & (1 << UDRE0)));
  UDR0 = c;
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
    uart_putchar(RESPONSE_INVALID_MESSAGE); 
    return; // too short to be valid
  }

  // Validate magic bytes
  if (frame[0] != PROTOCOL_MAGIC_0 || frame[1] != PROTOCOL_MAGIC_1) {
    uart_putchar(RESPONSE_INVALID_MESSAGE); 
    return; // invalid magic, discard
  }

  uint8_t version      = frame[2];
  uint8_t command      = frame[3];
  uint8_t payloadLen   = frame[4];
  uint8_t expectedSize = (uint8_t)(6 + payloadLen); // 2 magic + 1 ver + 1 cmd + 1 len + L + 1 checksum

  if (length != expectedSize) {
    uart_putchar(RESPONSE_INVALID_MESSAGE);
    return; // length mismatch, discard
  }

  // Validate version
  if (version != PROTOCOL_VERSION) {
    uart_putchar(RESPONSE_INVALID_VERSION);
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
    uart_putchar(RESPONSE_INVALID_CHECKSUM);
    return;
  }

  // At this point, the frame is valid.
  // Respond according to the command type:
  if (command == CMD_CHECK_STATUS) {
    uart_putchar(RESPONSE_OK);
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
    uart_putchar(RESPONSE_OK);
  } else if (command == CMD_STOP_PLAYING) {
    // Stop any active sequence playback
    sequenceIndex = -1;
    uart_putchar(RESPONSE_OK);
  } else if (command == CMD_DISPLAY_YES) {
    // Display YES: single-byte sequence with value 0x00
    sequencePayloadLength = 1;
    sequencePayload[0] = 0x00;
    repeatPlay = 0;
    sequenceIndex = 0;
    uart_putchar(RESPONSE_OK);
  } else if (command == CMD_DISPLAY_NO) {
    // Display NO: single-byte sequence with value 0x01
    sequencePayloadLength = 1;
    sequencePayload[0] = 0x01;
    repeatPlay = 0;
    sequenceIndex = 0;
    uart_putchar(RESPONSE_OK);
  } else {
    // Unknown/unsupported command
    uart_putchar(RESPONSE_UNKNOWN_COMMAND);
  }
}

// ---------------- Initialization helpers ----------------

void uart_init(uint32_t baud) {
  uint16_t ubrr = (F_CPU / 4 / baud - 1) / 2; // Using double speed (U2X0)
  UBRR0H = (ubrr >> 8) & 0xFF;
  UBRR0L = ubrr & 0xFF;
  UCSR0A = (1 << U2X0);            // double speed
  UCSR0B = (1 << RXEN0) | (1 << TXEN0) | (1 << RXCIE0); // RX/TX enable + RX interrupt
  UCSR0C = (1 << UCSZ01) | (1 << UCSZ00); // 8N1
}

ISR(USART_RX_vect) {
  uint8_t b = UDR0; // read received byte
  uint8_t next = (rxHead + 1) & (RX_BUF_SIZE - 1); // RX_BUF_SIZE must be power of two
  if (next != rxTail) {
    rxBuf[rxHead] = b;
    rxHead = next;
  }
  // else drop if buffer full
}

// Timer1 Compare Match A interrupt: fires at 1 Hz when Timer1 configured by initTimer1ForOneSecondInterrupt()
ISR(TIMER1_COMPA_vect)
{
  oneSecondElapsed = true;
}
bool rx_available() {
  return rxHead != rxTail;
}

uint8_t rx_read() {
  uint8_t b = rxBuf[rxTail];
  rxTail = (rxTail + 1) & (RX_BUF_SIZE - 1);
  return b;
}

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

  cli();
  uart_init(115200);
  sei();
}

void loop()
{
  // Fast, non-blocking handling — called in main context
  // Drain ISR ring buffer into application buffer
  while (rx_available()) {
    uint8_t b = rx_read();
    lastRxMillis = millis();

    // Store all bytes equally (no special treatment for CR/LF)
    if (appLen < APP_BUF_SIZE) {
      appBuf[appLen++] = b;
    } else {
      // buffer full: drop new bytes until flush (will flush below)
    }
  }

  // Flush conditions: buffer full or idle timeout
  if (appLen > 0 && (appLen >= APP_BUF_SIZE || (millis() - lastRxMillis >= RX_IDLE_TIMEOUT_MS))) {
    // Process frame
    processValidFrame(appBuf, appLen);

    // Reset buffer state
    appLen = 0;
  }

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
}

