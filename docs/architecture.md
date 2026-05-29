# Architecture

## High-Level Design
local-pdf-sanitizer has two runtime parts:

- A Chromium extension that listens for completed downloads and decides when a file should be sanitized.
- A Windows .NET native messaging host that receives the local file path and performs the PDF rewrite.

This split exists because Chrome and Edge extensions cannot directly launch arbitrary local executables. Native messaging provides the supported browser boundary between the extension and the native host.

## Browser Extension Responsibilities
The extension:
- watches `chrome.downloads.onChanged` for completed downloads;
- checks whether the download appears to be a PDF by filename, MIME type, or URL;
- reads local extension options such as enabled state, notifications, and backup preference;
- sends a native messaging request to `com.pdf_checker.sanitizer`;
- shows a success or failure notification to the user.

The extension does not upload files and does not parse PDF contents itself.

## Native Host Responsibilities
The native host:
- reads native messaging requests from standard input;
- validates the request structure and PDF path;
- opens the PDF locally with PDFsharp;
- removes clickable link annotations and selected action-oriented PDF objects;
- optionally creates a backup of the original file;
- writes a structured response to standard output for the extension.

## Data Flow
1. A PDF finishes downloading in Chrome or Edge.
2. The extension background service worker receives the completion event.
3. The extension resolves the downloaded file path and checks the current options.
4. The extension sends a native messaging request containing the command, file path, download ID, and backup setting.
5. The native host sanitizes the PDF locally and replaces the file if a change was made.
6. The native host returns success or error details to the extension.
7. The extension optionally notifies the user.

## Security Boundaries
- Browser boundary: the extension can only reach the local process registered through Chromium native messaging.
- Host boundary: the native host only receives what the extension sends and returns a structured response.
- File boundary: the sanitizer reads and writes local files only; there is no intended network egress path in the runtime workflow.

These boundaries help reduce accidental exposure, but they do not create a full containment or malware-analysis environment.

## Known Limitations
- Sanitization is selective, not comprehensive.
- Unsupported, malformed, or encrypted PDFs may fail to sanitize.
- The project does not verify every possible PDF action, exploit technique, or embedded payload type.
- Runtime safety still depends on the local machine, PDF library behavior, and the viewer used to open the file.
- Current automated tests focus on safe handling and message flow; fixture-based PDF behavior tests are still a future improvement.
