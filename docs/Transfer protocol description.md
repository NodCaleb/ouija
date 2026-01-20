# ESP32 LED Device Communication Protocol

## Overview

This document describes the binary communication protocol used to control an ESP32-based LED device with 35 LEDs.  
The protocol is transport-agnostic and can be used over Bluetooth (BLE) or wired connections (e.g., USB serial).

All communication is frame-based. Each frame is validated using a checksum before execution.

---

## Frame Structure

Each message sent to the device must follow the structure below:

| Byte Index | Field              | Size (bytes) | Description |
|-----------:|-------------------|-------------:|------------|
| 0–1        | Sync / Magic      | 2            | Fixed value `0xAA 0x55` |
| 2          | Protocol Version  | 1            | Protocol version number |
| 3          | Command Type      | 1            | Command identifier |
| 4          | Payload Length    | 1            | Length of payload in bytes |
| 5..N       | Payload           | Variable     | Command-specific data |
| Last       | Checksum          | 1            | XOR checksum |

---

## Sync / Magic Bytes

- **Value:** `0xAA 0x55`
- Used to detect the start of a frame.
- Allows the receiver to resynchronize if bytes are lost or corrupted.

---

## Protocol Version

- **Size:** 1 byte
- Defines the version of the protocol.
- Frames with unsupported versions must be rejected.

---

## Command Types

| Command | Hex  | Description |
|-------:|-----:|------------|
| CHECK_STATUS | `0x00` | Check device status |
| PLAY_SEQUENCE_ONCE | `0x01` | Play a sequence once |
| PLAY_SEQUENCE_REPEAT | `0x02` | Play a sequence repeatedly |
| STOP_PLAYING | `0x03` | Stop current playback |
| DISPLAY_YES | `0x04` | Display predefined **YES** pattern |
| DISPLAY_NO | `0x05` | Display predefined **NO** pattern |

---

## Payload Length

- **Size:** 1 byte
- Specifies the number of bytes in the payload.
- May be `0x00` for commands without payload.

---

## Payload

The payload format depends on the command type.

### Commands without payload
The following commands **must** use `Payload Length = 0`:

- `0x00` – Check device status
- `0x03` – Stop playing
- `0x04` – Display YES
- `0x05` – Display NO

### Commands with payload
Commands `0x01` and `0x02` may define payloads such as sequence identifiers, timing parameters, or flags.  
The exact payload structure is defined in higher-level sequence specifications.

---

## Checksum

- **Algorithm:** XOR
- **Size:** 1 byte
- The checksum is calculated by XOR-ing **all bytes from Protocol Version to the last payload byte**.

### Checksum Formula

checksum = byte[2] XOR byte[3] XOR byte[4] XOR ... XOR byte[N]


- Sync / Magic bytes (`0xAA 0x55`) are **not** included.
- The checksum byte itself is **not** included in the calculation.

Frames with invalid checksums must be discarded.

---

## Example Frame (No Payload)

**Command:** Display YES (`0x04`)  
**Protocol Version:** `0x01`

AA 55 01 04 00 05

Checksum calculation:

01 XOR 04 XOR 00 = 05


---

## Error Handling

The device must reject frames if any of the following occur:

- Invalid magic bytes
- Unsupported protocol version
- Payload length mismatch
- Invalid checksum
- Unknown command

Rejected frames must not trigger any device action.

---

## Design Notes

- The protocol is intentionally minimal to simplify firmware implementation.
- All commands are deterministic and idempotent.
- Transport-level concerns (BLE fragmentation, serial buffering) are handled outside the protocol definition.

---

## Future Extensions

- Additional commands may be added by extending the command set.
- Payload formats may evolve while maintaining backward compatibility via protocol versioning.
