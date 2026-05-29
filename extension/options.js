const DEFAULT_OPTIONS = {
  enabled: true,
  keepBackup: true,
  showNotifications: true
};

const fields = Object.keys(DEFAULT_OPTIONS).map((key) => ({
  key,
  element: document.getElementById(key)
}));

const statusElement = document.getElementById("status");

document.addEventListener("DOMContentLoaded", restoreOptions);

for (const field of fields) {
  field.element.addEventListener("change", saveOptions);
}

async function restoreOptions() {
  const options = await chrome.storage.local.get(DEFAULT_OPTIONS);
  for (const field of fields) {
    field.element.checked = Boolean(options[field.key]);
  }
}

async function saveOptions() {
  const next = {};
  for (const field of fields) {
    next[field.key] = field.element.checked;
  }

  await chrome.storage.local.set(next);
  statusElement.textContent = "Saved.";
  window.setTimeout(() => {
    statusElement.textContent = "";
  }, 1500);
}
