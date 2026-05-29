namespace PdfSanitizerHost;

internal static class Program
{
    private const string SanitizeCommand = "sanitize_pdf";

    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && StringComparer.OrdinalIgnoreCase.Equals(args[0], "sanitize"))
        {
            return RunCli(args);
        }

        var input = Console.OpenStandardInput();
        var output = Console.OpenStandardOutput();
        var sanitizer = new PdfSanitizer();

        while (true)
        {
            NativeRequest? request;
            try
            {
                request = await NativeMessaging.ReadRequestAsync(input, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(output, $"Failed to read native message: {ex.Message}");
                return 1;
            }

            if (request is null)
            {
                return 0;
            }

            var response = HandleRequest(sanitizer, request);
            await NativeMessaging.WriteResponseAsync(output, response, CancellationToken.None);
        }
    }

    private static int RunCli(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: PdfSanitizerHost.exe sanitize <pdf-path> [--no-backup]");
            return 2;
        }

        var keepBackup = !args.Any(arg => StringComparer.OrdinalIgnoreCase.Equals(arg, "--no-backup"));
        try
        {
            var result = new PdfSanitizer().Sanitize(args[1], keepBackup);
            Console.WriteLine(
                $"changed={result.Changed}; removedLinks={result.RemovedLinks}; removedActions={result.RemovedActions}; path={result.Path}");
            if (!string.IsNullOrWhiteSpace(result.BackupPath))
            {
                Console.WriteLine($"backupPath={result.BackupPath}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static NativeResponse HandleRequest(PdfSanitizer sanitizer, NativeRequest request)
    {
        try
        {
            if (!StringComparer.Ordinal.Equals(request.Command, SanitizeCommand))
            {
                throw new PdfSanitizerException("Unsupported command.");
            }

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                throw new PdfSanitizerException("Missing PDF path.");
            }

            var result = sanitizer.Sanitize(request.Path, request.KeepBackup);
            return new NativeResponse
            {
                Ok = true,
                Path = result.Path,
                Changed = result.Changed,
                RemovedLinks = result.RemovedLinks,
                RemovedActions = result.RemovedActions,
                BackupPath = result.BackupPath
            };
        }
        catch (Exception ex)
        {
            Log(ex);
            return new NativeResponse
            {
                Ok = false,
                Error = ex.Message
            };
        }
    }

    private static async Task WriteErrorAsync(Stream output, string message)
    {
        await NativeMessaging.WriteResponseAsync(
            output,
            new NativeResponse { Ok = false, Error = message },
            CancellationToken.None);
    }

    private static void Log(Exception ex)
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, "sanitizer.log");
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:u} {ex}\n");
        }
        catch
        {
        }
    }
}
