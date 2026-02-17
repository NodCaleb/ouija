// Custom AVR UART RX ISR echo example for ATmega328P (Uno / Pro Mini).
// Do not use Arduino `Serial` in this sketch.

#include <avr/io.h>
#include <avr/interrupt.h>
#include <Arduino.h>

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

void uart_putchar(uint8_t c) {
  while (!(UCSR0A & (1 << UDRE0)));
  UDR0 = c;
}

bool rx_available() {
  return rxHead != rxTail;
}

uint8_t rx_read() {
  uint8_t b = rxBuf[rxTail];
  rxTail = (rxTail + 1) & (RX_BUF_SIZE - 1);
  return b;
}

void setup() {
  cli();
  uart_init(115200);
  sei();
}

void loop() {
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
    // Send stored bytes in reverse order
    int n = (int)appLen;
    for (int i = n - 1; i >= 0; --i) {
      uart_putchar(appBuf[i]);
    }

    // Reset buffer state
    appLen = 0;
  }
  // Do other work here; no Serial dependency
}

