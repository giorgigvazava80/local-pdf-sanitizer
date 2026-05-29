using System.Reflection;

using PdfSanitizerHost;

namespace PdfSanitizerHost.Tests;

public sealed class PdfSanitizerTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "PdfSanitizerHost.Tests",
        Guid.NewGuid().ToString("N"));

    public PdfSanitizerTests()
    {
        Directory.CreateDirectory(tempDirectory);
    }

    [Fact]
    public void Sanitize_RejectsNonPdfPaths()
    {
        var inputPath = Path.Combine(tempDirectory, "notes.txt");
        File.WriteAllText(inputPath, "not a pdf");

        var sanitizer = new PdfSanitizer();

        var exception = Assert.Throws<PdfSanitizerException>(() => sanitizer.Sanitize(inputPath, keepBackup: false));

        Assert.Equal("Only .pdf files can be sanitized.", exception.Message);
    }

    [Fact]
    public void Sanitize_HandlesMissingFilesSafely()
    {
        var sanitizer = new PdfSanitizer();
        var missingPath = Path.Combine(tempDirectory, "missing.pdf");

        var exception = Assert.Throws<PdfSanitizerException>(() => sanitizer.Sanitize(missingPath, keepBackup: false));

        Assert.Equal("Downloaded PDF does not exist.", exception.Message);
    }

    [Fact]
    public void Sanitize_RejectsDirectoryPaths()
    {
        var directoryPath = Path.Combine(tempDirectory, "folder.pdf");
        Directory.CreateDirectory(directoryPath);

        var sanitizer = new PdfSanitizer();

        var exception = Assert.Throws<PdfSanitizerException>(() => sanitizer.Sanitize(directoryPath, keepBackup: false));

        Assert.Equal("Downloaded PDF path is not a regular file.", exception.Message);
    }

    [Fact]
    public void NextBackupPath_UsesOriginalSuffixWhenAvailable()
    {
        var pdfPath = Path.Combine(tempDirectory, "sample.pdf");
        File.WriteAllText(pdfPath, "placeholder");

        var backupPath = InvokeNextBackupPath(pdfPath);

        Assert.Equal(Path.Combine(tempDirectory, "sample.original.pdf"), backupPath);
    }

    [Fact]
    public void NextBackupPath_AddsIncrementingSuffixWhenNeeded()
    {
        var pdfPath = Path.Combine(tempDirectory, "sample.pdf");
        File.WriteAllText(pdfPath, "placeholder");
        File.WriteAllText(Path.Combine(tempDirectory, "sample.original.pdf"), "backup");
        File.WriteAllText(Path.Combine(tempDirectory, "sample.original-1.pdf"), "backup");

        var backupPath = InvokeNextBackupPath(pdfPath);

        Assert.Equal(Path.Combine(tempDirectory, "sample.original-2.pdf"), backupPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string InvokeNextBackupPath(string pdfPath)
    {
        var method = typeof(PdfSanitizer).GetMethod(
            "NextBackupPath",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { pdfPath });
        return Assert.IsType<string>(result);
    }
}
