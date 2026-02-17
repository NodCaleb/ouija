using System;
using System.Threading.Tasks;
using OuijaDesk.Application.Contracts;
using OuijaDesk.Application.DTO;
using OuijaDesk.Contracts.Models;
using OuijaDesk.Application.Services;
using Xunit;

namespace OuijaDesk.Tests;

public class DeviceClientTests
{
    [Fact]
    public async Task CheckStatusAsync_TransportThrows_ReturnsOffline()
    {
        // Arrange
        var encoder = new StubEncoder(_ => new byte[] { 0x01 });
        var transport = new StubTransport((port, payload) => throw new InvalidOperationException("transport failed"));
        var decoder = new StubDecoder(_ => new DeviceResponse { ResponseStatus = 0x00 });
        var validator = new StubValidator(_ => true);

        var client = new DeviceClient(encoder, transport, decoder, validator);

        // Act
        var status = await client.CheckStatusAsync(string.Empty);

        // Assert
        Assert.False(status.Online);
    }

    [Fact]
    public async Task SendAsync_EncoderThrows_ReturnsEncodingFailed()
    {
        // Arrange
        var encoder = new StubEncoder(_ => throw new Exception("encode error"));
        var transport = new StubTransport((p, b) => Task.FromResult(new byte[] { 0x00 }));
        var decoder = new StubDecoder(_ => new DeviceResponse { ResponseStatus = 0x00 });
        var validator = new StubValidator(_ => true);

        var client = new DeviceClient(encoder, transport, decoder, validator);

        // Act
        var result = await client.SendAsync(string.Empty, new DeviceCommand { CommandType = 0x01 });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Encoding failed", result.Message);
    }

    [Fact]
    public async Task SendAsync_Success_ReturnsSuccessTrue()
    {
        // Arrange
        var encoder = new StubEncoder(_ => new byte[] { 0xAA, 0x55, 0x01 });
        var transport = new StubTransport((p, b) => Task.FromResult(new byte[] { 0x00 }));
        var decoder = new StubDecoder(_ => new DeviceResponse { ResponseStatus = 0x00 });
        var validator = new StubValidator(_ => true);

        var client = new DeviceClient(encoder, transport, decoder, validator);

        // Act
        var result = await client.SendAsync(string.Empty, new DeviceCommand { CommandType = 0x01 });

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SendAsync_InvalidResponse_ReturnsInvalidResponseMessage()
    {
        // Arrange
        var encoder = new StubEncoder(_ => new byte[] { 0xAA, 0x55, 0x01 });
        var transport = new StubTransport((p, b) => Task.FromResult(new byte[] { 0x05 }));
        var decoder = new StubDecoder(_ => new DeviceResponse { ResponseStatus = 0x05 });
        var validator = new StubValidator(_ => false);

        var client = new DeviceClient(encoder, transport, decoder, validator);

        // Act
        var result = await client.SendAsync(string.Empty, new DeviceCommand { CommandType = 0x01 });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid response status", result.Message);
    }

    // --- Stubs ---
    private class StubEncoder : IProtocolEncoder
    {
        private readonly Func<DeviceCommand, byte[]> _fn;
        public StubEncoder(Func<DeviceCommand, byte[]> fn) => _fn = fn;
        public byte[] Encode(DeviceCommand command) => _fn(command);
    }

    private class StubTransport : ITransport
    {
        private readonly Func<string, byte[], Task<byte[]>> _fn;
        public StubTransport(Func<string, byte[], Task<byte[]>> fn) => _fn = fn;
        public Task<byte[]> TransferAsync(string port, byte[] payload) => _fn(port, payload);
    }

    private class StubDecoder : IProtocolDecoder
    {
        private readonly Func<byte[], DeviceResponse> _fn;
        public StubDecoder(Func<byte[], DeviceResponse> fn) => _fn = fn;
        public DeviceResponse Decode(byte[] bytes) => _fn(bytes);
    }

    private class StubValidator : IProtocolValidator
    {
        private readonly Func<DeviceResponse, bool> _fn;
        public StubValidator(Func<DeviceResponse, bool> fn) => _fn = fn;
        public bool Validate(DeviceResponse response) => _fn(response);
    }
}
