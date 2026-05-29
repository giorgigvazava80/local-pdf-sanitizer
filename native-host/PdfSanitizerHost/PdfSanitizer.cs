using PdfSharp.Pdf;
using PdfSharp.Pdf.Annotations;
using PdfSharp.Pdf.IO;

namespace PdfSanitizerHost;

internal sealed class PdfSanitizer
{
    private static readonly HashSet<string> ExternalActionTypes = new(StringComparer.Ordinal)
    {
        "/URI",
        "/Launch",
        "/GoToR",
        "/SubmitForm",
        "/ImportData",
        "/JavaScript"
    };

    public SanitizeResult Sanitize(string inputPath, bool keepBackup)
    {
        var pdfPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(inputPath));
        ValidatePdfPath(pdfPath);

        var tempPath = CreateTempPath(pdfPath);
        string? backupPath = null;
        var removedLinks = 0;
        var removedActions = 0;
        var changed = false;

        try
        {
            using (var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify))
            {
                foreach (PdfPage page in document.Pages)
                {
                    var pageResult = SanitizeAnnotations(page);
                    removedLinks += pageResult.RemovedLinks;
                    removedActions += pageResult.RemovedActions;
                }

                removedActions += RemoveDocumentActions(document);
                changed = removedLinks > 0 || removedActions > 0;

                if (!changed)
                {
                    return new SanitizeResult(pdfPath, false, 0, 0, null);
                }

                if (keepBackup)
                {
                    backupPath = CopyBackup(pdfPath);
                }

                document.Save(tempPath);
            }

            File.Move(tempPath, pdfPath, overwrite: true);
            return new SanitizeResult(pdfPath, true, removedLinks, removedActions, backupPath);
        }
        catch (Exception ex) when (ex is not PdfSanitizerException)
        {
            throw new PdfSanitizerException($"Failed to sanitize PDF: {ex.Message}", ex);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void ValidatePdfPath(string pdfPath)
    {
        if (!pdfPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfSanitizerException("Only .pdf files can be sanitized.");
        }

        if (!File.Exists(pdfPath))
        {
            throw new PdfSanitizerException("Downloaded PDF does not exist.");
        }

        var attributes = File.GetAttributes(pdfPath);
        if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
        {
            throw new PdfSanitizerException("Downloaded PDF path is not a regular file.");
        }
    }

    private static AnnotationSanitizeResult SanitizeAnnotations(PdfPage page)
    {
        var removedLinks = 0;
        var removedActions = 0;

        for (var i = page.Annotations.Count - 1; i >= 0; i--)
        {
            var annotation = page.Annotations[i];
            var subtype = annotation.Elements.GetName("/Subtype");
            if (subtype == "/Link")
            {
                page.Annotations.Remove(annotation);
                removedLinks++;
                continue;
            }

            if (RemoveExternalAction(annotation, "/A"))
            {
                removedActions++;
            }
            if (RemoveExternalAction(annotation, "/AA"))
            {
                removedActions++;
            }
        }

        return new AnnotationSanitizeResult(removedLinks, removedActions);
    }

    private static int RemoveDocumentActions(PdfDocument document)
    {
        var removed = 0;
        var catalog = document.Internals.Catalog;

        if (catalog.Elements.ContainsKey("/OpenAction"))
        {
            catalog.Elements.Remove("/OpenAction");
            removed++;
        }

        if (catalog.Elements.ContainsKey("/AA"))
        {
            catalog.Elements.Remove("/AA");
            removed++;
        }

        var names = catalog.Elements.GetDictionary("/Names");
        if (names is null)
        {
            return removed;
        }

        foreach (var key in new[] { "/JavaScript", "/EmbeddedFiles", "/URLS" })
        {
            if (names.Elements.ContainsKey(key))
            {
                names.Elements.Remove(key);
                removed++;
            }
        }

        return removed;
    }

    private static bool RemoveExternalAction(PdfAnnotation annotation, string key)
    {
        var action = annotation.Elements.GetDictionary(key);
        if (!IsExternalAction(action))
        {
            return false;
        }

        annotation.Elements.Remove(key);
        return true;
    }

    private static bool IsExternalAction(PdfDictionary? action)
    {
        if (action is null)
        {
            return false;
        }

        var actionType = action.Elements.GetName("/S");
        return ExternalActionTypes.Contains(actionType);
    }

    private static string CopyBackup(string pdfPath)
    {
        var backupPath = NextBackupPath(pdfPath);
        File.Copy(pdfPath, backupPath);
        return backupPath;
    }

    private static string NextBackupPath(string pdfPath)
    {
        var directory = Path.GetDirectoryName(pdfPath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(pdfPath);
        var extension = Path.GetExtension(pdfPath);
        var candidate = Path.Combine(directory, $"{name}.original{extension}");

        if (!File.Exists(candidate))
        {
            return candidate;
        }

        for (var index = 1; index < 10_000; index++)
        {
            candidate = Path.Combine(directory, $"{name}.original-{index}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new PdfSanitizerException("Could not choose a backup filename.");
    }

    private static string CreateTempPath(string pdfPath)
    {
        var directory = Path.GetDirectoryName(pdfPath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(pdfPath);
        return Path.Combine(directory, $".{name}.{Guid.NewGuid():N}.sanitized.tmp");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Cleanup failure should not hide the sanitizer result.
        }
    }

    private sealed record AnnotationSanitizeResult(int RemovedLinks, int RemovedActions);
}

internal sealed class PdfSanitizerException : Exception
{
    public PdfSanitizerException(string message)
        : base(message)
    {
    }

    public PdfSanitizerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
