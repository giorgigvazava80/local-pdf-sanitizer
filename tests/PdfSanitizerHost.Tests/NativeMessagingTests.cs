using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

using PdfSanitizerHost;

namespace PdfSanitizerHost.Tests;

public sealed class NativeMessagingTests
{
    [Fact]
    public async Task ReadRequestAsync_ReturnsNullAtEndOfStream()
    {
        using var stream = new MemoryStream();

        var request = await NativeMessaging.ReadRequestAsync(stream, CancellationToken.None);

        Assert.Null(request);
    }

    [Fact]
    public async Task ReadRequestAsync_RejectsIncompleteHeader()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NativeMessaging.ReadRequestAsync(stream, CancellationToken.None));

        Assert.Equal("Incomplete native messaging length header.", exception.Message);
    }

    [Fact]
    public async Task ReadRequestAsync_RejectsOversizeMessages()
    {
        var header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(header, 1024u * 1024u + 1u);

        using var stream = new MemoryStream(header);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NativeMessaging.ReadRequestAsync(stream, CancellationToken.None));

        Assert.Equal("Native message is too large.", exception.Message);
    }

    [Fact]
    public async Task ReadRequestAsync_RejectsInvalidJson()
    {
        using var stream = CreateMessageStream("not-json");

        await Assert.ThrowsAsync<JsonException>(
            () => NativeMessaging.ReadRequestAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadRequestAsync_ParsesValidRequests()
    {
        using var stream = CreateMessageStream(
            """
            {"command":"sanitize_pdf","path":"C:\\Docs\\sample.pdf","keepBackup":true,"downloadId":7}
            """);

        var request = await NativeMessaging.ReadRequestAsync(stream, CancellationToken.None);

        Assert.NotNull(request);
        Assert.Equal("sanitize_pdf", request.Command);
        Assert.Equal(@"C:\Docs\sample.pdf", request.Path);
        Assert.True(request.KeepBackup);
        Assert.Equal(7, request.DownloadId);
    }

    [Fact]
    public async Task WriteResponseAsync_WritesLengthPrefixedJson()
    {
        using var stream = new MemoryStream();

        await NativeMessaging.WriteResponseAsync(
            stream,
            new NativeResponse
            {
                Ok = true,
                Path = @"C:\Docs\sample.pdf",
                Changed = true,
                RemovedLinks = 2,
                RemovedActions = 1
            },
            CancellationToken.None);

        var bytes = stream.ToArray();
        Assert.True(bytes.Length > 4);

        var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4));
        Assert.Equal(bytes.Length - 4, (int)payloadLength);

        var payload = JsonDocument.Parse(bytes.AsMemory(4));
        Assert.True(payload.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(2, payload.RootElement.GetProperty("removedLinks").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("removedActions").GetInt32());
    }

    // TODO: Add fixture-based integration tests that validate real PDF mutations and
    // native-host request/response behavior against sample binary PDF files.
    private static MemoryStream CreateMessageStream(string json)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream();
        var header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)payload.Length);
        stream.Write(header, 0, header.Length);
        stream.Write(payload, 0, payload.Length);
        stream.Position = 0;
        return stream;
    }
}
