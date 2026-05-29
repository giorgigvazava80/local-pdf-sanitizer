# Security Policy

## Project Threat Model
local-pdf-sanitizer is designed to reduce exposure to a limited set of risky PDF behaviors after a file is downloaded through Chrome or Microsoft Edge. Its threat model is intentionally narrow:

- Reduce user interaction with clickable links embedded in PDFs.
- Remove selected action-oriented or external PDF objects before the file is opened locally.
- Keep all processing local to the endpoint without uploading documents to a remote service.

The project assumes:
- the browser extension and native messaging registration are installed intentionally by the user or administrator;
- the local system itself is trusted enough to run the extension and native host;
- PDF parsing is performed as a best-effort sanitization step, not as a complete hostile-document containment boundary.

## Scope
In scope:
- The Chromium extension in `extension/`
- The Windows .NET native messaging host in `native-host/PdfSanitizerHost/`
- Install and packaging scripts in `scripts/`
- Repository documentation that affects secure deployment or safe use

## Out-of-Scope Items
Out of scope:
- Guaranteeing that every malicious PDF is rendered safe
- General malware detection, antivirus capability, or document reputation analysis
- Sandboxing, behavioral detonation, or exploit containment
- Security of third-party PDF viewers or operating system PDF handlers
- Enterprise deployment mistakes such as incorrect extension IDs, registry scope, or file path placement

## Responsible Disclosure
Please report suspected security issues privately rather than opening a public issue first.

Recommended process:
1. Open a GitHub Security Advisory if the repository has security advisories enabled.
2. If not, contact the maintainer through GitHub with a private report and enough detail to reproduce the issue.
3. Include impact, affected files or functions, setup details, and any proof-of-concept material that helps validate the report safely.

I will review good-faith reports, confirm scope, and work toward a fix or documented mitigation as appropriate.

## Security Limitations
- local-pdf-sanitizer does not inspect all malicious PDF behaviors.
- It does not guarantee that every malicious or malformed PDF is neutralized.
- It does not emulate script execution, detect exploits, or score document risk.
- It relies on PDF parsing libraries and local operating system behavior that may have their own limitations.
- It only processes files that flow through the supported browser download path.

Use this project as a local hardening utility, not as a standalone security control.
