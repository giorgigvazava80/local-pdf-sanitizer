const HOST_NAME = "com.pdf_checker.sanitizer";

const DEFAULT_OPTIONS = {
  enabled: true,
  keepBackup: true,
  showNotifications: true
};

const handledDownloadIds = new Set();

chrome.runtime.onInstalled.addListener(async () => {
  const current = await chrome.storage.local.get(DEFAULT_OPTIONS);
  await chrome.storage.local.set({ ...DEFAULT_OPTIONS, ...current });
});

chrome.downloads.onChanged.addListener((delta) => {
  if (delta.state?.current !== "complete") {
    return;
  }

  sanitizeCompletedDownload(delta.id).catch((error) => {
    console.error("PDF sanitizer failed:", error);
    notify("PDF sanitizer failed", error.message || String(error));
  });
});

async function sanitizeCompletedDownload(downloadId) {
  if (handledDownloadIds.has(downloadId)) {
    return;
  }
  handledDownloadIds.add(downloadId);

  const options = await getOptions();
  if (!options.enabled) {
    return;
  }

  const [item] = await chrome.downloads.search({ id: downloadId });
  if (!item || !isPdfDownload(item)) {
    return;
  }

  const response = await sendNativeMessage({
    command: "sanitize_pdf",
    downloadId,
    path: item.filename,
    keepBackup: Boolean(options.keepBackup)
  });

  if (!response || response.ok !== true) {
    const message = response?.error || "The sanitizer app returned an invalid response.";
    throw new Error(message);
  }

  if (options.showNotifications) {
    const details = response.removedLinks === 1
      ? "Removed 1 link."
      : `Removed ${response.removedLinks || 0} links.`;
    notify("PDF sanitized", `${fileName(item.filename)}\n${details}`);
  }
}

async function getOptions() {
  const options = await chrome.storage.local.get(DEFAULT_OPTIONS);
  return { ...DEFAULT_OPTIONS, ...options };
}

function sendNativeMessage(payload) {
  return new Promise((resolve, reject) => {
    chrome.runtime.sendNativeMessage(HOST_NAME, payload, (response) => {
      const error = chrome.runtime.lastError;
      if (error) {
        reject(new Error(error.message));
        return;
      }
      resolve(response);
    });
  });
}

function isPdfDownload(item) {
  const filename = (item.filename || "").toLowerCase();
  if (filename.endsWith(".pdf")) {
    return true;
  }

  const mime = (item.mime || "").toLowerCase();
  if (mime === "application/pdf") {
    return true;
  }

  return urlLooksLikePdf(item.finalUrl || item.url || "");
}

function urlLooksLikePdf(url) {
  try {
    return new URL(url).pathname.toLowerCase().endsWith(".pdf");
  } catch {
    return url.toLowerCase().includes(".pdf");
  }
}

function fileName(path) {
  return String(path || "").split(/[\\/]/).pop() || "Downloaded PDF";
}

function notify(title, message) {
  chrome.storage.local.get(DEFAULT_OPTIONS).then((options) => {
    if (!options.showNotifications) {
      return;
    }

    chrome.notifications.create({
      type: "basic",
      iconUrl: "icon.svg",
      title,
      message
    });
  });
}
