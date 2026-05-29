using System.Text.Json.Serialization;

namespace PdfSanitizerHost;

internal sealed record NativeRequest
{
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("keepBackup")]
    public bool KeepBackup { get; init; } = true;

    [JsonPropertyName("downloadId")]
    public int? DownloadId { get; init; }
}

internal sealed record NativeResponse
{
    [JsonPropertyName("ok")]
    public required bool Ok { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("changed")]
    public bool? Changed { get; init; }

    [JsonPropertyName("removedLinks")]
    public int? RemovedLinks { get; init; }

    [JsonPropertyName("removedActions")]
    public int? RemovedActions { get; init; }

    [JsonPropertyName("backupPath")]
    public string? BackupPath { get; init; }
}

internal sealed record SanitizeResult(
    string Path,
    bool Changed,
    int RemovedLinks,
    int RemovedActions,
    string? BackupPath);
