using System.Buffers.Binary;
using System.Text.Json;

namespace PdfSanitizerHost;

internal static class NativeMessaging
{
    private const int MaxMessageSize = 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<NativeRequest?> ReadRequestAsync(Stream input, CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        var read = await ReadExactOrEndAsync(input, lengthBuffer, cancellationToken);
        if (read == 0)
        {
            return null;
        }

        if (read != lengthBuffer.Length)
        {
            throw new InvalidOperationException("Incomplete native messaging length header.");
        }

        var messageLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBuffer);
        if (messageLength > MaxMessageSize)
        {
            throw new InvalidOperationException("Native message is too large.");
        }

        var messageBuffer = new byte[messageLength];
        read = await ReadExactOrEndAsync(input, messageBuffer, cancellationToken);
        if (read != messageBuffer.Length)
        {
            throw new InvalidOperationException("Incomplete native messaging payload.");
        }

        return JsonSerializer.Deserialize<NativeRequest>(messageBuffer, JsonOptions);
    }

    public static async Task WriteResponseAsync(Stream output, NativeResponse response, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(response, JsonOptions);
        var lengthBuffer = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(lengthBuffer, (uint)payload.Length);

        await output.WriteAsync(lengthBuffer, cancellationToken);
        await output.WriteAsync(payload, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }

    private static async Task<int> ReadExactOrEndAsync(Stream input, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await input.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (read == 0)
            {
                return totalRead;
            }
            totalRead += read;
        }

        return totalRead;
    }
}
